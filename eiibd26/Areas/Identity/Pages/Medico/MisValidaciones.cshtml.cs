using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Models.Directorio.Enums;
using eiibd26.Models.Glossary;
using eiibd26.Models.Validacion;
using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using eiibd26.Services.Platillos;
using eiibd26.Services.Validacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Medico
{
    /// <summary>
    /// Panel "Mis Validaciones" del profesional: el ABC para arrancar, un TOP de términos
    /// con el estado de su propia validación, y el historial de lo que ya escribió.
    /// Solo lectura — validar sigue ocurriendo en /Termino/{slug}.
    /// MedicoPendiente queda fuera a propósito: registrarse no habilita validar.
    /// </summary>
    [Authorize(Roles = "Medico,Administrador")]
    public class MisValidacionesModel : PageModel
    {
        private const int TopLimite = 10;

        /// <summary>Umbral de "fila flaca": por debajo de esto se ofrece además el otro dominio.
        /// El traslape nutrición↔clínica en EII es real, así que nadie queda sin qué validar.</summary>
        private const int MinimoAntesDeOfrecerElOtro = 5;

        private readonly IValidacionContenidoService _validaciones;
        private readonly IGlossaryService _glossary;
        private readonly IPlatNotaClinicaService _notas;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MisValidacionesModel> _logger;

        public MisValidacionesModel(
            IValidacionContenidoService validaciones,
            IGlossaryService glossary,
            IPlatNotaClinicaService notas,
            ApplicationDbContext db,
            ILogger<MisValidacionesModel> logger)
        {
            _validaciones = validaciones ?? throw new ArgumentNullException(nameof(validaciones));
            _glossary     = glossary     ?? throw new ArgumentNullException(nameof(glossary));
            _notas        = notas        ?? throw new ArgumentNullException(nameof(notas));
            _db           = db           ?? throw new ArgumentNullException(nameof(db));
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Estado de la validación del propio médico sobre un término.</summary>
        public enum EstadoIndicador
        {
            Pendiente  = 0,
            EnRevision = 1,
            Validado   = 2
        }

        public class TopTerminoVm
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string Slug { get; set; } = "";
            public GlossaryTermType Tipo { get; set; }
            public int Score { get; set; }
            public int UsuariosConElTermino { get; set; }

            public EstadoIndicador Contenido { get; set; } = EstadoIndicador.Pendiente;
            public EstadoIndicador Relacion { get; set; } = EstadoIndicador.Pendiente;

            public string Url => $"/Termino/{Slug}";

            public string TipoTexto => Tipo == GlossaryTermType.Sintoma ? "Síntoma"
                                     : Tipo == GlossaryTermType.Tratamiento ? "Tratamiento"
                                     : "Concepto EII";

            /// <summary>Cuántos de los dos ejes ya están validados (0-2). Ordena "lo que falta" primero.</summary>
            public int Completado =>
                (Contenido == EstadoIndicador.Validado ? 1 : 0) +
                (Relacion  == EstadoIndicador.Validado ? 1 : 0);
        }

        public List<TopTerminoVm> Top { get; set; } = new();
        public List<PlatNotaParaValidarDto> TopAlimentos { get; set; } = new();
        public List<ValidacionAdminDto> ValidacionesContenido { get; set; } = new();
        public List<GlossaryRelationValidationDto> ValidacionesRelacion { get; set; } = new();

        /// <summary>Tipo declarado en el perfil del usuario. Null = "general" → comportamiento clínico de siempre.</summary>
        public TipoProfesional? Tipo { get; private set; }

        public bool EsNutriologo => Tipo == TipoProfesional.Nutriologo;

        /// <summary>Qué bloque va primero. El otro solo aparece si el primero salió flaco.</summary>
        public bool AlimentosPrimero => EsNutriologo;

        /// <summary>True cuando el dominio principal no llenó la lista y se ofrece además el otro.
        /// Es guía, no muro: siempre hay algo que hacer.</summary>
        public bool MostrarElOtroDominio { get; private set; }

        public int TotalContenido => ValidacionesContenido.Count;
        public int TotalRelacion  => ValidacionesRelacion.Count;
        public int TotalGeneral   => TotalContenido + TotalRelacion;

        /// <summary>True cuando el profesional todavía no ha validado nada — cambia el copy de la intro.</summary>
        public bool EsPrimeraVez => TotalGeneral == 0;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            // ── Tipo de profesional: sale del perfil POR-USUARIO, no de la ficha ──
            // Así también aplica a profesionales sin ficha vinculada, que antes quedaban
            // siempre en "general". Falla suave a null ("general"): que no se pueda leer el
            // tipo nunca debe dejar al profesional sin panel.
            try
            {
                if (Guid.TryParse(userId, out var userGuid))
                {
                    Tipo = await _db.MedicosPerfilExtendido.AsNoTracking()
                        .Where(p => p.UserId == userGuid)
                        .Select(p => p.TipoProfesional)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MisValidaciones: fallo al resolver el tipo de profesional de {UserId}", userId);
            }

            // ── Historial de contenido (términos, artículos, perfiles, notas clínicas) ──
            try
            {
                ValidacionesContenido = await _validaciones.ObtenerValidacionesMedicoAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MisValidaciones: fallo al cargar validaciones de contenido de {UserId}", userId);
            }

            // ── Historial de relación (glosario) ──
            try
            {
                ValidacionesRelacion = await _glossary.ObtenerValidacionesRelacionMedicoAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MisValidaciones: fallo al cargar validaciones de relación de {UserId}", userId);
            }

            // ── Sets del propio médico, para marcar el TOP sin N+1 ──
            // Contenido: solo las de tipo Termino; ContenidoId == GlossaryTermId.
            // Si hay varias sobre el mismo término, gana el mejor estado (Validado > EnRevision).
            var estadoContenidoPorTermino = new Dictionary<int, EstadoIndicador>();
            foreach (var v in ValidacionesContenido.Where(v => v.TipoContenido == TipoContenidoValidado.Termino))
            {
                var estado = v.Estado == EstadoValidacion.Validado
                    ? EstadoIndicador.Validado
                    : EstadoIndicador.EnRevision;

                if (!estadoContenidoPorTermino.TryGetValue(v.ContenidoId, out var previo) || estado > previo)
                    estadoContenidoPorTermino[v.ContenidoId] = estado;
            }

            var estadoRelacionPorTermino = new Dictionary<int, EstadoIndicador>();
            foreach (var v in ValidacionesRelacion)
            {
                var estado = v.Aprobada ? EstadoIndicador.Validado : EstadoIndicador.EnRevision;

                if (!estadoRelacionPorTermino.TryGetValue(v.GlossaryTermId, out var previo) || estado > previo)
                    estadoRelacionPorTermino[v.GlossaryTermId] = estado;
            }

            // ── TOP según el tipo: cada quien su terreno PRIMERO, sin bloquear el otro ──
            // Nutriólogo → alimentos; médico EII / Otro / null → clínico.
            // Si el dominio principal sale flaco (< 5), se carga además el otro con un divisor:
            // guía, no muro. El traslape nutrición↔clínica en EII es real y valioso.
            if (EsNutriologo)
            {
                await CargarTopAlimentosAsync(userId, cancellationToken);
                if (TopAlimentos.Count < MinimoAntesDeOfrecerElOtro)
                {
                    await CargarTopClinicoAsync(estadoContenidoPorTermino, estadoRelacionPorTermino, cancellationToken);
                    MostrarElOtroDominio = Top.Count > 0;
                }
            }
            else
            {
                await CargarTopClinicoAsync(estadoContenidoPorTermino, estadoRelacionPorTermino, cancellationToken);
                if (Top.Count < MinimoAntesDeOfrecerElOtro)
                {
                    await CargarTopAlimentosAsync(userId, cancellationToken);
                    MostrarElOtroDominio = TopAlimentos.Count > 0;
                }
            }

            return Page();
        }

        /// <summary>Notas de alimentos sugeridas. El candado de publicación lo aplica el servicio.</summary>
        private async Task CargarTopAlimentosAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                TopAlimentos = await _notas.ObtenerNotasParaValidarAsync(userId, TopLimite, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MisValidaciones: fallo al cargar el TOP de alimentos");
            }
        }

        /// <summary>
        /// TOP clínico: síntomas + tratamientos, merge y re-ranking. El score se recalcula desde
        /// el DTO con la misma fórmula del servicio (3*directa + 2*indirecta + 1*secundaria +
        /// usuarios) para poder mezclar dos rankings independientes en una sola lista.
        /// </summary>
        private async Task CargarTopClinicoAsync(
            Dictionary<int, EstadoIndicador> estadoContenidoPorTermino,
            Dictionary<int, EstadoIndicador> estadoRelacionPorTermino,
            CancellationToken cancellationToken)
        {
            try
            {
                var sintomas = await _glossary.GetTopTermsByQualityAsync(
                    GlossaryTermType.Sintoma, TopLimite, cancellationToken);
                var tratamientos = await _glossary.GetTopTermsByQualityAsync(
                    GlossaryTermType.Tratamiento, TopLimite, cancellationToken);

                var candidatos = sintomas.Select(d => Proyectar(d, GlossaryTermType.Sintoma))
                    .Concat(tratamientos.Select(d => Proyectar(d, GlossaryTermType.Tratamiento)))
                    .ToList();

                foreach (var t in candidatos)
                {
                    if (estadoContenidoPorTermino.TryGetValue(t.Id, out var ec)) t.Contenido = ec;
                    if (estadoRelacionPorTermino.TryGetValue(t.Id, out var er))  t.Relacion  = er;
                }

                // 1) Se eligen los TOP por score (el ranking manda qué términos entran).
                // 2) Solo para PRESENTAR, se suben los que al médico le faltan: si ya validó
                //    los mejores, la lista seguiría siendo la misma pero arrancaría accionable.
                Top = candidatos
                    .OrderByDescending(t => t.Score)
                    .ThenBy(t => t.Nombre)
                    .Take(TopLimite)
                    .OrderBy(t => t.Completado)
                    .ThenByDescending(t => t.Score)
                    .ThenBy(t => t.Nombre)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MisValidaciones: fallo al cargar el TOP de términos");
            }
        }

        private static TopTerminoVm Proyectar(GlossaryTermSummaryDto d, GlossaryTermType tipo) => new()
        {
            Id     = d.Id,
            Nombre = d.Nombre,
            Slug   = d.Slug,
            Tipo   = tipo,
            UsuariosConElTermino = d.UserRelationCount,
            Score  = (d.RelationDirectCount * 3)
                   + (d.RelationIndirectCount * 2)
                   + (d.RelationSecondaryCount * 1)
                   + d.UserRelationCount
        };

        // ── Copy adaptado al tipo ────────────────────────────────────────────
        // Nombra el terreno del profesional sin sugerir que lo demás le esté vedado.

        /// <summary>Qué elige/valida, en el paso 1 del ABC.</summary>
        public string AbcQueElegir => EsNutriologo
            ? "un ingrediente o grupo de alimentos"
            : "un término";

        /// <summary>Cola del paso 3 del ABC — el "Agrega tu validación" va en negritas en la vista.</summary>
        public string AbcDetalleValidacion => EsNutriologo
            ? "de la nota, con tu criterio de nutrición clínica."
            : "— del contenido y/o del nivel de relación — con tu comentario clínico.";

        /// <summary>Título del bloque principal del TOP.</summary>
        public string TituloTopPrincipal => EsNutriologo
            ? "Alimentos que más impactan"
            : "Términos que más impactan";

        public string HintTopPrincipal => EsNutriologo
            ? "Notas de tolerancia y precaución publicadas, empezando por las que menos respaldo profesional tienen."
            : "Rankeados por relevancia clínica y por cuántos pacientes los tienen en su perfil.";

        /// <summary>Encabezado del bloque secundario cuando el principal salió flaco.</summary>
        public string TituloTopSecundario => EsNutriologo
            ? "Términos clínicos"
            : "Alimentos";

        public static string EstadoTexto(EstadoValidacion estado) => estado switch
        {
            EstadoValidacion.Validado   => "Validada",
            EstadoValidacion.EnRevision => "En revisión",
            EstadoValidacion.Oculto     => "Oculta",
            _                           => ""
        };

        public static string TipoContenidoTexto(TipoContenidoValidado tipo) => tipo switch
        {
            TipoContenidoValidado.Termino                => "Término del glosario",
            TipoContenidoValidado.Articulo               => "Artículo",
            TipoContenidoValidado.PerfilMedico           => "Perfil profesional",
            TipoContenidoValidado.NotaClinicaIngrediente => "Nota clínica",
            _                                            => "Contenido"
        };
    }
}
