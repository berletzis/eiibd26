using eiibd26.Data;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services
{
    /// <summary>
    /// Servicio para buscar sugerencias de contenido relacionado sin usar IA.
    /// Usa búsqueda simple por texto en preguntas, respuestas y artículos.
    /// </summary>
    public class SearchSuggestionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SearchSuggestionService> _logger;

        // Stopwords en español que se ignoran en la búsqueda
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "el", "la", "los", "las", "un", "una", "unos", "unas",
            "de", "del", "a", "ante", "con", "contra", "desde", "en",
            "entre", "hacia", "hasta", "para", "por", "según", "sin",
            "sobre", "tras", "durante", "mediante",
            "que", "cual", "quien", "donde", "cuando", "como", "porque",
            "es", "son", "está", "están", "ser", "estar", "hay",
            "me", "te", "se", "nos", "os", "mi", "tu", "su",
            "y", "o", "pero", "si", "no", "ni", "qué", "cómo",
            "tengo", "tiene", "puedo", "puede", "hacer", "hace"
        };

        public SearchSuggestionService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<SearchSuggestionService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SuggestionResult> GetSuggestionsAsync(
            string query,
            int? condicionId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 20)
            {
                return new SuggestionResult();
            }

            // Normalizar query
            var normalizedQuery = NormalizeQuery(query);
            var keywords = ExtractKeywords(normalizedQuery);

            if (!keywords.Any())
            {
                return new SuggestionResult();
            }

            // Usar cache para queries repetidas (60 segundos)
            var cacheKey = $"suggestions_{normalizedQuery}_{condicionId}";
            if (_cache.TryGetValue<SuggestionResult>(cacheKey, out var cached))
            {
                _logger.LogInformation("✅ [Suggestions] Cache hit para: '{Query}'", query);
                return cached;
            }

            _logger.LogInformation("🔍 [Suggestions] Buscando para: '{Query}', Keywords: {Keywords}", 
                query, string.Join(", ", keywords));

            var result = new SuggestionResult();

            try
            {
                // Buscar preguntas similares
                result.Preguntas = await BuscarPreguntasAsync(keywords, condicionId, cancellationToken);

                // Buscar artículos relacionados
                result.Articulos = await BuscarArticulosAsync(keywords, condicionId, cancellationToken);

                // Buscar respuestas destacadas
                result.Respuestas = await BuscarRespuestasAsync(keywords, cancellationToken);

                // Guardar en cache
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));

                _logger.LogInformation(
                    "✅ [Suggestions] Encontrado: {Preguntas} preguntas, {Articulos} artículos, {Respuestas} respuestas",
                    result.Preguntas.Count, result.Articulos.Count, result.Respuestas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Suggestions] Error buscando sugerencias");
            }

            return result;
        }

        private async Task<List<SuggestionPregunta>> BuscarPreguntasAsync(
            List<string> keywords,
            int? condicionId,
            CancellationToken cancellationToken)
        {
            if (!keywords.Any())
                return new List<SuggestionPregunta>();

            // PERF-001: Un único query que combina todos los keywords con OR dinámico en SQL.
            // Reemplaza el loop de N queries independientes (uno por keyword).
            var kws = keywords.Take(5).ToList();

            var query = _db.Preguntas
                .AsNoTracking()
                .Where(p => !p.Eliminado);

            if (condicionId.HasValue)
                query = query.Where(p => p.PreguntaCondiciones.Any(pc => pc.CondicionId == condicionId.Value));

            // Construir el predicado OR dinámico en una sola expresión SQL
            query = query.Where(p =>
                kws.Any(k => p.Titulo.Contains(k) || p.Cuerpo.Contains(k)));

            var preguntas = await query
                .OrderByDescending(p => p.FechaCreacion)
                .Take(20)
                .Select(p => new { p.Id, p.Titulo, p.Slug, p.FechaCreacion })
                .ToListAsync(cancellationToken);

            // Ranking en memoria: cuántos keywords coinciden por pregunta
            var ranked = preguntas
                .Select(p => new
                {
                    Pregunta = p,
                    MatchCount = kws.Count(k =>
                        p.Titulo.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                        p.Slug.Contains(k, StringComparison.OrdinalIgnoreCase))
                })
                .OrderByDescending(x => x.MatchCount)
                .Take(5)
                .ToList();

            // Contar respuestas en un solo query para todas las preguntas seleccionadas (evita N+1)
            var preguntaIds = ranked.Select(x => x.Pregunta.Id).ToList();
            var respuestasCounts = await _db.Respuestas
                .AsNoTracking()
                .Where(r => preguntaIds.Contains(r.PreguntaId) && !r.Eliminado)
                .GroupBy(r => r.PreguntaId)
                .Select(g => new { PreguntaId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PreguntaId, x => x.Count, cancellationToken);

            return ranked
                .Select(x => new SuggestionPregunta
                {
                    Id = x.Pregunta.Id,
                    Titulo = x.Pregunta.Titulo,
                    Slug = x.Pregunta.Slug,
                    RespuestasCount = respuestasCounts.TryGetValue(x.Pregunta.Id, out var cnt) ? cnt : 0,
                    FechaCreacion = x.Pregunta.FechaCreacion
                })
                .ToList();
        }

        private async Task<List<SuggestionArticulo>> BuscarArticulosAsync(
            List<string> keywords,
            int? condicionId,
            CancellationToken cancellationToken)
        {
            if (!keywords.Any())
                return new List<SuggestionArticulo>();

            var kws = keywords.Take(5).ToList();

            var query = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && c.EstadoPublicacion == 1);

            if (condicionId.HasValue)
            {
                var contenidoIds = _db.ContenidoCondiciones
                    .Where(cc => cc.CondicionId == condicionId.Value && !cc.Borrado)
                    .Select(cc => cc.ContenidoId)
                    .Distinct();
                query = query.Where(c => contenidoIds.Contains(c.Id));
            }

            // PERF-002: Un único query OR en lugar de N queries por keyword.
            query = query.Where(c =>
                kws.Any(k =>
                    (c.ContenidoTitulo != null && c.ContenidoTitulo.Contains(k)) ||
                    (c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(k)) ||
                    (c.ContenidoTextoL != null && c.ContenidoTextoL.Contains(k))));

            var articulos = await query
                .OrderByDescending(c => c.ContenidoFechaInicio ?? c.FechaCreado)
                .Take(20)
                .Select(c => new
                {
                    c.Id,
                    c.ContenidoTitulo,
                    c.ContenidoTituloSlug,
                    c.ContenidoTextoC,
                    c.URLImagenPrincipal,
                    FechaRef = c.ContenidoFechaInicio ?? c.FechaCreado
                })
                .ToListAsync(cancellationToken);

            // Ranking en memoria por número de keywords coincidentes
            var ranked = articulos
                .Select(a => new
                {
                    Articulo = a,
                    MatchCount = kws.Count(k =>
                        (a.ContenidoTitulo?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (a.ContenidoTextoC?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false))
                })
                .OrderByDescending(x => x.MatchCount)
                .ThenByDescending(x => x.Articulo.FechaRef)
                .Take(5)
                .ToList();

            // Obtener slugs de categorías en un único query (evita N+1)
            var articuloIds = ranked.Select(x => x.Articulo.Id).ToList();
            var categoriasSlugs = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(ccr => articuloIds.Contains(ccr.IdContenido) && !ccr.Borrado && ccr.IdCategoria.HasValue)
                .Join(_db.ContenidosCategorias,
                    ccr => ccr.IdCategoria!.Value,
                    cat => cat.Sequence,
                    (ccr, cat) => new { IdContenido = ccr.IdContenido, cat.CategoriaSlug })
                .Where(x => x.CategoriaSlug != null)
                .GroupBy(x => x.IdContenido)
                .Select(g => new { IdContenido = g.Key, CategoriaSlug = g.First().CategoriaSlug })
                .ToDictionaryAsync(x => x.IdContenido, x => x.CategoriaSlug, cancellationToken);

            return ranked
                .Select(x => new SuggestionArticulo
                {
                    Id = x.Articulo.Id,
                    Titulo = x.Articulo.ContenidoTitulo ?? "",
                    Slug = x.Articulo.ContenidoTituloSlug ?? "",
                    Resumen = x.Articulo.ContenidoTextoC != null && x.Articulo.ContenidoTextoC.Length > 200
                        ? x.Articulo.ContenidoTextoC.Substring(0, 200) + "..."
                        : x.Articulo.ContenidoTextoC,
                    ImagenUrl = x.Articulo.URLImagenPrincipal,
                    CategoriaSlug = categoriasSlugs.TryGetValue(x.Articulo.Id, out var catSlug) ? catSlug : null
                })
                .ToList();
        }

        private async Task<List<SuggestionRespuesta>> BuscarRespuestasAsync(
            List<string> keywords,
            CancellationToken cancellationToken)
        {
            if (!keywords.Any())
                return new List<SuggestionRespuesta>();

            var kws = keywords.Take(5).ToList();

            // PERF-003: Un único query OR en lugar de N queries por keyword.
            var respuestas = await _db.Respuestas
                .AsNoTracking()
                .Where(r => !r.Eliminado && !r.EsIA)
                .Where(r => kws.Any(k => r.Cuerpo.Contains(k)))
                .OrderByDescending(r => r.Puntuacion)
                .ThenByDescending(r => r.FechaCreacion)
                .Take(20)
                .Select(r => new { r.Id, r.PreguntaId, r.Cuerpo, r.Puntuacion, r.FechaCreacion })
                .ToListAsync(cancellationToken);

            // Ranking en memoria y top 5
            var top5 = respuestas
                .Select(r => new
                {
                    Respuesta = r,
                    MatchCount = kws.Count(k => r.Cuerpo.Contains(k, StringComparison.OrdinalIgnoreCase))
                })
                .OrderByDescending(x => x.MatchCount)
                .ThenByDescending(x => x.Respuesta.Puntuacion)
                .Take(5)
                .ToList();

            // Un solo query para obtener títulos y slugs de las preguntas involucradas (evita N+1)
            var preguntaIds = top5.Select(x => x.Respuesta.PreguntaId).Distinct().ToList();
            var preguntasInfo = await _db.Preguntas
                .AsNoTracking()
                .Where(p => preguntaIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Titulo, p.Slug })
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            return top5
                .Select(x =>
                {
                    preguntasInfo.TryGetValue(x.Respuesta.PreguntaId, out var pInfo);
                    return new SuggestionRespuesta
                    {
                        Id = x.Respuesta.Id,
                        PreguntaId = x.Respuesta.PreguntaId,
                        Cuerpo = x.Respuesta.Cuerpo,
                        Puntuacion = x.Respuesta.Puntuacion,
                        PreguntaTitulo = pInfo?.Titulo,
                        PreguntaSlug = pInfo?.Slug
                    };
                })
                .ToList();
        }

        private string NormalizeQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            // Convertir a minúsculas
            query = query.ToLowerInvariant();

            // Remover caracteres especiales
            query = Regex.Replace(query, @"[^a-záéíóúñü0-9\s]", " ");

            // Remover espacios múltiples
            query = Regex.Replace(query, @"\s+", " ");

            return query.Trim();
        }

        private List<string> ExtractKeywords(string normalizedQuery)
        {
            var words = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var keywords = new List<string>();

            foreach (var word in words)
            {
                // Ignorar palabras muy cortas o stopwords
                if (word.Length >= 3 && !StopWords.Contains(word))
                {
                    keywords.Add(word);
                }
            }

            return keywords;
        }
    }

    // ===== MODELOS DE RESULTADO =====

    public class SuggestionResult
    {
        public List<SuggestionPregunta> Preguntas { get; set; } = new();
        public List<SuggestionArticulo> Articulos { get; set; } = new();
        public List<SuggestionRespuesta> Respuestas { get; set; } = new();
    }

    public class SuggestionPregunta
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Slug { get; set; } = "";
        public int RespuestasCount { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
    }

    public class SuggestionArticulo
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Resumen { get; set; }
        public string? ImagenUrl { get; set; }
        public string? CategoriaSlug { get; set; } // ⭐ NUEVO: Para construir URL correcta
    }

    public class SuggestionRespuesta
    {
        public Guid Id { get; set; }
        public Guid PreguntaId { get; set; }
        public string? PreguntaTitulo { get; set; }
        public string? PreguntaSlug { get; set; }
        public string Cuerpo { get; set; } = "";
        public int Puntuacion { get; set; }
    }
}
