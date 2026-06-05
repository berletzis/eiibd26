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

            // Pre-calcular textos para comparación (truncar a 600 chars para performance)
            var textos = contenidos.Select(c => new
            {
                c.Id,
                c.IdTipo,
                Texto = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL)}", 600)
            }).ToList();

            // Detectar duplicados O(n²) — comparar dentro del mismo IdTipo si está definido
            var duplicados = contenidos.ToDictionary(c => c.Id, _ => new List<int>());

            for (int i = 0; i < textos.Count; i++)
            {
                for (int j = i + 1; j < textos.Count; j++)
                {
                    // Mitigación de performance: skip si diferente IdTipo (cuando ambos lo tienen)
                    if (textos[i].IdTipo.HasValue && textos[j].IdTipo.HasValue
                        && textos[i].IdTipo != textos[j].IdTipo)
                        continue;

                    var sim = _detector.CalcularSimilitud(textos[i].Texto, textos[j].Texto);
                    if (sim >= 0.80)
                    {
                        duplicados[textos[i].Id].Add(textos[j].Id);
                        duplicados[textos[j].Id].Add(textos[i].Id);
                    }
                }
            }

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

                if (!c.CategoriasRelacion.Any(r => !r.Borrado))
                    senales.Add(new SenalCalidad("SIN_CATEGORIA", "Sin categoría asignada", GravedadSenal.Mejorable));

                if (string.IsNullOrWhiteSpace(c.ContenidoTituloSlug))
                    senales.Add(new SenalCalidad("SIN_SLUG", "Sin slug", GravedadSenal.Mejorable));

                if (c.EstadoPublicacion == 0 && c.FechaCreado < DateTime.UtcNow.AddDays(-30))
                    senales.Add(new SenalCalidad("BORRADOR_VIEJO",
                        $"Borrador sin publicar hace {(DateTime.UtcNow - c.FechaCreado).Days} días",
                        GravedadSenal.Mejorable));

                // Nivel del semáforo
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
            var limpio = StripHtml(texto);
            return limpio.Split(new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string StripHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return Regex.Replace(html, "<[^>]+>", " ", RegexOptions.Compiled).Trim();
        }

        private static string TruncarTexto(string texto, int maxChars)
            => texto.Length > maxChars ? texto[..maxChars] : texto;
    }
}
