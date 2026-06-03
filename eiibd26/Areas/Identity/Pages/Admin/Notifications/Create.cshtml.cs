using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using eiibd26.Services;

namespace eiibd26.Areas.Identity.Pages.Admin.Notifications
{
    [Authorize(Roles = "Administrador")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly PushNotificationService _pushService;
        private readonly IBackgroundJobClient _backgroundJobs;

        public CreateModel(ApplicationDbContext db, PushNotificationService pushService, IBackgroundJobClient backgroundJobs)
        {
            _db = db;
            _pushService = pushService;
            _backgroundJobs = backgroundJobs;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public int TotalSubscribers { get; set; }

        public List<ConditionOption> AvailableConditions { get; set; } = new();
        public List<SintomaOption> AvailableSintomas { get; set; } = new();
        public List<TratamientoOption> AvailableTratamientos { get; set; } = new();

        public class InputModel
        {
            public string? TemplateType { get; set; } = "custom";

            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "General";

            [Required(ErrorMessage = "El título es obligatorio")]
            [StringLength(100, ErrorMessage = "El título no puede exceder los 100 caracteres")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "El mensaje es obligatorio")]
            [StringLength(500, ErrorMessage = "El mensaje no puede exceder los 500 caracteres")]
            public string Body { get; set; } = string.Empty;

            public string? Url { get; set; }

            public string? Icon { get; set; }

            public bool SendToAll { get; set; } = true;

            public bool SendNow { get; set; } = true;

            public string? ScheduledDate { get; set; }

            public string? ScheduledTime { get; set; }

            // NEW: Specific targeting options
            public string? TargetEmails { get; set; }
            public List<int> TargetConditionIds { get; set; } = new();
            public List<int> TargetSintomaIds { get; set; } = new();
            public List<int> TargetTratamientoIds { get; set; } = new();
        }

        public class ConditionOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class SintomaOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TratamientoOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            TotalSubscribers = await _db.NotificationSubscriptions.CountAsync(s => s.IsActive);
            await CargarFiltrosAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            TotalSubscribers = await _db.NotificationSubscriptions.CountAsync(s => s.IsActive);
            await CargarFiltrosAsync();

            // Apply template if selected
            if (Input.TemplateType != "custom")
            {
                ApplyTemplate(Input.TemplateType);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? createdBy = null;
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    createdBy = userId;
                }

                var notification = new eiibd26.Models.PushNotification
                {
                    Title = Input.Title,
                    Body = Input.Body,
                    Icon = string.IsNullOrWhiteSpace(Input.Icon) ? "/img/icons/icon-192x192.png" : Input.Icon,
                    Url = string.IsNullOrWhiteSpace(Input.Url) ? "/" : Input.Url,
                    CreatedBy = createdBy,
                    Tipo = Input.Tipo
                };

                // Handle targeting
                if (Input.SendToAll)
                {
                    notification.TargetUserIds = "all";
                }
                else
                {
                    bool hayFiltro = !string.IsNullOrWhiteSpace(Input.TargetEmails) ||
                                     Input.TargetConditionIds.Any() ||
                                     Input.TargetSintomaIds.Any() ||
                                     Input.TargetTratamientoIds.Any();

                    if (!hayFiltro)
                    {
                        ModelState.AddModelError(string.Empty, "Selecciona al menos un criterio: email, condición, síntoma o tratamiento.");
                        return Page();
                    }

                    var targetUserIds = new HashSet<Guid>();

                    // Por emails
                    if (!string.IsNullOrWhiteSpace(Input.TargetEmails))
                    {
                        var emails = Input.TargetEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim().ToLower())
                            .ToList();

                        var usersByEmail = await _db.Users
                            .Where(u => emails.Contains(u.Email.ToLower()))
                            .Select(u => u.Id)
                            .ToListAsync();

                        foreach (var id in usersByEmail) targetUserIds.Add(id);
                    }

                    // Por condición
                    if (Input.TargetConditionIds.Any())
                    {
                        var ids = await _db.condicionUsuario
                            .Where(u => Input.TargetConditionIds.Contains(u.idCondicion ?? 0) && !u.Eliminado)
                            .Select(u => u.idUsuario)
                            .Distinct()
                            .ToListAsync();
                        foreach (var id in ids) targetUserIds.Add(id);
                    }

                    // Por síntoma
                    if (Input.TargetSintomaIds.Any())
                    {
                        var ids = await _db.sintomasUsuario
                            .Where(u => Input.TargetSintomaIds.Contains(u.idSintoma ?? 0) && !u.Eliminado)
                            .Select(u => u.idUsuario)
                            .Distinct()
                            .ToListAsync();
                        foreach (var id in ids) targetUserIds.Add(id);
                    }

                    // Por tratamiento
                    if (Input.TargetTratamientoIds.Any())
                    {
                        var ids = await _db.tratamientoUsuario
                            .Where(u => Input.TargetTratamientoIds.Contains(u.idTratamiento ?? 0) && !u.Eliminado)
                            .Select(u => u.idUsuario)
                            .Distinct()
                            .ToListAsync();
                        foreach (var id in ids) targetUserIds.Add(id);
                    }

                    if (targetUserIds.Any())
                    {
                        notification.TargetUserIds = JsonSerializer.Serialize(targetUserIds.ToList());
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "No se encontraron usuarios activos con los criterios seleccionados.");
                        return Page();
                    }
                }

                // Handle scheduling
                if (!Input.SendNow && !string.IsNullOrEmpty(Input.ScheduledDate) && !string.IsNullOrEmpty(Input.ScheduledTime))
                {
                    var dateStr = $"{Input.ScheduledDate} {Input.ScheduledTime}";
                    if (DateTime.TryParse(dateStr, out var scheduledDateTime))
                    {
                        // Asumir que la hora ingresada es hora LOCAL del servidor
                        // y convertirla correctamente a UTC
                        var localDateTime = DateTime.SpecifyKind(scheduledDateTime, DateTimeKind.Local);
                        notification.ScheduledFor = new DateTimeOffset(localDateTime);
                    }
                }

                // Guarda anti-doble-POST: bloquea notificaciones idénticas creadas en los últimos 10 segundos
                var ventana = DateTimeOffset.UtcNow.AddSeconds(-10);
                var existe = await _db.PushNotifications.AnyAsync(n =>
                    n.Title == notification.Title &&
                    n.Body == notification.Body &&
                    n.CreatedBy == notification.CreatedBy &&
                    n.CreatedAt >= ventana);

                if (existe)
                {
                    TempData["Error"] = "Ya se creó una notificación idéntica recientemente. Espera unos segundos antes de intentarlo de nuevo.";
                    return RedirectToPage("Index");
                }

                _db.PushNotifications.Add(notification);
                await _db.SaveChangesAsync();

                if (Input.SendNow)
                {
                    _backgroundJobs.Enqueue<eiibd26.Jobs.PushNotificationJob>(j => j.EnviarMasivoAsync(notification.Id));
                    TempData["Success"] = "✅ Envío encolado. Los mensajes se están enviando en segundo plano.";
                }
                else
                {
                    TempData["Success"] = "⏰ Notificación programada correctamente";
                }

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear la notificación: {ex.Message}");
                return Page();
            }
        }

        private async Task CargarFiltrosAsync()
        {
            AvailableConditions = await _db.condiciones
                .Where(c => !c.Eliminado)
                .OrderBy(c => c.nombre)
                .Select(c => new ConditionOption { Id = c.id, Name = c.nombre ?? "" })
                .ToListAsync();

            AvailableSintomas = await _db.sintomas
                .Where(s => !s.Eliminado)
                .OrderBy(s => s.nombre)
                .Select(s => new SintomaOption { Id = s.id, Name = s.nombre ?? "" })
                .ToListAsync();

            AvailableTratamientos = await _db.tratamientos
                .Where(t => !t.Eliminado)
                .OrderBy(t => t.nombre)
                .Select(t => new TratamientoOption { Id = t.id, Name = t.nombre ?? "" })
                .ToListAsync();
        }

        private void ApplyTemplate(string? templateType)
        {
            switch (templateType)
            {
                case "mood":
                    Input.Title = "¿Cómo te sientes hoy?";
                    Input.Body = "Comparte tu estado de ánimo con la comunidad";
                    Input.Icon = "/img/icons/icon-192x192.png";
                    Input.Url = "";
                    Input.Tipo = "Mood";
                    break;

                case "medication":
                    Input.Title = "¿Tomaste tu medicamento?";
                    Input.Body = "Recuerda tomar tu medicamento de hoy";
                    Input.Icon = "/img/icons/icon-192x192.png";
                    Input.Url = "";
                    Input.Tipo = "Recordatorio";
                    break;

            }
        }
    }
}
