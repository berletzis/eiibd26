using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace eiibd26.Services
{
    // Implementación de ISmsSender usando Twilio.
    // En entornos sin credenciales configuradas no lanza excepción: solo hace log (útil para DEV).
    public class TwilioSmsSender : ISmsSender
    {
        private readonly ILogger<TwilioSmsSender> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public TwilioSmsSender(IConfiguration configuration, ILogger<TwilioSmsSender> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _accountSid = _configuration["Twilio:AccountSid"];
            _authToken = _configuration["Twilio:AuthToken"];
            _fromNumber = _configuration["Twilio:FromNumber"]; // Debe estar en formato +1234567890
        }

        public async Task SendSmsAsync(string to, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("TwilioSmsSender: destinatario (to) vacío. Se omite envío.");
                return;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("TwilioSmsSender: body vacío. Se omite envío a {To}.", to);
                return;
            }

            if (string.IsNullOrWhiteSpace(_accountSid) || string.IsNullOrWhiteSpace(_authToken) || string.IsNullOrWhiteSpace(_fromNumber))
            {
                _logger.LogWarning("Twilio credentials not configured. SMS not sent. To: {To} BodyPreview: {Preview}", to, body.Length > 120 ? body.Substring(0, 120) + "..." : body);
                return;
            }

            try
            {
                TwilioClient.Init(_accountSid, _authToken);

                var message = await MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(to),
                    from: new Twilio.Types.PhoneNumber(_fromNumber),
                    body: body
                ).ConfigureAwait(false);

                _logger.LogInformation("Twilio SMS SID {Sid} sent to {To} (status: {Status})", message.Sid, to, message.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via Twilio to {To}", to);
                throw;
            }
        }
    }
}