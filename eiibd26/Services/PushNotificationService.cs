using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebPush;

namespace eiibd26.Services
{
    public class PushNotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly IPushMoodTokenService _moodTokenService;

        public PushNotificationService(
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<PushNotificationService> logger,
            IPushMoodTokenService moodTokenService)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _moodTokenService = moodTokenService;
        }

        public string GetVapidPublicKey()
        {
            return _config["VapidKeys:PublicKey"] ?? "";
        }

        public async Task<bool> SaveSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth)
        {
            try
            {
                // Check if subscription already exists
                var existing = await _db.NotificationSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

                if (existing != null)
                {
                    existing.IsActive = true;
                    existing.P256dh = p256dh;
                    existing.Auth = auth;
                    existing.LastUsed = DateTimeOffset.UtcNow;
                }
                else
                {
                    var subscription = new eiibd26.Models.NotificationSubscription
                    {
                        UserId = userId,
                        Endpoint = endpoint,
                        P256dh = p256dh,
                        Auth = auth
                    };
                    _db.NotificationSubscriptions.Add(subscription);
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification subscription");
                return false;
            }
        }

        public async Task<(int sent, int failed)> SendNotificationAsync(Guid notificationId)
        {
            var notification = await _db.PushNotifications.FindAsync(notificationId);
            if (notification == null) return (0, 0);

            var publicKey = _config["VapidKeys:PublicKey"];
            var privateKey = _config["VapidKeys:PrivateKey"];
            var subject = _config["VapidKeys:Subject"] ?? "mailto:admin@eiibd.com";

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogError("VAPID keys not configured");
                return (0, 0);
            }

            // Get subscriptions based on targeting
            IQueryable<eiibd26.Models.NotificationSubscription> query = _db.NotificationSubscriptions.Where(s => s.IsActive);

            if (notification.TargetUserIds != "all" && !string.IsNullOrEmpty(notification.TargetUserIds))
            {
                try
                {
                    var targetIds = JsonConvert.DeserializeObject<List<Guid>>(notification.TargetUserIds);
                    if (targetIds != null && targetIds.Any())
                    {
                        _logger.LogInformation($"Sending to specific users: {targetIds.Count} user IDs");
                        query = query.Where(s => targetIds.Contains(s.UserId));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing TargetUserIds");
                }
            }
            else
            {
                _logger.LogInformation("Sending to all active subscribers");
            }

            var subscriptions = await query.ToListAsync();

            _logger.LogInformation($"Found {subscriptions.Count} subscriptions to send to");

            int sent = 0;
            int failed = 0;

            var webPushClient = new WebPushClient();
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var actions = GetActionsByTipo(notification.Tipo);
            var notificationTag = "eiibd-" + notification.Id.ToString();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    // Generar token de corta duración (5 min) por usuario para el quick-mood
                    var moodToken = _moodTokenService.GenerarToken(subscription.UserId);

                    var payload = JsonConvert.SerializeObject(new
                    {
                        title = notification.Title,
                        body = notification.Body,
                        icon = notification.Icon ?? "/img/icons/icon-192x192.png",
                        url = notification.Url ?? "/",
                        tag = notificationTag,
                        actions,
                        actionData = new { moodToken }
                    });

                    var pushSubscription = new PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );

                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                    subscription.LastUsed = DateTimeOffset.UtcNow;
                    sent++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning($"WebPush error for subscription {subscription.Id}: {ex.Message}");

                    // If subscription is no longer valid, deactivate it
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || 
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        subscription.IsActive = false;
                    }
                    failed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending notification to subscription {subscription.Id}");
                    failed++;
                }
            }

            notification.IsSent = true;
            notification.SentAt = DateTimeOffset.UtcNow;
            notification.TotalSent = sent;
            notification.TotalFailed = failed;

            await _db.SaveChangesAsync();

            _logger.LogInformation($"Notification {notificationId} sent. Success: {sent}, Failed: {failed}");

            return (sent, failed);
        }

        /// <summary>Envía la notificación únicamente a las suscripciones activas del usuario indicado.</summary>
        public async Task<(int sent, int failed)> SendNotificationToUserAsync(Guid notificationId, Guid userId)
        {
            var notification = await _db.PushNotifications.FindAsync(notificationId);
            if (notification == null) return (0, 0);

            var publicKey = _config["VapidKeys:PublicKey"];
            var privateKey = _config["VapidKeys:PrivateKey"];
            var subject = _config["VapidKeys:Subject"] ?? "mailto:admin@eiibd.com";

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogError("VAPID keys not configured");
                return (0, 0);
            }

            var subscriptions = await _db.NotificationSubscriptions
                .Where(s => s.IsActive && s.UserId == userId)
                .ToListAsync();

            if (subscriptions.Count == 0) return (0, 0);

            int sent = 0;
            int failed = 0;

            var webPushClient = new WebPushClient();
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var actions = GetActionsByTipo(notification.Tipo);
            var notificationTag = "eiibd-" + notification.Id.ToString();
            var moodToken = _moodTokenService.GenerarToken(userId);

            var payload = JsonConvert.SerializeObject(new
            {
                title = notification.Title,
                body = notification.Body,
                icon = notification.Icon ?? "/img/icons/icon-192x192.png",
                url = notification.Url ?? "/",
                tag = notificationTag,
                actions,
                actionData = new { moodToken }
            });

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth);

                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                    subscription.LastUsed = DateTimeOffset.UtcNow;
                    sent++;
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning("WebPush error for subscription {Id}: {Message}", subscription.Id, ex.Message);
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        subscription.IsActive = false;
                    }
                    failed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification to subscription {Id}", subscription.Id);
                    failed++;
                }
            }

            notification.IsSent = true;
            notification.SentAt = DateTimeOffset.UtcNow;
            notification.TotalSent = sent;
            notification.TotalFailed = failed;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId}. Success: {Sent}, Failed: {Failed}",
                notificationId, userId, sent, failed);

            return (sent, failed);
        }

        private static object? GetActionsByTipo(string tipo)
        {
            switch (tipo)
            {
                case "Mood":
                    return new[]
                    {
                        new { action = "mood-muy-bien", title = "😁 Muy bien", icon = "/img/icons/icon-72x72.png" },
                        new { action = "mood-bien",     title = "😊 Bien",     icon = "/img/icons/icon-72x72.png" },
                        new { action = "mood-normal",   title = "😐 Normal",   icon = "/img/icons/icon-72x72.png" },
                        new { action = "mood-mal",      title = "😟 Mal",      icon = "/img/icons/icon-72x72.png" },
                        new { action = "mood-muy-mal",  title = "😢 Muy mal",  icon = "/img/icons/icon-72x72.png" }
                    };

                case "Respuesta":
                    return new[]
                    {
                        new { action = "survey-si",   title = "✓ Sí",   icon = "/img/icons/icon-72x72.png" },
                        new { action = "survey-no",   title = "✗ No",    icon = "/img/icons/icon-72x72.png" },
                        new { action = "view",         title = "Ver más", icon = "/img/icons/icon-72x72.png" }
                    };

                case "Recordatorio":
                    return new[]
                    {
                        new { action = "reminder-done",   title = "✅ Listo",      icon = "/img/icons/icon-72x72.png" },
                        new { action = "reminder-later",  title = "⏰ Más tarde", icon = "/img/icons/icon-72x72.png" }
                    };

                default: // General
                    return null;
            }
        }
    }
}
