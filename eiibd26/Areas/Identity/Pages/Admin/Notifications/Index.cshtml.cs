using Hangfire;
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
        private readonly IBackgroundJobClient _backgroundJobs;

        public IndexModel(ApplicationDbContext db, PushNotificationService pushService, IBackgroundJobClient backgroundJobs)
        {
            _db = db;
            _pushService = pushService;
            _backgroundJobs = backgroundJobs;
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
            var notification = await _db.PushNotifications.FindAsync(id);
            if (notification == null)
            {
                TempData["Error"] = "Notificación no encontrada.";
                return RedirectToPage();
            }

            if (notification.IsSent)
            {
                TempData["Error"] = "La notificación ya fue enviada y no puede reenviarse.";
                return RedirectToPage();
            }

            _backgroundJobs.Enqueue<eiibd26.Jobs.PushNotificationJob>(j => j.EnviarMasivoAsync(id));
            TempData["Success"] = "✅ Envío encolado. Los mensajes se están enviando en segundo plano.";

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
