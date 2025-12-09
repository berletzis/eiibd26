using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                StatusMessage = "Parámetros insuficientes para confirmar el correo.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Usuario no encontrado.";
                _logger.LogWarning("ConfirmEmail: user not found: {UserId}", userId);
                return Page();
            }

            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(code);
                var decodedCode = Encoding.UTF8.GetString(decodedBytes);

                var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
                if (result.Succeeded)
                {
                    StatusMessage = "Gracias. Tu correo ha sido confirmado.";
                    _logger.LogInformation("User {UserId} confirmed email.", userId);
                }
                else
                {
                    StatusMessage = "No fue posible confirmar el correo. El token es inválido o expiró.";
                    _logger.LogWarning("ConfirmEmail failed for user {UserId}. Errors: {Errors}", userId, string.Join(";", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding confirmation token for user {UserId}", userId);
                StatusMessage = "Error procesando el token de confirmación.";
            }

            return Page();
        }
    }
}