using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Account.Manage
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ConfirmEmailChangeModel> _logger;

        public ConfirmEmailChangeModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<ConfirmEmailChangeModel> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [TempData]
        public string StatusMessage { get; set; }

        // GET handler: code parameter is Base64Url encoded; decode it before use
        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                StatusMessage = "Parámetros insuficientes para confirmar el correo.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Usuario no encontrado.";
                return Page();
            }

            // Antes de aplicar el cambio, volvemos a comprobar unicidad: ni el email ni el username (que en tu app es email) deben pertenecer a otra cuenta
            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null)
            {
                var existingId = await _userManager.GetUserIdAsync(existingByEmail);
                if (!string.Equals(existingId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "El correo indicado ya está en uso por otra cuenta.";
                    _logger.LogWarning("ConfirmEmailChange: email {Email} already in use by {ExistingId}", email, existingId);
                    return Page();
                }
            }

            var existingByName = await _userManager.FindByNameAsync(email);
            if (existingByName != null)
            {
                var existingId = await _userManager.GetUserIdAsync(existingByName);
                if (!string.Equals(existingId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "El correo indicado ya está en uso por otra cuenta (como nombre de usuario).";
                    _logger.LogWarning("ConfirmEmailChange: username {Email} already in use by {ExistingId}", email, existingId);
                    return Page();
                }
            }

            try
            {
                // Decodificar token Base64Url
                var decodedBytes = WebEncoders.Base64UrlDecode(code);
                var decodedCode = Encoding.UTF8.GetString(decodedBytes);

                var result = await _userManager.ChangeEmailAsync(user, email, decodedCode);
                if (!result.Succeeded)
                {
                    StatusMessage = "No fue posible cambiar el correo. El token es inválido o expiró.";
                    _logger.LogWarning("ChangeEmailAsync failed for user {UserId} with errors: {Errors}", userId, string.Join(";", result.Errors));
                    return Page();
                }

                // Intentar sincronizar UserName con el nuevo email (si esa es la política)
                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    // Aquí puede haber una condición de carrera: otro usuario pudo ocupar el username simultáneamente.
                    _logger.LogError("SetUserNameAsync failed for user {UserId} when setting username to {Email}. Errors: {Errors}", userId, email, string.Join(";", setUserNameResult.Errors));
                    StatusMessage = "El correo fue cambiado, pero no fue posible actualizar el nombre de usuario porque ya existe otra cuenta con ese valor. Contacta soporte.";
                    // refresh sign-in anyway so claims reflect the email change
                    await _signInManager.RefreshSignInAsync(user);
                    return Page();
                }

                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Gracias. Tu correo ha sido cambiado y confirmado.";
                _logger.LogInformation("User {UserId} confirmed email change to {Email}", userId, email);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding token for email change for user {UserId}", userId);
                StatusMessage = "Error procesando el token de confirmación.";
                return Page();
            }
        }
    }
}