using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Glossary;
using eiibd26.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/tratamientos")]
    public class TratamientosAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ISintomasTratamientosAiService _aiService;
        private readonly ILogger<TratamientosAdminController> _logger;

        public TratamientosAdminController(
            ApplicationDbContext db,
            ISintomasTratamientosAiService aiService,
            ILogger<TratamientosAdminController> logger)
        {
            _db = db;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Genera descripción IA para un tratamiento
        /// POST /api/admin/tratamientos/{id}/generate-ia-description
        /// </summary>
        [HttpPost("{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateIaDescription(int id, CancellationToken cancellationToken)
        {
            try
            {
                var tratamiento = await _db.tratamientos
                    .FirstOrDefaultAsync(t => t.id == id && !t.Eliminado, cancellationToken);

                if (tratamiento == null)
                    return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

                if (string.IsNullOrWhiteSpace(tratamiento.nombre))
                    return BadRequest(new { ok = false, error = "El tratamiento no tiene nombre" });

                _logger.LogInformation("Generando descripción IA para tratamiento {Id}: {Nombre}", id, tratamiento.nombre);

                var (descripcion, relacionEII, nombreTraducido) = await _aiService.GenerarDescripcionTratamientoAsync(
                    tratamiento.nombre, 
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
                    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "IA sugirió nombre '{NombreTraducido}' para tratamiento '{NombreOriginal}' — pendiente aprobación admin.",
                        nombreTraducido, tratamiento.nombre);
                    tratamiento.NombreSugeridoIA = nombreTraducido;
                    tratamiento.ValidadoHumano = false;
                }

                tratamiento.DescripcionIA = descripcion;
                tratamiento.ValidadoIA = true;
                tratamiento.RelacionEII = relacionEII;
                tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                tratamiento.Fuentes = _aiService.UltimasFuentes;
                tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                tratamiento.fechaModificado = DateTime.Now;

                await _db.SaveChangesAsync(cancellationToken);

                // Propagar nivel de relación y razonamiento al GlossaryTerm vinculado
                await PropagateToGlossaryTermAsync(tratamientoId: id, cancellationToken);

                _logger.LogInformation("Descripción IA guardada exitosamente para tratamiento {Id}", id);

                return Ok(new
                {
                    ok = true,
                    descripcion,
                    relacionEII,
                    relacionEIITexto  = tratamiento.RelacionEIIDescripcion,
                    nivelRelacion     = _aiService.UltimoNivelRelacion?.ToString(),
                    razonamiento      = _aiService.UltimoRazonamiento,
                    fuentes           = tratamiento.Fuentes,
                    nombreTraducido   = nombreTraducido
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para tratamiento {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar la descripción: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un tratamiento por ID
        /// GET /api/admin/tratamientos/{id}
        /// </summary>
        // {id:int} — sin la restricción, esta plantilla compite con las rutas literales
        // ("ramas", "basura-preview"). La precedencia de literal sobre parámetro las salvaría,
        // pero el constraint lo vuelve explícito en vez de depender de la regla.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTratamiento(int id, CancellationToken cancellationToken)
        {
            var tratamiento = await _db.tratamientos
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.id == id, cancellationToken);

            if (tratamiento == null)
                return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

            return Ok(new 
            {
                ok = true,
                id = tratamiento.id,
                nombre = tratamiento.nombre ?? "",
                idPadre = tratamiento.idPadre,
                idIdioma = tratamiento.idIdioma,
                icono = tratamiento.icono ?? "",
                eliminado = tratamiento.Eliminado,
                descripcionIA = tratamiento.DescripcionIA ?? "",
                validadoIA = tratamiento.ValidadoIA,
                validadoHumano = tratamiento.ValidadoHumano,
                relacionEII = tratamiento.RelacionEII,
                relacionEIIDescripcion = tratamiento.RelacionEIIDescripcion ?? "",
                fuentes = tratamiento.Fuentes ?? ""
            });
        }

        /// <summary>
        /// Datos del GlossaryTerm vinculado al tratamiento: niveles de relación y validaciones.
        /// GET /api/admin/tratamientos/{id}/glossary
        /// </summary>
        [HttpGet("{id}/glossary")]
        public async Task<IActionResult> GetGlossaryData(int id, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .AsNoTracking()
                    .Where(l => l.TratamientoId == id)
                    .Select(l => new { l.GlossaryTermId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (link == null)
                    return Ok(new { ok = true, hasGlossaryTerm = false });

                int? nivelSugerido  = null;
                int? nivelConfirmado = null;
                string? aiReasoning = null;
                bool createdByAI    = true;
                try
                {
                    var term = await _db.GlossaryTerms
                        .AsNoTracking()
                        .Where(t => t.Id == link.GlossaryTermId)
                        .Select(t => new
                        {
                            t.CreatedByAI,
                            SuggestedId  = t.MedicalRelationSuggestedId.HasValue ? (int?)t.MedicalRelationSuggestedId : null,
                            ConfirmedId  = t.MedicalRelationTypeId.HasValue      ? (int?)t.MedicalRelationTypeId      : null,
                            t.AiReasoning
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (term != null)
                    {
                        createdByAI     = term.CreatedByAI;
                        nivelSugerido   = term.SuggestedId;
                        nivelConfirmado = term.ConfirmedId;
                        aiReasoning     = term.AiReasoning;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Columnas nuevas no disponibles aún (GlossaryTerm {Id})", link.GlossaryTermId);
                }

                var validations = new List<object>();
                try
                {
                    var raw = await _db.GlossaryValidations
                        .AsNoTracking()
                        .Where(v => v.GlossaryTermId == link.GlossaryTermId && v.Approved)
                        .OrderByDescending(v => v.CreatedAt)
                        .Select(v => new
                        {
                            v.UserId,
                            ValidationType = (int)v.ValidationType,
                            RelationTypeId = v.MedicalRelationTypeId.HasValue ? (int?)v.MedicalRelationTypeId : null,
                            v.Comment,
                            v.CreatedAt
                        })
                        .ToListAsync(cancellationToken);

                    var userGuids = raw
                        .Select(v => v.UserId)
                        .Distinct()
                        .Where(uid => Guid.TryParse(uid, out _))
                        .Select(Guid.Parse)
                        .ToList();

                    var nameDict = await _db.Users
                        .AsNoTracking()
                        .Where(u => userGuids.Contains(u.Id))
                        .Select(u => new { Id = u.Id.ToString(), Display = u.UserName ?? u.Email ?? "Usuario" })
                        .ToDictionaryAsync(u => u.Id.ToLowerInvariant(), u => u.Display, cancellationToken);

                    validations = raw.Select(v => (object)new
                    {
                        userDisplay    = nameDict.TryGetValue(v.UserId.ToLowerInvariant(), out var n) ? n : "Usuario",
                        validationType = v.ValidationType,
                        relationTypeId = v.RelationTypeId,
                        comment        = v.Comment,
                        createdAt      = v.CreatedAt
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "GlossaryValidation no disponible aún (término {Id})", link.GlossaryTermId);
                }

                return Ok(new
                {
                    ok              = true,
                    hasGlossaryTerm = true,
                    glossaryTermId  = link.GlossaryTermId,
                    createdByAI,
                    nivelSugerido,
                    nivelConfirmado,
                    aiReasoning,
                    validations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de glosario para tratamiento {Id}", id);
                return Ok(new { ok = true, hasGlossaryTerm = false });
            }
        }

        /// <summary>
        /// Actualiza un tratamiento
        /// PUT /api/admin/tratamientos/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTratamiento(int id, [FromBody] UpdateTratamientoRequest request, CancellationToken cancellationToken)
        {
            var tratamiento = await _db.tratamientos
                .FirstOrDefaultAsync(t => t.id == id, cancellationToken);

            if (tratamiento == null)
                return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

            tratamiento.nombre = request.Nombre;
            tratamiento.idPadre = request.IdPadre;
            tratamiento.idIdioma = request.IdIdioma;
            tratamiento.icono = request.Icono;
            tratamiento.Eliminado = request.Eliminado;
            tratamiento.DescripcionIA = request.DescripcionIA;
            tratamiento.ValidadoHumano = request.ValidadoHumano;
            tratamiento.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { ok = true });
        }

        /// <summary>
        /// Genera descripciones IA para múltiples tratamientos
        /// POST /api/admin/tratamientos/batch-generate-ia
        /// </summary>
        [HttpPost("batch-generate-ia")]
        public async Task<IActionResult> BatchGenerateIaDescriptions([FromBody] BatchGenerateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var resultados = new List<BatchResultItem>();

                // Obtener los siguientes N tratamientos sin descripción IA (no eliminados)
                var tratamientos = await _db.tratamientos
                    .Where(t => !t.Eliminado)
                    .Where(t => string.IsNullOrEmpty(t.DescripcionIA) || !t.ValidadoIA)
                    .OrderBy(t => t.id)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Procesando batch de {Count} tratamientos. Skip: {Skip}", tratamientos.Count, request.Skip);

                foreach (var tratamiento in tratamientos)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(tratamiento.nombre))
                        {
                            resultados.Add(new BatchResultItem
                            {
                                Id = tratamiento.id,
                                Nombre = tratamiento.nombre ?? "Sin nombre",
                                Success = false,
                                Error = "El tratamiento no tiene nombre"
                            });
                            continue;
                        }

                        _logger.LogInformation("Generando descripción IA para tratamiento {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);

                        var (descripcion, relacionEII, nombreTraducido) = await _aiService.GenerarDescripcionTratamientoAsync(
                            tratamiento.nombre, 
                            cancellationToken);

                        var nombreOriginal = tratamiento.nombre;
                        if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
                            !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation(
                                "IA sugirió nombre '{NombreTraducido}' para tratamiento '{NombreOriginal}' — pendiente aprobación admin.",
                                nombreTraducido, tratamiento.nombre);
                            tratamiento.NombreSugeridoIA = nombreTraducido;
                            tratamiento.ValidadoHumano = false;
                        }

                        // Actualizar el tratamiento
                        tratamiento.DescripcionIA = descripcion;
                        tratamiento.ValidadoIA = true;
                        tratamiento.ValidadoHumano = false; // Resetear validación humana
                        tratamiento.RelacionEII = relacionEII;
                        tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                        tratamiento.Fuentes = _aiService.UltimasFuentes;
                        tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                        tratamiento.fechaModificado = DateTime.Now;

                        await _db.SaveChangesAsync(cancellationToken);

                        await PropagateToGlossaryTermAsync(tratamientoId: tratamiento.id, cancellationToken);

                        resultados.Add(new BatchResultItem
                        {
                            Id = tratamiento.id,
                            Nombre = tratamiento.nombre,
                            NombreSugeridoIA = tratamiento.NombreSugeridoIA,
                            Success = true,
                            RelacionEII = relacionEII
                        });

                        _logger.LogInformation("Tratamiento {Id} actualizado exitosamente", tratamiento.id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar tratamiento {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);

                        resultados.Add(new BatchResultItem
                        {
                            Id = tratamiento.id,
                            Nombre = tratamiento.nombre ?? "Sin nombre",
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                // Contar cuántos faltan por procesar
                var totalPendientes = await _db.tratamientos
                    .Where(t => !t.Eliminado)
                    .Where(t => string.IsNullOrEmpty(t.DescripcionIA) || !t.ValidadoIA)
                    .CountAsync(cancellationToken);

                return Ok(new 
                { 
                    ok = true,
                    procesados = resultados.Count,
                    exitosos = resultados.Count(r => r.Success),
                    fallidos = resultados.Count(r => !r.Success),
                    pendientes = totalPendientes,
                    resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento batch de tratamientos");
                return StatusCode(500, new { ok = false, error = "Error en procesamiento: " + ex.Message });
            }
        }

        /// <summary>Confianza mínima para que la IA pueda desactivar por su cuenta.</summary>
        private const double UmbralDesactivacion = 0.85;

        /// <summary>
        /// Triage de limpieza con NINA — clasifica los siguientes N tratamientos sin revisar.
        /// POST /api/admin/tratamientos/batch-review
        /// </summary>
        /// <remarks>
        /// No recibe Skip: reanuda solo, filtrando por <c>RevisionLimpiezaEstado IS NULL</c>.
        /// Con <c>DryRun = true</c> (default) NO toca <c>Eliminado</c> de nadie ni genera
        /// descripciones: únicamente estampa el sello de clasificación.
        /// </remarks>
        [HttpPost("batch-review")]
        public async Task<IActionResult> BatchReview([FromBody] BatchReviewRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var take = Math.Clamp(request.Take, 1, 1000);
                var resultados = new List<BatchReviewItem>();

                var query = _db.tratamientos
                    .Where(t => !t.Eliminado && t.RevisionLimpiezaEstado == null);

                // Enfocar por rama. El árbol es de 2 niveles (no hay nietos), así que la raíz
                // más sus hijos directos es la rama completa. Sin RaizId barre todo por id, que
                // gasta horas de IA en el catálogo curado antes de llegar a lo que la gente llenó.
                if (request.RaizId.HasValue)
                {
                    var raiz = request.RaizId.Value;
                    query = query.Where(t => t.id == raiz || t.idPadre == raiz);
                }

                var pendientes = await query
                    .OrderBy(t => t.id)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Triage NINA: {Count} tratamientos, DryRun={DryRun}, RaizId={RaizId}",
                    pendientes.Count, request.DryRun, request.RaizId?.ToString() ?? "(todo)");

                // Protegidos: validados por un humano o con pacientes usándolos hoy.
                // No son basura por definición — no se les gasta una llamada a la IA.
                var idsLote = pendientes.Select(t => t.id).ToList();
                var conUsuariosActivos = await _db.tratamientoUsuario
                    .Where(tu => tu.idTratamiento != null
                              && idsLote.Contains(tu.idTratamiento!.Value)
                              && !tu.Eliminado)
                    .Select(tu => tu.idTratamiento!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var setUsuariosActivos = conUsuariosActivos.ToHashSet();

                // Nodos padre con hijos activos: son la estructura del catálogo, no registros
                // sueltos. Desactivar uno deja a sus hijos colgando. El borrado manual ya lo
                // impide (OnPostEliminarTratamientoAsync); el lote debe respetar la misma regla.
                var conHijosActivos = await _db.tratamientos
                    .Where(h => h.idPadre != null && idsLote.Contains(h.idPadre!.Value) && !h.Eliminado)
                    .Select(h => h.idPadre!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var setConHijosActivos = conHijosActivos.ToHashSet();

                foreach (var tratamiento in pendientes)
                {
                    try
                    {
                        var protegidoPorHumano   = tratamiento.ValidadoHumano;
                        var protegidoPorUsuarios = setUsuariosActivos.Contains(tratamiento.id);

                        if (protegidoPorHumano || protegidoPorUsuarios)
                        {
                            var motivoProteccion = protegidoPorHumano
                                ? "Validado por un humano — se conserva sin consultar a la IA."
                                : "Pacientes lo tienen registrado hoy — se conserva sin consultar a la IA.";

                            tratamiento.RevisionLimpiezaEstado    = (byte)TriageLimpieza.Valido;
                            tratamiento.RevisionLimpiezaConfianza = 1m;
                            tratamiento.RevisionLimpiezaMotivo    = motivoProteccion;
                            tratamiento.RevisionLimpiezaFecha     = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);

                            resultados.Add(new BatchReviewItem
                            {
                                Id        = tratamiento.id,
                                Nombre    = tratamiento.nombre ?? "Sin nombre",
                                Estado    = (byte)TriageLimpieza.Valido,
                                Confianza = 1d,
                                Motivo    = motivoProteccion,
                                Protegido = true,
                                Success   = true
                            });
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(tratamiento.nombre))
                        {
                            // Sin nombre no hay nada que clasificar, pero tampoco se desactiva solo.
                            tratamiento.RevisionLimpiezaEstado = (byte)TriageLimpieza.Dudoso;
                            tratamiento.RevisionLimpiezaMotivo = "El registro no tiene nombre — requiere revisión humana.";
                            tratamiento.RevisionLimpiezaFecha  = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);

                            resultados.Add(new BatchReviewItem
                            {
                                Id      = tratamiento.id,
                                Nombre  = "Sin nombre",
                                Estado  = (byte)TriageLimpieza.Dudoso,
                                Motivo  = "El registro no tiene nombre — requiere revisión humana.",
                                Success = true
                            });
                            continue;
                        }

                        var (estado, confianza, motivo, nivel, razonamiento) =
                            await _aiService.ClasificarTratamientoAsync(
                                tratamiento.nombre, tratamiento.DescripcionIA, cancellationToken);

                        tratamiento.RevisionLimpiezaEstado    = estado;
                        tratamiento.RevisionLimpiezaConfianza = (decimal)confianza;
                        tratamiento.RevisionLimpiezaMotivo    = motivo;
                        tratamiento.RevisionLimpiezaFecha     = DateTime.UtcNow;

                        var desactivado = false;

                        var esPadreConHijos = setConHijosActivos.Contains(tratamiento.id);

                        if (estado == (byte)TriageLimpieza.Basura)
                        {
                            if (esPadreConHijos)
                            {
                                // Se conserva el veredicto para que el humano lo vea en el bucket,
                                // pero la IA NO puede desactivar un nodo del que cuelgan otros.
                                tratamiento.RevisionLimpiezaMotivo =
                                    $"[nodo padre con hijos activos, no desactivado] {motivo}";
                                if (tratamiento.RevisionLimpiezaMotivo.Length > 400)
                                    tratamiento.RevisionLimpiezaMotivo = tratamiento.RevisionLimpiezaMotivo[..400];
                            }
                            else if (!request.DryRun && confianza >= UmbralDesactivacion)
                            {
                                tratamiento.Eliminado       = true;
                                tratamiento.fechaEliminado  = DateTime.Now.Date;
                                tratamiento.fechaModificado = DateTime.Now;
                                desactivado = true;
                            }
                            else if (!request.DryRun)
                            {
                                // Basura por debajo del umbral: queda en el bucket para que lo vea
                                // un humano, pero la IA no lo desactiva sola.
                                tratamiento.RevisionLimpiezaMotivo =
                                    $"[confianza {confianza:0.00} < {UmbralDesactivacion:0.00}, no desactivado] {motivo}";
                                if (tratamiento.RevisionLimpiezaMotivo.Length > 400)
                                    tratamiento.RevisionLimpiezaMotivo = tratamiento.RevisionLimpiezaMotivo[..400];
                            }
                        }

                        await _db.SaveChangesAsync(cancellationToken);

                        // Enriquecimiento (descripción + nivel de relación) SOLO fuera de dry-run:
                        // el dry-run se limita al sello de clasificación, y describir cuesta otra
                        // llamada por registro.
                        var descripcionGenerada = false;
                        if (!request.DryRun
                            && estado == (byte)TriageLimpieza.Valido
                            && string.IsNullOrWhiteSpace(tratamiento.DescripcionIA))
                        {
                            try
                            {
                                var (descripcion, relacionEII, nombreTraducido) =
                                    await _aiService.GenerarDescripcionTratamientoAsync(tratamiento.nombre, cancellationToken);

                                if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
                                    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                                {
                                    tratamiento.NombreSugeridoIA = nombreTraducido;
                                }

                                tratamiento.DescripcionIA          = descripcion;
                                tratamiento.ValidadoIA             = true;
                                tratamiento.RelacionEII            = relacionEII;
                                tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                                tratamiento.Fuentes                = _aiService.UltimasFuentes;
                                tratamiento.FechaActualizacionIA   = DateTime.UtcNow;
                                tratamiento.fechaModificado        = DateTime.Now;

                                await _db.SaveChangesAsync(cancellationToken);
                                await PropagateToGlossaryTermAsync(tratamiento.id, cancellationToken);
                                descripcionGenerada = true;
                            }
                            catch (Exception exDesc)
                            {
                                // Que falle el enriquecimiento no invalida la clasificación ya guardada.
                                _logger.LogWarning(exDesc,
                                    "Triage OK pero falló la descripción de {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);
                            }
                        }

                        resultados.Add(new BatchReviewItem
                        {
                            Id                  = tratamiento.id,
                            Nombre              = tratamiento.nombre,
                            Estado              = estado,
                            Confianza           = confianza,
                            Motivo              = tratamiento.RevisionLimpiezaMotivo ?? motivo,
                            Nivel               = nivel?.ToString(),
                            Razonamiento        = razonamiento,
                            Desactivado         = desactivado,
                            DescripcionGenerada = descripcionGenerada,
                            Success             = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al clasificar tratamiento {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);
                        resultados.Add(new BatchReviewItem
                        {
                            Id      = tratamiento.id,
                            Nombre  = tratamiento.nombre ?? "Sin nombre",
                            Success = false,
                            Error   = ex.Message
                        });
                    }
                }

                // Pendientes del MISMO alcance que se procesó: si se pidió una rama, el contador
                // habla de esa rama (si no, la UI diría "faltan 9000" al terminar una rama chica).
                var totalPendientes = await _db.tratamientos
                    .Where(t => !t.Eliminado && t.RevisionLimpiezaEstado == null)
                    .Where(t => request.RaizId == null || t.id == request.RaizId || t.idPadre == request.RaizId)
                    .CountAsync(cancellationToken);

                return Ok(new
                {
                    ok           = true,
                    dryRun       = request.DryRun,
                    raizId       = request.RaizId,
                    umbral       = UmbralDesactivacion,
                    procesados   = resultados.Count,
                    validos      = resultados.Count(r => r.Success && r.Estado == (byte)TriageLimpieza.Valido),
                    basura       = resultados.Count(r => r.Success && r.Estado == (byte)TriageLimpieza.Basura),
                    dudosos      = resultados.Count(r => r.Success && r.Estado == (byte)TriageLimpieza.Dudoso),
                    protegidos   = resultados.Count(r => r.Protegido),
                    desactivados = resultados.Count(r => r.Desactivado),
                    fallidos     = resultados.Count(r => !r.Success),
                    pendientes   = totalPendientes,
                    resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el triage de limpieza de tratamientos");
                return StatusCode(500, new { ok = false, error = "Error en el triage: " + ex.Message });
            }
        }

        /// <summary>
        /// Categorías raíz con su avance de triage, para elegir qué rama revisar.
        /// GET /api/admin/tratamientos/ramas
        /// </summary>
        [HttpGet("ramas")]
        public async Task<IActionResult> GetRamas(CancellationToken cancellationToken)
        {
            // El árbol es de 2 niveles: la rama = la raíz + sus hijos directos.
            var ramas = await _db.tratamientos
                .Where(p => p.idPadre == null)
                .Select(p => new
                {
                    raizId    = p.id,
                    categoria = p.nombre ?? "(sin nombre)",
                    hijos     = _db.tratamientos.Count(h => h.idPadre == p.id),
                    sinRevisar = _db.tratamientos.Count(h =>
                        (h.idPadre == p.id || h.id == p.id) && h.RevisionLimpiezaEstado == null && !h.Eliminado),
                    basuraPendiente = _db.tratamientos.Count(h =>
                        (h.idPadre == p.id || h.id == p.id) && h.RevisionLimpiezaEstado == 2 && !h.Eliminado),
                    dudosos = _db.tratamientos.Count(h =>
                        (h.idPadre == p.id || h.id == p.id) && h.RevisionLimpiezaEstado == 3 && !h.Eliminado)
                })
                .OrderByDescending(r => r.sinRevisar)
                .ToListAsync(cancellationToken);

            return Ok(new { ok = true, ramas });
        }

        /// <summary>
        /// Resuelve qué registros marcados Basura se pueden desactivar y cuáles bloquea un guard.
        /// Fuente ÚNICA para el conteo previo y para la aplicación: si divergieran, la confirmación
        /// que ve el admin mentiría sobre lo que va a pasar.
        /// </summary>
        private async Task<(List<tratamientos> Aplicables, List<(tratamientos Reg, string Motivo)> Bloqueados)>
            ResolverBasuraAplicableAsync(int? raizId, CancellationToken cancellationToken)
        {
            var candidatos = await _db.tratamientos
                .Where(t => t.RevisionLimpiezaEstado == 2 && !t.Eliminado)
                .Where(t => raizId == null || t.id == raizId || t.idPadre == raizId)
                .OrderBy(t => t.id)
                .ToListAsync(cancellationToken);

            var ids = candidatos.Select(t => t.id).ToList();

            var conHijosActivos = (await _db.tratamientos
                .Where(h => h.idPadre != null && ids.Contains(h.idPadre!.Value) && !h.Eliminado)
                .Select(h => h.idPadre!.Value).Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var conUsuariosActivos = (await _db.tratamientoUsuario
                .Where(tu => tu.idTratamiento != null && ids.Contains(tu.idTratamiento!.Value) && !tu.Eliminado)
                .Select(tu => tu.idTratamiento!.Value).Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var aplicables = new List<tratamientos>();
            var bloqueados = new List<(tratamientos, string)>();

            foreach (var t in candidatos)
            {
                // Mismos guards que el batch-review. Defensa en profundidad: si algo quedó
                // marcado Basura pese a los guards de allá, aquí tampoco se desactiva.
                if (conHijosActivos.Contains(t.id))
                    bloqueados.Add((t, "Nodo padre con hijos activos — desactivarlo dejaría huérfanos."));
                else if (t.ValidadoHumano)
                    bloqueados.Add((t, "Validado por un humano."));
                else if (conUsuariosActivos.Contains(t.id))
                    bloqueados.Add((t, "Pacientes lo tienen registrado hoy."));
                else
                    aplicables.Add(t);
            }

            return (aplicables, bloqueados);
        }

        /// <summary>
        /// Cuántos se desactivarían — para la confirmación, ANTES de tocar nada.
        /// GET /api/admin/tratamientos/basura-preview?raizId=123
        /// </summary>
        [HttpGet("basura-preview")]
        public async Task<IActionResult> PreviewBasura(int? raizId, CancellationToken cancellationToken)
        {
            var (aplicables, bloqueados) = await ResolverBasuraAplicableAsync(raizId, cancellationToken);

            return Ok(new
            {
                ok           = true,
                raizId,
                aDesactivar  = aplicables.Count,
                bloqueados   = bloqueados.Count,
                total        = aplicables.Count + bloqueados.Count,
                muestra      = aplicables.Take(15).Select(t => new { t.id, nombre = t.nombre, motivo = t.RevisionLimpiezaMotivo }),
                bloqueadosDetalle = bloqueados.Select(b => new { b.Reg.id, nombre = b.Reg.nombre, motivo = b.Motivo })
            });
        }

        /// <summary>
        /// Aplica la desactivación en bloque a lo ya marcado Basura. SIN llamadas a la IA.
        /// POST /api/admin/tratamientos/batch-apply-basura
        /// </summary>
        /// <remarks>
        /// Este es el paso destructivo (equivale a DryRun=false). Reversible:
        /// <c>UPDATE dbo.tratamientos SET Eliminado = 0, fechaEliminado = '1900-01-01'
        /// WHERE RevisionLimpiezaEstado = 2 AND Eliminado = 1 [AND (id = @raiz OR idPadre = @raiz)]</c>
        /// </remarks>
        [HttpPost("batch-apply-basura")]
        public async Task<IActionResult> BatchApplyBasura([FromBody] BatchApplyBasuraRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var (aplicables, bloqueados) = await ResolverBasuraAplicableAsync(request.RaizId, cancellationToken);

                var ahora = DateTime.Now;
                foreach (var t in aplicables)
                {
                    t.Eliminado       = true;
                    t.fechaEliminado  = ahora.Date;
                    t.fechaModificado = ahora;
                }

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Aplicar Basura: {Desactivados} desactivados, {Bloqueados} bloqueados por guard. RaizId={RaizId}",
                    aplicables.Count, bloqueados.Count, request.RaizId?.ToString() ?? "(todo)");

                return Ok(new
                {
                    ok           = true,
                    raizId       = request.RaizId,
                    desactivados = aplicables.Count,
                    bloqueados   = bloqueados.Count,
                    detalle      = aplicables.Take(50).Select(t => new { t.id, nombre = t.nombre }),
                    bloqueadosDetalle = bloqueados.Select(b => new { b.Reg.id, nombre = b.Reg.nombre, motivo = b.Motivo })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar la desactivación de Basura");
                return StatusCode(500, new { ok = false, error = "Error al aplicar: " + ex.Message });
            }
        }

        /// <summary>
        /// Busca el GlossaryTerm vinculado al tratamiento y actualiza nivel + razonamiento de NINA.
        /// </summary>
        private async Task PropagateToGlossaryTermAsync(int tratamientoId, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .Include(l => l.GlossaryTerm)
                    .FirstOrDefaultAsync(l => l.TratamientoId == tratamientoId, cancellationToken);

                if (link?.GlossaryTerm == null)
                    return;

                link.GlossaryTerm.MedicalRelationSuggestedId = _aiService.UltimoNivelRelacion;
                link.GlossaryTerm.AiReasoning                = _aiService.UltimoRazonamiento;
                link.GlossaryTerm.CreatedByAI                = true;
                link.GlossaryTerm.FechaActualizacion         = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "GlossaryTerm {TermId} actualizado: nivel={Nivel}, razonamiento={Razonamiento}",
                    link.GlossaryTerm.Id,
                    _aiService.UltimoNivelRelacion,
                    _aiService.UltimoRazonamiento);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo propagar nivel/razonamiento al GlossaryTerm para tratamiento {Id}", tratamientoId);
            }
        }

        public class UpdateTratamientoRequest
        {
            public string Nombre { get; set; } = "";
            public int? IdPadre { get; set; }
            public int? IdIdioma { get; set; }
            public string? Icono { get; set; }
            public bool Eliminado { get; set; }
            public string? DescripcionIA { get; set; }
            public bool ValidadoHumano { get; set; }
        }

        public class BatchGenerateRequest
        {
            public int Skip { get; set; } = 0;
            public int Take { get; set; } = 10;
        }

        public class BatchReviewRequest
        {
            /// <summary>Cuántos registros sin revisar tomar. No hay Skip: reanuda solo.</summary>
            public int Take { get; set; } = 10;

            /// <summary>true = solo clasifica; NO desactiva a nadie ni genera descripciones.</summary>
            public bool DryRun { get; set; } = true;

            /// <summary>Categoría raíz a revisar (id con idPadre NULL). Null = todo el catálogo.</summary>
            public int? RaizId { get; set; }
        }

        public class BatchApplyBasuraRequest
        {
            /// <summary>Rama a la que se aplica. Null = todo el catálogo.</summary>
            public int? RaizId { get; set; }
        }

        public class BatchReviewItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            /// <summary>1 = Válido · 2 = Basura · 3 = Dudoso. 0 si falló.</summary>
            public byte Estado { get; set; }
            public double Confianza { get; set; }
            public string? Motivo { get; set; }
            public string? Nivel { get; set; }
            public string? Razonamiento { get; set; }
            /// <summary>Conservado sin consultar a la IA (validado por humano o con usuarios activos).</summary>
            public bool Protegido { get; set; }
            public bool Desactivado { get; set; }
            public bool DescripcionGenerada { get; set; }
            public bool Success { get; set; }
            public string? Error { get; set; }
        }

        public class BatchResultItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string? NombreSugeridoIA { get; set; }
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool RelacionEII { get; set; }
        }
    }
}
