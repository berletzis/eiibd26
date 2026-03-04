using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Services;

namespace eiibd26.Areas.Identity.Pages.Admin.Notifications
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly PushNotificationService _pushService;

        public IndexModel(ApplicationDbContext db, PushNotificationService pushService)
        {
            _db = db;
            _pushService = pushService;
        }

        public List<eiibd26.Models.PushNotification> Notifications { get; set; } = new();
        public int TotalSent { get; set; }
        public int TotalScheduled { get; set; }
        public int TotalSubscribers { get; set; }
        public int SuccessRate { get; set; }
        public string Filter { get; set; } = "all";

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = filter;

            // Get notifications based on filter
            IQueryable<eiibd26.Models.PushNotification> query = _db.PushNotifications
                .Include(n => n.Creator)
                .AsQueryable();

            switch (filter?.ToLower())
            {
                case "sent":
                    query = query.Where(n => n.IsSent);
                    break;
                case "scheduled":
                    query = query.Where(n => !n.IsSent && n.ScheduledFor.HasValue && n.ScheduledFor > DateTimeOffset.UtcNow);
                    break;
                default: // "all"
                    break;
            }

            Notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            // Calculate stats
            TotalSent = await _db.PushNotifications.CountAsync(n => n.IsSent);
            TotalScheduled = await _db.PushNotifications.CountAsync(n => !n.IsSent && n.ScheduledFor.HasValue && n.ScheduledFor > DateTimeOffset.UtcNow);
            TotalSubscribers = await _db.NotificationSubscriptions.CountAsync(s => s.IsActive);

            var allSent = await _db.PushNotifications.Where(n => n.IsSent).ToListAsync();
            if (allSent.Any())
            {
                var totalAttempts = allSent.Sum(n => n.TotalSent + n.TotalFailed);
                var totalSuccess = allSent.Sum(n => n.TotalSent);
                SuccessRate = totalAttempts > 0 ? (int)((totalSuccess / (double)totalAttempts) * 100) : 0;
            }
        }

        public async Task<IActionResult> OnPostSendAsync(Guid id)
        {
            try
            {
                var (sent, failed) = await _pushService.SendNotificationAsync(id);
                TempData["Success"] = $"Notificación enviada correctamente. Enviadas: {sent}, Fallidas: {failed}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al enviar notificación: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                var notification = await _db.PushNotifications.FindAsync(id);
                if (notification != null)
                {
                    _db.PushNotifications.Remove(notification);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Notificación eliminada correctamente";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar notificación: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
