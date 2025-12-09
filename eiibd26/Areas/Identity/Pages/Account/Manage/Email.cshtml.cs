using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmailModel> _logger;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<EmailModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public string Username { get; set; }

        public string CurrentEmail { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Nuevo correo")]
            public string NewEmail { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Usuario no encontrado");

            CurrentEmail = await _userManager.GetEmailAsync(user);
            Username = await _userManager.GetUserNameAsync(user);

            return Page();
        }

        // Handler que actualiza el email (envía link de confirmación)
        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            if (!ModelState.IsValid)
            {
                var u = await _userManager.GetUserAsync(User);
                if (u != null)
                {
                    CurrentEmail = await _userManager.GetEmailAsync(u);
                    Username = await _userManager.GetUserNameAsync(u);
                }
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Usuario no encontrado.");

            CurrentEmail = await _userManager.GetEmailAsync(user);
            Username = await _userManager.GetUserNameAsync(user);

            var newEmail = Input.NewEmail?.Trim();
            if (string.Equals(newEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "El correo no ha cambiado.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(newEmail))
            {
                ModelState.AddModelError(string.Empty, "Debes indicar un correo válido.");
                return Page();
            }

            // Verificar si ya existe otro usuario con ese email o username (porque aquí UserName == Email)
            var existingByEmail = await _userManager.FindByEmailAsync(newEmail);
            if (existingByEmail != null)
            {
                var existingId = await _userManager.GetUserIdAsync(existingByEmail);
                var currentId = await _userManager.GetUserIdAsync(user);
                if (!string.Equals(existingId, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "El correo indicado ya está en uso por otra cuenta.");
                    return Page();
                }
            }

            var existingByName = await _userManager.FindByNameAsync(newEmail);
            if (existingByName != null)
            {
                var existingId = await _userManager.GetUserIdAsync(existingByName);
                var currentId = await _userManager.GetUserIdAsync(user);
                if (!string.Equals(existingId, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "El correo indicado ya está en uso por otra cuenta (como nombre de usuario).");
                    return Page();
                }
            }

            try
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                if (token == null)
                {
                    _logger.LogWarning("No se pudo generar token para cambio de email para user {UserId}", await _userManager.GetUserIdAsync(user));
                    ModelState.AddModelError(string.Empty, "No fue posible generar token de confirmación. Intenta nuevamente.");
                    return Page();
                }

                // Codificar token como Base64Url para que viaje seguro en URLs
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                string callbackUrl;
                try
                {
                    callbackUrl = Url.Page(
                        "/Account/Manage/ConfirmEmailChange",
                        pageHandler: null,
                        values: new { userId = await _userManager.GetUserIdAsync(user), email = newEmail, code = code },
                        protocol: Request.Scheme);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Url.Page falló generando callbackUrl; se intentará construir manualmente.");
                    var userId = await _userManager.GetUserIdAsync(user);
                    var encodedEmail = WebUtility.UrlEncode(newEmail ?? string.Empty);
                    var host = Request.Host.HasValue ? Request.Host.Value : "localhost";
                    var scheme = !string.IsNullOrEmpty(Request.Scheme) ? Request.Scheme : "https";
                    callbackUrl = $"{scheme}://{host}/Identity/Account/Manage/ConfirmEmailChange?userId={userId}&email={encodedEmail}&code={WebUtility.UrlEncode(code)}";
                }

                _logger.LogInformation("Email confirmation link generated: {CallbackUrl}", callbackUrl);

                var subject = "Confirmación de correo - eiibd26";
                var htmlMessage = $@"
                    <div style=""font-family:Segoe UI, Arial, sans-serif; line-height:1.4;"">
                        <p>Por favor confirma tu correo haciendo clic <a href=""{callbackUrl}"">aquí</a>.</p>
                        <hr style=""border:none;border-top:1px solid #e6e6e6;"" />
                        <p>Si no funciona el link, copia y pega la siguiente liga en tu navegador:</p>
                        <p style=""word-break:break-all; background:#f8f9fb; padding:8px; border-radius:4px;""><small>{callbackUrl}</small></p>
                    </div>";
                var plainTextMessage = $"Por favor confirma tu correo visitando: {callbackUrl}";

                try
                {
                    if (_emailSender is eiibd26.Services.SendGridEmailSender sgSender)
                    {
                        await sgSender.SendEmailAsyncWithCategories(newEmail, subject, htmlMessage, new[] { "email-confirmation" });
                    }
                    else
                    {
                        await _emailSender.SendEmailAsync(newEmail, subject, htmlMessage);
                    }
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Error sending confirmation email to {Email}", newEmail);
                    ModelState.AddModelError(string.Empty, "Error enviando el correo de confirmación. Intenta nuevamente más tarde.");
                    return Page();
                }

                StatusMessage = "Se ha enviado un correo de confirmación al nuevo email. Revisa tu bandeja.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnPostChangeEmailAsync para user {UserId}", await _userManager.GetUserIdAsync(user));
                ModelState.AddModelError(string.Empty, "Ocurrió un error al intentar enviar el correo de confirmación. Intenta nuevamente.");
                return Page();
            }
        }
    }
}