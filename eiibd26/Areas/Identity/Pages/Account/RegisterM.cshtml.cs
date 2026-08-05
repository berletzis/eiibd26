using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Directorio;
using eiibd26.Models.Medico;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Account;

public class RegisterMModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterMModel> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;

    public RegisterMModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterMModel> logger,
        ApplicationDbContext db,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _db = db;
        _emailSender = emailSender;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public SelectList PaisesSelectList { get; set; } = new(Enumerable.Empty<object>());
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un país.")]
        [Display(Name = "País")]
        public string PaisCodigo { get; set; } = string.Empty;

        /// <summary>Tipo estructurado: GUÍA qué se le sugiere validar primero, no es un permiso.
        /// Nullable — si no lo indica, la ficha queda "general" y ve el TOP clínico por defecto.</summary>
        [Display(Name = "Tipo de profesional")]
        public Models.Directorio.Enums.TipoProfesional? TipoProfesional { get; set; }

        [Required(ErrorMessage = "La especialidad es requerida.")]
        [MaxLength(200)]
        [Display(Name = "Especialidad")]
        public string Especialidad { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Cédula profesional")]
        public string? CedulaProfesional { get; set; }
    }

    public async Task OnGetAsync()
    {
        ReturnUrl = Request.Query["returnUrl"].FirstOrDefault() ?? "/";
        await PopulatePaisesAsync();
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
            _logger.LogError(ex, "Error cargando países para RegisterM.");
            PaisesSelectList = new SelectList(Enumerable.Empty<object>());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = Request.Form["ReturnUrl"].FirstOrDefault() ?? "/";
        await PopulatePaisesAsync();

        if (!ModelState.IsValid) return Page();

        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return Page();
        }

        try
        {
            string codigoPais = Input.PaisCodigo.Trim().ToLowerInvariant();
            var emailLocal = (user.Email ?? "medico").Split('@')[0];

            var perfil = new Perfil
            {
                idUser           = user.Id,
                Avatar           = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110",
                Titulo           = string.Empty,
                Nombre           = string.Empty,
                Apellidos        = string.Empty,
                FechaCreacion    = DateTime.UtcNow,
                UltimaActividad  = DateTime.UtcNow,
                NombrePais       = codigoPais,
                FechaCreado      = DateTime.UtcNow,
                PermitirTelefonoReal    = true,
                PermitirCorreoNoticias  = true,
                PermitirMostrarPais     = true
            };

            try { perfil.slug = await GenerateUniqueSlugAsync(emailLocal); }
            catch (Exception ex) { _logger.LogWarning(ex, "No se pudo generar slug para {Email}.", emailLocal); }

            _db.Perfil.Add(perfil);

            // Ficha del directorio, creada al registro pero NO pública y SIN reclamar: así el pendiente
            // aparece en el panel admin (con su especialidad/cédula declaradas) para poder aprobarlo,
            // sin depender de una propuesta/claim de paciente. NombreCompleto = placeholder (el correo):
            // el nombre REAL lo pone el admin al aprobar — el registrante no puede autoasignarse un nombre
            // que se muestre (el display exige badge verificado, que solo da el admin).
            var ficha = new MedicoDirectorio
            {
                NombreCompleto     = emailLocal,
                Especialidad       = string.IsNullOrWhiteSpace(Input.Especialidad) ? null : Input.Especialidad.Trim(),
                CedulaProfesional  = string.IsNullOrWhiteSpace(Input.CedulaProfesional) ? null : Input.CedulaProfesional.Trim(),
                NombrePais         = codigoPais,
                AspNetUserId       = user.Id,
                EstatusValidacion  = Models.Directorio.Enums.EstatusValidacionCedula.PendienteValidacion,
                EstatusReclamacion = Models.Directorio.Enums.EstatusReclamacion.NoReclamado,
                Activo             = false,
                VisiblePublicamente = false,
                FechaCreacion      = DateTimeOffset.UtcNow
            };
            _db.MedicosDirectorio.Add(ficha);
            await _db.SaveChangesAsync();   // obtener ficha.Id para vincular

            _db.MedicosPerfilExtendido.Add(new MedicoPerfilExtendido
            {
                MedicoId        = ficha.Id,   // vinculada desde el registro
                UserId          = user.Id,
                // El tipo declarado al registrarse vive en el perfil por-usuario, no en la ficha:
                // así lo conserva aunque después se le desvincule o se le rehaga la ficha.
                TipoProfesional = Input.TipoProfesional,
                FechaCreado     = DateTime.UtcNow,
                FechaModificado = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Registrarse ≠ poder validar. Se otorga "MedicoPendiente" (completa su perfil, pero NO
            // valida). El admin lo promueve a "Medico" al aprobarlo. El flujo token (Activar) sigue
            // dando "Medico" directo — esa vía ya trae validación de identidad por email.
            await _userManager.AddToRoleAsync(user, "MedicoPendiente");
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email!));

            _logger.LogInformation("Profesional de la salud registrado (pendiente de aprobación): {Email}", user.Email);

            try
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email,
                    "Confirma tu correo — EIIBD",
                    $"<p>Bienvenido al directorio médico de EIIBD.</p>" +
                    $"<p>Por favor <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>confirma tu cuenta</a>.</p>");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error enviando confirmación a {Email}.", user.Email); }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Account/Manage/PerfilMedico", new { area = "Identity" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completando registro médico para {Email}. Revirtiendo.", user.Email);
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al completar el registro. Intenta de nuevo.");
            return Page();
        }
    }

    private string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.ToLowerInvariant().Trim();
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        text = sb.ToString().Normalize(NormalizationForm.FormC);
        text = Regex.Replace(text, @"[^a-z0-9]+", "-");
        text = Regex.Replace(text, @"-+", "-").Trim('-');
        return text;
    }

    private async Task<string> GenerateUniqueSlugAsync(string baseText)
    {
        var baseSlug = string.IsNullOrWhiteSpace(Slugify(baseText)) ? "medico" : Slugify(baseText);
        string candidate = baseSlug;
        int suffix = 0;
        while (await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == candidate))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
            if (suffix > 10000) break;
        }
        return candidate;
    }
}
