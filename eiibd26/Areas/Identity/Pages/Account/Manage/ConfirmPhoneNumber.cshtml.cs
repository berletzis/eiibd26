using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using eiibd26.Models;
using eiibd26.Services;

namespace eiibd26.Areas.Identity.Pages.Account.Manage
{
    public class ConfirmPhoneNumberModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<ConfirmPhoneNumberModel> _logger;
        private readonly IWebHostEnvironment _env;

        public ConfirmPhoneNumberModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ISmsSender smsSender,
            ILogger<ConfirmPhoneNumberModel> logger,
            IWebHostEnvironment env)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [Phone]
            [Display(Name = "Número de teléfono (E.164, p.ej. +521XXXXXXXXXX)")]
            public string PhoneNumber { get; set; }

            [Required]
            [Display(Name = "Código de verificación")]
            public string Code { get; set; }
        }

        [TempData]
        public string StatusMessage { get; set; }

        // Normalize phone to E.164-like: keep leading + then digits, or only digits.
        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            phone = phone.Trim();
            if (phone.StartsWith("+"))
                return "+" + Regex.Replace(phone.Substring(1), @"\D", "");
            return Regex.Replace(phone, @"\D", "");
        }

        // GET: prefill phone and/or code if provided in query
        public async Task<IActionResult> OnGetAsync(string userId = null, string phoneNumber = null, string code = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Usuario no encontrado.");

            // Normalize incoming phoneNumber for display so it's same string used for token generation
            Input.PhoneNumber = phoneNumber != null
                ? NormalizePhone(WebUtility.UrlDecode(phoneNumber))
                : NormalizePhone(await _userManager.GetPhoneNumberAsync(user));

            if (!string.IsNullOrEmpty(code))
            {
                try
                {
                    var decodedBytes = WebEncoders.Base64UrlDecode(code);
                    var decoded = Encoding.UTF8.GetString(decodedBytes);
                    Input.Code = decoded;
                }
                catch
                {
                    Input.Code = code;
                }
            }

            return Page();
        }

        // POST: submit phone + code to confirm
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Usuario no encontrado.");

            var rawPhone = Input.PhoneNumber?.Trim();
            var rawCode = Input.Code?.Trim();

            if (string.IsNullOrEmpty(rawPhone) || string.IsNullOrEmpty(rawCode))
            {
                ModelState.AddModelError(string.Empty, "Número o código vacíos.");
                return Page();
            }

            var phone = NormalizePhone(rawPhone);

            // Try to decode if code is Base64Url encoded; if fails, use raw code
            string tokenToUse = rawCode;
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(rawCode);
                var maybe = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrEmpty(maybe))
                {
                    tokenToUse = maybe;
                }
            }
            catch
            {
                tokenToUse = rawCode;
            }

            // DEBUG log: token submitted
            _logger.LogDebug("Phone confirmation attempted. userId={UserId} phone={Phone} tokenSubmitted={TokenSubmitted}",
                await _userManager.GetUserIdAsync(user), phone, tokenToUse);

            try
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, phone, tokenToUse);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    StatusMessage = "Gracias. Tu teléfono ha sido confirmado.";
                    _logger.LogInformation("User {UserId} confirmed phone number {PhoneNumber}", await _userManager.GetUserIdAsync(user), phone);
                    return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "No fue posible confirmar el teléfono. El código es inválido o expiró.");
                    _logger.LogWarning("ChangePhoneNumberAsync failed for user {UserId}. Errors: {Errors}", await _userManager.GetUserIdAsync(user), string.Join(";", result.Errors));
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing phone confirmation for user {UserId}", await _userManager.GetUserIdAsync(user));
                ModelState.AddModelError(string.Empty, "Error procesando el código. Intenta nuevamente.");
                return Page();
            }
        }

        // POST handler: generate & send the short token to the phone via ISmsSender
        // form uses asp-page-handler="SendCode"
        public async Task<IActionResult> OnPostSendCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(Input?.PhoneNumber))
            {
                ModelState.AddModelError(string.Empty, "Proporciona un número de teléfono válido para enviar el código.");
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Usuario no encontrado.");

            var phoneNormalized = NormalizePhone(Input.PhoneNumber);

            try
            {
                // IMPORTANT: generate token using the NORMALIZED phone
                var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNormalized);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No se pudo generar token de teléfono para user {UserId}", await _userManager.GetUserIdAsync(user));
                    ModelState.AddModelError(string.Empty, "No fue posible generar el token. Intenta nuevamente.");
                    return Page();
                }

                // Send the raw token via SMS so user can paste it
                var smsBody = $"Tu código de verificación para eiibd26 es: {token}";
                await _smsSender.SendSmsAsync(phoneNormalized, smsBody);

                // DEBUG: show token and callback (dev only)
                if (_env.IsDevelopment())
                {
                    TempData["DebugSmsCode"] = token;
                    var tokenBytes = Encoding.UTF8.GetBytes(token);
                    var codeEncoded = WebEncoders.Base64UrlEncode(tokenBytes);
                    var callbackUrl = Url.Page(
                        "/Account/Manage/ConfirmPhoneNumber",
                        pageHandler: null,
                        values: new { userId = await _userManager.GetUserIdAsync(user), phoneNumber = phoneNormalized, code = codeEncoded },
                        protocol: Request.Scheme);
                    TempData["DebugCallbackUrl"] = callbackUrl;
                }

                // DEBUG log: token generated
                _logger.LogDebug("Phone token generated for user {UserId} phone {Phone}: {Token}", await _userManager.GetUserIdAsync(user), phoneNormalized, token);

                StatusMessage = "Código enviado por SMS. Revisa tu teléfono e ingresa el código en el campo de verificación.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS token to {Phone} for user {UserId}", Input?.PhoneNumber, await _userManager.GetUserIdAsync(user));
                ModelState.AddModelError(string.Empty, "Ocurrió un error al enviar el SMS. Revisa configuración de SMS.");
                return Page();
            }
        }
    }
}