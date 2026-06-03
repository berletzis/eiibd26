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

        // FaseLog estable para campañas generales (template libre, TodosConfirmados).
        // Distinto de 1/2/3 reservados para reactivacion/bienvenida/recordatorio.
        private const int FaseLogGeneral = 10;

        public void OnGet() { }

        /// <summary>
        /// Devuelve los templates de SendGrid configurados en SendGrid:Templates (para el envío de prueba).
        /// No incluye campañas — éstas viven en SendGrid:Campanas.
        /// </summary>
        public IActionResult OnGetTemplatesCorreo()
        {
            var templates = _configuration.GetSection("SendGrid:Templates")
                .Get<List<SendGridTemplateInfo>>()
                ?? new List<SendGridTemplateInfo>();
            return new JsonResult(templates);
        }

        /// <summary>
        /// Devuelve los códigos y nombres de las campañas configuradas en SendGrid:Campanas.
        /// Usado por el JS para poblar el selector de campaña.
        /// </summary>
        public IActionResult OnGetCampanasConfig()
        {
            var campanas = _configuration.GetSection("SendGrid:Campanas")
                .Get<List<CampanaInfo>>()
                ?? new List<CampanaInfo>();
            return new JsonResult(campanas.Select(c => new { c.Codigo, c.Nombre, c.Descripcion }));
        }

        /// <summary>
        /// Conteo de usuarios que cumplen el criterio TodosConfirmados (denominador del "Enviar campaña").
        /// </summary>
        public async Task<IActionResult> OnGetConteoTodosConfirmadosAsync()
        {
            var total = await _targeting
                .AplicarCriterio(_userManager.Users.AsQueryable(), PublicoCampana.TodosConfirmados)
                .CountAsync();
            return new JsonResult(new { total });
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
        /// Resumen por campaña: total de público, cuántos ya la recibieron, cuántos elegibles faltan.
        /// Cada campaña es independiente (sin encadenamiento entre ellas).
        /// </summary>
        public async Task<IActionResult> OnGetCampanaResumenAsync()
        {
            var totalUsuarios = await _userManager.Users.CountAsync();

            var campanas = _configuration.GetSection("SendGrid:Campanas")
                .Get<List<CampanaInfo>>() ?? new();

            var resumenCampanas = new List<object>();

            foreach (var campana in campanas.Where(c => !string.IsNullOrEmpty(c.Codigo)))
            {
                var totalPublico = await _targeting
                    .AplicarCriterio(_userManager.Users.AsQueryable(), campana.Publico)
                    .CountAsync();

                var recibieron = await _db.EmailCampanaLogs
                    .Where(l => l.Fase == campana.FaseLog && l.Exito)
                    .Select(l => l.UserId)
                    .Distinct()
                    .CountAsync();

                resumenCampanas.Add(new
                {
                    campana.Codigo,
                    campana.Nombre,
                    totalPublico,
                    recibieron,
                    elegibles = Math.Max(0, totalPublico - recibieron)
                });
            }

            return new JsonResult(new { totalUsuarios, campanas = resumenCampanas });
        }

        public record CampanaEnviarInput(string CampanaCodigo);

        /// <summary>
        /// Envía un batch de hasta 100 correos a usuarios elegibles de la campaña indicada por código.
        /// Usa CampanaTargetingService para el filtro de público (sin encadenamiento entre campañas).
        /// Registra cada intento en EmailCampanaLog.Fase usando el FaseLog estable de la campaña.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostCampanaEnviarBatchAsync([FromBody] CampanaEnviarInput input)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.CampanaCodigo))
                return new JsonResult(new { success = false, error = "Código de campaña requerido." }) { StatusCode = 400 };

            var campanas = _configuration.GetSection("SendGrid:Campanas").Get<List<CampanaInfo>>() ?? new();
            var campana = campanas.FirstOrDefault(c => c.Codigo == input.CampanaCodigo);

            if (campana is null)
                return new JsonResult(new { success = false, error = $"Campaña '{input.CampanaCodigo}' no encontrada en configuración." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(campana.TemplateId) || campana.TemplateId.Contains("AQUI"))
                return new JsonResult(new { success = false, error = $"No hay template SendGrid configurado para la campaña '{campana.Nombre}'. Actualiza appsettings.json." }) { StatusCode = 400 };

            // Usuarios ya receptores de ESTA campaña (identificados por FaseLog estable)
            var yaRecibieron = await _db.EmailCampanaLogs
                .Where(l => l.Fase == campana.FaseLog && l.Exito)
                .Select(l => l.UserId)
                .ToListAsync();
            var yaRecibieronSet = new HashSet<Guid>(yaRecibieron);

            // Aplicar criterio de público y excluir quienes ya la recibieron
            var elegiblesQuery = _targeting
                .AplicarCriterio(_userManager.Users.AsQueryable(), campana.Publico)
                .Where(u => !yaRecibieronSet.Contains(u.Id));

            var elegibles = await elegiblesQuery
                .OrderBy(u => u.Email)
                .Take(100)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            if (elegibles.Count == 0)
                return new JsonResult(new { success = true, procesados = 0, resultados = Array.Empty<object>(), mensaje = $"No hay usuarios elegibles para la campaña '{campana.Nombre}'." });

            var resultados = new List<object>();

            foreach (var u in elegibles)
            {
                var log = new EmailCampanaLog
                {
                    UserId = u.Id,
                    Fase = campana.FaseLog,    // int estable; mapea al código de campaña sin tocar esquema
                    TemplateId = campana.TemplateId,
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
                        campana = campana.Codigo
                    };

                    await _emailSender.SendDynamicTemplateAsync(
                        u.Email,
                        templateId: campana.TemplateId,
                        templateData: templateData,
                        categories: new[] { "EIIBD", $"Campana-{campana.Codigo}" },
                        customArgs: new Dictionary<string, string>
                        {
                            // Keys exactas que F2 (webhook) leerá de cada evento
                            ["userId"]    = u.Id.ToString(),
                            ["campana"]   = campana.Codigo,
                            ["fase"]      = campana.FaseLog.ToString(),
                            ["envio_ts"]  = DateTime.UtcNow.ToString("O")
                        });

                    log.Exito = true;
                    resultados.Add(new { email = u.Email, exito = true, error = (string)null });
                }
                catch (Exception ex)
                {
                    log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    resultados.Add(new { email = u.Email, exito = false, error = log.Error });
                    _logger.LogError(ex, "Error enviando campaña '{Campana}' a {Email}", campana.Codigo, u.Email);
                }
                finally
                {
                    _db.EmailCampanaLogs.Add(log);
                }
            }

            await _db.SaveChangesAsync();

            var exitosos = resultados.Count(r => (bool)r.GetType().GetProperty("exito").GetValue(r));
            _logger.LogInformation("Campaña '{Campana}': {Exitosos}/{Total} enviados exitosamente.", campana.Codigo, exitosos, resultados.Count);

            return new JsonResult(new
            {
                success = true,
                procesados = resultados.Count,
                exitosos,
                fallidos = resultados.Count - exitosos,
                resultados
            });
        }
        public record EnviarCampanaGeneralInput(string TemplateId);

        /// <summary>
        /// Envía un batch de hasta 100 correos usando el template seleccionado a TodosConfirmados.
        /// Tracking: Fase=10 (FaseLogGeneral) + TemplateId — así cada template es una campaña independiente.
        /// FaseLog 10 no colisiona con reactivacion=1, bienvenida=2, recordatorio=3.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostEnviarCampanaGeneralAsync([FromBody] EnviarCampanaGeneralInput input)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.TemplateId))
                return new JsonResult(new { success = false, error = "Selecciona un template." }) { StatusCode = 400 };

            var templates = _configuration.GetSection("SendGrid:Templates").Get<List<SendGridTemplateInfo>>() ?? new();
            var template = templates.FirstOrDefault(t => t.Id == input.TemplateId);
            if (template is null)
                return new JsonResult(new { success = false, error = "Template no encontrado en configuración." }) { StatusCode = 400 };

            // Excluir quienes ya recibieron exactamente ESTE template como campaña general
            var yaRecibieron = await _db.EmailCampanaLogs
                .Where(l => l.Fase == FaseLogGeneral && l.Exito && l.TemplateId == input.TemplateId)
                .Select(l => l.UserId)
                .ToListAsync();
            var yaRecibieronSet = new HashSet<Guid>(yaRecibieron);

            // Público: TodosConfirmados (nuevos + viejos, todos con EmailConfirmed=true)
            var elegiblesQuery = _targeting
                .AplicarCriterio(_userManager.Users.AsQueryable(), PublicoCampana.TodosConfirmados)
                .Where(u => !yaRecibieronSet.Contains(u.Id));

            var elegibles = await elegiblesQuery
                .OrderBy(u => u.Email)
                .Take(100)
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            if (elegibles.Count == 0)
                return new JsonResult(new { success = true, procesados = 0, resultados = Array.Empty<object>(), mensaje = "No hay usuarios elegibles (todos ya recibieron este template)." });

            var resultados = new List<object>();

            foreach (var u in elegibles)
            {
                var log = new EmailCampanaLog
                {
                    UserId = u.Id,
                    Fase = FaseLogGeneral,
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
                        reset_link = resetLink
                    };

                    await _emailSender.SendDynamicTemplateAsync(
                        u.Email,
                        templateId: input.TemplateId,
                        templateData: templateData,
                        categories: new[] { "EIIBD", "Campana-General" },
                        customArgs: new Dictionary<string, string>
                        {
                            // Keys exactas que F2 (webhook) leerá de cada evento
                            ["userId"]     = u.Id.ToString(),
                            ["campana"]    = "general",
                            ["fase"]       = FaseLogGeneral.ToString(),
                            ["templateId"] = input.TemplateId,
                            ["envio_ts"]   = DateTime.UtcNow.ToString("O")
                        });

                    log.Exito = true;
                    resultados.Add(new { email = u.Email, exito = true, error = (string)null });
                }
                catch (Exception ex)
                {
                    log.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    resultados.Add(new { email = u.Email, exito = false, error = log.Error });
                    _logger.LogError(ex, "Error en campaña general template '{Template}' a {Email}", input.TemplateId, u.Email);
                }
                finally
                {
                    _db.EmailCampanaLogs.Add(log);
                }
            }

            await _db.SaveChangesAsync();

            var exitosos = resultados.Count(r => (bool)r.GetType().GetProperty("exito").GetValue(r));
            _logger.LogInformation("Campaña general template '{Template}': {Exitosos}/{Total} enviados.", input.TemplateId, exitosos, resultados.Count);

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
        /// Borra TODOS los registros EmailCampanaLog con Fase == 1 (reactivación).
        /// Solo afecta Fase=1. No toca Fase=0, 2, 3, ni 10 (campaña general).
        /// Usa ExecuteDeleteAsync (EF Core 7+) para borrado masivo eficiente sin cargar entidades.
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostResetearReactivacionAsync()
        {
            var eliminados = await _db.EmailCampanaLogs
                .Where(l => l.Fase == 1)
                .ExecuteDeleteAsync();

            _logger.LogWarning(
                "[Campanas] Admin reseteó tracking de reactivación: {Eliminados} registros Fase=1 borrados.",
                eliminados);

            return new JsonResult(new { success = true, eliminados });
        }

        // ──────────────────────────────────────────────────────────────────────
        // GRID DE ESTATUS — F3
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resumen de eventos por campaña: usuarios únicos por tipo de evento + tasas.
        /// Ignora registros sin CampanaCodigo (reset individual, push, etc.).
        /// Conteo: usuarios DISTINTOS por tipo (un usuario que abrió 3 veces cuenta 1).
        /// </summary>
        public async Task<IActionResult> OnGetEstatusCampanasAsync()
        {
            // Proyección mínima — solo 3 columnas por fila, independiente del volumen de RawJson
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
                        campana    = g.Key,
                        entregados,
                        abiertos,
                        clicks,
                        rebotes,
                        dropped,
                        spam,
                        bajas,
                        totalEventos  = g.Count(),
                        // Tasas sobre entregados (usuarios únicos)
                        tasaApertura  = entregados > 0 ? Math.Round(abiertos  * 100.0 / entregados, 1) : 0,
                        tasaClick     = entregados > 0 ? Math.Round(clicks    * 100.0 / entregados, 1) : 0,
                        tasaRebote    = entregados > 0 ? Math.Round(rebotes   * 100.0 / entregados, 1) : 0
                    };
                })
                .OrderBy(r => r.campana)
                .ToList();

            return new JsonResult(new { campanas = resumen });
        }

        /// <summary>
        /// Detalle por usuario de una campaña.
        /// Approach de paginación: Take(200) + filtro opcional por tipo de evento.
        /// 200 cubre el batch típico; el admin usa el filtro para reducir el set.
        /// Incluye nombre del usuario (join con Perfil).
        /// </summary>
        public async Task<IActionResult> OnGetEstatusDetalleAsync(string campana, string? filtro = null)
        {
            if (string.IsNullOrWhiteSpace(campana))
                return new JsonResult(new { error = "campana requerida" }) { StatusCode = 400 };

            // Todos los eventos de esta campaña con sus usuarios
            var eventos = await _db.SendGridEventLogs
                .Where(l => l.CampanaCodigo == campana && l.UserId != null)
                .Select(l => new { l.UserId, l.EventType, l.Email, l.Timestamp })
                .ToListAsync();

            if (!eventos.Any())
                return new JsonResult(new { usuarios = Array.Empty<object>() });

            // Agrupar por usuario → flags booleanos
            var porUsuario = eventos
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    userId    = g.Key!,
                    email     = g.First().Email,
                    entregado = g.Any(e => e.EventType == "delivered"),
                    abierto   = g.Any(e => e.EventType == "open"),
                    click     = g.Any(e => e.EventType == "click"),
                    rebote    = g.Any(e => e.EventType == "bounce"),
                    spam      = g.Any(e => e.EventType == "spamreport"),
                    baja      = g.Any(e => e.EventType is "unsubscribe" or "group_unsubscribe"),
                    dropped   = g.Any(e => e.EventType == "dropped"),
                    ultimoEvento = g.Max(e => e.Timestamp)
                });

            // Filtro por tipo de evento
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

            // Resolver nombres desde Perfil (un solo query adicional)
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
                nombre        = nombres.TryGetValue(u.userId, out var n) ? n : null,
                u.entregado,
                u.abierto,
                u.click,
                u.rebote,
                u.spam,
                u.baja,
                u.dropped,
                ultimoEvento  = u.ultimoEvento.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            return new JsonResult(new { campana, filtro, total = resultado.Count, usuarios = resultado });
        }
    }
}
