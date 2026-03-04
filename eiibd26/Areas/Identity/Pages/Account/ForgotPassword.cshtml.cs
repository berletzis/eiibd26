// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using eiibd26.Models;


namespace eiibd26.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
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
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // HTML email template profesional
                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f7fb;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f5f7fb; padding: 20px 0;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 12px; box-shadow: 0 2px 16px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">?? EIIBD</h1>
                            <p style=""color: #ffffff; margin: 10px 0 0 0; font-size: 16px;"">Comunidad EII</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #14253f; margin: 0 0 20px 0; font-size: 24px;"">Recuperación de Contraseña</h2>
                            <p style=""color: #555; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;"">Hola,</p>
                            <p style=""color: #555; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;"">
                                Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en EIIBD.
                            </p>
                            <p style=""color: #555; line-height: 1.6; margin: 0 0 30px 0; font-size: 16px;"">
                                Para establecer una nueva contraseña, haz clic en el siguiente botón:
                            </p>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 0 0 30px 0;"">
                                        <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" 
                                           style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;"">
                                            Restablecer Contraseña
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #555; line-height: 1.6; margin: 0 0 10px 0; font-size: 14px;"">
                                <strong>O copia y pega este enlace:</strong>
                            </p>
                            <p style=""color: #667eea; line-height: 1.6; margin: 0 0 30px 0; font-size: 12px; word-break: break-all; background-color: #f5f7fb; padding: 12px; border-radius: 6px;"">
                                {HtmlEncoder.Default.Encode(callbackUrl)}
                            </p>
                            <div style=""border-top: 2px solid #f0f0f0; padding-top: 20px; margin-top: 20px;"">
                                <p style=""color: #888; line-height: 1.6; margin: 0 0 10px 0; font-size: 13px;"">
                                    ?? <strong>Importante:</strong>
                                </p>
                                <ul style=""color: #888; line-height: 1.8; margin: 0; font-size: 13px; padding-left: 20px;"">
                                    <li>Este enlace expirará en 24 horas</li>
                                    <li>Si no solicitaste este cambio, ignora este correo</li>
                                    <li>Tu contraseña actual seguirá válida hasta que la cambies</li>
                                </ul>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f5f7fb; padding: 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                            <p style=""color: #888; margin: 0 0 10px 0; font-size: 13px;"">
                                ¿Necesitas ayuda? Responde este correo
                            </p>
                            <p style=""color: #aaa; margin: 0; font-size: 12px;"">
                                © 2024 EIIBD - Comunidad de apoyo para pacientes con EII
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "?? Restablece tu contraseña - EIIBD",
                    emailBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
