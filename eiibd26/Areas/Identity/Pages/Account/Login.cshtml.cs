// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using eiibd26.Models;


namespace eiibd26.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string ResetPasswordMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "¿Recordar mis datos?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ✅ CORREGIDO: Filtrar returnUrl no deseados
            if (!string.IsNullOrEmpty(returnUrl))
            {
                // Rechazar returnUrl si apunta a Logout, Login, Register, etc.
                var invalidPaths = new[] { "/logout", "/login", "/register", "/account/logout", "/account/login" };
                if (invalidPaths.Any(p => returnUrl.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    returnUrl = null;
                }
            }

            // Si no hay returnUrl válido, usar Dashboard por defecto
            returnUrl ??= Url.Content("~/Identity/Usuario/Dashboard");

            if (ModelState.IsValid)
            {
                // Busca el usuario por correo
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null && IsHashInvalid(user.PasswordHash))
                {
                    // Mensaje por seguridad
                    ResetPasswordMessage = "Por seguridad debes realizar el cambio de contraseña.";
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var tokenBytes = Encoding.UTF8.GetBytes(token);
                    var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);
                    return RedirectToPage("./ResetPassword", new { email = Input.Email, code = encodedToken });
                }

                // ✅ SEGURIDAD: Habilitar lockout para protección contra fuerza bruta
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {Email} inició sesión correctamente.", Input.Email);

                    // ✅ Validar que returnUrl sea local y seguro
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    // ✅ Por defecto, ir al Dashboard
                    return RedirectToPage("/Usuario/Dashboard", new { area = "Identity" });
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("La Cuenta ha sido Bloqueada.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Intento de Inicio de Sesión Incorrecto.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        // Lógica auxiliar para detectar hash inválido
        private bool IsHashInvalid(string passwordHash)
        {
            return string.IsNullOrEmpty(passwordHash)
                || passwordHash.Length < 50 // Los hashes Identity .NET normalmente tienen 60+
                || !passwordHash.StartsWith("AQAAAA"); // Prefijo típico de Identity
        }
    }
}