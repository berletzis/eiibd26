using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _db;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public SelectList PaisesSelectList { get; set; }
        public SelectList CondicionesSelectList { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El correo electrónico es requerido.")]
            [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
            [Display(Name = "Correo electrónico")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es requerida.")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }

            // País (obligatorio)
            [Required(ErrorMessage = "Selecciona un país.")]
            [Display(Name = "País")]
            public string PaisCodigo { get; set; }

            // Condición padre (obligatoria)
            [Required(ErrorMessage = "Selecciona una condición.")]
            [Display(Name = "Condición (padre)")]
            public int? CondicionPadreId { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await PopulatePaisesAsync();
            await PopulateCondicionesPadreAsync();
        }

        private async Task PopulatePaisesAsync()
        {
            try
            {
                var paises = await _db.Paises
                    .Where(p => !p.Borrado && p.VIsibleBuscador)
                    .OrderBy(p => p.PaisNombre)
                    .ToListAsync();

                PaisesSelectList = new SelectList(paises, "PaisCodigo", "PaisNombre");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando países para registro.");
                PaisesSelectList = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private async Task PopulateCondicionesPadreAsync()
        {
            try
            {
                var padres = await _db.condiciones
                    .Where(c => c.idPadre == null && !c.Eliminado)
                    .OrderBy(c => c.nombre)
                    .ToListAsync();

                CondicionesSelectList = new SelectList(padres, "id", "nombre");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando condiciones padre para registro.");
                CondicionesSelectList = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            // Re-populate selects in case of validation failure
            await PopulatePaisesAsync();
            await PopulateCondicionesPadreAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Crear el usuario en Identity (email + password)
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return Page();
            }

            _logger.LogInformation("Usuario creado: {Email}", user.Email);

            // Añadir claim de email (opcional)
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email));

            // Crear perfil asociado con la información mínima solicitada
            try
            {
                // En tu ApplicationDbContext IdentityDbContext<..., Guid>, user.Id es Guid
                Guid userGuid = user.Id;

                // Guardar el código del país directamente en NombrePais (puedes cambiar a PaisCodigo si agregas la columna)
                string codigoPais = Input.PaisCodigo;

                // Rellenamos campos obligatorios del modelo Perfil con valores por defecto razonables
                var emailLocal = (user.Email ?? "usuario").Split('@')[0];
                if (string.IsNullOrWhiteSpace(emailLocal))
                    emailLocal = "usuario";

                var avatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110";

                var perfil = new Perfil
                {
                    idUser = userGuid,
                    Avatar = avatarUrl,                     // [Required]
                    Titulo = string.Empty,                  // [Required]
                    Activo = true,
                    Nombre = string.Empty,
                    Apellidos = string.Empty,
                    Telefono = string.Empty,                // [Required]
                    Email = user.Email,                     // [Required]
                    FechaDeNacimiento = null,
                    UsoPlataforma = "Paciente",             // [Required]
                    idZone = null,
                    FechaCreacion = DateTime.UtcNow,
                    UltimaActividad = DateTime.UtcNow,
                    // slug se generará justo a continuación
                    slug = null,
                    Genero = null,
                    Latitud = "0",                          // [Required]
                    Longitud = "0",                         // [Required]
                    NombreCiudad = null,
                    NombrePais = codigoPais,                // guarda la clave/código del país
                    AceptoPP = null,
                    UltimosEstudios = null,
                    ExperienciaLaboral = null,
                    UltimaCertificacion = null,
                    AcercaDe = null,
                    Extras = null,
                    UsuarioModificacion = null,
                    UsuarioCreacion = null,
                    FechaModificado = null,
                    FechaCreado = DateTime.UtcNow,
                    Eliminado = false,
                    PermitirTelefonoReal = true,
                    PermitirCorreoNoticias = true,
                    PermitirMostrarPais = true
                };

                // Generar slug inicial a partir del local-part del email y asegurar unicidad
                try
                {
                    var slugCandidate = await GenerateUniqueSlugAsync(emailLocal);
                    perfil.slug = slugCandidate;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo generar slug único automáticamente para {EmailLocal}. Dejo slug en null.", emailLocal);
                    perfil.slug = null;
                }

                _db.Perfil.Add(perfil);

                // Vincular la condición seleccionada al usuario (ajustado al modelo condicionUsuario)
                if (Input.CondicionPadreId.HasValue)
                {
                    // Agregar la condición seleccionada por el usuario
                    var condicionUsuarioPrincipal = new condicionUsuario
                    {
                        idCondicion = Input.CondicionPadreId.Value,
                        idUsuario = userGuid,
                        fechaInicio = null,
                        fechaCreado = DateTime.UtcNow,
                        fechaModificado = DateTime.UtcNow,
                        Eliminado = false
                    };
                    _db.condicionUsuario.Add(condicionUsuarioPrincipal);

                    // Reglas adicionales:
                    // - Si la condición seleccionada ES 1 -> además agregar condicionUsuario con idCondicion = 20
                    // - Si la condición seleccionada ES 7 -> además agregar condicionUsuario con idCondicion = 19
                    if (Input.CondicionPadreId.Value == 1)
                    {
                        var extra1 = new condicionUsuario
                        {
                            idCondicion = 20,
                            idUsuario = userGuid,
                            fechaInicio = null,
                            fechaCreado = DateTime.UtcNow,
                            fechaModificado = DateTime.UtcNow,
                            Eliminado = false
                        };
                        _db.condicionUsuario.Add(extra1);
                    }
                    else if (Input.CondicionPadreId.Value == 7)
                    {
                        var extra7 = new condicionUsuario
                        {
                            idCondicion = 19,
                            idUsuario = userGuid,
                            fechaInicio = null,
                            fechaCreado = DateTime.UtcNow,
                            fechaModificado = DateTime.UtcNow,
                            Eliminado = false
                        };
                        _db.condicionUsuario.Add(extra7);
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando perfil o condicionesUsuario para el usuario {Email}", user.Email);
                // No interrumpimos el flujo principal por este error.
            }

            // Intentamos iniciar sesión (ten en cuenta SignIn.RequireConfirmedAccount en Program.cs)
            try
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo iniciar sesión automáticamente para {Email}", user.Email);
            }

            // Redirigimos al usuario a su perfil para que complete los datos
            return RedirectToPage("/Usuario/UsuarioPerfil", new { area = "Identity" });
        }

        // Genera un slug "limpio" a partir de texto
        private string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.ToLowerInvariant().Trim();

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            text = sb.ToString().Normalize(NormalizationForm.FormC);

            // Reemplaza cualquier cosa que no sea a-z0-9 por '-'
            text = Regex.Replace(text, @"[^a-z0-9]+", "-");
            text = Regex.Replace(text, @"-+", "-").Trim('-');
            return text;
        }

        // Genera un slug único en la tabla Perfil (añade -1, -2, ... si es necesario)
        private async Task<string> GenerateUniqueSlugAsync(string baseText)
        {
            var baseSlug = Slugify(baseText);
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = "usuario";

            string candidate = baseSlug;
            int suffix = 0;

            // Buscar colisiones repetidamente
            while (await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == candidate))
            {
                suffix++;
                candidate = $"{baseSlug}-{suffix}";
                if (suffix > 10000) // safety guard
                {
                    _logger.LogWarning("Generación de slug alcanzó límite de intentos para base '{BaseSlug}'", baseSlug);
                    break;
                }
            }

            return candidate;
        }
    }
}