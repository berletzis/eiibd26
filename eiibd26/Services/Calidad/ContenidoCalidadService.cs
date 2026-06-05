using eiibd26.Data;
using eiibd26.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services.Calidad
{
    public class ContenidoCalidadService : IContenidoCalidadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISimilarQuestionDetector _detector;
        private readonly ILogger<ContenidoCalidadService> _logger;

        // Compiled once at startup — correcto para static (vs. RegexOptions.Compiled en Regex.Replace estático que recompila cada vez)
        private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex NormalizarRegex = new(@"[^a-záéíóúñü0-9\s]", RegexOptions.Compiled);
        private static readonly Regex EspaciosRegex = new(@"\s+", RegexOptions.Compiled);

        // Mismas stopwords que SimilarQuestionDetector para que el pre-filtro Jaccard sea coherente
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "el", "la", "los", "las", "un", "una", "unos", "unas",
            "de", "del", "a", "ante", "con", "contra", "desde", "en",
            "entre", "hacia", "hasta", "para", "por", "según", "sin",
            "sobre", "tras", "durante", "mediante",
            "que", "cual", "quien", "donde", "cuando", "como", "porque",
            "es", "son", "está", "están", "ser", "estar", "hay",
            "me", "te", "se", "nos", "os", "mi", "tu", "su",
            "y", "o", "pero", "si", "no", "ni"
        };

        public ContenidoCalidadService(
            ApplicationDbContext db,
            ISimilarQuestionDetector detector,
            ILogger<ContenidoCalidadService> logger)
        {
            _db = db;
            _detector = detector;
            _logger = logger;
        }

        public async Task<List<ContenidoCalidadDto>> AnalizarTodosAsync()
        {
            _logger.LogInformation("[CalidadContenido] Iniciando análisis de calidad");

            var contenidos = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .Include(c => c.CategoriasRelacion.Where(r => !r.Borrado))
                .OrderBy(c => c.Id)
                .ToListAsync();

            _logger.LogInformation("[CalidadContenido] {Count} contenidos a evaluar", contenidos.Count);

            // Pre-calcular textos y keyword-sets (O(n)) — todo en memoria, sin tocar la BD de nuevo
            var textos = contenidos.Select(c => new
            {
                c.Id,
                c.IdTipo,
                // StripHtml truncado a 1500 antes de procesar, luego resultado a 600 para comparación
                Texto = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL, 1500)}", 600)
            }).ToList();

            // Keywords pre-computadas con la misma normalización que SimilarQuestionDetector
            var kwSets = textos.ToDictionary(t => t.Id, t => ExtraerKeywordsLocal(t.Texto));

            // Detectar duplicados O(n²) con pre-filtro Jaccard para evitar LOH de Levenshtein
            // Sin pre-filtro: n=100 → 4.950 pares × 354 KB de int[,] en LOH = ~1,75 GB → Gen2 GC thrash
            var duplicados = contenidos.ToDictionary(c => c.Id, _ => new List<int>());
            int llamadasCalcularSimilitud = 0;

            for (int i = 0; i < textos.Count; i++)
            {
                for (int j = i + 1; j < textos.Count; j++)
                {
                    // Skip pares de diferente IdTipo (ambos definidos)
                    if (textos[i].IdTipo.HasValue && textos[j].IdTipo.HasValue
                        && textos[i].IdTipo != textos[j].IdTipo)
                        continue;

                    // Pre-filtro matemático: score = Jaccard×0.7 + Levenshtein×0.3
                    // Para score ≥ 0.80 con Levenshtein ≤ 1.0 → necesitamos Jaccard ≥ 0.70
                    // Si Jaccard local < 0.70, el score combinado máximo es 0.70×0.7 + 1.0×0.3 = 0.79 < 0.80
                    // → imposible ser duplicado → skip sin llamar CalcularSimilitud
                    var jaccardLocal = JaccardLocal(kwSets[textos[i].Id], kwSets[textos[j].Id]);
                    if (jaccardLocal < 0.70) continue;

                    // Solo llegamos aquí para pares con ≥70% de keywords en común (~1% del total)
                    var sim = _detector.CalcularSimilitud(textos[i].Texto, textos[j].Texto);
                    llamadasCalcularSimilitud++;

                    if (sim >= 0.80)
                    {
                        duplicados[textos[i].Id].Add(textos[j].Id);
                        duplicados[textos[j].Id].Add(textos[i].Id);
                    }
                }
            }

            _logger.LogInformation(
                "[CalidadContenido] Similitud: {Total} pares totales, {Llamadas} comparaciones completas ({Pct:P1} del total)",
                textos.Count * (textos.Count - 1) / 2,
                llamadasCalcularSimilitud,
                textos.Count > 1
                    ? (double)llamadasCalcularSimilitud / (textos.Count * (textos.Count - 1) / 2)
                    : 0);

            var resultados = new List<ContenidoCalidadDto>();

            foreach (var c in contenidos)
            {
                var senales = new List<SenalCalidad>();
                var palabras = ContarPalabras(c.ContenidoTextoL);

                // Señales CRÍTICAS
                if (palabras < 50)
                    senales.Add(new SenalCalidad("SIN_CUERPO",
                        palabras == 0 ? "Sin cuerpo" : $"Cuerpo muy corto ({palabras} palabras, mínimo 50)",
                        GravedadSenal.Critica));

                if (duplicados[c.Id].Count > 0)
                    senales.Add(new SenalCalidad("DUPLICADO",
                        $"Similar a {duplicados[c.Id].Count} contenido(s)",
                        GravedadSenal.Critica));

                // Señales MEJORABLES
                if (string.IsNullOrWhiteSpace(c.URLImagenPrincipal))
                    senales.Add(new SenalCalidad("SIN_IMAGEN", "Sin imagen principal", GravedadSenal.Mejorable));

                if (string.IsNullOrWhiteSpace(c.ContenidoTextoC))
                    senales.Add(new SenalCalidad("SIN_RESUMEN", "Sin resumen/descripción", GravedadSenal.Mejorable));

                if (palabras >= 50 && palabras <= 100)
                    senales.Add(new SenalCalidad("CUERPO_CORTO",
                        $"Cuerpo corto ({palabras} palabras)",
                        GravedadSenal.Mejorable));

                if (!c.CategoriasRelacion.Any())
                    senales.Add(new SenalCalidad("SIN_CATEGORIA", "Sin categoría asignada", GravedadSenal.Mejorable));

                if (string.IsNullOrWhiteSpace(c.ContenidoTituloSlug))
                    senales.Add(new SenalCalidad("SIN_SLUG", "Sin slug", GravedadSenal.Mejorable));

                if (c.EstadoPublicacion == 0 && c.FechaCreado < DateTime.UtcNow.AddDays(-30))
                    senales.Add(new SenalCalidad("BORRADOR_VIEJO",
                        $"Borrador sin publicar hace {(DateTime.UtcNow - c.FechaCreado).Days} días",
                        GravedadSenal.Mejorable));

                NivelSemaforo nivel;
                if (senales.Any(s => s.Gravedad == GravedadSenal.Critica))
                    nivel = NivelSemaforo.Critico;
                else if (senales.Any())
                    nivel = NivelSemaforo.Mejorable;
                else
                    nivel = NivelSemaforo.Ok;

                resultados.Add(new ContenidoCalidadDto
                {
                    Id = c.Id,
                    Titulo = string.IsNullOrWhiteSpace(c.ContenidoTitulo) ? "(sin título)" : c.ContenidoTitulo,
                    Slug = c.ContenidoTituloSlug,
                    EstadoPublicacion = c.EstadoPublicacion,
                    FechaCreado = c.FechaCreado,
                    Senales = senales,
                    NivelSemaforo = nivel,
                    DuplicadoDeIds = duplicados[c.Id]
                });
            }

            _logger.LogInformation(
                "[CalidadContenido] Análisis completo — Críticos: {Criticos} / Mejorables: {Mejorables} / Ok: {Ok}",
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Critico),
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Mejorable),
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Ok));

            return resultados.OrderBy(r => r.NivelSemaforo).ToList();
        }

        private static int ContarPalabras(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            // Truncar a 5000 chars antes de procesar — solo necesitamos saber si hay > 100 palabras
            var limpio = StripHtml(texto, 5000);
            return limpio.Split(new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        // maxInputChars evita procesar megabytes de HTML (ej. imágenes base64 incrustadas)
        private static string StripHtml(string? html, int maxInputChars = 5000)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var entrada = html.Length > maxInputChars ? html[..maxInputChars] : html;
            return HtmlTagRegex.Replace(entrada, " ").Trim();
        }

        private static string TruncarTexto(string texto, int maxChars)
            => texto.Length > maxChars ? texto[..maxChars] : texto;

        // Misma normalización que SimilarQuestionDetector para que el Jaccard local sea coherente
        private static HashSet<string> ExtraerKeywordsLocal(string texto)
        {
            var normalizado = texto.ToLowerInvariant();
            normalizado = NormalizarRegex.Replace(normalizado, " ");
            normalizado = EspaciosRegex.Replace(normalizado, " ").Trim();

            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var palabra in normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (palabra.Length >= 3 && !StopWords.Contains(palabra))
                    keywords.Add(palabra);
            }
            return keywords;
        }

        private static double JaccardLocal(HashSet<string> set1, HashSet<string> set2)
        {
            if (set1.Count == 0 && set2.Count == 0) return 1.0;
            if (set1.Count == 0 || set2.Count == 0) return 0.0;
            var interseccion = set1.Count(w => set2.Contains(w));
            var union = set1.Count + set2.Count - interseccion;
            return union > 0 ? (double)interseccion / union : 0;
        }
    }
}
