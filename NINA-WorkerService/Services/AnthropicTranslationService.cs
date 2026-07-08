using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NINA_WorkerService.Services;

public interface ITranslationService
{
    /// <summary>True si hay API key configurada (si no, el Worker no firma inglés).</summary>
    bool Habilitado { get; }

    /// <summary>
    /// Traduce texto inglés→español con Anthropic. Devuelve la traducción, o null si falla
    /// o no está habilitado. La traducción vive solo en memoria (el llamador la descarta).
    /// </summary>
    Task<string?> TraducirAEspanolAsync(string texto, CancellationToken ct);
}

/// <summary>
/// Traducción EN→ES con la API de Anthropic (mismo patrón que el cliente del Web:
/// POST messages, headers x-api-key + anthropic-version). Key en config del Worker
/// (user-secrets/env), NUNCA hardcodeada. NO toca NINA/GRIS/AiAnswerService del Web.
/// </summary>
public class AnthropicTranslationService : ITranslationService
{
    // HttpClient estático reutilizable; el timeout por request lo gobierna un CTS enlazado.
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly ILogger<AnthropicTranslationService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly string _apiVersion;
    private readonly int _maxTokens;
    private readonly int _maxInputChars;
    private readonly int _timeoutSeconds;

    private const string Placeholder = "SET_IN_ENVIRONMENT_OR_SECRETS";

    private const string SystemPrompt =
        "Eres un traductor profesional. Traduce el texto del inglés al español de forma fiel y natural. " +
        "Devuelve ÚNICAMENTE la traducción, sin comentarios, sin notas, sin markdown y sin comillas.";

    public AnthropicTranslationService(IConfiguration cfg, ILogger<AnthropicTranslationService> logger)
    {
        _logger = logger;

        var key = cfg["Anthropic:ApiKey"];
        _apiKey = (string.IsNullOrWhiteSpace(key) || key == Placeholder) ? null : key;

        _model = cfg["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
        _baseUrl = (cfg["Anthropic:ApiBaseUrl"] ?? "https://api.anthropic.com/v1").TrimEnd('/');
        _apiVersion = cfg["Anthropic:ApiVersion"] ?? "2023-06-01";
        _maxTokens = cfg.GetValue<int?>("Anthropic:MaxTokens") ?? 4000;
        _maxInputChars = cfg.GetValue<int?>("Anthropic:MaxInputChars") ?? 6000;
        _timeoutSeconds = cfg.GetValue<int?>("Anthropic:TimeoutSeconds") ?? 60;
    }

    public bool Habilitado => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string?> TraducirAEspanolAsync(string texto, CancellationToken ct)
    {
        if (!Habilitado || string.IsNullOrWhiteSpace(texto)) return null;

        // Acotar la entrada para controlar costo/tokens (la firma no necesita el texto entero).
        var entrada = texto.Length > _maxInputChars ? texto.Substring(0, _maxInputChars) : texto;

        var requestBody = new
        {
            model = _model,
            max_tokens = _maxTokens,
            temperature = 0.0,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = entrada } }
        };

        var json = JsonSerializer.Serialize(requestBody);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/messages");
            req.Headers.Add("x-api-key", _apiKey);
            req.Headers.Add("anthropic-version", _apiVersion);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("Anthropic traducción HTTP {Code}: {Err}", (int)resp.StatusCode,
                    err.Length > 300 ? err.Substring(0, 300) : err);
                return null;
            }

            var body = await resp.Content.ReadAsStringAsync(cts.Token);
            var data = JsonSerializer.Deserialize<ClaudeResponse>(body);
            var text = data?.Content is { Length: > 0 } ? data.Content[0].Text : null;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error traduciendo con Anthropic");
            return null;
        }
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")] public ContentItem[]? Content { get; set; }
    }

    private class ContentItem
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }
}
