using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Campanas;
using eiibd26.Services;
using eiibd26.Services.Campanas;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano para enviar un batch de campaña a una audiencia.
    /// Replica el patrón de PushNotificationJob (encolado vía Hangfire), pero resuelve sus
    /// dependencias scoped dentro de un scope propio porque un job vive fuera del request.
    /// Soporta cualquier tamaño de batch (incluido "Todos") sin timeout de IIS.
    /// </summary>
    public class CampanaEnvioJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CampanaEnvioJob> _logger;

        public CampanaEnvioJob(
            IServiceScopeFactory scopeFactory,
            ILogger<CampanaEnvioJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Envía un batch de <paramref name="cantidad"/> correos a los elegibles de la audiencia,
        /// usando el template indicado. <paramref name="cantidad"/> = int.MaxValue significa "Todos".
        /// <paramref name="baseUrl"/> (ej. https://eiibd.com) reemplaza a Url.Page/Request, que no
        /// existen dentro de un job Hangfire — el reset-link se arma manualmente con esa base.
        /// Registra en EmailCampanaLog con el FaseLog correspondiente a la audiencia.
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task EnviarAudienciaBatchAsync(int audiencia, string templateId, int cantidad, string baseUrl)
        {
            if (!Enum.IsDefined(typeof(AudienciaCampana), audiencia))
            {
                _logger.LogWarning("[CampanaJob] Audiencia inválida: {Audiencia}. Abortando.", audiencia);
                return;
            }
            if (string.IsNullOrWhiteSpace(templateId))
            {
                _logger.LogWarning("[CampanaJob] TemplateId vacío. Abortando.");
                return;
            }

            var aud = (AudienciaCampana)audiencia;

            // Scope propio: el job vive fuera del request, así que DbContext/UserManager/etc.
            // (scoped) deben resolverse en un scope creado aquí, no inyectarse en el constructor.
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var emailSender = sp.GetRequiredService<SendGridEmailSender>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var audienciaSvc = sp.GetRequiredService<ICampanaAudienciaService>();

            var (eligiblesQuery, faseLog) = await audienciaSvc.BuildAudienciaQueryAsync(aud, templateId);

            // cantidad = int.MaxValue → "Todos" (Take no limita en la práctica).
            var elegibles = await eligiblesQuery
                .OrderBy(u => u.Email)
                .Take(cantidad)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            if (elegibles.Count == 0)
            {
                _logger.LogInformation("[CampanaJob] Sin elegibles para '{Audiencia}'. Nada que enviar.", aud);
                return;
            }

            var campanaCodigo = aud switch
            {
                AudienciaCampana.ViejosSinToque1 => "toque1",
                AudienciaCampana.Toque2          => "toque2",
                AudienciaCampana.Toque3          => "toque3",
                AudienciaCampana.SinCondicion       => "sin-condicion",
                AudienciaCampana.SinMood            => "sin-mood",
                AudienciaCampana.ConRespuestasSemana  => "con-respuestas",
                AudienciaCampana.DiagnosticoPendiente => "diagnostico-pendiente",
                AudienciaCampana.SinAvatar            => "sin-avatar",
                AudienciaCampana.CompletarFechaDiagnostico => "completar-fecha-diag",
                AudienciaCampana.SinMoodReciente      => "sin-mood-reciente",
                _                                     => "general"
            };

            // Base sin barra final para concatenar la ruta sin duplicar "/".
            var baseUrlLimpia = (baseUrl ?? string.Empty).TrimEnd('/');
            var unsubGroupId = configuration.GetValue<int?>("SendGrid:UnsubscribeGroupId");

            int exitosos = 0, fallidos = 0;

            foreach (var u in elegibles)
            {
                var log = new EmailCampanaLog
                {
                    UserId = u.Id,
                    Fase = faseLog,
                    TemplateId = templateId,
                    FechaEnvio = DateTime.UtcNow,
                    Exito = false
                };

                try
                {
                    var user = await userManager.FindByIdAsync(u.Id.ToString());
                    var perfil = await db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == u.Id);

                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    // Reset-link armado manualmente: equivale exacto a
                    // Url.Page("/Account/ResetPassword", null, new { area="Identity", code, email }, scheme).
                    // El área va en el path; code y email como query string URL-encoded (igual que Url.Page).
                    var resetLink = $"{baseUrlLimpia}/Identity/Account/ResetPassword" +
                                    $"?code={Uri.EscapeDataString(encodedToken)}" +
                                    $"&email={Uri.EscapeDataString(u.Email)}";

                    var templateData = new
                    {
                        nombre = perfil?.Nombre ?? u.UserName,
                        correo = u.Email,
                        reset_link = resetLink,
                        campana = campanaCodigo
                    };

                    await emailSender.SendDynamicTemplateAsync(
                        u.Email,
                        templateId: templateId,
                        templateData: templateData,
                        categories: new[] { "EIIBD", $"Campana-{campanaCodigo}" },
                        customArgs: new Dictionary<string, string>
                        {
                            ["userId"]     = u.Id.ToString(),
                            ["campana"]    = campanaCodigo,
                            ["fase"]       = faseLog.ToString(),
                            ["templateId"] = templateId,
                            ["envio_ts"]   = DateTime.UtcNow.ToString("O")
                        },
                        unsubscribeGroupId: unsubGroupId);

                    log.Exito = true;
                    exitosos++;
                }
                catch (Exception ex)
                {
                    log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    fallidos++;
                    _logger.LogError(ex, "[CampanaJob] Error enviando '{Audiencia}' a {Email}", aud, u.Email);
                }
                finally
                {
                    db.EmailCampanaLogs.Add(log);
                }
            }

            await db.SaveChangesAsync();

            _logger.LogInformation(
                "[CampanaJob] Audiencia '{Audiencia}' template '{Template}' cantidad={Cantidad}: {Exitosos}/{Total} enviados ({Fallidos} fallidos).",
                aud, templateId, cantidad == int.MaxValue ? "Todos" : cantidad.ToString(), exitosos, elegibles.Count, fallidos);
        }
    }
}
