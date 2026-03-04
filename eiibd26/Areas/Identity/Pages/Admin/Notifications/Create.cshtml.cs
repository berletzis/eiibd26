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

        public CreateModel(ApplicationDbContext db, PushNotificationService pushService)
        {
            _db = db;
            _pushService = pushService;
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
        }

        public async Task<IActionResult> OnPostAsync()
        {
            TotalSubscribers = await _db.NotificationSubscriptions.CountAsync(s => s.IsActive);

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
                    CreatedBy = createdBy
                };

                // Handle targeting
                if (Input.SendToAll)
                {
                    notification.TargetUserIds = "all";
                }
                else
                {
                    // Build list of target user IDs based on criteria
                    var targetUserIds = new HashSet<Guid>();

                    // By emails
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

                    if (targetUserIds.Any())
                    {
                        notification.TargetUserIds = JsonSerializer.Serialize(targetUserIds.ToList());
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "No se encontraron usuarios con los emails especificados.");
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

                _db.PushNotifications.Add(notification);
                await _db.SaveChangesAsync();

                // If send now, trigger sending
                if (Input.SendNow)
                {
                    try
                    {
                        var (sent, failed) = await _pushService.SendNotificationAsync(notification.Id);

                        if (sent > 0)
                        {
                            TempData["Success"] = $"✅ Notificación enviada correctamente. Enviadas: {sent}, Fallidas: {failed}";
                        }
                        else if (failed > 0)
                        {
                            TempData["Error"] = $"⚠️ La notificación no pudo enviarse. Fallidas: {failed}. Verifica que haya suscriptores activos.";
                        }
                        else
                        {
                            TempData["Error"] = "❌ No hay suscriptores activos para enviar la notificación. La notificación quedó como 'Pendiente'.";
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"❌ Error al enviar: {ex.Message}. La notificación quedó como 'Pendiente'. Puedes reenviarla manualmente.";
                    }
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

        private void ApplyTemplate(string? templateType)
        {
            switch (templateType)
            {
                case "mood":
                    Input.Title = "¿Cómo te sientes hoy?";
                    Input.Body = "Comparte tu estado de ánimo con la comunidad";
                    Input.Icon = "/img/icons/icon-192x192.png";
                    Input.Url = "";
                    break;

                case "medication":
                    Input.Title = "¿Tomaste tu medicamento?";
                    Input.Body = "Recuerda tomar tu medicamento de hoy";
                    Input.Icon = "/img/icons/icon-192x192.png";
                    Input.Url = "";
                    break;

            }
        }
    }
}
