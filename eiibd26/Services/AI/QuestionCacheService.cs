using eiibd26.Data;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del servicio de caché de respuestas
    /// Busca preguntas similares para reutilizar respuestas existentes
    /// </summary>
    public class QuestionCacheService : IQuestionCacheService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<QuestionCacheService> _logger;

        public QuestionCacheService(
            ApplicationDbContext db,
            ILogger<QuestionCacheService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Respuesta?> BuscarRespuestaSimilarAsync(
            Pregunta pregunta, 
            double umbralSimilitud = 0.85)
        {
            try
            {
                var textoPreguntaActual = NormalizarTexto($"{pregunta.Titulo} {pregunta.Cuerpo}");

                // Buscar preguntas que ya tienen respuesta de IA
                var preguntasConRespuestaIA = await _db.Preguntas
                    .Where(p => p.TieneRespuestaIA && !p.Eliminado && p.Id != pregunta.Id)
                    .Include(p => p.Respuestas.Where(r => r.EsIA && !r.Eliminado))
                    .OrderByDescending(p => p.FechaGeneracionIA)
                    .Take(100) // Limitar a las 100 más recientes para performance
                    .ToListAsync();

                _logger.LogInformation(
                    "🔍 [Question Cache] Analizando {Count} preguntas con respuesta IA",
                    preguntasConRespuestaIA.Count);

                Respuesta? mejorCoincidencia = null;
                double mejorSimilitud = 0.0;

                foreach (var preguntaExistente in preguntasConRespuestaIA)
                {
                    var textoExistente = NormalizarTexto($"{preguntaExistente.Titulo} {preguntaExistente.Cuerpo}");
                    var similitud = CalcularSimilitud(textoPreguntaActual, textoExistente);

                    if (similitud > mejorSimilitud && similitud >= umbralSimilitud)
                    {
                        mejorSimilitud = similitud;
                        mejorCoincidencia = preguntaExistente.Respuestas.FirstOrDefault(r => r.EsIA && !r.Eliminado);

                        _logger.LogInformation(
                            "✅ [Question Cache] Coincidencia encontrada: {Similitud:P2} - PreguntaId={PreguntaId}",
                            similitud, preguntaExistente.Id);
                    }
                }

                if (mejorCoincidencia != null)
                {
                    _logger.LogInformation(
                        "🎯 [Question Cache] CACHE HIT: Similitud {Similitud:P2} - RespuestaId={RespuestaId}",
                        mejorSimilitud, mejorCoincidencia.Id);

                    return mejorCoincidencia;
                }

                _logger.LogInformation(
                    "❌ [Question Cache] CACHE MISS: No se encontraron preguntas similares (umbral: {Umbral:P2})",
                    umbralSimilitud);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar respuesta similar en caché");
                return null; // En caso de error, continuar con generación nueva
            }
        }

        public double CalcularSimilitud(string pregunta1, string pregunta2)
        {
            if (string.IsNullOrWhiteSpace(pregunta1) || string.IsNullOrWhiteSpace(pregunta2))
                return 0.0;

            var texto1 = NormalizarTexto(pregunta1);
            var texto2 = NormalizarTexto(pregunta2);

            // Usar distancia de Levenshtein normalizada
            var distancia = DistanciaLevenshtein(texto1, texto2);
            var longitudMaxima = Math.Max(texto1.Length, texto2.Length);

            if (longitudMaxima == 0)
                return 1.0;

            var similitud = 1.0 - ((double)distancia / longitudMaxima);
            return Math.Max(0.0, similitud); // Asegurar que no sea negativo
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Convertir a minúsculas
            texto = texto.ToLowerInvariant();

            // Remover caracteres especiales (mantener solo letras, números y espacios)
            texto = Regex.Replace(texto, @"[^a-záéíóúñü0-9\s]", " ");

            // Normalizar espacios múltiples
            texto = Regex.Replace(texto, @"\s+", " ");

            // Remover palabras muy comunes (stopwords)
            var stopwords = new HashSet<string> 
            { 
                "el", "la", "de", "que", "y", "a", "en", "un", "una", "por", 
                "con", "para", "es", "como", "del", "los", "las", "al", "se" 
            };

            var palabras = texto.Split(' ')
                .Where(p => !string.IsNullOrWhiteSpace(p) && !stopwords.Contains(p))
                .ToList();

            return string.Join(" ", palabras).Trim();
        }

        private int DistanciaLevenshtein(string s1, string s2)
        {
            var len1 = s1.Length;
            var len2 = s2.Length;
            var matriz = new int[len1 + 1, len2 + 1];

            // Inicializar primera fila y columna
            for (int i = 0; i <= len1; i++)
                matriz[i, 0] = i;

            for (int j = 0; j <= len2; j++)
                matriz[0, j] = j;

            // Calcular distancia
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    var costo = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    matriz[i, j] = Math.Min(
                        Math.Min(
                            matriz[i - 1, j] + 1,      // Eliminación
                            matriz[i, j - 1] + 1),     // Inserción
                        matriz[i - 1, j - 1] + costo); // Sustitución
                }
            }

            return matriz[len1, len2];
        }
    }
}
