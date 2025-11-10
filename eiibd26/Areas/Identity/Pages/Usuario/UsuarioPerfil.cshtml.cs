using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    public class UsuarioPerfilModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioPerfilModel> _logger;

        public UsuarioPerfilModel(ApplicationDbContext db, ILogger<UsuarioPerfilModel> logger)
        {
            _db = db;
            _logger = logger;
            PaisesLista = new List<Paises>();
        }

        [BindProperty]
        public Perfil Perfil { get; set; }

        public List<Paises> PaisesLista { get; set; }

        public IEnumerable<SelectListItem> GenerosList { get; set; }
        public SelectList PaisesSelectList { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id = null)
        {
            await PopulatePaisesAsync();

            if (id == null)
            {
                var u = GetUserIdGuid();
                if (u == null) return NotFound();
                id = u.Value;
            }

            Perfil = await _db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == id.Value);

            if (Perfil == null)
            {
                Perfil = new Perfil
                {
                    idUser = id.Value,
                    Avatar = $"https://ui-avatars.com/api/?name=Usuario+{id.Value.ToString().Substring(0, 6)}&size=110",
                    Titulo = string.Empty,
                    Nombre = string.Empty,
                    Telefono = string.Empty,
                    Email = string.Empty,
                    UsoPlataforma = string.Empty,
                    Latitud = string.Empty,
                    Longitud = string.Empty,
                    FechaCreacion = DateTime.UtcNow,
                    FechaCreado = DateTime.UtcNow
                };
            }

            CreateSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await PopulatePaisesAsync();

            var form = Request.Form;

            // Debug: dump form pairs when Debug enabled
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var pairs = form.Keys.Select(k =>
                {
                    var vals = form[k];
                    var joined = string.Join(",", vals);
                    return $"{k}=[{joined}]";
                });
                _logger.LogDebug("Form pairs: {pairs}", string.Join(" | ", pairs));
            }

            // Robust FormBool: checks all posted values for the given name and returns true if any indicate true.
            bool FormBool(string name)
            {
                if (form.TryGetValue(name, out StringValues values))
                {
                    foreach (var v in values)
                    {
                        if (string.IsNullOrEmpty(v)) continue;
                        var s = v.Trim().ToLowerInvariant();
                        if (s == "true" || s == "on" || s == "1") return true;
                        // Also handle cases like "false,true" or "false,true,false"
                        var tokens = s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var t in tokens)
                        {
                            var tt = t.Trim();
                            if (tt == "true" || tt == "on" || tt == "1") return true;
                        }
                    }
                }
                return false;
            }

            if (Perfil == null)
            {
                ErrorMessage = "Datos de perfil inválidos.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                CreateSelectLists();
                return Page();
            }

            // Ensure Avatar exists to avoid validation errors
            if (string.IsNullOrWhiteSpace(Perfil.Avatar))
            {
                Perfil.Avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(Perfil.Nombre ?? "Usuario")}&size=110";
                ModelState.Remove(nameof(Perfil.Avatar));
                ModelState.Remove("Perfil.Avatar");
            }

            // Remove navigation prop validation noise
            ModelState.Remove(nameof(Perfil.Usuario));
            ModelState.Remove("Perfil.Usuario");

            // Map booleans *explicitly* using the form names we set in the view ("Perfil.Propiedad")
            Perfil.PermitirTelefonoReal = FormBool("Perfil.PermitirTelefonoReal");
            Perfil.PermitirCorreoNoticias = FormBool("Perfil.PermitirCorreoNoticias");
            Perfil.PermitirMostrarPais = FormBool("Perfil.PermitirMostrarPais");

            Perfil.Activo = FormBool("Perfil.Activo");
            Perfil.AceptoPP = FormBool("Perfil.AceptoPP");
            Perfil.Eliminado = FormBool("Perfil.Eliminado");

            // Log mapped booleans for debugging
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Mapped booleans: PermitirTelefonoReal={p0}, PermitirCorreoNoticias={p1}, PermitirMostrarPais={p2}, Activo={p3}, AceptoPP={p4}, Eliminado={p5}",
                    Perfil.PermitirTelefonoReal, Perfil.PermitirCorreoNoticias, Perfil.PermitirMostrarPais, Perfil.Activo, Perfil.AceptoPP, Perfil.Eliminado);
            }

            // Ensure idUser present
            if (Perfil.idUser == Guid.Empty)
            {
                var current = GetUserIdGuid();
                if (current == null)
                {
                    ErrorMessage = "Usuario no autenticado.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    CreateSelectLists();
                    return Page();
                }
                Perfil.idUser = current.Value;
            }

            // City/lat/lng validation
            if (!string.IsNullOrWhiteSpace(Perfil.NombreCiudad) &&
                (string.IsNullOrWhiteSpace(Perfil.Latitud) || string.IsNullOrWhiteSpace(Perfil.Longitud)))
            {
                ModelState.AddModelError(nameof(Perfil.NombreCiudad), "Debes seleccionar una ciudad válida para obtener latitud/longitud.");
            }

            CreateSelectLists();

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Corrige los errores del formulario.";
                if (!ModelState.Any(ms => ms.Key == string.Empty))
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            try
            {
                var existing = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == Perfil.idUser);

                if (existing == null)
                {
                    Perfil.FechaCreado = DateTime.UtcNow;
                    Perfil.FechaCreacion = DateTime.UtcNow;
                    Perfil.UltimaActividad = DateTime.UtcNow;
                    Perfil.UsuarioCreacion = GetUserIdGuid();

                    _db.Perfil.Add(Perfil);
                    await _db.SaveChangesAsync();

                    SuccessMessage = "Actualizacion de datos Correcta";
                    return RedirectToPage(new { id = Perfil.idUser });
                }
                else
                {
                    // Update allowed fields (including booleans)
                    existing.Avatar = string.IsNullOrWhiteSpace(Perfil.Avatar) ? existing.Avatar : Perfil.Avatar;
                    existing.imagenFondo = Perfil.imagenFondo;
                    existing.Titulo = Perfil.Titulo;
                    existing.Activo = Perfil.Activo;
                    existing.Nombre = Perfil.Nombre;
                    existing.Apellidos = Perfil.Apellidos;
                    existing.Telefono = Perfil.Telefono;
                    existing.Email = Perfil.Email;
                    existing.FechaDeNacimiento = Perfil.FechaDeNacimiento;
                    existing.UsoPlataforma = Perfil.UsoPlataforma;
                    existing.idZone = Perfil.idZone;
                    existing.slug = Perfil.slug;
                    existing.Genero = Perfil.Genero;
                    existing.Latitud = Perfil.Latitud;
                    existing.Longitud = Perfil.Longitud;
                    existing.NombreCiudad = Perfil.NombreCiudad;
                    existing.NombrePais = Perfil.NombrePais;
                    existing.AceptoPP = Perfil.AceptoPP;
                    existing.UltimosEstudios = Perfil.UltimosEstudios;
                    existing.ExperienciaLaboral = Perfil.ExperienciaLaboral;
                    existing.UltimaCertificacion = Perfil.UltimaCertificacion;
                    existing.AcercaDe = Perfil.AcercaDe;
                    existing.Extras = Perfil.Extras;
                    existing.FechaModificado = DateTime.UtcNow;
                    existing.UsuarioModificacion = GetUserIdGuid();
                    existing.Eliminado = Perfil.Eliminado;

                    existing.PermitirTelefonoReal = Perfil.PermitirTelefonoReal;
                    existing.PermitirCorreoNoticias = Perfil.PermitirCorreoNoticias;
                    existing.PermitirMostrarPais = Perfil.PermitirMostrarPais;
                    existing.Activo = Perfil.Activo;
                    existing.AceptoPP = Perfil.AceptoPP;

                    _db.Perfil.Update(existing);
                    await _db.SaveChangesAsync();

                    SuccessMessage = "Actualizacion de datos Correcta";
                    return RedirectToPage(new { id = existing.idUser });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando perfil para {User}", Perfil?.idUser);
                ErrorMessage = "Ocurrió un error al guardar. Intenta más tarde.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }
        }

        private async Task PopulatePaisesAsync()
        {
            try
            {
                PaisesLista = await _db.Paises
                    .Where(p => !p.Borrado && p.VIsibleBuscador)
                    .OrderBy(p => p.PaisNombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando lista de países");
                PaisesLista = new List<Paises>();
            }
        }

        private void CreateSelectLists()
        {
            GenerosList = new List<SelectListItem>
            {
                new SelectListItem("Masculino","Masculino", Perfil?.Genero == "Masculino"),
                new SelectListItem("Femenino","Femenino", Perfil?.Genero == "Femenino")
            };

            PaisesSelectList = new SelectList(PaisesLista ?? new List<Paises>(), "PaisCodigo", "PaisNombre", Perfil?.NombrePais);
        }

        private Guid? GetUserIdGuid()
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;
            if (Guid.TryParse(userId, out var g)) return g;
            return null;
        }
    }
}