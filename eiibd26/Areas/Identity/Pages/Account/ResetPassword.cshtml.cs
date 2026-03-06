using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string Code { get; set; }

        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                _logger.LogWarning("⚠️ [ResetPassword] Token faltante en URL");
                ModelState.AddModelError(string.Empty, "Token de reseteo faltante.");
                return BadRequest("Se requiere un código para restablecer la contraseña.");
            }

            // ✅ NO decodificar aquí - el code viene correctamente codificado desde ForgotPassword
            Input = new InputModel
            {
                Code = code,
                Email = email ?? ""
            };

            _logger.LogInformation("📧 [ResetPassword] Página cargada");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("⚠️ [ResetPassword] ModelState inválido");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ [ResetPassword] Usuario no encontrado: {Email}", Input.Email);
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            try
            {
                // ✅ Decodificar el token SOLO aquí, una vez
                string decodedToken;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
                    _logger.LogInformation("🔓 [ResetPassword] Token decodificado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ResetPassword] Error al decodificar token");
                    ModelState.AddModelError(string.Empty, "El token de reseteo es inválido o está corrupto.");
                    return Page();
                }

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ [ResetPassword] Contraseña reseteada exitosamente para: {Email}", Input.Email);
                    return RedirectToPage("./ResetPasswordConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("⚠️ [ResetPassword] Error: {Code} - {Description}", error.Code, error.Description);

                    // Mensajes más amigables
                    if (error.Code == "InvalidToken")
                    {
                        ModelState.AddModelError(string.Empty, 
                            "El enlace de recuperación ha expirado o ya fue usado. Por favor, solicita uno nuevo.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ResetPassword] Excepción al resetear contraseña");
                ModelState.AddModelError(string.Empty, "Ocurrió un error al restablecer tu contraseña. Por favor, intenta de nuevo.");
            }

            return Page();
        }
    }
}
