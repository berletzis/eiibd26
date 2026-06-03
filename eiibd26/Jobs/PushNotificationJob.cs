using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eiibd26.Jobs
{
    public class PushNotificationJob
    {
        private readonly Services.PushNotificationService _pushService;
        private readonly ILogger<PushNotificationJob> _logger;

        public PushNotificationJob(
            Services.PushNotificationService pushService,
            ILogger<PushNotificationJob> logger)
        {
            _pushService = pushService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task EnviarMasivoAsync(Guid notificationId)
        {
            _logger.LogInformation("[PushJob] Starting batch send for NotificationId={NotificationId}", notificationId);

            var (sent, failed) = await _pushService.SendNotificationBatchedAsync(notificationId);

            _logger.LogInformation(
                "[PushJob] Completed. NotificationId={NotificationId}, Sent={Sent}, Failed={Failed}",
                notificationId, sent, failed);
        }
    }
}
