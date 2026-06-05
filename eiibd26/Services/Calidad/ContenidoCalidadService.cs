using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
                var senales = EvaluarSenalesContenido(c, duplicados[c.Id]);
                var nivel = CalcularNivel(senales);

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

        public async Task<CalidadBatchResultDto> AnalizarBatchAsync(int skip, int take)
        {
            _logger.LogInformation("[CalidadContenido] Batch skip={Skip} take={Take}", skip, take);

            // Query 1 (ligera): todos los contenidos — necesarios para contexto de duplicados
            var todosLigeros = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.IdTipo, c.ContenidoTitulo, c.ContenidoTextoL })
                .ToListAsync();

            var total = todosLigeros.Count;

            // Query 2 (completa): solo el batch — con todos los campos y categorías
            var batchContenidos = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .Include(c => c.CategoriasRelacion.Where(r => !r.Borrado))
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            // Pre-calcular textos y keyword-sets de TODOS (para comparación de duplicados)
            var todosTextos = todosLigeros
                .Select(c => new
                {
                    c.Id,
                    c.IdTipo,
                    Texto = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL, 1500)}", 600)
                })
                .ToDictionary(t => t.Id);

            var kwSets = todosTextos.ToDictionary(
                kv => kv.Key,
                kv => ExtraerKeywordsLocal(kv.Value.Texto));

            // Duplicados: comparar cada item del batch contra TODOS los contenidos
            var duplicadosDeBatch = batchContenidos.ToDictionary(c => c.Id, _ => new List<int>());

            foreach (var batchItem in batchContenidos)
            {
                if (!todosTextos.TryGetValue(batchItem.Id, out var textoItem)) continue;
                var kwBatch = kwSets.GetValueOrDefault(batchItem.Id, new HashSet<string>());
                if (kwBatch.Count == 0) continue;

                foreach (var (otherId, otroTexto) in todosTextos)
                {
                    if (otherId == batchItem.Id) continue;

                    // Skip diferente IdTipo (cuando ambos lo tienen definido)
                    if (textoItem.IdTipo.HasValue && otroTexto.IdTipo.HasValue
                        && textoItem.IdTipo != otroTexto.IdTipo)
                        continue;

                    // Pre-filtro Jaccard: si Jaccard < 0.70, score combinado máximo = 0.79 < 0.80
                    var kwOtro = kwSets.GetValueOrDefault(otherId, new HashSet<string>());
                    if (JaccardLocal(kwBatch, kwOtro) < 0.70) continue;

                    var sim = _detector.CalcularSimilitud(textoItem.Texto, otroTexto.Texto);
                    if (sim >= 0.80)
                        duplicadosDeBatch[batchItem.Id].Add(otherId);
                }
            }

            // Evaluar señales para los items del batch
            var resultados = new List<ContenidoCalidadDto>();

            foreach (var c in batchContenidos)
            {
                var senales = EvaluarSenalesContenido(c, duplicadosDeBatch[c.Id]);
                var nivel = CalcularNivel(senales);

                resultados.Add(new ContenidoCalidadDto
                {
                    Id = c.Id,
                    Titulo = string.IsNullOrWhiteSpace(c.ContenidoTitulo) ? "(sin título)" : c.ContenidoTitulo,
                    Slug = c.ContenidoTituloSlug,
                    EstadoPublicacion = c.EstadoPublicacion,
                    FechaCreado = c.FechaCreado,
                    Senales = senales,
                    NivelSemaforo = nivel,
                    DuplicadoDeIds = duplicadosDeBatch[c.Id]
                });
            }

            // UPSERT en ContenidoCalidad — 10 filas, trivial
            var batchIds = resultados.Select(r => r.Id).ToList();
            var existentes = await _db.ContenidoCalidad
                .Where(x => batchIds.Contains(x.ContenidoId))
                .ToDictionaryAsync(x => x.ContenidoId);

            var ahora = DateTime.UtcNow;
            foreach (var dto in resultados)
            {
                var senalesJson = JsonSerializer.Serialize(
                    dto.Senales.Select(s => new SenalJson(s.Codigo, s.Descripcion, s.Gravedad.ToString())));
                var duplicadosJson = JsonSerializer.Serialize(dto.DuplicadoDeIds);

                if (existentes.TryGetValue(dto.Id, out var fila))
                {
                    fila.NivelSemaforo = (byte)dto.NivelSemaforo;
                    fila.Senales = senalesJson;
                    fila.DuplicadoDeIds = duplicadosJson;
                    fila.FechaAnalisis = ahora;
                }
                else
                {
                    _db.ContenidoCalidad.Add(new Models.Calidad.ContenidoCalidad
                    {
                        ContenidoId = dto.Id,
                        NivelSemaforo = (byte)dto.NivelSemaforo,
                        Senales = senalesJson,
                        DuplicadoDeIds = duplicadosJson,
                        FechaAnalisis = ahora
                    });
                }
            }
            await _db.SaveChangesAsync();

            return new CalidadBatchResultDto { Total = total, Items = resultados };
        }

        public async Task<ResultadosGuardadosDto?> ObtenerResultadosGuardadosAsync()
        {
            var filas = await (
                from cq in _db.ContenidoCalidad
                join c in _db.Contenidos on cq.ContenidoId equals c.Id
                where !c.Eliminado
                orderby cq.NivelSemaforo, c.ContenidoTitulo
                select new { cq, c }
            ).AsNoTracking().ToListAsync();

            if (!filas.Any()) return null;

            var resultados = filas.Select(x =>
            {
                var senales = DeserializarSenales(x.cq.Senales);
                var duplicados = string.IsNullOrWhiteSpace(x.cq.DuplicadoDeIds)
                    ? new List<int>()
                    : JsonSerializer.Deserialize<List<int>>(x.cq.DuplicadoDeIds) ?? new();

                return new ContenidoCalidadDto
                {
                    Id = x.c.Id,
                    Titulo = string.IsNullOrWhiteSpace(x.c.ContenidoTitulo) ? "(sin título)" : x.c.ContenidoTitulo,
                    Slug = x.c.ContenidoTituloSlug,
                    EstadoPublicacion = x.c.EstadoPublicacion,
                    FechaCreado = x.c.FechaCreado,
                    NivelSemaforo = (NivelSemaforo)x.cq.NivelSemaforo,
                    Senales = senales,
                    DuplicadoDeIds = duplicados
                };
            }).ToList();

            return new ResultadosGuardadosDto
            {
                Resultados = resultados,
                UltimoAnalisis = filas.Max(x => x.cq.FechaAnalisis)
            };
        }

        public async Task AnalizarYGuardarUnoAsync(int contenidoId)
        {
            _logger.LogInformation("[CalidadContenido] Análisis individual contenidoId={Id}", contenidoId);

            var c = await _db.Contenidos
                .AsNoTracking()
                .Where(x => x.Id == contenidoId && !x.Eliminado)
                .Include(x => x.CategoriasRelacion.Where(r => !r.Borrado))
                .FirstOrDefaultAsync();

            if (c == null)
            {
                _logger.LogWarning("[CalidadContenido] Contenido {Id} no encontrado — análisis omitido", contenidoId);
                return;
            }

            // Todos los demás (ligero) para detección de duplicados
            var todosLigeros = await _db.Contenidos
                .AsNoTracking()
                .Where(x => !x.Eliminado && x.Id != contenidoId)
                .Select(x => new { x.Id, x.IdTipo, x.ContenidoTitulo, x.ContenidoTextoL })
                .ToListAsync();

            var textoC = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL, 1500)}", 600);
            var kwC = ExtraerKeywordsLocal(textoC);

            var duplicados = new List<int>();
            if (kwC.Count > 0)
            {
                foreach (var otro in todosLigeros)
                {
                    if (c.IdTipo.HasValue && otro.IdTipo.HasValue && c.IdTipo != otro.IdTipo) continue;

                    var textoOtro = TruncarTexto($"{otro.ContenidoTitulo} {StripHtml(otro.ContenidoTextoL, 1500)}", 600);
                    var kwOtro = ExtraerKeywordsLocal(textoOtro);

                    if (JaccardLocal(kwC, kwOtro) < 0.70) continue;

                    var sim = _detector.CalcularSimilitud(textoC, textoOtro);
                    if (sim >= 0.80)
                        duplicados.Add(otro.Id);
                }
            }

            var senales = EvaluarSenalesContenido(c, duplicados);
            var nivel = CalcularNivel(senales);

            var senalesJson = JsonSerializer.Serialize(
                senales.Select(s => new SenalJson(s.Codigo, s.Descripcion, s.Gravedad.ToString())));
            var duplicadosJson = JsonSerializer.Serialize(duplicados);
            var ahora = DateTime.UtcNow;

            var fila = await _db.ContenidoCalidad.FirstOrDefaultAsync(x => x.ContenidoId == contenidoId);
            if (fila != null)
            {
                fila.NivelSemaforo = (byte)nivel;
                fila.Senales = senalesJson;
                fila.DuplicadoDeIds = duplicadosJson;
                fila.FechaAnalisis = ahora;
            }
            else
            {
                _db.ContenidoCalidad.Add(new Models.Calidad.ContenidoCalidad
                {
                    ContenidoId = contenidoId,
                    NivelSemaforo = (byte)nivel,
                    Senales = senalesJson,
                    DuplicadoDeIds = duplicadosJson,
                    FechaAnalisis = ahora
                });
            }
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "[CalidadContenido] Contenido {Id} — {Nivel}, {Senales} señales, {Dups} duplicados",
                contenidoId, nivel, senales.Count, duplicados.Count);
        }

        private static List<SenalCalidad> EvaluarSenalesContenido(Contenido c, List<int> duplicadosIds)
        {
            var senales = new List<SenalCalidad>();
            var palabras = ContarPalabras(c.ContenidoTextoL);

            if (palabras < 50)
                senales.Add(new SenalCalidad("SIN_CUERPO",
                    palabras == 0 ? "Sin cuerpo" : $"Cuerpo muy corto ({palabras} palabras, mínimo 50)",
                    GravedadSenal.Critica));

            if (duplicadosIds.Count > 0)
                senales.Add(new SenalCalidad("DUPLICADO",
                    $"Similar a {duplicadosIds.Count} contenido(s)",
                    GravedadSenal.Critica));

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

            return senales;
        }

        private static NivelSemaforo CalcularNivel(List<SenalCalidad> senales) =>
            senales.Any(s => s.Gravedad == GravedadSenal.Critica) ? NivelSemaforo.Critico
            : senales.Any() ? NivelSemaforo.Mejorable
            : NivelSemaforo.Ok;

        private static List<SenalCalidad> DeserializarSenales(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                var raw = JsonSerializer.Deserialize<List<SenalJson>>(json);
                if (raw == null) return new();
                return raw.Select(s => new SenalCalidad(
                    s.Codigo,
                    s.Descripcion,
                    Enum.TryParse<GravedadSenal>(s.Gravedad, out var g) ? g : GravedadSenal.Mejorable
                )).ToList();
            }
            catch { return new(); }
        }

        private record SenalJson(string Codigo, string Descripcion, string Gravedad);

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
