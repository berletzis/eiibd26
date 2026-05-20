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
using Microsoft.AspNetCore.WebUtilities;

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

        public async Task OnGetAsync()
        {
            var returnUrl = Request.Query["returnUrl"].FirstOrDefault()
                         ?? Request.Query["ReturnUrl"].FirstOrDefault();

            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains('%'))
            {
                try { returnUrl = Uri.UnescapeDataString(returnUrl); } catch { returnUrl = null; }
            }

            ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl;

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

        public async Task<IActionResult> OnPostAsync()
        {
            var returnUrl = Request.Form["ReturnUrl"].FirstOrDefault();

            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains('%'))
            {
                try { returnUrl = Uri.UnescapeDataString(returnUrl); } catch { returnUrl = null; }
            }

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Content("~/");

            await PopulatePaisesAsync();
            await PopulateCondicionesPadreAsync();

            if (!ModelState.IsValid)
                return Page();

            // Fix #2: validar condición ANTES de crear el usuario
            if (Input.CondicionPadreId.HasValue)
            {
                var exists = await _db.condiciones.AnyAsync(c => c.id == Input.CondicionPadreId.Value && !c.Eliminado);
                if (!exists)
                {
                    ModelState.AddModelError("Input.CondicionPadreId", "La condición seleccionada no es válida.");
                    return Page();
                }
            }

            var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }

            // Fix #1: todo lo que sigue en un solo try — si falla, se elimina el usuario creado
            try
            {
                // Fix #5: NombrePais en minúsculas, consistente con Paises.PaisCodigo
                string codigoPais = Input.PaisCodigo.Trim().ToLowerInvariant();

                var paisData = await _db.Paises
                    .AsNoTracking()
                    .Where(p => p.PaisCodigo == codigoPais)
                    .Select(p => new { p.LatitudDefault, p.LongitudDefault })
                    .FirstOrDefaultAsync();

                string latDefault = paisData?.LatitudDefault.HasValue == true
                    ? paisData.LatitudDefault.Value.ToString(CultureInfo.InvariantCulture) : null;
                string lngDefault = paisData?.LongitudDefault.HasValue == true
                    ? paisData.LongitudDefault.Value.ToString(CultureInfo.InvariantCulture) : null;

                var emailLocal = (user.Email ?? "usuario").Split('@')[0];
                if (string.IsNullOrWhiteSpace(emailLocal)) emailLocal = "usuario";

                var perfil = new Perfil
                {
                    idUser = user.Id,
                    Avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110",
                    Titulo = string.Empty,
                    Nombre = string.Empty,
                    Apellidos = string.Empty,
                    FechaDeNacimiento = null,
                    idZone = null,
                    FechaCreacion = DateTime.UtcNow,
                    UltimaActividad = DateTime.UtcNow,
                    slug = null,
                    Genero = null,
                    Latitud = latDefault,
                    Longitud = lngDefault,
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
                    perfil.slug = await GenerateUniqueSlugAsync(emailLocal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo generar slug para {EmailLocal}.", emailLocal);
                }

                _db.Perfil.Add(perfil);
                _db.condicionUsuario.Add(new condicionUsuario
                {
                    idCondicion = Input.CondicionPadreId.Value,
                    idUsuario = user.Id,
                    fechaInicio = null,
                    fechaCreado = DateTime.UtcNow,
                    fechaModificado = DateTime.UtcNow,
                    Eliminado = false
                });

                await _db.SaveChangesAsync();

                // Fix #1: rol y claim DESPUÉS de SaveChangesAsync
                await _userManager.AddToRoleAsync(user, "Paciente");
                await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email));

                _logger.LogInformation("Usuario registrado: {Email}", user.Email);

                // Email de confirmación (no bloquea el flujo si falla)
                try
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl }, // Fix #3: variable local
                        protocol: Request.Scheme);

                    var emailBody = $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head>
<body>
    <h2>Bienvenido a eiibd</h2>
    <p>Por favor confirma tu cuenta haciendo <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clic aquí</a>.</p>
    <p>Si no creaste esta cuenta, ignora este mensaje.</p>
</body></html>";

                    await _emailSender.SendEmailAsync(Input.Email, "Confirma tu correo electrónico", emailBody);
                    _logger.LogInformation("Email de confirmación enviado a {Email}", Input.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email de confirmación a {Email}", Input.Email);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["IsNewRegistration"] = true;

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToPage("/Usuario/Dashboard", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                // Fix #1: revertir el usuario de Identity si algo falló
                _logger.LogError(ex, "Error completando registro para {Email}. Revirtiendo usuario.", user.Email);
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al completar el registro. Por favor intenta de nuevo.");
                return Page();
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