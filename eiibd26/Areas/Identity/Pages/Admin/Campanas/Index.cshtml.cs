using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Campanas;
using eiibd26.Services;
using eiibd26.Services.Campanas;
using Hangfire;
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
        private readonly ICampanaAudienciaService _audiencia;
        private readonly eiibd26.Services.Email.BounceClasificador _bounceClasificador;
        private readonly IBackgroundJobClient _jobs;

        public CampanasIndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<CampanasIndexModel> logger,
            SendGridEmailSender emailSender,
            IConfiguration configuration,
            ICampanaAudienciaService audiencia,
            eiibd26.Services.Email.BounceClasificador bounceClasificador,
            IBackgroundJobClient jobs)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
            _audiencia = audiencia;
            _bounceClasificador = bounceClasificador;
            _jobs = jobs;
        }

        public void OnGet() { }

        // Devuelve templates para el selector de envío libre.
        // Enfoque híbrido: combina los Dynamic Templates EN VIVO de la API de SendGrid
        // con los de SendGrid:Templates (config). Los de config aportan el etiquetado "Fase N";
        // los nuevos creados en SendGrid aparecen automáticamente con Fase=0.
        // Si la API falla/devuelve vacío, FALLBACK a solo config (el combo nunca queda sin templates).
        public async Task<IActionResult> OnGetTemplatesCorreoAsync()
        {
            var config = _configuration.GetSection("SendGrid:Templates")
                .Get<List<SendGridTemplateInfo>>()
                ?? new List<SendGridTemplateInfo>();

            var apiTemplates = await _emailSender.ListarTemplatesApiAsync();

            // FALLBACK: la API falló o no devolvió nada → usar solo config (preserva el módulo).
            if (apiTemplates.Count == 0)
            {
                _logger.LogWarning("[Campanas] OnGetTemplatesCorreo: la API de SendGrid no devolvió templates. Fallback a SendGrid:Templates (config). Items: {Count}", config.Count);
                var soloConfig = config
                    .OrderByDescending(t => t.Fase > 0)
                    .ThenBy(t => t.Fase)
                    .ThenBy(t => t.Nombre)
                    .Select(t => new { id = t.Id, nombre = t.Nombre, fase = t.Fase })
                    .ToList();
                return new JsonResult(soloConfig);
            }

            var configPorId = config
                .Where(t => !string.IsNullOrWhiteSpace(t.Id))
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Cada template de la API: si coincide con config, hereda Nombre+Fase de config; si no, Fase=0.
            var combinados = apiTemplates
                .Where(t => !string.IsNullOrWhiteSpace(t.Id))
                .Select(t => configPorId.TryGetValue(t.Id, out var c)
                    ? new { id = t.Id, nombre = c.Nombre, fase = c.Fase }
                    : new { id = t.Id, nombre = t.Nombre, fase = 0 })
                .ToList();

            // Incluir templates de config que NO vinieron de la API (p.ej. distinta cuenta/subuser o aún sin generación dynamic).
            var idsApi = new HashSet<string>(apiTemplates.Select(t => t.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var c in config.Where(t => !string.IsNullOrWhiteSpace(t.Id) && !idsApi.Contains(t.Id)))
                combinados.Add(new { id = c.Id, nombre = c.Nombre, fase = c.Fase });

            // Sin duplicados por Id; los de config con Fase primero, luego el resto alfabético.
            var lista = combinados
                .GroupBy(t => t.id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderByDescending(t => t.fase > 0)
                .ThenBy(t => t.fase)
                .ThenBy(t => t.nombre)
                .ToList();

            return new JsonResult(lista);
        }

        /// <summary>
        /// Resuelve un templateId contra el conjunto combinado config + API en vivo.
        /// Prioridad: si está en SendGrid:Templates (config), devuelve ese (con su Fase).
        /// Si no, busca en los Dynamic Templates de la API (cacheados 5 min) y devuelve Fase=0.
        /// Devuelve null solo si el Id no existe en ninguna fuente → el envío lo rechaza.
        /// </summary>
        private async Task<SendGridTemplateInfo?> ResolverTemplateAsync(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return null;

            var config = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var enConfig = config.FirstOrDefault(t => string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase));
            if (enConfig is not null)
                return enConfig;

            var api = await _emailSender.ListarTemplatesApiAsync();
            var enApi = api.FirstOrDefault(t => string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase));
            if (enApi is not null)
                return new SendGridTemplateInfo { Id = enApi.Id, Nombre = enApi.Nombre, Fase = 0 };

            return null;
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

            var (query, _) = await _audiencia.BuildAudienciaQueryAsync((AudienciaCampana)audiencia, templateId);
            var elegibles = await query.CountAsync();
            return new JsonResult(new { elegibles });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ENVÍO UNIFICADO
        // ──────────────────────────────────────────────────────────────────────

        public record EnviarAudienciaInput(int Audiencia, string TemplateId, int Cantidad);

        // Tamaños de tanda permitidos. El sentinel -1 (UI "Todos") se mapea a int.MaxValue = sin límite.
        private static readonly int[] CantidadesPermitidas = { 50, 100, 300, 500, int.MaxValue };

        /// <summary>
        /// Encola en Hangfire el envío de un batch a la audiencia indicada con el template elegido.
        /// El tamaño de tanda viene en Cantidad (50/100/300/500 o -1 = Todos → int.MaxValue).
        /// El envío real (token, plantilla, SendGrid, EmailCampanaLog) lo hace CampanaEnvioJob en
        /// segundo plano, así soporta cualquier tamaño sin timeout de IIS. Responde de inmediato.
        /// </summary>
        public async Task<IActionResult> OnPostEnviarAudienciaAsync([FromBody] EnviarAudienciaInput input)
        {
            if (input is null)
                return new JsonResult(new { success = false, error = "Input requerido." }) { StatusCode = 400 };

            if (!Enum.IsDefined(typeof(AudienciaCampana), input.Audiencia))
                return new JsonResult(new { success = false, error = "Audiencia inválida." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(input.TemplateId))
                return new JsonResult(new { success = false, error = "Selecciona un template." }) { StatusCode = 400 };

            // Sentinel -1 (UI "Todos") → sin límite (int.MaxValue).
            var cantidad = input.Cantidad == -1 ? int.MaxValue : input.Cantidad;
            if (!CantidadesPermitidas.Contains(cantidad))
                return new JsonResult(new { success = false, error = "Cantidad inválida. Usa 50, 100, 300, 500 o Todos." }) { StatusCode = 400 };

            // Acepta templates de config (con Fase) o nuevos en vivo de la API de SendGrid.
            var template = await ResolverTemplateAsync(input.TemplateId);
            if (template is null)
                return new JsonResult(new { success = false, error = "Template no encontrado (ni en config ni en SendGrid)." }) { StatusCode = 400 };

            // baseUrl para que el job arme el reset-link: Url.Page/Request.Scheme no existen dentro del job.
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            _jobs.Enqueue<eiibd26.Jobs.CampanaEnvioJob>(j =>
                j.EnviarAudienciaBatchAsync(input.Audiencia, input.TemplateId, cantidad, baseUrl));

            _logger.LogInformation(
                "[Campanas] Envío encolado: audiencia={Audiencia} template={Template} cantidad={Cantidad}",
                (AudienciaCampana)input.Audiencia, input.TemplateId, cantidad == int.MaxValue ? "Todos" : cantidad.ToString());

            return new JsonResult(new
            {
                success = true,
                encolado = true,
                mensaje = "Envío encolado. Los correos se enviarán en segundo plano."
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // PRUEBA
        // ──────────────────────────────────────────────────────────────────────

        public record CampanaEnviarPruebaInput(string Email, string TemplateId);

        /// <summary>
        /// Envía un correo de prueba con el template seleccionado. No registra en EmailCampanaLog.
        /// </summary>
        public async Task<IActionResult> OnPostCampanaEnviarPruebaAsync([FromBody] CampanaEnviarPruebaInput input)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.Email))
                return new JsonResult(new { success = false, error = "Correo requerido." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(input.TemplateId) || input.TemplateId.Contains("AQUI"))
                return new JsonResult(new { success = false, error = "Selecciona un template válido." }) { StatusCode = 400 };

            // Acepta templates de config (con Fase) o nuevos en vivo de la API de SendGrid.
            var template = await ResolverTemplateAsync(input.TemplateId);
            if (template is null)
                return new JsonResult(new { success = false, error = "Template no encontrado (ni en config ni en SendGrid)." }) { StatusCode = 400 };

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
        // RESETEAR ENVÍOS DE UNA AUDIENCIA (global)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cuenta los registros EmailCampanaLog del FaseLog de la audiencia indicada.
        /// Lo usa la confirmación del reset para mostrar cuántos registros se borrarán.
        /// </summary>
        public async Task<IActionResult> OnGetConteoResetAudienciaAsync(int audiencia)
        {
            if (!Enum.IsDefined(typeof(AudienciaCampana), audiencia))
                return new JsonResult(new { error = "Audiencia inválida." }) { StatusCode = 400 };

            var faseLog = _audiencia.FaseLogPara((AudienciaCampana)audiencia);
            var registros = await _db.EmailCampanaLogs.CountAsync(l => l.Fase == faseLog);
            return new JsonResult(new { registros, faseLog });
        }

        /// <summary>
        /// Borra todos los registros EmailCampanaLog del FaseLog de la audiencia indicada,
        /// reabriendo su lista de elegibles desde cero. Funciona para CUALQUIER audiencia
        /// (Familia A y B). Usa ExecuteDeleteAsync (borrado masivo sin cargar entidades).
        /// Nota: para TodosConfirmados (FaseLog=10) borra el tracking de TODOS los templates.
        /// </summary>
        public async Task<IActionResult> OnPostResetearAudienciaAsync([FromBody] ResetearAudienciaInput input)
        {
            if (input is null || !Enum.IsDefined(typeof(AudienciaCampana), input.Audiencia))
                return new JsonResult(new { success = false, error = "Audiencia inválida." }) { StatusCode = 400 };

            var audiencia = (AudienciaCampana)input.Audiencia;
            var faseLog = _audiencia.FaseLogPara(audiencia);

            var eliminados = await _db.EmailCampanaLogs
                .Where(l => l.Fase == faseLog)
                .ExecuteDeleteAsync();

            _logger.LogWarning(
                "[Campanas] Admin reseteó envíos de '{Audiencia}' (FaseLog={FaseLog}): {Eliminados} registros borrados.",
                audiencia, faseLog, eliminados);

            return new JsonResult(new { success = true, eliminados });
        }

        public record ResetearAudienciaInput(int Audiencia);

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
        // EXCLUIDOS POR REBOTE — panel de transparencia
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista las direcciones excluidas de los envíos por rebote (hard / soft reincidente),
        /// con nombre (cruzado con Perfil si hay usuario), motivo, nº de rebotes, fecha del
        /// último rebote y razón legible. Calculado al vuelo desde SendGridEventLog.
        /// </summary>
        public async Task<IActionResult> OnGetExcluidosPorReboteAsync()
        {
            var detalle = await _bounceClasificador.ObtenerDetalleExcluidosAsync();

            if (detalle.Count == 0)
                return new JsonResult(new { total = 0, hard = 0, soft = 0, items = Array.Empty<object>() });

            // Cruce email → nombre vía AspNetUsers + Perfil (algunos emails no tendrán usuario).
            var emails = detalle.Select(d => d.Email).ToList(); // ya vienen lower/trim
            var usuarios = await _userManager.Users
                .Where(u => u.Email != null && emails.Contains(u.Email.ToLower()))
                .Select(u => new { u.Id, Email = u.Email!.ToLower() })
                .ToListAsync();

            var userIds = usuarios.Select(u => u.Id).Distinct().ToList();
            var perfiles = await _db.Perfil
                .Where(p => userIds.Contains(p.idUser))
                .Select(p => new { p.idUser, p.Nombre })
                .ToListAsync();
            var nombrePorUser = perfiles
                .GroupBy(p => p.idUser)
                .ToDictionary(g => g.Key, g => g.First().Nombre);

            var nombrePorEmail = usuarios
                .GroupBy(u => u.Email)
                .ToDictionary(
                    g => g.Key,
                    g => nombrePorUser.TryGetValue(g.First().Id, out var n) ? n : null,
                    StringComparer.OrdinalIgnoreCase);

            var items = detalle.Select(d => new
            {
                email        = d.Email,
                nombre       = nombrePorEmail.TryGetValue(d.Email, out var nn) ? nn : null,
                motivo       = d.Motivo == Services.Email.BounceClasificador.MotivoExclusion.Hard ? "hard" : "soft",
                motivoTexto  = d.MotivoTexto,
                rebotes      = d.NumeroRebotes,
                ultimoRebote = d.UltimoRebote.ToString("yyyy-MM-dd HH:mm"),
                razon        = d.RazonLegible
            }).ToList();

            return new JsonResult(new
            {
                total = items.Count,
                hard  = items.Count(i => i.motivo == "hard"),
                soft  = items.Count(i => i.motivo == "soft"),
                items
            });
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
