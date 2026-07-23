using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Models.Platillos;
using eiibd26.Models.Validacion;
using eiibd26.Services.Platillos;
using eiibd26.Services.Validacion;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Platillos
{
    // "¿Puedo comer queso?" — vista pública de ingrediente, ingrediente-primero.
    // Principio: evidencia, no veredicto. NUNCA dice sí/no. Ruta /Platillos/Ingrediente/{slug}.
    public class IngredienteModel : PageModel
    {
        // Roles que pueden validar una nota (señal de confianza; NO cambia la visibilidad al paciente).
        private static readonly string[] _validationRoles = ["Medico", "Administrador"];

        private readonly ApplicationDbContext _db;
        private readonly IPlatNotaClinicaService _notas;
        private readonly IValidacionContenidoService _validaciones;
        private readonly UserManager<ApplicationUser> _userManager;
        public IngredienteModel(
            ApplicationDbContext db,
            IPlatNotaClinicaService notas,
            IValidacionContenidoService validaciones,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _notas = notas;
            _validaciones = validaciones;
            _userManager = userManager;
        }

        public string Slug { get; private set; } = "";
        public int IngredienteId { get; private set; }
        public string Nombre { get; private set; } = "";
        public string GrupoNombre { get; private set; } = "";
        // Notas clínicas YA filtradas por el candado (null = no hay nada que mostrar). El candado
        // vive en PlatNotaClinicaService; esta vista NUNCA consulta la tabla directo.
        public PlatNotaVisibleDto? IngredienteNota { get; private set; }
        public PlatNotaVisibleDto? GrupoNota { get; private set; }
        // Anexo 5: nota de PRECAUCIÓN de seguridad del grupo (si el grupo es de riesgo y la nota está
        // publicada). Se pinta como callout ámbar aparte. Su leyenda depende de la validación médica.
        public PlatNotaVisibleDto? PrecaucionNota { get; private set; }
        public List<string> Atributos { get; private set; } = new();

        // ===== Validación médica (F2b) — señal de confianza, inline con cada nota.
        // NO cambia lo que ve el paciente; solo el flag Publicado del admin controla la visibilidad.
        public bool CanValidate { get; private set; }
        public ValidacionExistenteDto? MiValidIngrediente { get; private set; }
        public ValidacionExistenteDto? MiValidGrupo { get; private set; }
        public ValidacionExistenteDto? MiValidPrecaucion { get; private set; }
        public List<ValidacionPublicaDto> ValidadoresIngrediente { get; private set; } = new();
        public List<ValidacionPublicaDto> ValidadoresGrupo { get; private set; } = new();
        public List<ValidacionPublicaDto> ValidadoresPrecaucion { get; private set; } = new();

        [TempData] public string? ValidationMessage { get; set; }
        [TempData] public bool ValidationSuccess { get; set; }

        public bool ExcluidoPorTi { get; private set; }
        public string? MotivoExclusion { get; private set; }

        // ===== Tolerancia comunitaria (#16) — solo lectura; votar sigue viviendo en /tolero/{slug}.
        /// <summary>Resultado "Todos" del helper compartido: mismo número que /tolero. Null solo si falla la carga.</summary>
        public ToleranciaResultado? Tolerancia { get; private set; }
        /// <summary>Voto propio del usuario LOGUEADO (no se lee la cookie anónima aquí). Null = no votó / anónimo.</summary>
        public PlatToleraNivel? MiVotoTolerancia { get; private set; }

        public List<PlatilloMiniVm> Platillos { get; private set; } = new();
        public int PlatillosCount { get; private set; }
        public bool PlatillosHasMore { get; private set; }
        public int PlatillosNextOffset { get; private set; }
        private const int PlatillosPageSize = 5;

        public string MetaTitle { get; private set; } = "";
        public string MetaDescription { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            slug = slug.Trim().ToLowerInvariant();

            // Resolver el ingrediente por slug (catálogo chico y curado → match en memoria).
            var activos = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new { i.Id, i.Nombre, i.GrupoId })
                .ToListAsync();
            var ing = activos.FirstOrDefault(i => SlugHelper.GenerateSlug(i.Nombre) == slug);
            if (ing == null) return NotFound();

            Slug = slug;
            IngredienteId = ing.Id;
            Nombre = ing.Nombre;

            var grupo = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Id == ing.GrupoId)
                .Select(g => new { g.Nombre }).FirstOrDefaultAsync();
            GrupoNombre = grupo?.Nombre ?? "";

            // Notas clínicas: SIEMPRE por el servicio (candado). Devuelve null si no hay nada visible.
            IngredienteNota = await _notas.ObtenerNotaVisibleParaPacienteAsync("Ingrediente", ing.Id);
            GrupoNota = await _notas.ObtenerNotaVisibleParaPacienteAsync("Grupo", ing.GrupoId);
            // Precaución de seguridad: nota de GRUPO con TipoNota='Precaucion'. Aplica a todo el grupo.
            PrecaucionNota = await _notas.ObtenerNotaVisibleParaPacienteAsync("Grupo", ing.GrupoId, "Precaucion");

            // Atributos intrínsecos (cómo es siempre: gluten, lactosa, picante…).
            var atrIds = await _db.PlatIngredienteAtributos.AsNoTracking()
                .Where(a => a.IngredienteId == ing.Id)
                .Select(a => a.AtributoId).ToListAsync();
            if (atrIds.Any())
            {
                Atributos = await _db.PlatAtributos.AsNoTracking()
                    .Where(a => atrIds.Contains(a.Id))
                    .OrderBy(a => a.Nombre)
                    .Select(a => a.Nombre).ToListAsync();
            }

            // ¿El usuario autenticado lo tiene excluido? (directo, por grupo o por atributo).
            if ((User?.Identity?.IsAuthenticated ?? false)
                && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
            {
                var exclus = await _db.PlatPerfilExclusiones.AsNoTracking()
                    .Where(e => e.idUsuario == uid && !e.Eliminado)
                    .Select(e => new { e.Tipo, e.RefId }).ToListAsync();

                // "Tu experiencia": el voto propio (solo logueado; la cookie anónima no se lee en la ficha).
                MiVotoTolerancia = await _db.PlatTolerVotos.AsNoTracking()
                    .Where(v => v.IngredienteId == ing.Id && v.UserId == uid)
                    .Select(v => (PlatToleraNivel?)v.Tolera).FirstOrDefaultAsync();

                if (exclus.Any(e => e.Tipo == "Ingrediente" && e.RefId == ing.Id))
                {
                    ExcluidoPorTi = true;
                    MotivoExclusion = $"Marcaste que no toleras \"{Nombre}\".";
                }
                else if (exclus.Any(e => e.Tipo == "Grupo" && e.RefId == ing.GrupoId))
                {
                    ExcluidoPorTi = true;
                    MotivoExclusion = $"Tienes excluido el grupo \"{GrupoNombre}\", al que pertenece.";
                }
                else
                {
                    var atrExcl = exclus.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).ToHashSet();
                    if (atrExcl.Overlaps(atrIds))
                    {
                        ExcluidoPorTi = true;
                        MotivoExclusion = "Tienes excluida una característica de este alimento.";
                    }
                }
            }

            // Tolerancia comunitaria "Todos" — MISMO helper que /tolero, para no mostrar cifras distintas.
            Tolerancia = await ToleranciaResultadoCalculo.ParaIngredienteAsync(_db, ing.Id);

            // Platillos ACTIVOS que lo contienen — primera página (5); el resto por "Ver más" AJAX.
            var (platItems, platTotal) = await CargarPlatillosAsync(ing.Id, 0, PlatillosPageSize);
            Platillos = platItems;
            PlatillosCount = platTotal;
            PlatillosHasMore = platTotal > platItems.Count;
            PlatillosNextOffset = platItems.Count;

            // Meta: en el idioma del paciente, no el del catálogo.
            MetaTitle = $"¿Puedo comer {Nombre} con colitis o Crohn (EII)?";
            MetaDescription = "En la enfermedad inflamatoria intestinal no hay alimentos prohibidos por regla; "
                + "lo que cambia es cuánto los tolera cada persona. Consulta las notas clínicas, el contexto "
                + $"y los platillos con {Nombre}, y decídelo con tu médico.";

            await CargarValidacionAsync();

            return Page();
        }

        // Validación por nota visible. El sello (validadores públicos) lo ve cualquiera; la tarjeta de
        // validar y el pre-llenado solo si el usuario es Médico/Administrador. La nota que llega aquí YA
        // está publicada (el candado del servicio solo entrega notas visibles), así que validar aplica
        // únicamente a contenido live.
        private async Task CargarValidacionAsync()
        {
            if (IngredienteNota != null)
                ValidadoresIngrediente = await _validaciones.ObtenerValidacionesPublicasAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, IngredienteNota.Id);
            if (GrupoNota != null)
                ValidadoresGrupo = await _validaciones.ObtenerValidacionesPublicasAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, GrupoNota.Id);
            if (PrecaucionNota != null)
                ValidadoresPrecaucion = await _validaciones.ObtenerValidacionesPublicasAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, PrecaucionNota.Id);

            if (!(User?.Identity?.IsAuthenticated ?? false)) return;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;
            var roles = await _userManager.GetRolesAsync(user);
            CanValidate = roles.Any(r => _validationRoles.Contains(r));
            if (!CanValidate) return;

            var uid = user.Id.ToString();
            if (IngredienteNota != null)
                MiValidIngrediente = await _validaciones.ObtenerMiValidacionAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, IngredienteNota.Id, uid);
            if (GrupoNota != null)
                MiValidGrupo = await _validaciones.ObtenerMiValidacionAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, GrupoNota.Id, uid);
            if (PrecaucionNota != null)
                MiValidPrecaucion = await _validaciones.ObtenerMiValidacionAsync(
                    TipoContenidoValidado.NotaClinicaIngrediente, PrecaucionNota.Id, uid);
        }

        // POST: guardar/actualizar la validación de UNA nota (por su NotaClinicaId). Solo Médico/Admin.
        // Es una SEÑAL: no publica ni despublica. Redirige a la misma página del ingrediente (slug).
        public async Task<IActionResult> OnPostGuardarValidacionNotaAsync(int? contenidoId, string? slug, string? comentario)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false)) return Challenge();

            if (contenidoId == null || string.IsNullOrWhiteSpace(slug))
            {
                ValidationMessage = "Datos de la nota inválidos.";
                ValidationSuccess = false;
                return RedirectToPage(new { slug });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => _validationRoles.Contains(r)))
            {
                ValidationMessage = "No tienes permiso para validar. Se requiere rol Médico o Administrador.";
                ValidationSuccess = false;
                return RedirectToPage(new { slug });
            }

            var result = await _validaciones.GuardarValidacionAsync(
                TipoContenidoValidado.NotaClinicaIngrediente, contenidoId.Value, user.Id.ToString(), comentario);

            (ValidationSuccess, ValidationMessage) = result switch
            {
                UpsertResult.Creada      => (true,  "Validación registrada. Gracias por respaldar esta nota."),
                UpsertResult.Actualizada => (true,  "Comentario de validación actualizado."),
                UpsertResult.SinCambios  => (true,  "No hay cambios que guardar."),
                _                        => (false, "Error al guardar la validación. Intenta de nuevo.")
            };

            return RedirectToPage(new { slug });
        }

        // Carga una página de platillos ACTIVOS que contienen el ingrediente, como mini-VMs para el
        // sidebar. Mismo criterio que la vista pública (Activo, orden por Codigo). Reusado por el
        // render inicial y por el handler AJAX de "Ver más".
        private async Task<(List<PlatilloMiniVm> Items, int Total)> CargarPlatillosAsync(int ingredienteId, int skip, int take)
        {
            var platIds = await _db.PlatPlatilloIngredientes.AsNoTracking()
                .Where(pi => pi.IngredienteId == ingredienteId)
                .Select(pi => pi.PlatilloId).Distinct().ToListAsync();
            if (!platIds.Any()) return (new List<PlatilloMiniVm>(), 0);

            var baseQ = _db.PlatPlatillos.AsNoTracking()
                .Where(p => p.Activo && platIds.Contains(p.Id))
                .OrderBy(p => p.Codigo);

            var total = await baseQ.CountAsync();

            var pagina = await baseQ.Skip(skip).Take(take)
                .Select(p => new
                {
                    p.Nombre,
                    Categoria = _db.PlatCategorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault(),
                    NumIng = _db.PlatPlatilloIngredientes.Count(pi => pi.PlatilloId == p.Id)
                })
                .ToListAsync();

            var items = pagina.Select(p => new PlatilloMiniVm
            {
                Nombre = p.Nombre,
                Slug = SlugHelper.GenerateSlug(p.Nombre),
                Meta = !string.IsNullOrWhiteSpace(p.Categoria) ? p.Categoria! : $"{p.NumIng} ingredientes"
            }).ToList();

            return (items, total);
        }

        // "Ver más" AJAX (mismo patrón que Contenidos OnGetMasExternos): devuelve SOLO el partial de
        // filas + X-Has-More / X-Next-Offset en headers para que el JS sepa si dejar el botón.
        public async Task<IActionResult> OnGetMasPlatillosAsync(int ing, int offset)
        {
            var (items, total) = await CargarPlatillosAsync(ing, offset, PlatillosPageSize);
            var served = offset + items.Count;
            Response.Headers["X-Has-More"] = served < total ? "1" : "0";
            Response.Headers["X-Next-Offset"] = served.ToString();
            return Partial("_PlatilloMiniItems", items);
        }
    }

    // Mini-VM para las filas de "Platillos que lo incluyen" (sidebar): título + slug + meta chica.
    public class PlatilloMiniVm
    {
        public string Nombre { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Meta { get; set; } = "";
    }
}
