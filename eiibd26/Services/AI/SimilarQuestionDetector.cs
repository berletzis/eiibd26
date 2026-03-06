using eiibd26.Data;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del detector de preguntas similares
    /// Usa algoritmo híbrido: Keywords + Levenshtein Distance
    /// </summary>
    public class SimilarQuestionDetector : ISimilarQuestionDetector
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SimilarQuestionDetector> _logger;

        // Palabras comunes en español que se ignoran en la comparación
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

        public SimilarQuestionDetector(
            ApplicationDbContext db,
            ILogger<SimilarQuestionDetector> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Respuesta?> BuscarRespuestaSimilarAsync(
            Pregunta pregunta,
            double umbralSimilitud = 0.80,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "🔍 [Similitud] Buscando preguntas similares a: '{Titulo}'",
                    pregunta.Titulo);

                // 1. Obtener preguntas con respuesta IA (últimos 90 días para performance)
                var fechaLimite = DateTimeOffset.UtcNow.AddDays(-90);

                var preguntasConIA = await _db.Preguntas
                    .Where(p => p.TieneRespuestaIA 
                                && !p.Eliminado 
                                && p.FechaGeneracionIA.HasValue
                                && p.FechaGeneracionIA.Value >= fechaLimite
                                && p.Id != pregunta.Id) // Excluir la pregunta actual
                    .Include(p => p.Respuestas.Where(r => r.EsIA && !r.Eliminado))
                    .OrderByDescending(p => p.FechaGeneracionIA)
                    .Take(100) // Limitar a últimas 100 para performance
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "📊 [Similitud] Encontradas {Count} preguntas con IA en últimos 90 días",
                    preguntasConIA.Count);

                if (!preguntasConIA.Any())
                {
                    _logger.LogInformation("📭 [Similitud] No hay preguntas previas con IA para comparar");
                    return null;
                }

                // 2. Calcular similitud con cada pregunta
                var textoActual = $"{pregunta.Titulo} {pregunta.Cuerpo}";
                double mejorSimilitud = 0;
                Pregunta? preguntaSimilar = null;

                foreach (var p in preguntasConIA)
                {
                    var textoComparar = $"{p.Titulo} {p.Cuerpo}";
                    var similitud = CalcularSimilitud(textoActual, textoComparar);

                    if (similitud > mejorSimilitud)
                    {
                        mejorSimilitud = similitud;
                        preguntaSimilar = p;
                    }

                    // Optimización: si encontramos 95%+ similitud, no seguir buscando
                    if (similitud >= 0.95)
                    {
                        _logger.LogInformation(
                            "🎯 [Similitud] Coincidencia casi exacta ({Similitud:P1}) encontrada, detiendo búsqueda",
                            similitud);
                        break;
                    }
                }

                // 3. Verificar si supera el umbral
                if (mejorSimilitud >= umbralSimilitud && preguntaSimilar != null)
                {
                    var respuestaIA = preguntaSimilar.Respuestas.FirstOrDefault(r => r.EsIA && !r.Eliminado);

                    if (respuestaIA != null)
                    {
                        _logger.LogInformation(
                            "✅ [Similitud] Pregunta similar encontrada con {Similitud:P1} de similitud:\n" +
                            "  Original: '{TituloOriginal}'\n" +
                            "  Similar: '{TituloSimilar}'\n" +
                            "  RespuestaId: {RespuestaId}",
                            mejorSimilitud,
                            pregunta.Titulo,
                            preguntaSimilar.Titulo,
                            respuestaIA.Id);

                        return respuestaIA;
                    }
                }

                _logger.LogInformation(
                    "📉 [Similitud] No se encontró coincidencia suficiente. Mejor similitud: {Similitud:P1} (umbral: {Umbral:P1})",
                    mejorSimilitud,
                    umbralSimilitud);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Similitud] Error al buscar preguntas similares");
                return null;
            }
        }

        public double CalcularSimilitud(string texto1, string texto2)
        {
            if (string.IsNullOrWhiteSpace(texto1) || string.IsNullOrWhiteSpace(texto2))
                return 0;

            // Normalizar textos
            var t1Normalizado = NormalizarTexto(texto1);
            var t2Normalizado = NormalizarTexto(texto2);

            // Extraer keywords (sin stopwords)
            var keywords1 = ExtraerKeywords(t1Normalizado);
            var keywords2 = ExtraerKeywords(t2Normalizado);

            // Similitud por keywords (Jaccard Index)
            double similitudKeywords = CalcularJaccardSimilarity(keywords1, keywords2);

            // Similitud por Levenshtein (sobre primeras 300 caracteres para performance)
            var t1Corto = t1Normalizado.Length > 300 ? t1Normalizado.Substring(0, 300) : t1Normalizado;
            var t2Corto = t2Normalizado.Length > 300 ? t2Normalizado.Substring(0, 300) : t2Normalizado;
            double similitudLevenshtein = CalcularLevenshteinSimilarity(t1Corto, t2Corto);

            // Promedio ponderado (keywords 70%, levenshtein 30%)
            double similitudFinal = (similitudKeywords * 0.7) + (similitudLevenshtein * 0.3);

            return similitudFinal;
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Convertir a minúsculas
            texto = texto.ToLowerInvariant();

            // Remover caracteres especiales, dejar solo letras, números y espacios
            texto = Regex.Replace(texto, @"[^a-záéíóúñü0-9\s]", " ");

            // Remover espacios múltiples
            texto = Regex.Replace(texto, @"\s+", " ");

            return texto.Trim();
        }

        private HashSet<string> ExtraerKeywords(string textoNormalizado)
        {
            var palabras = textoNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var palabra in palabras)
            {
                // Ignorar palabras muy cortas o stopwords
                if (palabra.Length >= 3 && !StopWords.Contains(palabra))
                {
                    keywords.Add(palabra);
                }
            }

            return keywords;
        }

        private double CalcularJaccardSimilarity(HashSet<string> set1, HashSet<string> set2)
        {
            if (set1.Count == 0 && set2.Count == 0)
                return 1.0; // Ambos vacíos = idénticos

            if (set1.Count == 0 || set2.Count == 0)
                return 0.0;

            var interseccion = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return (double)interseccion / union;
        }

        private double CalcularLevenshteinSimilarity(string s1, string s2)
        {
            var distancia = LevenshteinDistance(s1, s2);
            var maxLongitud = Math.Max(s1.Length, s2.Length);

            if (maxLongitud == 0)
                return 1.0;

            return 1.0 - ((double)distancia / maxLongitud);
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }
    }
}
