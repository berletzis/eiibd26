using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;
using System.Threading.Tasks;

public class SendGridEmailSender : IEmailSender
{
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
    {
        _logger = logger;
        _configuration = configuration;
        // Puedes poner bajo "SendGrid:ApiKey" y "SendGrid:FromEmail"/"SendGrid:FromName" en appsettings.json
        _sendGridApiKey = _configuration["SendGrid:ApiKey"];
        _fromEmail = _configuration["SendGrid:FromEmail"] ?? "no-reply@tudominio.com";
        _fromName = _configuration["SendGrid:FromName"] ?? "Tu App";
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
            throw new System.Exception("No SendGrid API Key configured.");
        }

        var client = new SendGridClient(_sendGridApiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlMessage);

        var response = await client.SendEmailAsync(msg);

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed: {Body}", body);
            throw new System.Exception($"Failed to send email: {body}");
        }
    }
}