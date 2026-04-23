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
        public string QueryString { get; set; }

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

        public async Task<IActionResult> OnGetAsync()
        {
            // DEBUG: volcar TODOS los query params que llegan
            var allParams = string.Join(" | ", Request.Query.Select(kv => $"{kv.Key}={kv.Value}"));
            ReturnUrl = "DEBUG_PARAMS:[" + allParams + "]  RAW_URL:[" + Request.QueryString + "]";
            // Exponer ReturnUrl y QueryString para la vista
            QueryString = Request.QueryString.HasValue ? Request.QueryString.Value : "";

            // Leer returnUrl del query string (case-insensitive)
            var returnUrl = Request.Query["returnUrl"].FirstOrDefault()
                         ?? Request.Query["ReturnUrl"].FirstOrDefault();

            // Normalizar si viene URL-encoded
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains('%'))
            {
                try { returnUrl = Uri.UnescapeDataString(returnUrl); } catch { returnUrl = null; }
            }

            ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl;

            // Si el usuario ya está autenticado, redirigir
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToPage("/Usuario/Dashboard", new { area = "Identity" });
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Leer ReturnUrl del form body (el hidden field lo envía con nombre "ReturnUrl")
            var returnUrl = Request.Form["ReturnUrl"].FirstOrDefault();

            // Normalizar si viene URL-encoded
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains('%'))
            {
                try { returnUrl = Uri.UnescapeDataString(returnUrl); } catch { returnUrl = null; }
            }

            // Rechazar si apunta a rutas de autenticación
            if (!string.IsNullOrEmpty(returnUrl))
            {
                var invalidPaths = new[] { "/logout", "/login", "/register", "/account/logout", "/account/login" };
                if (invalidPaths.Any(p => returnUrl.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    returnUrl = null;
            }

            _logger.LogDebug("OnPostAsync ReturnUrl='{ReturnUrl}'", returnUrl);

            if (ModelState.IsValid)
            {
                // Busca el usuario por correo
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // ✅ SEGURIDAD: Habilitar lockout para protección contra fuerza bruta
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {Email} inició sesión. ReturnUrl='{ReturnUrl}'", Input.Email, returnUrl);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);

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

        }
}