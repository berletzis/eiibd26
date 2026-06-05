using eiibd26.Configuration;
using eiibd26.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace eiibd26.Services.Calidad
{
    public class GrisEvaluadorService : IGrisEvaluadorService
    {
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly string _model;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GrisEvaluadorService> _logger;

        // GRIS necesita más tokens que NINA: 7 aspectos + observaciones + sugerencias
        private const int MaxTokensGris = 1500;

        private const string SystemPrompt =
            "Sos GRIS, un evaluador editorial experto. Evaluás artículos de un sitio de salud " +
            "sobre enfermedad inflamatoria intestinal (EII). NO das consejo médico; solo evaluás " +
            "la CALIDAD EDITORIAL del texto. " +
            "Respondés ÚNICAMENTE en JSON válido, sin texto adicional antes ni después, " +
            "sin markdown, sin ```json fences.";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GrisEvaluadorService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            IOptions<GrisConfiguration> grisConfig,
            ApplicationDbContext db,
            ILogger<GrisEvaluadorService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _model = !string.IsNullOrWhiteSpace(grisConfig.Value.Model)
                ? grisConfig.Value.Model
                : _config.Model;
            _db = db;
            _logger = logger;
        }

        public async Task<GrisEvaluacionDto> EvaluarAsync(int contenidoId)
        {
            _logger.LogInformation("[GRIS] Evaluación editorial contenidoId={Id} modelo={Modelo}", contenidoId, _model);

            var contenido = await _db.Contenidos
                .AsNoTracking()
                .Where(c => c.Id == contenidoId && !c.Eliminado)
                .Select(c => new { c.Id, c.ContenidoTitulo, c.ContenidoTextoL })
                .FirstOrDefaultAsync();

            if (contenido == null)
                return ErrorDto(contenidoId, "Contenido no encontrado o eliminado.");

            // Strip HTML y truncar a ~3000 chars para no desperdiciar tokens
            var textoLimpio = StripHtml(contenido.ContenidoTextoL ?? string.Empty);
            if (textoLimpio.Length > 3000)
                textoLimpio = textoLimpio[..3000] + "\n[... texto truncado para evaluación ...]";

            // Catálogo de categorías (una sola query para todo el análisis)
            var categoriasDb = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado)
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.Sequence, c.Nombre, c.Descripcion })
                .ToListAsync();

            var validCatIds = categoriasDb.Select(c => c.Sequence).ToHashSet();

            var catalogoTexto = string.Join("\n", categoriasDb.Select(c =>
            {
                var desc = string.IsNullOrWhiteSpace(c.Descripcion) ? string.Empty
                    : " — " + (c.Descripcion.Length > 80 ? c.Descripcion[..80] + "…" : c.Descripcion).Trim();
                return $"  ID={c.Sequence}: {c.Nombre?.Trim()}{desc}";
            }));

            // Categorías actualmente asignadas a este artículo
            var catActualesIds = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.IdCategoria != null)
                .Select(r => r.IdCategoria!.Value)
                .ToListAsync();

            var actualesTexto = catActualesIds.Any()
                ? string.Join(", ", categoriasDb
                    .Where(c => catActualesIds.Contains(c.Sequence))
                    .Select(c => $"{c.Nombre} (ID={c.Sequence})"))
                : "ninguna";

            var userPrompt = BuildUserPrompt(contenido.ContenidoTitulo ?? "(sin título)", textoLimpio, catalogoTexto, actualesTexto);

            string rawResponse;
            try
            {
                rawResponse = await LlamarClaudeAsync(userPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GRIS] Error en llamada a Claude API para contenido {Id}", contenidoId);
                return ErrorDto(contenidoId, $"Error de API: {ex.Message}");
            }

            // Parsear JSON — robusto frente a posibles ```json fences
            GrisRespuestaJson? parsed = null;
            var jsonText = ExtraerJson(rawResponse);
            try
            {
                parsed = JsonSerializer.Deserialize<GrisRespuestaJson>(jsonText, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[GRIS] No se pudo parsear JSON para contenido {Id}. Raw: {Raw}",
                    contenidoId, rawResponse);
            }

            var dto = BuildDto(contenidoId, parsed, validCatIds);

            // UPSERT en ContenidoCalidad (fallo aquí no invalida el DTO devuelto)
            try { await GuardarResultadoAsync(contenidoId, dto, parsed, rawResponse); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GRIS] Error guardando resultado para contenido {Id}", contenidoId);
            }

            _logger.LogInformation(
                "[GRIS] Contenido {Id} evaluado — puntaje={Puntaje} ok={Ok}",
                contenidoId, dto.PuntajeGlobal, dto.Ok);

            return dto;
        }

        // Con $$"""...""" (dos $): {{variable}} es interpolación; { y } en el template son literales.
        private static string BuildUserPrompt(string titulo, string cuerpo, string catalogoCateg, string actualesCateg) =>
            $$"""
            Evaluá el siguiente artículo de salud sobre EII según la rúbrica de 7 aspectos.

            TÍTULO: {{titulo}}

            CUERPO:
            {{cuerpo}}

            ---
            Evaluá cada aspecto con puntaje 1-10 y una observación breve (1-2 oraciones):
            1. Claridad y estructura: introducción que engancha, desarrollo ordenado, conclusión
            2. Relevancia y utilidad: responde necesidad real, aporta soluciones/ejemplos, sin relleno
            3. Precisión y credibilidad: datos correctos, sin exageraciones, opinión marcada como tal
            4. Lenguaje adecuado: al público, frases cortas, sin jerga innecesaria
            5. Originalidad y voz propia: ángulo distinto, personalidad, sin clichés
            6. Engagement y experiencia: ejemplos/metáforas/historias, llamadas a la acción
            7. Optimización técnica: keywords naturales, meta descripción, legibilidad móvil

            ---
            ANÁLISIS DE CATEGORÍAS
            Categorías disponibles en el sitio:
            {{catalogoCateg}}

            Categorías actualmente asignadas al artículo: {{actualesCateg}}

            Basándote exclusivamente en el texto del artículo:
            (1) categoriasSugeridas: qué categorías del catálogo encajan mejor con el contenido (máximo 5, solo las más relevantes). Usá los IDs exactos del catálogo — no inventes IDs.
            (2) categoriasAlerta: si alguna categoría actualmente asignada NO corresponde al contenido, listala con la razón. Si todas encajan (o no hay asignadas), devolvé lista vacía.

            Respondé SOLO con este JSON (sin texto adicional, sin markdown):
            {
              "puntajeGlobal": <número 0-100>,
              "aspectos": [
                {"nombre": "Claridad y estructura", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Relevancia y utilidad", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Precisión y credibilidad", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Lenguaje adecuado", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Originalidad y voz propia", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Engagement y experiencia", "puntaje": <1-10>, "observacion": "..."},
                {"nombre": "Optimización técnica", "puntaje": <1-10>, "observacion": "..."}
              ],
              "sugerencias": ["sugerencia concreta 1", "sugerencia concreta 2", "sugerencia concreta 3"],
              "categoriasSugeridas": [{"categoriaId": <id_exacto_del_catalogo>, "nombre": "...", "razon": "..."}],
              "categoriasAlerta": [{"categoriaId": <id_exacto_del_catalogo>, "nombre": "...", "razon": "..."}]
            }
            """;

        private async Task<string> LlamarClaudeAsync(string userPrompt)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = MaxTokensGris,
                temperature = 0.3,
                system = SystemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Claude API {(int)response.StatusCode}: {error}");
            }

            var raw = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ClaudeApiResponse>(raw, JsonOpts);

            if (data?.Content == null || data.Content.Length == 0)
                throw new InvalidOperationException("Respuesta vacía de Claude API");

            return data.Content[0].Text ?? string.Empty;
        }

        private static string ExtraerJson(string text)
        {
            var t = text.Trim();
            var m = Regex.Match(t, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();
            var idx = t.IndexOf('{');
            return idx >= 0 ? t[idx..] : t;
        }

        private static GrisEvaluacionDto BuildDto(int contenidoId, GrisRespuestaJson? parsed, HashSet<int> validCatIds)
        {
            if (parsed == null)
                return ErrorDto(contenidoId, "No se pudo parsear la respuesta de GRIS.");

            return new GrisEvaluacionDto
            {
                ContenidoId = contenidoId,
                Ok = true,
                PuntajeGlobal = Math.Clamp(parsed.PuntajeGlobal, 0, 100),
                Aspectos = (parsed.Aspectos ?? new()).Select(a => new GrisAspectoDto
                {
                    Nombre = a.Nombre ?? string.Empty,
                    Puntaje = Math.Clamp(a.Puntaje, 1, 10),
                    Observacion = a.Observacion ?? string.Empty
                }).ToList(),
                Sugerencias = parsed.Sugerencias ?? new(),
                // Validar IDs contra el catálogo real para descartar alucinaciones
                CategoriasSugeridas = (parsed.CategoriasSugeridas ?? new())
                    .Where(c => validCatIds.Contains(c.CategoriaId))
                    .Select(c => new GrisCategoriaSugeridaDto
                    {
                        CategoriaId = c.CategoriaId,
                        Nombre = c.Nombre ?? string.Empty,
                        Razon = c.Razon ?? string.Empty
                    }).ToList(),
                CategoriasAlerta = (parsed.CategoriasAlerta ?? new())
                    .Where(c => validCatIds.Contains(c.CategoriaId))
                    .Select(c => new GrisCategoriaSugeridaDto
                    {
                        CategoriaId = c.CategoriaId,
                        Nombre = c.Nombre ?? string.Empty,
                        Razon = c.Razon ?? string.Empty
                    }).ToList(),
                FechaEvaluacion = DateTime.UtcNow
            };
        }

        private static GrisEvaluacionDto ErrorDto(int contenidoId, string error) => new()
        {
            ContenidoId = contenidoId,
            Ok = false,
            Error = error,
            FechaEvaluacion = DateTime.UtcNow
        };

        private async Task GuardarResultadoAsync(
            int contenidoId,
            GrisEvaluacionDto dto,
            GrisRespuestaJson? parsed,
            string rawFallback)
        {
            var resultadoJson = parsed != null
                ? JsonSerializer.Serialize(dto.Aspectos)
                : null;
            var sugerenciasJson = parsed != null
                ? JsonSerializer.Serialize(dto.Sugerencias)
                : rawFallback;
            var sugeridasJson = dto.Ok && dto.CategoriasSugeridas.Count > 0
                ? JsonSerializer.Serialize(dto.CategoriasSugeridas)
                : (string?)null;
            var alertasJson = dto.Ok && dto.CategoriasAlerta.Count > 0
                ? JsonSerializer.Serialize(dto.CategoriasAlerta)
                : (string?)null;

            var fila = await _db.ContenidoCalidad
                .FirstOrDefaultAsync(x => x.ContenidoId == contenidoId);

            if (fila != null)
            {
                fila.GrisEvaluado = dto.Ok;
                fila.GrisPuntajeGlobal = dto.Ok ? (byte?)dto.PuntajeGlobal : null;
                fila.GrisResultado = resultadoJson;
                fila.GrisSugerencias = sugerenciasJson;
                fila.GrisCategoriasSugeridas = sugeridasJson;
                fila.GrisCategoriasAlerta = alertasJson;
                fila.GrisFechaEvaluacion = dto.FechaEvaluacion;
            }
            else
            {
                // La fila de calidad mecánica no existe aún — crear mínima
                _db.ContenidoCalidad.Add(new Models.Calidad.ContenidoCalidad
                {
                    ContenidoId = contenidoId,
                    NivelSemaforo = 2, // Ok por defecto — mecánico no corrió todavía
                    FechaAnalisis = dto.FechaEvaluacion,
                    GrisEvaluado = dto.Ok,
                    GrisPuntajeGlobal = dto.Ok ? (byte?)dto.PuntajeGlobal : null,
                    GrisResultado = resultadoJson,
                    GrisSugerencias = sugerenciasJson,
                    GrisCategoriasSugeridas = sugeridasJson,
                    GrisCategoriasAlerta = alertasJson,
                    GrisFechaEvaluacion = dto.FechaEvaluacion
                });
            }

            await _db.SaveChangesAsync();
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return Regex.Replace(html, "<[^>]+>", " ").Trim();
        }

        #region DTOs internos para Claude API

        private class ClaudeApiResponse
        {
            [JsonPropertyName("content")]
            public ContentItem[]? Content { get; set; }
        }

        private class ContentItem
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class GrisRespuestaJson
        {
            [JsonPropertyName("puntajeGlobal")]
            public int PuntajeGlobal { get; set; }

            [JsonPropertyName("aspectos")]
            public List<GrisAspectoJson>? Aspectos { get; set; }

            [JsonPropertyName("sugerencias")]
            public List<string>? Sugerencias { get; set; }

            [JsonPropertyName("categoriasSugeridas")]
            public List<GrisCategoriaJson>? CategoriasSugeridas { get; set; }

            [JsonPropertyName("categoriasAlerta")]
            public List<GrisCategoriaJson>? CategoriasAlerta { get; set; }
        }

        private class GrisAspectoJson
        {
            [JsonPropertyName("nombre")]
            public string? Nombre { get; set; }

            [JsonPropertyName("puntaje")]
            public int Puntaje { get; set; }

            [JsonPropertyName("observacion")]
            public string? Observacion { get; set; }
        }

        private class GrisCategoriaJson
        {
            [JsonPropertyName("categoriaId")]
            public int CategoriaId { get; set; }

            [JsonPropertyName("nombre")]
            public string? Nombre { get; set; }

            [JsonPropertyName("razon")]
            public string? Razon { get; set; }
        }

        #endregion
    }
}
