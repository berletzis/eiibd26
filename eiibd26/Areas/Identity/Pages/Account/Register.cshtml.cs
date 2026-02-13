using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace eiibd26.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender; // ✅ NUEVO

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext db,
            IEmailSender emailSender) // ✅ NUEVO
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
            _emailSender = emailSender; // ✅ NUEVO
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
            ReturnUrl = returnUrl ?? Url.Content("~/Usuario/UsuarioPerfil");
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

            // ✅ NUEVO: Generar token y enviar email de confirmación
            try
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                var emailBody = $@"
            <h2>Bienvenido a eiibd</h2>
            <p>Por favor confirma tu cuenta haciendo <a href='{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl)}'>clic aquí</a>.</p>
            <p>Si no creaste esta cuenta, ignora este mensaje.</p>
        ";

                await _emailSender.SendEmailAsync(Input.Email, "Confirma tu correo electrónico", emailBody);

                _logger.LogInformation("Email de confirmación enviado a {Email}", Input.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de confirmación a {Email}", Input.Email);
                // No interrumpimos el flujo, pero lo registramos
            }

            // Añadir claim de email (opcional)
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email));

            // Variable para guardar el slug generado
            string slugGenerado = null;

            // Crear perfil asociado con la información mínima solicitada
            try
            {
                Guid userGuid = user.Id;
                string codigoPais = Input.PaisCodigo;

                var emailLocal = (user.Email ?? "usuario").Split('@')[0];
                if (string.IsNullOrWhiteSpace(emailLocal))
                    emailLocal = "usuario";

                var avatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110";

                var perfil = new Perfil
                {
                    idUser = userGuid,
                    Avatar = avatarUrl,
                    Titulo = string.Empty,
                    Nombre = string.Empty,
                    Apellidos = string.Empty,
                    FechaDeNacimiento = null,
                    idZone = null,
                    FechaCreacion = DateTime.UtcNow,
                    UltimaActividad = DateTime.UtcNow,
                    slug = null,
                    Genero = null,
                    Latitud = "0",
                    Longitud = "0",
                    NombreCiudad = null,
                    NombrePais = codigoPais,
                    AceptoPP = null,
                    AcercaDe = null,
                    UsuarioModificacion = null,
                    UsuarioCreacion = null,
                    FechaModificado = null,
                    FechaCreado = DateTime.UtcNow,
                    PermitirTelefonoReal = true,
                    PermitirCorreoNoticias = true,
                    PermitirMostrarPais = true
                };

                try
                {
                    var slugCandidate = await GenerateUniqueSlugAsync(emailLocal);
                    perfil.slug = slugCandidate;
                    slugGenerado = slugCandidate;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo generar slug único automáticamente para {EmailLocal}. Dejo slug en null.", emailLocal);
                    perfil.slug = null;
                }

                _db.Perfil.Add(perfil);

                if (Input.CondicionPadreId.HasValue)
                {
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

                try
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo iniciar sesión automáticamente para {Email}", user.Email);
                }

                // ✅ NUEVO: Marcar que es un registro nuevo
                TempData["IsNewRegistration"] = true;

                // ✅ ACTUALIZADO: Redirigir SIEMPRE a Manage/Index después del registro
                return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando perfil o condicionesUsuario para el usuario {Email}", user.Email);
            }

            // Intentamos iniciar sesión
            try
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo iniciar sesión automáticamente para {Email}", user.Email);
            }

            // Redirigir
            if (!string.IsNullOrWhiteSpace(slugGenerado))
            {
                return Redirect($"/u/{slugGenerado}");
            }
            else
            {
                return RedirectToPage("/Usuario/Dashboard", new { area = "Identity" });
            }
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