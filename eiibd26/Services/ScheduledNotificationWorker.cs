using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services
{
    /// <summary>
    /// Background Service que revisa cada minuto si hay notificaciones programadas
    /// que deben enviarse y las procesa automáticamente
    /// </summary>
    public class ScheduledNotificationWorker : BackgroundService
    {
        private readonly ILogger<ScheduledNotificationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ScheduledNotificationWorker(
            ILogger<ScheduledNotificationWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 Scheduled Notification Worker iniciado");

            // Esperar 10 segundos antes de empezar (para que la app termine de iniciar)
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledNotifications(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error procesando notificaciones programadas");
                }

                // Revisar cada 1 minuto
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("🔔 Scheduled Notification Worker detenido");
        }

        private async Task ProcessScheduledNotifications(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

            var now = DateTimeOffset.UtcNow;

            // Buscar notificaciones programadas que ya deberían haberse enviado
            var pendingNotifications = await db.PushNotifications
                .Where(n => !n.IsSent && n.ScheduledFor.HasValue && n.ScheduledFor <= now)
                .ToListAsync(stoppingToken);

            if (pendingNotifications.Any())
            {
                _logger.LogInformation($"📬 Encontradas {pendingNotifications.Count} notificaciones pendientes de envío");

                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        _logger.LogInformation($"📤 Enviando notificación ID {notification.Id}: '{notification.Title}'");

                        var (sent, failed) = await pushService.SendNotificationAsync(notification.Id);

                        _logger.LogInformation(
                            $"✅ Notificación {notification.Id} procesada. Enviadas: {sent}, Fallidas: {failed}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Error enviando notificación {notification.Id}");
                    }
                }
            }
            else
            {
                _logger.LogDebug("✓ No hay notificaciones programadas pendientes");
            }
        }
    }
}
