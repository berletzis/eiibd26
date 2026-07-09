using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eiibd26.Voyage
{
    /// <summary>
    /// Implementación HTTP del cliente de embeddings de Voyage. Replica la disciplina del cliente
    /// Anthropic del Worker: HttpClient estático reutilizable, timeout por CTS enlazado, key desde
    /// config (con guard de placeholder), reintento con backoff en 429/5xx. La biblioteca no toca
    /// EF ni NINA/GRIS.
    /// </summary>
    public sealed class VoyageEmbeddingClient : IVoyageEmbeddingClient
    {
        // HttpClient estático reutilizable; el timeout por request lo gobierna un CTS enlazado.
        private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly VoyageOptions _opt;

        public VoyageEmbeddingClient(VoyageOptions options) => _opt = options;

        public bool Habilitado => _opt.TieneKey;
        public string Modelo => _opt.Model;

        public async Task<float[]?> EmbedUnoAsync(string texto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var batch = await EmbedAsync(new[] { texto }, ct);
            return batch is { Vectores.Count: > 0 } ? batch.Vectores[0] : null;
        }

        public async Task<VoyageBatch?> EmbedAsync(IReadOnlyList<string> textos, CancellationToken ct = default)
        {
            if (!Habilitado) { _opt.Warn?.Invoke("[Voyage] Sin API key: no se embebe."); return null; }
            if (textos == null || textos.Count == 0) return null;

            var payload = new Dictionary<string, object?>
            {
                ["input"] = textos,
                ["model"] = _opt.Model,
                ["input_type"] = _opt.InputType
            };
            if (_opt.OutputDimension is int d) payload["output_dimension"] = d;

            var json = JsonSerializer.Serialize(payload, JsonOpts);

            for (int attempt = 1; attempt <= Math.Max(1, _opt.MaxRetries); attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));

                    using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrlTrimmed}/embeddings");
                    req.Headers.Add("Authorization", $"Bearer {_opt.ApiKey}");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var resp = await Http.SendAsync(req, cts.Token);

                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadAsStringAsync(cts.Token);
                        var parsed = JsonSerializer.Deserialize<VoyageResponse>(body);
                        if (parsed?.Data == null || parsed.Data.Length == 0)
                        {
                            _opt.Warn?.Invoke("[Voyage] Respuesta sin data.");
                            return null;
                        }

                        // Ordenar por index para devolver los vectores en el orden de entrada.
                        var vectores = new float[parsed.Data.Length][];
                        foreach (var datum in parsed.Data)
                        {
                            if (datum.Embedding == null) continue;
                            if (datum.Index < 0 || datum.Index >= vectores.Length) continue;
                            vectores[datum.Index] = datum.Embedding;
                        }
                        if (Array.Exists(vectores, v => v == null))
                        {
                            _opt.Warn?.Invoke("[Voyage] Faltan vectores en la respuesta.");
                            return null;
                        }

                        return new VoyageBatch(vectores, parsed.Usage?.TotalTokens ?? 0);
                    }

                    // 429 (rate limit) y 5xx → reintentar con backoff. Otros 4xx → fallar directo.
                    var esReintentable = resp.StatusCode == HttpStatusCode.TooManyRequests
                                         || (int)resp.StatusCode >= 500;
                    var err = await SafeReadAsync(resp, cts.Token);

                    if (!esReintentable || attempt == _opt.MaxRetries)
                    {
                        _opt.Warn?.Invoke($"[Voyage] HTTP {(int)resp.StatusCode}: {err}");
                        return null;
                    }

                    var espera = BackoffSegundos(resp, attempt);
                    _opt.Warn?.Invoke($"[Voyage] HTTP {(int)resp.StatusCode} (intento {attempt}/{_opt.MaxRetries}), reintento en {espera}s.");
                    await Task.Delay(TimeSpan.FromSeconds(espera), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw; // cancelación real del llamador
                }
                catch (Exception ex)
                {
                    if (attempt == _opt.MaxRetries)
                    {
                        _opt.Warn?.Invoke($"[Voyage] Error tras {attempt} intentos: {ex.Message}");
                        return null;
                    }
                    var espera = _opt.RetryBaseSeconds * (int)Math.Pow(2, attempt - 1);
                    _opt.Warn?.Invoke($"[Voyage] Error (intento {attempt}/{_opt.MaxRetries}): {ex.Message}. Reintento en {espera}s.");
                    await Task.Delay(TimeSpan.FromSeconds(espera), ct);
                }
            }

            return null;
        }

        private int BackoffSegundos(HttpResponseMessage resp, int attempt)
        {
            // Respetar Retry-After si el servidor lo manda; si no, backoff exponencial.
            if (resp.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
                return (int)Math.Ceiling(delta.TotalSeconds);
            return _opt.RetryBaseSeconds * (int)Math.Pow(2, attempt - 1);
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            try
            {
                var s = await resp.Content.ReadAsStringAsync(ct);
                return s.Length > 300 ? s.Substring(0, 300) : s;
            }
            catch { return "(sin cuerpo)"; }
        }

        // ─────────── DTOs de respuesta de Voyage ───────────
        private sealed class VoyageResponse
        {
            [JsonPropertyName("data")] public VoyageDatum[]? Data { get; set; }
            [JsonPropertyName("usage")] public VoyageUsage? Usage { get; set; }
            [JsonPropertyName("model")] public string? Model { get; set; }
        }

        private sealed class VoyageDatum
        {
            [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
            [JsonPropertyName("index")] public int Index { get; set; }
        }

        private sealed class VoyageUsage
        {
            [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
        }
    }
}
