using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Campanas
{
    [Authorize(Roles = "Administrador")]
    public class CampanasIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CampanasIndexModel> _logger;
        private readonly SendGridEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public CampanasIndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<CampanasIndexModel> logger,
            SendGridEmailSender emailSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public void OnGet() { }

        /// <summary>
        /// Returns the list of SendGrid templates configured in appsettings (SendGrid:Templates).
        /// </summary>
        public IActionResult OnGetTemplatesCorreo()
        {
            var templates = _configuration.GetSection("SendGrid:Templates")
                .Get<List<SendGridTemplateInfo>>()
                ?? new List<SendGridTemplateInfo>();
            return new JsonResult(templates);
        }

        public record CampanaEnviarPruebaInput(string Email, string TemplateId);

        /// <summary>
        /// Envía un correo de prueba con el template seleccionado al correo especificado.
        /// No registra en EmailCampanaLog ni afecta los contadores de elegibilidad.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostCampanaEnviarPruebaAsync([FromBody] CampanaEnviarPruebaInput input)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.Email))
                return new JsonResult(new { success = false, error = "Correo requerido." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(input.TemplateId) || input.TemplateId.Contains("AQUI"))
                return new JsonResult(new { success = false, error = "Selecciona un template válido." }) { StatusCode = 400 };

            var templates = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var template = templates.FirstOrDefault(t => t.Id == input.TemplateId);
            if (template is null)
                return new JsonResult(new { success = false, error = "Template no encontrado en configuración." }) { StatusCode = 400 };

            try
            {
                var templateData = new
                {
                    nombre = "Usuario de prueba",
                    correo = input.Email,
                    reset_link = Url.Page("/Account/ResetPassword", pageHandler: null,
                        values: new { area = "Identity", code = "TEST", email = input.Email },
                        protocol: Request.Scheme),
                    fase = template.Fase
                };

                await _emailSender.SendDynamicTemplateAsync(
                    input.Email,
                    templateId: template.Id,
                    templateData: templateData,
                    categories: new[] { "EIIBD", "Prueba" });

                _logger.LogInformation("Correo de prueba template '{Template}' enviado a {Email}.", template.Nombre, input.Email);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo de prueba template '{Template}' a {Email}.", template.Nombre, input.Email);
                return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = 500 };
            }
        }

        /// <summary>
        /// Resumen de cuántos usuarios están en cada fase de la campaña.
        /// </summary>
        public async Task<IActionResult> OnGetCampanaResumenAsync()
        {
            var totalUsuarios = await _userManager.Users.CountAsync();

            var conFase1 = await _db.EmailCampanaLogs
                .Where(l => l.Fase == 1 && l.Exito)
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();

            var conFase2 = await _db.EmailCampanaLogs
                .Where(l => l.Fase == 2 && l.Exito)
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();

            var conFase3 = await _db.EmailCampanaLogs
                .Where(l => l.Fase == 3 && l.Exito)
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();

            var elegiblesFase1 = totalUsuarios - conFase1;
            var elegiblesFase2 = conFase1 - conFase2;
            var elegiblesFase3 = conFase2 - conFase3;

            return new JsonResult(new
            {
                totalUsuarios,
                conFase1,
                conFase2,
                conFase3,
                elegiblesFase1 = Math.Max(0, elegiblesFase1),
                elegiblesFase2 = Math.Max(0, elegiblesFase2),
                elegiblesFase3 = Math.Max(0, elegiblesFase3),
                completados = conFase3
            });
        }

        public record CampanaEnviarInput(int Fase);

        /// <summary>
        /// Envía un batch de hasta 100 correos a usuarios elegibles de la fase indicada.
        /// Registra cada intento en EmailCampanaLog. Devuelve resultados individuales.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostCampanaEnviarBatchAsync([FromBody] CampanaEnviarInput input)
        {
            if (input is null || input.Fase < 1 || input.Fase > 3)
                return new JsonResult(new { success = false, error = "Fase inválida. Debe ser 1, 2 o 3." }) { StatusCode = 400 };

            var templates = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var template = templates.FirstOrDefault(t => t.Fase == input.Fase);
            if (template is null || string.IsNullOrWhiteSpace(template.Id) || template.Id.Contains("AQUI"))
                return new JsonResult(new { success = false, error = $"No hay template configurado para la fase {input.Fase}. Actualiza appsettings.json con el ID real de SendGrid." }) { StatusCode = 400 };

            var usuariosConFaseAnterior = input.Fase == 1
                ? new HashSet<Guid>(_userManager.Users.Select(u => u.Id))
                : await _db.EmailCampanaLogs
                    .Where(l => l.Fase == (input.Fase - 1) && l.Exito)
                    .Select(l => l.UserId)
                    .ToListAsync()
                    .ContinueWith(t => new HashSet<Guid>(t.Result));

            var yaRecibieronEstaFase = await _db.EmailCampanaLogs
                .Where(l => l.Fase == input.Fase && l.Exito)
                .Select(l => l.UserId)
                .ToListAsync()
                .ContinueWith(t => new HashSet<Guid>(t.Result));

            IQueryable<ApplicationUser> elegiblesQuery;
            if (input.Fase == 1)
            {
                var sinFase1 = await _db.EmailCampanaLogs
                    .Where(l => l.Fase == 1 && l.Exito)
                    .Select(l => l.UserId)
                    .ToListAsync();
                var sinFase1Set = new HashSet<Guid>(sinFase1);
                elegiblesQuery = _userManager.Users
                    .Where(u => !sinFase1Set.Contains(u.Id)
                                && u.EmailConfirmed
                                && u.Email != null && u.Email != "");
            }
            else
            {
                elegiblesQuery = _userManager.Users
                    .Where(u => usuariosConFaseAnterior.Contains(u.Id)
                                && !yaRecibieronEstaFase.Contains(u.Id)
                                && u.EmailConfirmed
                                && u.Email != null && u.Email != "");
            }

            var elegibles = await elegiblesQuery
                .OrderBy(u => u.Email)
                .Take(100)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            if (elegibles.Count == 0)
                return new JsonResult(new { success = true, procesados = 0, resultados = Array.Empty<object>(), mensaje = "No hay usuarios elegibles para esta fase." });

            var resultados = new List<object>();

            foreach (var u in elegibles)
            {
                var log = new EmailCampanaLog
                {
                    UserId = u.Id,
                    Fase = input.Fase,
                    TemplateId = template.Id,
                    FechaEnvio = DateTime.UtcNow,
                    Exito = false
                };

                try
                {
                    var user = await _userManager.FindByIdAsync(u.Id.ToString());
                    var perfil = await _db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == u.Id);

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var resetLink = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = encodedToken, email = u.Email },
                        protocol: Request.Scheme);

                    var templateData = new
                    {
                        nombre = perfil?.Nombre ?? u.UserName,
                        correo = u.Email,
                        reset_link = resetLink,
                        fase = input.Fase
                    };

                    await _emailSender.SendDynamicTemplateAsync(
                        u.Email,
                        templateId: template.Id,
                        templateData: templateData,
                        categories: new[] { "EIIBD", $"Campana-Fase{input.Fase}" });

                    log.Exito = true;
                    resultados.Add(new { email = u.Email, exito = true, error = (string)null });
                }
                catch (Exception ex)
                {
                    log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    resultados.Add(new { email = u.Email, exito = false, error = log.Error });
                    _logger.LogError(ex, "Error enviando campaña fase {Fase} a {Email}", input.Fase, u.Email);
                }
                finally
                {
                    _db.EmailCampanaLogs.Add(log);
                }
            }

            await _db.SaveChangesAsync();

            var exitosos = resultados.Count(r => (bool)r.GetType().GetProperty("exito").GetValue(r));
            _logger.LogInformation("Campaña fase {Fase}: {Exitosos}/{Total} enviados exitosamente.", input.Fase, exitosos, resultados.Count);

            return new JsonResult(new
            {
                success = true,
                procesados = resultados.Count,
                exitosos,
                fallidos = resultados.Count - exitosos,
                resultados
            });
        }
    }
}
