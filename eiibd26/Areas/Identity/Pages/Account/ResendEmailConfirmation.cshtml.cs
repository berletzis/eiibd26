using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ResendEmailConfirmationModel> _logger;

        // Rate limit settings
        private const int RateLimitSeconds = 60; // allow one resend per minute (adjust if needed)
        private const string CacheKeyPrefix = "resend_email_";

        public ResendEmailConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IMemoryCache cache,
            ILogger<ResendEmailConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _cache = cache;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        // Expose to view
        [TempData]
        public string StatusMessage { get; set; }

        public bool EmailConfirmed { get; private set; } = false;

        // Rate limit indicators for UI
        public bool IsRateLimited { get; private set; } = false;
        public int RateLimitRemainingSeconds { get; private set; } = 0;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            // Prefill email for convenience
            Input = new InputModel { Email = user.Email };

            // If there is a cache entry indicate rate limit state
            var key = CacheKeyPrefix + user.Id;
            if (_cache.TryGetValue<DateTimeOffset>(key, out var expiresAt))
            {
                var now = DateTimeOffset.UtcNow;
                if (expiresAt > now)
                {
                    IsRateLimited = true;
                    RateLimitRemainingSeconds = (int)Math.Ceiling((expiresAt - now).TotalSeconds);
                }
                else
                {
                    // expired - remove
                    _cache.Remove(key);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Refresh confirmation status server-side to avoid client manipulation
            EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (EmailConfirmed)
            {
                StatusMessage = "Tu correo ya está confirmado. No es necesario reenviar el correo.";
                return RedirectToPage();
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                // show page with validation errors
                return Page();
            }

            // For extra safety require the requested email to match the current user email
            if (!string.Equals(Input.Email?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Don't reveal whether other emails exist; just show generic message
                ModelState.AddModelError(string.Empty, "El correo proporcionado no coincide con el correo de tu cuenta.");
                return Page();
            }

            // Rate-limiting: check cache keyed by user id
            var key = CacheKeyPrefix + user.Id;
            if (_cache.TryGetValue<DateTimeOffset>(key, out var expiresAt))
            {
                var now = DateTimeOffset.UtcNow;
                if (expiresAt > now)
                {
                    IsRateLimited = true;
                    RateLimitRemainingSeconds = (int)Math.Ceiling((expiresAt - now).TotalSeconds);
                    ModelState.AddModelError(string.Empty, $"Has solicitado un reenvío recientemente. Intenta de nuevo en {RateLimitRemainingSeconds} segundos.");
                    return Page();
                }
                else
                {
                    _cache.Remove(key);
                }
            }

            try
            {
                // Generate confirmation token and send email
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var codeBytes = Encoding.UTF8.GetBytes(code);
                var encodedCode = WebEncoders.Base64UrlEncode(codeBytes);

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = encodedCode },
                    protocol: Request.Scheme);

                // Send email (the IEmailSender implementation is used)
                var emailBody = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>Por favor confirma tu correo haciendo <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clic aquí</a>.</body></html>";
                await _emailSender.SendEmailAsync(user.Email, "Confirmación de correo", emailBody);

                // Set rate-limit marker in cache
                var expiration = DateTimeOffset.UtcNow.AddSeconds(RateLimitSeconds);
                _cache.Set<DateTimeOffset>(key, expiration, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiration
                });

                _logger.LogInformation("Se reenviará correo de confirmación a user {UserId}", user.Id);

                StatusMessage = "Se ha enviado un correo de confirmación. Revisa tu bandeja de entrada.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar correo de confirmación para user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al intentar reenviar el correo. Intenta más tarde.");
                return Page();
            }
        }
    }

    // NOTE: make sure your project has a reference to the ApplicationUser class used by your Identity setup.
    // If ApplicationUser is in another namespace adjust the using or fully qualify the type above.
}