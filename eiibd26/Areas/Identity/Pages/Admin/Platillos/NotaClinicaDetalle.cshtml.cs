using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models.Validacion;
using eiibd26.Services.Platillos;
using eiibd26.Services.Validacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    /// <summary>
    /// Edición de la nota clínica de un destino (grupo o ingrediente), en PÁGINA COMPLETA.
    /// Sustituye al panel lateral de F2a. Una sola página sirve a los dos tipos, igual que el
    /// partial que reemplaza: el servicio ya está llaveado por (TipoDestino, DestinoId).
    ///
    /// Las reglas NO viven aquí — siguen en PlatNotaAdminService (guardar deja en borrador,
    /// publicar exige ≥1 sección con contenido). Aquí solo hay handlers finos y el bloque de
    /// contexto: abrir una nota nunca debe ser un formulario en el vacío.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class NotaClinicaDetalleModel : PageModel
    {
        public const string TipoGrupo = "Grupo";
        public const string TipoIngrediente = "Ingrediente";
        public const string NotaTolerancia = "Tolerancia";
        public const string NotaPrecaucion = "Precaucion";

        private readonly ApplicationDbContext _db;
        private readonly IPlatNotaAdminService _notas;
        private readonly IValidacionContenidoService _validaciones;
        private readonly Configuration.AiAnswerConfiguration _aiConfig;

        public NotaClinicaDetalleModel(
            ApplicationDbContext db,
            IPlatNotaAdminService notas,
            IValidacionContenidoService validaciones,
            Microsoft.Extensions.Options.IOptions<Configuration.AiAnswerConfiguration> aiConfig)
        {
            _db = db;
            _notas = notas;
            _validaciones = validaciones;
            _aiConfig = aiConfig.Value;
        }

        /// <summary>Lista blanca de fuentes clínicas aprobadas (lectura en vivo desde config). Se pasa
        /// a la vista para MARCAR en ámbar las referencias manuales que no la matcheen — no bloquea,
        /// solo hace revisar. El candado de la IA vive en PlatillosAiService y no se toca.</summary>
        public IReadOnlyList<string> FuentesPermitidas => _aiConfig.FuentesClinicasPermitidas ?? new List<string>();

        public string Tipo { get; private set; } = "";
        public PlatNotaEditVm Nota { get; private set; } = new();

        /// <summary>Anexo 5: 'Tolerancia' (default) | 'Precaucion'. Viene por query string; la precaución
        /// solo es válida para un GRUPO de riesgo.</summary>
        [BindProperty(SupportsGet = true)] public string TipoNota { get; set; } = NotaTolerancia;
        public bool EsPrecaucion => TipoNota == NotaPrecaucion;
        /// <summary>Solo para precaución: el tipo de riesgo del grupo (null si el grupo no es de riesgo).</summary>
        public string? RiesgoTipo { get; private set; }

        private string TipoNotaNorm => TipoNota == NotaPrecaucion ? NotaPrecaucion : NotaTolerancia;

        // ---- Bloque de contexto ----
        public bool DestinoActivo { get; private set; }
        public string? DestinoSlug { get; private set; }
        /// <summary>Solo para Ingrediente: a qué grupo pertenece.</summary>
        public string? GrupoNombre { get; private set; }
        /// <summary>Solo para Ingrediente: sus atributos intrínsecos.</summary>
        public List<string> Atributos { get; private set; } = new();
        /// <summary>Solo para Grupo: su alcance — a qué ingredientes aplica esta nota.</summary>
        public List<IngredienteRef> IngredientesDelGrupo { get; private set; } = new();
        /// <summary>Validaciones médicas de la nota (0 si aún no existe fila).</summary>
        public int ValidacionesCount { get; private set; }

        public class IngredienteRef
        {
            public string Nombre { get; set; } = "";
            public bool Activo { get; set; }
            public string Slug { get; set; } = "";
        }

        // Binding del formulario de nota.
        [BindProperty] public int NotaDestinoId { get; set; }
        [BindProperty] public string? NotaTitulo { get; set; }
        [BindProperty] public List<PlatNotaSeccionInput> NotaSecciones { get; set; } = new();
        [BindProperty] public List<PlatNotaReferenciaInput> NotaReferencias { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public bool EsGrupo => Tipo == TipoGrupo;

        /// <summary>Página del listado a la que pertenece este destino.</summary>
        public string ListaPage => EsGrupo ? "Grupos" : "Ingredientes";

        private static bool TipoValido(string? tipo) => tipo == TipoGrupo || tipo == TipoIngrediente;

        public async Task<IActionResult> OnGetAsync(string? tipo, int destinoId)
        {
            if (!TipoValido(tipo)) return NotFound();
            Tipo = tipo;
            TipoNota = TipoNotaNorm;
            // La precaución (Anexo 5) es SIEMPRE de grupo: se escribe una vez y aplica a sus ingredientes.
            if (EsPrecaucion && Tipo != TipoGrupo) return NotFound();

            var cargado = await CargarContextoAsync(destinoId);
            if (!cargado)
            {
                ErrorMessage = EsGrupo ? "Grupo no encontrado." : "Ingrediente no encontrado.";
                return RedirectToPage(ListaPage);
            }
            return Page();
        }

        /// <summary>
        /// Arma el bloque de contexto + carga la nota. Devuelve false si el destino no existe.
        /// </summary>
        private async Task<bool> CargarContextoAsync(int destinoId)
        {
            NotaDestinoId = destinoId;

            if (EsGrupo)
            {
                var grupo = await _db.PlatGrupos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == destinoId);
                if (grupo == null) return false;

                DestinoActivo = grupo.Activo;
                RiesgoTipo = grupo.RiesgoTipo;
                Nota = await _notas.CargarAsync(TipoGrupo, grupo.Id, grupo.Nombre, TipoNotaNorm);

                // El alcance: una nota de "lácteos" aplica a queso, leche, yogur… El editor
                // tiene que verlo antes de escribir.
                IngredientesDelGrupo = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => i.GrupoId == grupo.Id)
                    .OrderBy(i => i.Nombre)
                    .Select(i => new IngredienteRef { Nombre = i.Nombre, Activo = i.Activo })
                    .ToListAsync();
                foreach (var i in IngredientesDelGrupo)
                    i.Slug = SlugHelper.GenerateSlug(i.Nombre);
            }
            else
            {
                var ing = await _db.PlatIngredientes.AsNoTracking()
                    .Include(i => i.Grupo)
                    .Include(i => i.Atributos).ThenInclude(a => a.Atributo)
                    .FirstOrDefaultAsync(i => i.Id == destinoId);
                if (ing == null) return false;

                DestinoActivo = ing.Activo;
                DestinoSlug = SlugHelper.GenerateSlug(ing.Nombre);
                GrupoNombre = ing.Grupo?.Nombre;
                Atributos = ing.Atributos
                    .Where(a => a.Atributo != null)
                    .Select(a => a.Atributo!.Nombre)
                    .OrderBy(n => n)
                    .ToList();

                Nota = await _notas.CargarAsync(TipoIngrediente, ing.Id, ing.Nombre);
            }

            // Conteo de validaciones médicas: reusa el servicio genérico de F2b. Sin fila de nota
            // todavía no hay nada que validar.
            if (Nota.NotaId.HasValue)
            {
                var vals = await _validaciones.ObtenerValidacionesPublicasAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, Nota.NotaId.Value);
                ValidacionesCount = vals.Count;
            }

            return true;
        }

        // ---- Handlers finos. Las reglas viven en PlatNotaAdminService. ----

        private Guid CurrentUserId() =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

        public async Task<IActionResult> OnPostGuardarNotaAsync(string? tipo, int destinoId)
        {
            if (!TipoValido(tipo)) return NotFound();
            if (EsPrecaucion && tipo != TipoGrupo) return NotFound();

            var (ok, msg) = await _notas.GuardarBorradorAsync(
                tipo, destinoId, NotaTitulo, NotaSecciones, NotaReferencias, TipoNotaNorm);
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { tipo, destinoId, tipoNota = TipoNotaNorm });
        }

        public async Task<IActionResult> OnPostPublicarNotaAsync(string? tipo, int destinoId)
        {
            if (!TipoValido(tipo)) return NotFound();
            if (EsPrecaucion && tipo != TipoGrupo) return NotFound();

            var (ok, msg) = await _notas.PublicarAsync(tipo, destinoId, CurrentUserId(), TipoNotaNorm);
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { tipo, destinoId, tipoNota = TipoNotaNorm });
        }

        public async Task<IActionResult> OnPostDespublicarNotaAsync(string? tipo, int destinoId)
        {
            if (!TipoValido(tipo)) return NotFound();
            if (EsPrecaucion && tipo != TipoGrupo) return NotFound();

            var (ok, msg) = await _notas.DespublicarAsync(tipo, destinoId, TipoNotaNorm);
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { tipo, destinoId, tipoNota = TipoNotaNorm });
        }
    }
}
