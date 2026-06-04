using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Campanas;
using eiibd26.Services;
using eiibd26.Services.Campanas;
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
        private readonly ICampanaTargetingService _targeting;

        public CampanasIndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<CampanasIndexModel> logger,
            SendGridEmailSender emailSender,
            IConfiguration configuration,
            ICampanaTargetingService targeting)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
            _targeting = targeting;
        }

        // FaseLog para TodosConfirmados. No colisiona con 1/2/3 (toques de secuencia) ni 0 (reset pw).
        private const int FaseLogGeneral = 10;

        public void OnGet() { }

        // Devuelve templates de SendGrid:Templates para el selector de template.
        public IActionResult OnGetTemplatesCorreo()
        {
            var templates = _configuration.GetSection("SendGrid:Templates")
                .Get<List<SendGridTemplateInfo>>()
                ?? new List<SendGridTemplateInfo>();
            return new JsonResult(templates);
        }

        // ──────────────────────────────────────────────────────────────────────
        // AUDIENCIA — conteo en vivo
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve el número de usuarios elegibles para la audiencia + template dados.
        /// Para TodosConfirmados, si se pasa templateId, excluye quienes ya lo recibieron.
        /// Para Toque1/2/3, el templateId se ignora en el filtro (la exclusión es por FaseLog).
        /// </summary>
        public async Task<IActionResult> OnGetConteoAudienciaAsync(int audiencia, string? templateId = null)
        {
            if (!Enum.IsDefined(typeof(AudienciaCampana), audiencia))
                return new JsonResult(new { error = "Audiencia inválida." }) { StatusCode = 400 };

            var (query, _) = await BuildAudienciaQueryAsync((AudienciaCampana)audiencia, templateId);
            var elegibles = await query.CountAsync();
            return new JsonResult(new { elegibles });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ENVÍO UNIFICADO
        // ──────────────────────────────────────────────────────────────────────

        public record EnviarAudienciaInput(int Audiencia, string TemplateId);

        /// <summary>
        /// Envía un batch de hasta 100 correos a los elegibles de la audiencia indicada,
        /// usando el template libre elegido por el admin.
        /// Registra en EmailCampanaLog con el FaseLog correspondiente a la audiencia.
        /// Custom args y unsubscribeGroupId idénticos al patrón de campañas existente.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostEnviarAudienciaAsync([FromBody] EnviarAudienciaInput input)
        {
            if (input is null)
                return new JsonResult(new { success = false, error = "Input requerido." }) { StatusCode = 400 };

            if (!Enum.IsDefined(typeof(AudienciaCampana), input.Audiencia))
                return new JsonResult(new { success = false, error = "Audiencia inválida." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(input.TemplateId))
                return new JsonResult(new { success = false, error = "Selecciona un template." }) { StatusCode = 400 };

            var templates = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var template = templates.FirstOrDefault(t => t.Id == input.TemplateId);
            if (template is null)
                return new JsonResult(new { success = false, error = "Template no encontrado en configuración." }) { StatusCode = 400 };

            var audiencia = (AudienciaCampana)input.Audiencia;
            var (eligiblesQuery, faseLog) = await BuildAudienciaQueryAsync(audiencia, input.TemplateId);

            var elegibles = await eligiblesQuery
                .OrderBy(u => u.Email)
                .Take(100)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            if (elegibles.Count == 0)
                return new JsonResult(new
                {
                    success = true,
                    procesados = 0,
                    resultados = Array.Empty<object>(),
                    mensaje = $"No hay usuarios elegibles para '{audiencia}'."
                });

            var campanaCodigo = audiencia switch
            {
                AudienciaCampana.ViejosSinToque1 => "toque1",
                AudienciaCampana.Toque2          => "toque2",
                AudienciaCampana.Toque3          => "toque3",
                _                                => "general"
            };

            var unsubGroupId = _configuration.GetValue<int?>("SendGrid:UnsubscribeGroupId");
            var resultados = new List<object>();

            foreach (var u in elegibles)
            {
                var log = new EmailCampanaLog
                {
                    UserId = u.Id,
                    Fase = faseLog,
                    TemplateId = input.TemplateId,
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
                        campana = campanaCodigo
                    };

                    await _emailSender.SendDynamicTemplateAsync(
                        u.Email,
                        templateId: input.TemplateId,
                        templateData: templateData,
                        categories: new[] { "EIIBD", $"Campana-{campanaCodigo}" },
                        customArgs: new Dictionary<string, string>
                        {
                            ["userId"]     = u.Id.ToString(),
                            ["campana"]    = campanaCodigo,
                            ["fase"]       = faseLog.ToString(),
                            ["templateId"] = input.TemplateId,
                            ["envio_ts"]   = DateTime.UtcNow.ToString("O")
                        },
                        unsubscribeGroupId: unsubGroupId);

                    log.Exito = true;
                    resultados.Add(new { email = u.Email, exito = true, error = (string)null });
                }
                catch (Exception ex)
                {
                    log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    resultados.Add(new { email = u.Email, exito = false, error = log.Error });
                    _logger.LogError(ex, "Error enviando audiencia '{Audiencia}' a {Email}", audiencia, u.Email);
                }
                finally
                {
                    _db.EmailCampanaLogs.Add(log);
                }
            }

            await _db.SaveChangesAsync();

            var exitosos = resultados.Count(r => (bool)r.GetType().GetProperty("exito").GetValue(r));
            _logger.LogInformation("Audiencia '{Audiencia}' template '{Template}': {Exitosos}/{Total} enviados.",
                audiencia, input.TemplateId, exitosos, resultados.Count);

            return new JsonResult(new
            {
                success = true,
                procesados = resultados.Count,
                exitosos,
                fallidos = resultados.Count - exitosos,
                resultados
            });
        }

        /// <summary>
        /// Construye la query de elegibles para la audiencia indicada y devuelve el FaseLog asignado.
        /// Toque2 y Toque3 parten de UsuariosViejos (len=68): los reactivados ya no tienen ese hash
        /// y quedan excluidos automáticamente sin lógica adicional.
        /// Para TodosConfirmados con templateId, excluye quienes ya recibieron ese template (Fase=10).
        /// </summary>
        private async Task<(IQueryable<ApplicationUser> query, int faseLog)> BuildAudienciaQueryAsync(
            AudienciaCampana audiencia, string? templateId = null)
        {
            var users = _userManager.Users.AsQueryable();

            switch (audiencia)
            {
                case AudienciaCampana.ViejosSinToque1:
                {
                    var yaF1 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 1 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => !yaF1.Contains(u.Id)),
                        1);
                }

                case AudienciaCampana.Toque2:
                {
                    var conF1 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 1 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    var yaF2 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 2 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => conF1.Contains(u.Id) && !yaF2.Contains(u.Id)),
                        2);
                }

                case AudienciaCampana.Toque3:
                {
                    var conF2 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 2 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    var yaF3 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 3 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => conF2.Contains(u.Id) && !yaF3.Contains(u.Id)),
                        3);
                }

                case AudienciaCampana.TodosConfirmados:
                default:
                {
                    var q = _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados);
                    if (!string.IsNullOrWhiteSpace(templateId))
                    {
                        var yaRecibieron = new HashSet<Guid>(await _db.EmailCampanaLogs
                            .Where(l => l.Fase == FaseLogGeneral && l.Exito && l.TemplateId == templateId)
                            .Select(l => l.UserId).Distinct().ToListAsync());
                        q = q.Where(u => !yaRecibieron.Contains(u.Id));
                    }
                    return (q, FaseLogGeneral);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // PRUEBA
        // ──────────────────────────────────────────────────────────────────────

        public record CampanaEnviarPruebaInput(string Email, string TemplateId);

        /// <summary>
        /// Envía un correo de prueba con el template seleccionado. No registra en EmailCampanaLog.
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

                var unsubGroupId = _configuration.GetValue<int?>("SendGrid:UnsubscribeGroupId");
                await _emailSender.SendDynamicTemplateAsync(
                    input.Email,
                    templateId: template.Id,
                    templateData: templateData,
                    categories: new[] { "EIIBD", "Prueba" },
                    unsubscribeGroupId: unsubGroupId);

                _logger.LogInformation("Correo de prueba template '{Template}' enviado a {Email}.", template.Nombre, input.Email);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo de prueba template '{Template}' a {Email}.", template.Nombre, input.Email);
                return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = 500 };
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // RESETEAR TRACKING TOQUE 1
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Borra todos los registros EmailCampanaLog con Fase=1.
        /// No toca Fase=2, 3, ni 10. Usa ExecuteDeleteAsync (borrado masivo sin cargar entidades).
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostResetearReactivacionAsync()
        {
            var eliminados = await _db.EmailCampanaLogs
                .Where(l => l.Fase == 1)
                .ExecuteDeleteAsync();

            _logger.LogWarning(
                "[Campanas] Admin reseteó tracking Toque 1: {Eliminados} registros Fase=1 borrados.",
                eliminados);

            return new JsonResult(new { success = true, eliminados });
        }

        // ──────────────────────────────────────────────────────────────────────
        // GRID DE ESTATUS — F3
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resumen de eventos por campaña: usuarios únicos por tipo de evento + tasas.
        /// Agrupa por CampanaCodigo del SendGridEventLog (campo "campana" en custom args).
        /// Ignorausuarios sin CampanaCodigo (reset individual, push, etc.).
        /// </summary>
        public async Task<IActionResult> OnGetEstatusCampanasAsync()
        {
            var rawData = await _db.SendGridEventLogs
                .Where(l => l.CampanaCodigo != null && l.UserId != null)
                .Select(l => new { l.CampanaCodigo, l.EventType, l.UserId })
                .ToListAsync();

            var resumen = rawData
                .GroupBy(l => l.CampanaCodigo)
                .Select(g =>
                {
                    var entregados = g.Where(x => x.EventType == "delivered").Select(x => x.UserId).Distinct().Count();
                    var abiertos   = g.Where(x => x.EventType == "open").Select(x => x.UserId).Distinct().Count();
                    var clicks     = g.Where(x => x.EventType == "click").Select(x => x.UserId).Distinct().Count();
                    var rebotes    = g.Where(x => x.EventType == "bounce").Select(x => x.UserId).Distinct().Count();
                    var dropped    = g.Where(x => x.EventType == "dropped").Select(x => x.UserId).Distinct().Count();
                    var spam       = g.Where(x => x.EventType == "spamreport").Select(x => x.UserId).Distinct().Count();
                    var bajas      = g.Where(x => x.EventType is "unsubscribe" or "group_unsubscribe").Select(x => x.UserId).Distinct().Count();

                    return new
                    {
                        campana       = g.Key,
                        entregados,
                        abiertos,
                        clicks,
                        rebotes,
                        dropped,
                        spam,
                        bajas,
                        totalEventos  = g.Count(),
                        tasaApertura  = entregados > 0 ? Math.Round(abiertos * 100.0 / entregados, 1) : 0,
                        tasaClick     = entregados > 0 ? Math.Round(clicks   * 100.0 / entregados, 1) : 0,
                        tasaRebote    = entregados > 0 ? Math.Round(rebotes  * 100.0 / entregados, 1) : 0
                    };
                })
                .OrderBy(r => r.campana)
                .ToList();

            return new JsonResult(new { campanas = resumen });
        }

        /// <summary>
        /// Detalle por usuario de una campaña. Take(200) + filtro opcional por tipo de evento.
        /// </summary>
        public async Task<IActionResult> OnGetEstatusDetalleAsync(string campana, string? filtro = null)
        {
            if (string.IsNullOrWhiteSpace(campana))
                return new JsonResult(new { error = "campana requerida" }) { StatusCode = 400 };

            var eventos = await _db.SendGridEventLogs
                .Where(l => l.CampanaCodigo == campana && l.UserId != null)
                .Select(l => new { l.UserId, l.EventType, l.Email, l.Timestamp })
                .ToListAsync();

            if (!eventos.Any())
                return new JsonResult(new { usuarios = Array.Empty<object>() });

            var porUsuario = eventos
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    userId       = g.Key!,
                    email        = g.First().Email,
                    entregado    = g.Any(e => e.EventType == "delivered"),
                    abierto      = g.Any(e => e.EventType == "open"),
                    click        = g.Any(e => e.EventType == "click"),
                    rebote       = g.Any(e => e.EventType == "bounce"),
                    spam         = g.Any(e => e.EventType == "spamreport"),
                    baja         = g.Any(e => e.EventType is "unsubscribe" or "group_unsubscribe"),
                    dropped      = g.Any(e => e.EventType == "dropped"),
                    ultimoEvento = g.Max(e => e.Timestamp)
                });

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                porUsuario = filtro switch
                {
                    "delivered"   => porUsuario.Where(u => u.entregado),
                    "open"        => porUsuario.Where(u => u.abierto),
                    "click"       => porUsuario.Where(u => u.click),
                    "bounce"      => porUsuario.Where(u => u.rebote),
                    "spamreport"  => porUsuario.Where(u => u.spam),
                    "unsubscribe" => porUsuario.Where(u => u.baja),
                    "dropped"     => porUsuario.Where(u => u.dropped),
                    "noopen"      => porUsuario.Where(u => u.entregado && !u.abierto),
                    _             => porUsuario
                };
            }

            var lista = porUsuario
                .OrderByDescending(u => u.ultimoEvento)
                .Take(200)
                .ToList();

            var userGuids = lista
                .Select(u => { Guid.TryParse(u.userId, out var g); return g; })
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();

            var nombres = await _db.Perfil
                .Where(p => userGuids.Contains(p.idUser))
                .Select(p => new { p.idUser, p.Nombre })
                .ToDictionaryAsync(p => p.idUser.ToString(), p => p.Nombre ?? "");

            var resultado = lista.Select(u => new
            {
                u.userId,
                u.email,
                nombre       = nombres.TryGetValue(u.userId, out var n) ? n : null,
                u.entregado,
                u.abierto,
                u.click,
                u.rebote,
                u.spam,
                u.baja,
                u.dropped,
                ultimoEvento = u.ultimoEvento.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            return new JsonResult(new { campana, filtro, total = resultado.Count, usuarios = resultado });
        }

        // ──────────────────────────────────────────────────────────────────────
        // DIAGNÓSTICO SENDGRID
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve estado de configuración SendGrid sin exponer secretos.
        /// apiKeyPreview = primeros 6 chars + "..." para confirmar qué key está activa.
        /// </summary>
        public async Task<IActionResult> OnGetDiagnosticoSendGridAsync()
        {
            var apiKey    = _configuration["SendGrid:ApiKey"] ?? string.Empty;
            var webhookKey = _configuration["SendGrid:WebhookPublicKey"] ?? string.Empty;

            var templates   = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var campanas    = _configuration.GetSection("SendGrid:Campanas").Get<List<CampanaInfo>>() ?? new();
            var defaultCats = _configuration.GetSection("SendGrid:DefaultCategories").Get<List<string>>() ?? new();

            static bool IdValido(string? id) =>
                !string.IsNullOrWhiteSpace(id) && id.StartsWith("d-") && !id.Contains("AQUI") && id.Length > 10;

            var templatesInfo = templates.Select(t => new { t.Nombre, t.Fase, idValido = IdValido(t.Id) }).ToList();
            var campanasInfo  = campanas.Select(c => new
            {
                c.Codigo, c.Nombre, Publico = c.Publico.ToString(), c.FaseLog, idValido = IdValido(c.TemplateId)
            }).ToList();

            var templatesConProblema = templatesInfo.Count(t => !t.idValido) + campanasInfo.Count(c => !c.idValido);

            var totalSuscriptoresEmail = await _userManager.Users
                .CountAsync(u => u.EmailConfirmed && u.Email != null);
            var eventosWebhookRecibidos = await _db.SendGridEventLogs.CountAsync();

            return new JsonResult(new
            {
                apiKeyPresente           = !string.IsNullOrWhiteSpace(apiKey),
                apiKeyPreview            = apiKey.Length >= 6 ? apiKey[..6] + "..." : (apiKey.Length > 0 ? "***" : "(vacía)"),
                fromEmail                = _configuration["SendGrid:FromEmail"] ?? "(no configurado)",
                fromName                 = _configuration["SendGrid:FromName"] ?? "(no configurado)",
                defaultCategories        = defaultCats,
                templates                = templatesInfo,
                campanas                 = campanasInfo,
                templatesConProblema,
                webhookPublicKeyPresente = !string.IsNullOrWhiteSpace(webhookKey),
                totalSuscriptoresEmail,
                eventosWebhookRecibidos
            });
        }
    }
}
