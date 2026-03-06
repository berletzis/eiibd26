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

            var baseQuery = _db.Preguntas
                .AsNoTracking()
                .Where(p => !p.Eliminado);

            // Filtrar por condición si se especifica
            if (condicionId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.PreguntaCondiciones.Any(pc => pc.CondicionId == condicionId.Value));
            }

            // ⭐ CAMBIO: Buscar preguntas que contengan CUALQUIER keyword (OR en lugar de AND)
            // Construir una expresión OR para todos los keywords
            var matchingQuestions = new List<Pregunta>();

            foreach (var keyword in keywords.Take(5)) // Limitar a primeros 5 keywords más relevantes
            {
                var k = keyword;
                var matches = await baseQuery
                    .Where(p => p.Titulo.Contains(k) || p.Cuerpo.Contains(k))
                    .ToListAsync(cancellationToken);

                matchingQuestions.AddRange(matches);
            }

            // Agrupar por pregunta y ordenar por número de coincidencias (relevancia)
            var grouped = matchingQuestions
                .GroupBy(p => p.Id)
                .Select(g => new
                {
                    Pregunta = g.First(),
                    MatchCount = g.Count() // Cuántos keywords coincidieron
                })
                .OrderByDescending(x => x.MatchCount) // Más coincidencias = más relevante
                .ThenByDescending(x => x.Pregunta.FechaCreacion)
                .Take(5)
                .Select(x => new SuggestionPregunta
                {
                    Id = x.Pregunta.Id,
                    Titulo = x.Pregunta.Titulo,
                    Slug = x.Pregunta.Slug,
                    RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == x.Pregunta.Id && !r.Eliminado),
                    FechaCreacion = x.Pregunta.FechaCreacion
                })
                .ToList();

            return grouped;
        }

        private async Task<List<SuggestionArticulo>> BuscarArticulosAsync(
            List<string> keywords,
            int? condicionId,
            CancellationToken cancellationToken)
        {
            if (!keywords.Any())
                return new List<SuggestionArticulo>();

            var baseQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && c.EstadoPublicacion == 1); // Solo publicados

            // Filtrar por condición si se especifica
            if (condicionId.HasValue)
            {
                var contenidoIdsConCondicion = _db.ContenidoCondiciones
                    .Where(cc => cc.CondicionId == condicionId.Value && !cc.Borrado)
                    .Select(cc => cc.ContenidoId)
                    .Distinct();

                baseQuery = baseQuery.Where(c => contenidoIdsConCondicion.Contains(c.Id));
            }

            // ⭐ CAMBIO: Buscar artículos que contengan CUALQUIER keyword (OR en lugar de AND)
            // Y buscar también en el contenido completo, no solo en título y resumen
            var matchingArticulos = new List<Contenido>();

            foreach (var keyword in keywords.Take(5)) // Limitar a primeros 5 keywords
            {
                var k = keyword;
                var matches = await baseQuery
                    .Where(c => 
                        (c.ContenidoTitulo != null && c.ContenidoTitulo.Contains(k)) || 
                        (c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(k)) ||
                        (c.ContenidoTextoL != null && c.ContenidoTextoL.Contains(k))) // ⭐ NUEVO: Buscar en contenido completo
                    .ToListAsync(cancellationToken);

                matchingArticulos.AddRange(matches);
            }

            // Agrupar por artículo y ordenar por número de coincidencias (relevancia)
            var articulos = matchingArticulos
                .GroupBy(c => c.Id)
                .Select(g => new
                {
                    Articulo = g.First(),
                    MatchCount = g.Count() // Cuántos keywords coincidieron
                })
                .OrderByDescending(x => x.MatchCount) // Más coincidencias = más relevante
                .ThenByDescending(x => x.Articulo.ContenidoFechaInicio ?? x.Articulo.FechaCreado)
                .Take(5)
                .ToList();

            // Obtener slugs de categorías para construir URLs correctas
            var articulosIds = articulos.Select(x => x.Articulo.Id).ToList();
            var categoriasSlugs = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(ccr => articulosIds.Contains(ccr.IdContenido) && !ccr.Borrado && ccr.IdCategoria.HasValue)
                .Join(_db.ContenidosCategorias,
                    ccr => ccr.IdCategoria.Value,
                    cat => cat.Sequence,
                    (ccr, cat) => new { IdContenido = ccr.IdContenido, CategoriaSlug = cat.CategoriaSlug })
                .Where(x => x.CategoriaSlug != null)
                .GroupBy(x => x.IdContenido)
                .Select(g => new { IdContenido = g.Key, CategoriaSlug = g.First().CategoriaSlug })
                .ToDictionaryAsync(x => x.IdContenido, x => x.CategoriaSlug, cancellationToken);

            var result = articulos
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

            return result;
        }

        private async Task<List<SuggestionRespuesta>> BuscarRespuestasAsync(
            List<string> keywords,
            CancellationToken cancellationToken)
        {
            if (!keywords.Any())
                return new List<SuggestionRespuesta>();

            var baseQuery = _db.Respuestas
                .AsNoTracking()
                .Where(r => !r.Eliminado && !r.EsIA); // Solo respuestas de humanos

            // ⭐ CAMBIO: Buscar respuestas que contengan CUALQUIER keyword (OR)
            var matchingRespuestas = new List<Respuesta>();

            foreach (var keyword in keywords.Take(5))
            {
                var k = keyword;
                var matches = await baseQuery
                    .Where(r => r.Cuerpo.Contains(k))
                    .ToListAsync(cancellationToken);

                matchingRespuestas.AddRange(matches);
            }

            var grouped = matchingRespuestas
                .GroupBy(r => r.Id)
                .Select(g => new
                {
                    Respuesta = g.First(),
                    MatchCount = g.Count()
                })
                .OrderByDescending(x => x.MatchCount)
                .ThenByDescending(x => x.Respuesta.Puntuacion)
                .ThenByDescending(x => x.Respuesta.FechaCreacion)
                .Take(5)
                .Select(x => new SuggestionRespuesta
                {
                    Id = x.Respuesta.Id,
                    PreguntaId = x.Respuesta.PreguntaId,
                    Cuerpo = x.Respuesta.Cuerpo,
                    Puntuacion = x.Respuesta.Puntuacion,
                    PreguntaTitulo = _db.Preguntas
                        .Where(p => p.Id == x.Respuesta.PreguntaId)
                        .Select(p => p.Titulo)
                        .FirstOrDefault(),
                    PreguntaSlug = _db.Preguntas
                        .Where(p => p.Id == x.Respuesta.PreguntaId)
                        .Select(p => p.Slug)
                        .FirstOrDefault()
                })
                .ToList();

            return grouped;
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
