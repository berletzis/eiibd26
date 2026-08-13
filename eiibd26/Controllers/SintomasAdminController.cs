using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Glossary;
using eiibd26.Services.AI;
using eiibd26.Services.Glossary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/sintomas")]
    public class SintomasAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ISintomasTratamientosAiService _aiService;
        private readonly IReconocimientoEntidadService _gate;
        private readonly IGlossaryService _glossary;
        private readonly ILogger<SintomasAdminController> _logger;

        public SintomasAdminController(
            ApplicationDbContext db,
            ISintomasTratamientosAiService aiService,
            IReconocimientoEntidadService gate,
            IGlossaryService glossary,
            ILogger<SintomasAdminController> logger)
        {
            _db = db;
            _aiService = aiService;
            _gate = gate;
            _glossary = glossary;
            _logger = logger;
        }

        /// <summary>
        /// Corre el gate de reconocimiento sobre un síntoma y aplica los efectos de un veredicto
        /// negativo. Devuelve <c>true</c> solo si se puede llamar al generador.
        /// Espejo de <c>TratamientosAdminController.PasaGateAsync</c> — ver ahí el detalle del
        /// fail-safe asimétrico y de por qué el Tier 0 no puede producir NoReconocido.
        /// </summary>
        private async Task<bool> PasaGateAsync(
            sintomas sintoma,
            DecisionReconocimiento decision,
            CancellationToken cancellationToken)
        {
            if (decision.PermiteGenerar)
                return true;

            if (decision.EsSinVeredicto)
            {
                _logger.LogWarning(
                    "Gate SIN VEREDICTO para síntoma {Id} ('{Nombre}'): {Motivo}. No se escribe nada.",
                    sintoma.id, sintoma.nombre, decision.Motivo);
                return false;
            }

            var motivo = $"[gate] {decision.Motivo}";
            if (motivo.Length > 1000) motivo = motivo[..1000];

            sintoma.RevisionLimpiezaEstado    = (byte)TriageLimpieza.Dudoso;
            sintoma.RevisionLimpiezaConfianza = (decimal)decision.Confianza;
            sintoma.RevisionLimpiezaMotivo    = motivo;
            sintoma.RevisionLimpiezaFecha     = DateTime.UtcNow;

            if (decision.Resultado == ResultadoReconocimiento.NoReconocido)
            {
                sintoma.RelacionEII = false;
                sintoma.fechaModificado = DateTime.Now;
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (decision.Resultado == ResultadoReconocimiento.NoReconocido && _gate.DesactivaNoReconocidos)
            {
                await _glossary.SincronizarActivoPorSintomasAsync(
                    new[] { sintoma.id }, activo: false, cancellationToken);
            }

            _logger.LogWarning(
                "Gate {Resultado} ({Fuente}, conf {Confianza:0.00}) para síntoma {Id} ('{Nombre}'): {Motivo}",
                decision.Resultado, decision.Fuente, decision.Confianza,
                sintoma.id, sintoma.nombre, decision.Motivo);

            return false;
        }

        /// <summary>Entrada del gate a partir del registro ya cargado.</summary>
        private static EntradaReconocimiento AEntradaGate(sintomas s) =>
            new(s.id, s.nombre, s.ValidadoHumano, s.ValidadoIA, s.DescripcionIA);

        /// <summary>
        /// Genera descripción IA para un síntoma
        /// POST /api/admin/sintomas/{id}/generate-ia-description
        /// </summary>
        [HttpPost("{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateIaDescription(int id, CancellationToken cancellationToken)
        {
            try
            {
                var sintoma = await _db.sintomas
                    .FirstOrDefaultAsync(s => s.id == id && !s.Eliminado, cancellationToken);

                if (sintoma == null)
                    return NotFound(new { ok = false, error = "Síntoma no encontrado" });

                if (string.IsNullOrWhiteSpace(sintoma.nombre))
                    return BadRequest(new { ok = false, error = "El síntoma no tiene nombre" });

                // GATE de reconocimiento — ANTES de generar. Llamada separada a propósito: un
                // modelo al que ya se le pidió describir X tiene sesgo a completar la descripción.
                var decision = await _gate.EvaluarAsync(
                    TipoEntidadClinica.Sintoma, AEntradaGate(sintoma), cancellationToken);

                if (!await PasaGateAsync(sintoma, decision, cancellationToken))
                {
                    return Ok(new
                    {
                        ok        = false,
                        gate      = decision.Resultado.ToString(),
                        fuente    = decision.Fuente,
                        confianza = decision.Confianza,
                        error     = $"NINA no reconoce «{sintoma.nombre}» como un síntoma real, así que no generó la ficha. {decision.Motivo}"
                    });
                }

                _logger.LogInformation("Generando descripción IA para síntoma {Id}: {Nombre}", id, sintoma.nombre);

                var (descripcion, relacionEII, reconocido) = await _aiService.GenerarDescripcionSintomaAsync(
                    sintoma.nombre,
                    cancellationToken);

                // 2ª red: el propio generador se declaró incapaz. No se persiste NADA.
                if (!reconocido)
                {
                    var deGenerador = new DecisionReconocimiento(
                        ResultadoReconocimiento.NoReconocido, "generador", 1d,
                        "El generador declaró no reconocer el término.");
                    await PasaGateAsync(sintoma, deGenerador, cancellationToken);
                    return Ok(new
                    {
                        ok    = false,
                        gate  = deGenerador.Resultado.ToString(),
                        error = $"NINA no reconoce «{sintoma.nombre}»; no se guardó ninguna descripción."
                    });
                }

                // Actualizar el síntoma
                sintoma.DescripcionIA = descripcion;
                sintoma.ValidadoIA = true;
                sintoma.RelacionEII = relacionEII;
                sintoma.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                sintoma.Fuentes = _aiService.UltimasFuentes;
                sintoma.FechaActualizacionIA = DateTime.UtcNow;
                sintoma.fechaModificado = DateTime.Now;

                await _db.SaveChangesAsync(cancellationToken);

                // Propagar nivel de relación y razonamiento al GlossaryTerm vinculado
                await PropagateToGlossaryTermAsync(sintomaId: id, cancellationToken);

                _logger.LogInformation("Descripción IA guardada exitosamente para síntoma {Id}", id);

                return Ok(new
                {
                    ok = true,
                    descripcion,
                    relacionEII,
                    relacionEIITexto      = sintoma.RelacionEIIDescripcion,
                    nivelRelacion         = _aiService.UltimoNivelRelacion?.ToString(),
                    razonamiento          = _aiService.UltimoRazonamiento,
                    fuentes               = sintoma.Fuentes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para síntoma {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar la descripción: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un síntoma por ID
        /// GET /api/admin/sintomas/{id}
        /// </summary>
        // {id:int} — sin la restricción, esta plantilla compite con las rutas literales
        // ("ramas", "basura-preview"). La precedencia de literal sobre parámetro las salvaría,
        // pero el constraint lo vuelve explícito en vez de depender de la regla.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSintoma(int id, CancellationToken cancellationToken)
        {
            var sintoma = await _db.sintomas
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.id == id, cancellationToken);

            if (sintoma == null)
                return NotFound(new { ok = false, error = "Síntoma no encontrado" });

            return Ok(new 
            {
                ok = true,
                id = sintoma.id,
                nombre = sintoma.nombre ?? "",
                idPadre = sintoma.idPadre,
                idIdioma = sintoma.idIdioma,
                icono = sintoma.icono ?? "",
                eliminado = sintoma.Eliminado,
                descripcionIA = sintoma.DescripcionIA ?? "",
                validadoIA = sintoma.ValidadoIA,
                validadoHumano = sintoma.ValidadoHumano,
                relacionEII = sintoma.RelacionEII,
                relacionEIIDescripcion = sintoma.RelacionEIIDescripcion ?? "",
                fuentes = sintoma.Fuentes ?? ""
            });
        }

        /// <summary>
        /// Datos del GlossaryTerm vinculado al síntoma: niveles de relación y validaciones.
        /// GET /api/admin/sintomas/{id}/glossary
        /// </summary>
        [HttpGet("{id}/glossary")]
        public async Task<IActionResult> GetGlossaryData(int id, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .AsNoTracking()
                    .Where(l => l.SintomaId == id)
                    .Select(l => new { l.GlossaryTermId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (link == null)
                    return Ok(new { ok = true, hasGlossaryTerm = false });

                // Columnas nuevas — cargar con degradación elegante si aún no existen en BD
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

                // Historial de validaciones
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
                _logger.LogError(ex, "Error al obtener datos de glosario para síntoma {Id}", id);
                return Ok(new { ok = true, hasGlossaryTerm = false });
            }
        }

        /// <summary>
        /// Actualiza un síntoma
        /// PUT /api/admin/sintomas/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSintoma(int id, [FromBody] UpdateSintomaRequest request, CancellationToken cancellationToken)
        {
            var sintoma = await _db.sintomas
                .FirstOrDefaultAsync(s => s.id == id, cancellationToken);

            if (sintoma == null)
                return NotFound(new { ok = false, error = "Síntoma no encontrado" });

            sintoma.nombre = request.Nombre;
            sintoma.idPadre = request.IdPadre;
            sintoma.idIdioma = request.IdIdioma;
            sintoma.icono = request.Icono;
            sintoma.Eliminado = request.Eliminado;
            sintoma.DescripcionIA = request.DescripcionIA;
            sintoma.ValidadoHumano = request.ValidadoHumano;
            sintoma.TipoSintoma = Math.Clamp(request.TipoSintoma, 0, 2);
            sintoma.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);

            // Invariante: eliminado ⇒ término del glosario inactivo (y al revés al restaurar).
            await _glossary.SincronizarActivoPorSintomasAsync(
                new[] { sintoma.id }, activo: !request.Eliminado, cancellationToken);

            return Ok(new { ok = true });
        }

        /// <summary>
        /// Genera descripciones IA para múltiples síntomas
        /// POST /api/admin/sintomas/batch-generate-ia
        /// </summary>
        [HttpPost("batch-generate-ia")]
        public async Task<IActionResult> BatchGenerateIaDescriptions([FromBody] BatchGenerateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var resultados = new List<BatchResultItem>();

                // Obtener los siguientes N síntomas sin descripción IA (no eliminados)
                var baseQuery = _db.sintomas.Where(s => !s.Eliminado);

                if (request.Regenerar)
                {
                    // RE-PROCESO de fichas ya generadas. Ver el gemelo en TratamientosAdminController:
                    // preserva ValidadoHumano y las validaciones aprobadas del glosario (donde
                    // valida el médico desde /Termino/{slug}), y salta lo marcado Basura.
                    baseQuery = baseQuery
                        .Where(s => s.ValidadoIA && !string.IsNullOrEmpty(s.DescripcionIA))
                        .Where(s => !s.ValidadoHumano)
                        .Where(s => s.RevisionLimpiezaEstado == 1 || s.RevisionLimpiezaEstado == 3)
                        .Where(s => !_db.GlossaryTermMedicalLinks.Any(l =>
                            l.SintomaId == s.id &&
                            _db.GlossaryValidations.Any(v => v.GlossaryTermId == l.GlossaryTermId && v.Approved)));
                }
                else
                {
                    baseQuery = baseQuery.Where(s => string.IsNullOrEmpty(s.DescripcionIA) || !s.ValidadoIA);
                }

                var sintomas = await baseQuery
                    .OrderBy(s => s.id)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Procesando batch de {Count} síntomas. Skip: {Skip}, Regenerar: {Regenerar}",
                    sintomas.Count, request.Skip, request.Regenerar);

                var gateNoReconocidos = 0;
                var gateRevisionHumana = 0;
                var gateSinVeredicto = 0;

                foreach (var sintoma in sintomas)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(sintoma.nombre))
                        {
                            resultados.Add(new BatchResultItem
                            {
                                Id = sintoma.id,
                                Nombre = sintoma.nombre ?? "Sin nombre",
                                Success = false,
                                Error = "El síntoma no tiene nombre"
                            });
                            continue;
                        }

                        // GATE de reconocimiento — antes de gastar una llamada de generación.
                        var decision = await _gate.EvaluarAsync(
                            TipoEntidadClinica.Sintoma, AEntradaGate(sintoma), cancellationToken);

                        if (!await PasaGateAsync(sintoma, decision, cancellationToken))
                        {
                            switch (decision.Resultado)
                            {
                                case ResultadoReconocimiento.NoReconocido:   gateNoReconocidos++;  break;
                                case ResultadoReconocimiento.RevisionHumana: gateRevisionHumana++; break;
                                default:                                     gateSinVeredicto++;   break;
                            }

                            resultados.Add(new BatchResultItem
                            {
                                Id      = sintoma.id,
                                Nombre  = sintoma.nombre ?? "Sin nombre",
                                Success = false,
                                Gate    = decision.Resultado.ToString(),
                                Error   = $"Gate {decision.Resultado}: {decision.Motivo}"
                            });
                            continue;
                        }

                        _logger.LogInformation("Generando descripción IA para síntoma {Id}: {Nombre}", sintoma.id, sintoma.nombre);

                        var (descripcion, relacionEII, reconocido) = await _aiService.GenerarDescripcionSintomaAsync(
                            sintoma.nombre,
                            cancellationToken);

                        // 2ª red: el generador se declaró incapaz. No se persiste NADA.
                        if (!reconocido)
                        {
                            gateNoReconocidos++;
                            await PasaGateAsync(sintoma, new DecisionReconocimiento(
                                ResultadoReconocimiento.NoReconocido, "generador", 1d,
                                "El generador declaró no reconocer el término."), cancellationToken);

                            resultados.Add(new BatchResultItem
                            {
                                Id      = sintoma.id,
                                Nombre  = sintoma.nombre ?? "Sin nombre",
                                Success = false,
                                Gate    = ResultadoReconocimiento.NoReconocido.ToString(),
                                Error   = "El generador no reconoció el término — no se guardó descripción."
                            });
                            continue;
                        }

                        // Actualizar el síntoma
                        sintoma.DescripcionIA = descripcion;
                        sintoma.ValidadoIA = true;
                        sintoma.ValidadoHumano = false; // ⭐ Resetear validación humana
                        sintoma.RelacionEII = relacionEII;
                        sintoma.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                        sintoma.FechaActualizacionIA = DateTime.UtcNow;
                        sintoma.fechaModificado = DateTime.Now;

                        await _db.SaveChangesAsync(cancellationToken);

                        // Also propagate nivel + razonamiento for batch items
                        await PropagateToGlossaryTermAsync(sintomaId: sintoma.id, cancellationToken);

                        resultados.Add(new BatchResultItem
                        {
                            Id = sintoma.id,
                            Nombre = sintoma.nombre,
                            Success = true,
                            RelacionEII = relacionEII
                        });

                        _logger.LogInformation("Síntoma {Id} actualizado exitosamente", sintoma.id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar síntoma {Id}: {Nombre}", sintoma.id, sintoma.nombre);

                        resultados.Add(new BatchResultItem
                        {
                            Id = sintoma.id,
                            Nombre = sintoma.nombre ?? "Sin nombre",
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                // Cuántos faltan, en el MISMO alcance que se procesó.
                var totalEnAlcance = await baseQuery.CountAsync(cancellationToken);

                // En re-proceso el registro sigue cumpliendo el filtro después de regenerarse,
                // así que el contador se apoya en Skip para que la corrida pueda cerrar.
                var totalPendientes = request.Regenerar
                    ? Math.Max(0, totalEnAlcance - (request.Skip + resultados.Count))
                    : totalEnAlcance;

                if (gateNoReconocidos > 0 || gateSinVeredicto > 0)
                {
                    _logger.LogWarning(
                        "Gate en batch de síntomas: {NoReconocidos} no reconocidos, {RevisionHumana} a revisión humana, {SinVeredicto} sin veredicto (outage).",
                        gateNoReconocidos, gateRevisionHumana, gateSinVeredicto);
                }

                return Ok(new
                {
                    ok = true,
                    regenerar = request.Regenerar,
                    procesados = resultados.Count,
                    exitosos = resultados.Count(r => r.Success),
                    fallidos = resultados.Count(r => !r.Success),
                    gateNoReconocidos,
                    gateRevisionHumana,
                    gateSinVeredicto,
                    pendientes = totalPendientes,
                    resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento batch de síntomas");
                return StatusCode(500, new { ok = false, error = "Error en procesamiento: " + ex.Message });
            }
        }

        /// <summary>Confianza mínima para que la IA pueda desactivar por su cuenta.</summary>
        private const double UmbralDesactivacion = 0.85;

        /// <summary>
        /// Fallos seguidos de la IA que cortan el sub-lote. Un 4xx persistente (clave revocada,
        /// sin crédito) haría que TODO el catálogo se sellara como fallido, una llamada por
        /// registro. Cortar temprano deja el resto intacto para reanudar cuando se arregle.
        /// </summary>
        private const int FallosSeguidosParaAbortar = 3;

        /// <summary>
        /// Triage de limpieza con NINA — clasifica los siguientes N síntomas sin revisar.
        /// POST /api/admin/sintomas/batch-review
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

                var query = _db.sintomas
                    .Where(s => !s.Eliminado && s.RevisionLimpiezaEstado == null);

                // Enfocar por rama. El árbol es de 2 niveles (no hay nietos), así que la raíz
                // más sus hijos directos es la rama completa. Sin RaizId barre todo por id.
                if (request.RaizId.HasValue)
                {
                    var raiz = request.RaizId.Value;
                    query = query.Where(s => s.id == raiz || s.idPadre == raiz);
                }

                var pendientes = await query
                    .OrderBy(s => s.id)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Triage NINA (síntomas): {Count} registros, DryRun={DryRun}, RaizId={RaizId}",
                    pendientes.Count, request.DryRun, request.RaizId?.ToString() ?? "(todo)");

                // Protegidos: validados por un humano o con pacientes que los tienen registrados
                // hoy. No son basura por definición — no se les gasta una llamada a la IA.
                var idsLote = pendientes.Select(s => s.id).ToList();
                var conUsuariosActivos = await _db.sintomasUsuario
                    .Where(su => su.idSintoma != null
                              && idsLote.Contains(su.idSintoma!.Value)
                              && !su.Eliminado)
                    .Select(su => su.idSintoma!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var setUsuariosActivos = conUsuariosActivos.ToHashSet();

                // Nodos padre con hijos activos: son la estructura del catálogo, no registros
                // sueltos. Desactivar uno deja a sus hijos colgando. El borrado manual ya lo
                // impide (OnPostEliminarSintomaAsync); el lote debe respetar la misma regla.
                var conHijosActivos = await _db.sintomas
                    .Where(h => h.idPadre != null && idsLote.Contains(h.idPadre!.Value) && !h.Eliminado)
                    .Select(h => h.idPadre!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var setConHijosActivos = conHijosActivos.ToHashSet();

                var fallosSeguidos = 0;
                var abortadoPorFallos = false;

                foreach (var sintoma in pendientes)
                {
                    try
                    {
                        var protegidoPorHumano   = sintoma.ValidadoHumano;
                        var protegidoPorUsuarios = setUsuariosActivos.Contains(sintoma.id);

                        if (protegidoPorHumano || protegidoPorUsuarios)
                        {
                            var motivoProteccion = protegidoPorHumano
                                ? "Validado por un humano — se conserva sin consultar a la IA."
                                : "Pacientes lo tienen registrado hoy — se conserva sin consultar a la IA.";

                            sintoma.RevisionLimpiezaEstado    = (byte)TriageLimpieza.Valido;
                            sintoma.RevisionLimpiezaConfianza = 1m;
                            sintoma.RevisionLimpiezaMotivo    = motivoProteccion;
                            sintoma.RevisionLimpiezaFecha     = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);

                            resultados.Add(new BatchReviewItem
                            {
                                Id        = sintoma.id,
                                Nombre    = sintoma.nombre ?? "Sin nombre",
                                Estado    = (byte)TriageLimpieza.Valido,
                                Confianza = 1d,
                                Motivo    = motivoProteccion,
                                Protegido = true,
                                Success   = true
                            });
                            fallosSeguidos = 0;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(sintoma.nombre))
                        {
                            // Sin nombre no hay nada que clasificar, pero tampoco se desactiva solo.
                            sintoma.RevisionLimpiezaEstado = (byte)TriageLimpieza.Dudoso;
                            sintoma.RevisionLimpiezaMotivo = "El registro no tiene nombre — requiere revisión humana.";
                            sintoma.RevisionLimpiezaFecha  = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);

                            resultados.Add(new BatchReviewItem
                            {
                                Id      = sintoma.id,
                                Nombre  = "Sin nombre",
                                Estado  = (byte)TriageLimpieza.Dudoso,
                                Motivo  = "El registro no tiene nombre — requiere revisión humana.",
                                Success = true
                            });
                            fallosSeguidos = 0;
                            continue;
                        }

                        // La descripción autogenerada NO entra como contexto: es justo la que
                        // puede estar confabulada, y el triage la usaría para confirmarse a sí
                        // mismo. Solo se pasa si la escribió una persona. Misma regla que el gate.
                        var contextoConfiable = sintoma.ValidadoIA && !sintoma.ValidadoHumano
                            ? null
                            : sintoma.DescripcionIA;

                        var (estado, confianza, motivo, nivel, razonamiento) =
                            await _aiService.ClasificarSintomaAsync(
                                sintoma.nombre, contextoConfiable, cancellationToken);

                        sintoma.RevisionLimpiezaEstado    = estado;
                        sintoma.RevisionLimpiezaConfianza = (decimal)confianza;
                        sintoma.RevisionLimpiezaMotivo    = motivo;
                        sintoma.RevisionLimpiezaFecha     = DateTime.UtcNow;

                        var desactivado = false;

                        var esPadreConHijos = setConHijosActivos.Contains(sintoma.id);

                        if (estado == (byte)TriageLimpieza.Basura)
                        {
                            if (esPadreConHijos)
                            {
                                // Se conserva el veredicto para que el humano lo vea en el bucket,
                                // pero la IA NO puede desactivar un nodo del que cuelgan otros.
                                sintoma.RevisionLimpiezaMotivo =
                                    $"[nodo padre con hijos activos, no desactivado] {motivo}";
                                if (sintoma.RevisionLimpiezaMotivo.Length > 1000)
                                    sintoma.RevisionLimpiezaMotivo = sintoma.RevisionLimpiezaMotivo[..1000];
                            }
                            else if (!request.DryRun && confianza >= UmbralDesactivacion)
                            {
                                sintoma.Eliminado       = true;
                                sintoma.fechaEliminado  = DateTime.Now.Date;
                                sintoma.fechaModificado = DateTime.Now;
                                desactivado = true;
                            }
                            else if (!request.DryRun)
                            {
                                // Basura por debajo del umbral: queda en el bucket para que lo vea
                                // un humano, pero la IA no lo desactiva sola.
                                sintoma.RevisionLimpiezaMotivo =
                                    $"[confianza {confianza:0.00} < {UmbralDesactivacion:0.00}, no desactivado] {motivo}";
                                if (sintoma.RevisionLimpiezaMotivo.Length > 1000)
                                    sintoma.RevisionLimpiezaMotivo = sintoma.RevisionLimpiezaMotivo[..1000];
                            }
                        }

                        await _db.SaveChangesAsync(cancellationToken);

                        // Invariante: si la IA lo desactivó, su término sale del glosario.
                        if (desactivado)
                        {
                            await _glossary.SincronizarActivoPorSintomasAsync(
                                new[] { sintoma.id }, activo: false, cancellationToken);
                        }

                        // Enriquecimiento (descripción + nivel de relación) SOLO fuera de dry-run:
                        // el dry-run se limita al sello de clasificación, y describir cuesta otra
                        // llamada por registro.
                        var descripcionGenerada = false;
                        if (!request.DryRun
                            && estado == (byte)TriageLimpieza.Valido
                            && string.IsNullOrWhiteSpace(sintoma.DescripcionIA))
                        {
                            try
                            {
                                var (descripcion, relacionEII, reconocido) =
                                    await _aiService.GenerarDescripcionSintomaAsync(sintoma.nombre, cancellationToken);

                                // El triage acaba de decir Válido, pero si el generador no
                                // reconoce el término no se escribe ficha: el sello de triage ya
                                // guardado se respeta, simplemente no hay descripción que guardar.
                                if (!reconocido)
                                {
                                    _logger.LogWarning(
                                        "Triage Válido pero el generador NO reconoció {Id} ('{Nombre}') — sin descripción.",
                                        sintoma.id, sintoma.nombre);
                                }
                                else
                                {
                                    sintoma.DescripcionIA          = descripcion;
                                    sintoma.ValidadoIA             = true;
                                    sintoma.RelacionEII            = relacionEII;
                                    sintoma.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                                    sintoma.Fuentes                = _aiService.UltimasFuentes;
                                    sintoma.FechaActualizacionIA   = DateTime.UtcNow;
                                    sintoma.fechaModificado        = DateTime.Now;

                                    await _db.SaveChangesAsync(cancellationToken);
                                    await PropagateToGlossaryTermAsync(sintoma.id, cancellationToken);
                                    descripcionGenerada = true;
                                }
                            }
                            catch (Exception exDesc)
                            {
                                // Que falle el enriquecimiento no invalida la clasificación ya guardada.
                                _logger.LogWarning(exDesc,
                                    "Triage OK pero falló la descripción de {Id}: {Nombre}", sintoma.id, sintoma.nombre);
                            }
                        }

                        resultados.Add(new BatchReviewItem
                        {
                            Id                  = sintoma.id,
                            Nombre              = sintoma.nombre,
                            Estado              = estado,
                            Confianza           = confianza,
                            Motivo              = sintoma.RevisionLimpiezaMotivo ?? motivo,
                            Nivel               = nivel?.ToString(),
                            Razonamiento        = razonamiento,
                            Desactivado         = desactivado,
                            DescripcionGenerada = descripcionGenerada,
                            Success             = true
                        });
                        fallosSeguidos = 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al clasificar síntoma {Id}: {Nombre}", sintoma.id, sintoma.nombre);

                        // Circuit-breaker (parte 1): sellar el registro fallido como Dudoso para
                        // que la siguiente pasada NO vuelva a tomarlo. Sin el sello, un fallo
                        // persistente lo devuelve en cada sub-lote y la corrida gira en vacío
                        // quemando llamadas. Dudoso nunca se auto-desactiva, así que sellar es seguro.
                        var motivoFallo = $"[fallo IA] {ex.Message}";
                        if (motivoFallo.Length > 1000) motivoFallo = motivoFallo[..1000];
                        try
                        {
                            sintoma.RevisionLimpiezaEstado    = (byte)TriageLimpieza.Dudoso;
                            sintoma.RevisionLimpiezaConfianza = 0m;
                            sintoma.RevisionLimpiezaMotivo    = motivoFallo;
                            sintoma.RevisionLimpiezaFecha     = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                        catch (Exception exSello)
                        {
                            _logger.LogError(exSello, "Tampoco se pudo sellar el fallo del síntoma {Id}", sintoma.id);
                        }

                        resultados.Add(new BatchReviewItem
                        {
                            Id      = sintoma.id,
                            Nombre  = sintoma.nombre ?? "Sin nombre",
                            Success = false,
                            Error   = ex.Message
                        });

                        // Circuit-breaker (parte 2): varios fallos seguidos = la IA no está
                        // respondiendo. Cortar aquí y devolver lo hecho.
                        fallosSeguidos++;
                        if (fallosSeguidos >= FallosSeguidosParaAbortar)
                        {
                            abortadoPorFallos = true;
                            _logger.LogError(
                                "Triage de síntomas abortado: {Fallos} fallos seguidos de la IA. Se sellaron como Dudoso.",
                                fallosSeguidos);
                            break;
                        }
                    }
                }

                // Pendientes del MISMO alcance que se procesó: si se pidió una rama, el contador
                // habla de esa rama (si no, la UI diría "faltan miles" al terminar una rama chica).
                var totalPendientes = await _db.sintomas
                    .Where(s => !s.Eliminado && s.RevisionLimpiezaEstado == null)
                    .Where(s => request.RaizId == null || s.id == request.RaizId || s.idPadre == request.RaizId)
                    .CountAsync(cancellationToken);

                return Ok(new
                {
                    ok           = true,
                    dryRun       = request.DryRun,
                    raizId       = request.RaizId,
                    umbral       = UmbralDesactivacion,
                    abortadoPorFallos,
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
                _logger.LogError(ex, "Error en el triage de limpieza de síntomas");
                return StatusCode(500, new { ok = false, error = "Error en el triage: " + ex.Message });
            }
        }

        /// <summary>
        /// Categorías raíz con su avance de triage, para elegir qué rama revisar.
        /// GET /api/admin/sintomas/ramas
        /// </summary>
        [HttpGet("ramas")]
        public async Task<IActionResult> GetRamas(CancellationToken cancellationToken)
        {
            // El árbol es de 2 niveles: la rama = la raíz + sus hijos directos.
            var ramas = await _db.sintomas
                .Where(p => p.idPadre == null)
                .Select(p => new
                {
                    raizId    = p.id,
                    categoria = p.nombre ?? "(sin nombre)",
                    hijos     = _db.sintomas.Count(h => h.idPadre == p.id),
                    sinRevisar = _db.sintomas.Count(h =>
                        (h.idPadre == p.id || h.id == p.id) && h.RevisionLimpiezaEstado == null && !h.Eliminado),
                    basuraPendiente = _db.sintomas.Count(h =>
                        (h.idPadre == p.id || h.id == p.id) && h.RevisionLimpiezaEstado == 2 && !h.Eliminado),
                    dudosos = _db.sintomas.Count(h =>
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
        private async Task<(List<sintomas> Aplicables, List<(sintomas Reg, string Motivo)> Bloqueados)>
            ResolverBasuraAplicableAsync(int? raizId, CancellationToken cancellationToken)
        {
            var candidatos = await _db.sintomas
                .Where(s => s.RevisionLimpiezaEstado == 2 && !s.Eliminado)
                .Where(s => raizId == null || s.id == raizId || s.idPadre == raizId)
                .OrderBy(s => s.id)
                .ToListAsync(cancellationToken);

            var ids = candidatos.Select(s => s.id).ToList();

            var conHijosActivos = (await _db.sintomas
                .Where(h => h.idPadre != null && ids.Contains(h.idPadre!.Value) && !h.Eliminado)
                .Select(h => h.idPadre!.Value).Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var conUsuariosActivos = (await _db.sintomasUsuario
                .Where(su => su.idSintoma != null && ids.Contains(su.idSintoma!.Value) && !su.Eliminado)
                .Select(su => su.idSintoma!.Value).Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            var aplicables = new List<sintomas>();
            var bloqueados = new List<(sintomas, string)>();

            foreach (var s in candidatos)
            {
                // Mismos guards que el batch-review. Defensa en profundidad: si algo quedó
                // marcado Basura pese a los guards de allá, aquí tampoco se desactiva.
                if (conHijosActivos.Contains(s.id))
                    bloqueados.Add((s, "Nodo padre con hijos activos — desactivarlo dejaría huérfanos."));
                else if (s.ValidadoHumano)
                    bloqueados.Add((s, "Validado por un humano."));
                else if (conUsuariosActivos.Contains(s.id))
                    bloqueados.Add((s, "Pacientes lo tienen registrado hoy."));
                else if ((s.RevisionLimpiezaConfianza ?? 0m) < (decimal)UmbralDesactivacion)
                    bloqueados.Add((s, $"Confianza {(s.RevisionLimpiezaConfianza ?? 0m):0.00} por debajo del umbral {UmbralDesactivacion:0.00}."));
                else
                    aplicables.Add(s);
            }

            return (aplicables, bloqueados);
        }

        /// <summary>
        /// Cuántos se desactivarían — para la confirmación, ANTES de tocar nada.
        /// GET /api/admin/sintomas/basura-preview?raizId=123
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
                muestra      = aplicables.Take(15).Select(s => new { s.id, nombre = s.nombre, motivo = s.RevisionLimpiezaMotivo }),
                bloqueadosDetalle = bloqueados.Select(b => new { b.Reg.id, nombre = b.Reg.nombre, motivo = b.Motivo })
            });
        }

        /// <summary>
        /// Aplica la desactivación en bloque a lo ya marcado Basura. SIN llamadas a la IA.
        /// POST /api/admin/sintomas/batch-apply-basura
        /// </summary>
        /// <remarks>
        /// Este es el paso destructivo (equivale a DryRun=false). Reversible — ver
        /// <c>SQL/2026-08-12-glosario-sincronizar-activo-sintomas.sql</c>, bloque UNDO:
        /// hay que revertir las DOS tablas (<c>sintomas.Eliminado = 0</c> y
        /// <c>GlossaryTerm.Activo = 1</c>), si no el glosario queda desalineado del home.
        /// </remarks>
        [HttpPost("batch-apply-basura")]
        public async Task<IActionResult> BatchApplyBasura([FromBody] BatchApplyBasuraRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var (aplicables, bloqueados) = await ResolverBasuraAplicableAsync(request.RaizId, cancellationToken);

                var ahora = DateTime.Now;
                foreach (var s in aplicables)
                {
                    s.Eliminado       = true;
                    s.fechaEliminado  = ahora.Date;
                    s.fechaModificado = ahora;
                }

                await _db.SaveChangesAsync(cancellationToken);

                // Invariante: lo desactivado desaparece también del glosario, en bloque.
                var terminosDesactivados = await _glossary.SincronizarActivoPorSintomasAsync(
                    aplicables.Select(s => s.id).ToList(), activo: false, cancellationToken);

                _logger.LogInformation(
                    "Aplicar Basura (síntomas): {Desactivados} desactivados ({Terminos} términos del glosario), {Bloqueados} bloqueados por guard. RaizId={RaizId}",
                    aplicables.Count, terminosDesactivados, bloqueados.Count, request.RaizId?.ToString() ?? "(todo)");

                return Ok(new
                {
                    ok           = true,
                    raizId       = request.RaizId,
                    desactivados = aplicables.Count,
                    bloqueados   = bloqueados.Count,
                    detalle      = aplicables.Take(50).Select(s => new { s.id, nombre = s.nombre }),
                    bloqueadosDetalle = bloqueados.Select(b => new { b.Reg.id, nombre = b.Reg.nombre, motivo = b.Motivo })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar la desactivación de Basura en síntomas");
                return StatusCode(500, new { ok = false, error = "Error al aplicar: " + ex.Message });
            }
        }

        /// <summary>
        /// Busca el GlossaryTerm vinculado al síntoma y actualiza nivel + razonamiento de NINA.
        /// </summary>
        private async Task PropagateToGlossaryTermAsync(int sintomaId, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .Include(l => l.GlossaryTerm)
                    .FirstOrDefaultAsync(l => l.SintomaId == sintomaId, cancellationToken);

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
                _logger.LogWarning(ex, "No se pudo propagar nivel/razonamiento al GlossaryTerm para síntoma {Id}", sintomaId);
            }
        }

        public class UpdateSintomaRequest
        {
            public string Nombre { get; set; } = "";
            public int? IdPadre { get; set; }
            public int? IdIdioma { get; set; }
            public string? Icono { get; set; }
            public bool Eliminado { get; set; }
            public string? DescripcionIA { get; set; }
            public bool ValidadoHumano { get; set; }
            public int TipoSintoma { get; set; } = 0;
        }

        public class BatchGenerateRequest
        {
            public int Skip { get; set; } = 0;
            public int Take { get; set; } = 10;

            /// <summary>
            /// <c>true</c> = RE-PROCESO de fichas ya generadas (para rehacer lo confabulado).
            /// El filtro normal las excluye por tener <c>DescripcionIA</c> + <c>ValidadoIA</c>.
            /// Preserva lo validado por humanos y salta lo clasificado como Basura.
            /// </summary>
            public bool Regenerar { get; set; } = false;
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
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool RelacionEII { get; set; }
            /// <summary>Veredicto del gate cuando fue él quien impidió generar la ficha.</summary>
            public string? Gate { get; set; }
        }
    }
}
