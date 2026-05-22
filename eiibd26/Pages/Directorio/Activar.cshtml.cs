using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using eiibd26.Models;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Directorio;

public class ActivarModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMedicoBadgeService _badgeService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ActivarModel> _logger;

    public ActivarModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMedicoBadgeService badgeService,
        IEmailSender emailSender,
        ILogger<ActivarModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _badgeService = badgeService;
        _emailSender = emailSender;
        _logger = logger;
    }

    // "invalid" | "expired" | "used" | "vinculado" | "login_requerido"
    public string Estado { get; set; } = "invalid";

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public MedicoReclamacionToken? TokenData { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) { Estado = "invalid"; return Page(); }

        TokenData = await _db.MedicosReclamacionToken
            .FirstOrDefaultAsync(t => t.Token == Token);

        if (TokenData is null || !TokenData.Activo) { Estado = "invalid"; return Page(); }
        if (TokenData.FechaUsado.HasValue)           { Estado = "used";    return Page(); }
        if (TokenData.FechaExpira < DateTime.UtcNow) { Estado = "expired"; return Page(); }

        // Si el usuario ya está autenticado con rol Medico: vincular y redirigir
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Medico"))
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await VincularAsync(TokenData, userId);
            return RedirectToPage("/Account/Manage/PerfilMedico", new { area = "Identity" });
        }

        Estado = "login_requerido";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) { Estado = "invalid"; return Page(); }

        TokenData = await _db.MedicosReclamacionToken
            .FirstOrDefaultAsync(t => t.Token == Token);

        if (TokenData is null || !TokenData.Activo) { Estado = "invalid"; return Page(); }
        if (TokenData.FechaUsado.HasValue)           { Estado = "used";    return Page(); }
        if (TokenData.FechaExpira < DateTime.UtcNow) { Estado = "expired"; return Page(); }

        if (!ModelState.IsValid) { Estado = "login_requerido"; return Page(); }

        var email = TokenData.EmailDestino;
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            Estado = "login_requerido";
            return Page();
        }

        try
        {
            var emailLocal = email.Split('@')[0];
            var perfil = new Perfil
            {
                idUser           = user.Id,
                Avatar           = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110",
                Titulo           = string.Empty,
                Nombre           = string.Empty,
                Apellidos        = string.Empty,
                FechaCreacion    = DateTime.UtcNow,
                UltimaActividad  = DateTime.UtcNow,
                FechaCreado      = DateTime.UtcNow,
                PermitirTelefonoReal    = true,
                PermitirCorreoNoticias  = true,
                PermitirMostrarPais     = true
            };
            _db.Perfil.Add(perfil);
            await _db.SaveChangesAsync();

            await _userManager.AddToRoleAsync(user, "Medico");
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, email));

            await VincularAsync(TokenData, user.Id);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToPage("/Account/Manage/PerfilMedico", new { area = "Identity" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vinculando perfil médico para {Email}", email);
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Error al vincular el perfil. Intenta de nuevo.");
            Estado = "login_requerido";
            return Page();
        }
    }

    private async Task VincularAsync(MedicoReclamacionToken tokenData, Guid userId)
    {
        var perfil = await _db.MedicosPerfilExtendido
            .FirstOrDefaultAsync(p => p.MedicoId == tokenData.MedicoId);

        if (perfil is null)
        {
            _db.MedicosPerfilExtendido.Add(new MedicoPerfilExtendido
            {
                MedicoId        = tokenData.MedicoId,
                UserId          = userId,
                FechaCreado     = DateTime.UtcNow,
                FechaModificado = DateTime.UtcNow
            });
        }
        else
        {
            perfil.UserId          = userId;
            perfil.FechaModificado = DateTime.UtcNow;
        }

        tokenData.FechaUsado = DateTime.UtcNow;
        tokenData.Activo     = false;
        tokenData.UserId     = userId;

        await _db.SaveChangesAsync();

        await _badgeService.OtorgarBadgeAsync(tokenData.MedicoId, "perfil_reclamado", "sistema");
        await _badgeService.EvaluarBadgesAutomaticosAsync(tokenData.MedicoId);
    }
}
