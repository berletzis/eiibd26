using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace eiibd26.Services
{
    // Implementación de IEmailSender usando SendGrid.
    // Lee categorías por defecto desde configuration["SendGrid:DefaultCategories"] (array) o "SendGrid:DefaultCategoriesCsv" (csv).
    public class SendGridEmailSender : IEmailSender
    {
        private readonly ILogger<SendGridEmailSender> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly List<string> _defaultCategories;

        public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _sendGridApiKey = _configuration["SendGrid:ApiKey"];
            _fromEmail = _configuration["SendGrid:FromEmail"] ?? "no-reply@tudominio.com";
            _fromName = _configuration["SendGrid:FromName"] ?? "eiibd26";

            // Leer categorías por defecto: preferimos una lista en config (array), si no, leemos CSV
            _defaultCategories = _configuration.GetSection("SendGrid:DefaultCategories")?.Get<List<string>>()
                                 ?? (ParseCsv(_configuration["SendGrid:DefaultCategoriesCsv"]) ?? new List<string>());
        }

        // IEmailSender contract - usa las categorías por defecto si existen
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return SendEmailAsyncWithCategories(email, subject, htmlMessage, _defaultCategories);
        }

        // Método público adicional que permite pasar categorías ad-hoc para un envío específico.
        public async Task SendEmailAsyncWithCategories(string email, string subject, string htmlMessage, IEnumerable<string> categories)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("SendGridEmailSender: email is null/empty - skipping send.");
                return;
            }

            // If no API key configured, do not throw in dev — just log and return.
            if (string.IsNullOrWhiteSpace(_sendGridApiKey))
            {
                _logger.LogWarning("SendGrid API key not configured. Email was not sent. To enable sending, set SendGrid:ApiKey in configuration.");
                _logger.LogInformation("To: {Email} Subject: {Subject} BodyPreview: {Preview}", email, subject, (htmlMessage ?? string.Empty).Substring(0, Math.Min(240, (htmlMessage ?? string.Empty).Length)));
                return;
            }

            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(email);
                var plainTextContent = StripHtml(htmlMessage) ?? string.Empty;
                var msg = MailHelper.CreateSingleEmail(from, to, subject ?? string.Empty, plainTextContent, htmlMessage ?? string.Empty);

                // Asignar categorías (SendGrid supports multiple)
                if (categories != null)
                {
                    var cats = categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToList();
                    if (cats.Any())
                    {
                        // Usamos AddCategory para cada categoría (más explícito y compatible con SDK)
                        foreach (var cat in cats)
                        {
                            try
                            {
                                // En las versiones más recientes AddCategory está disponible
                                msg.AddCategory(cat);
                            }
                            catch (MissingMethodException)
                            {
                                // Si por alguna razón AddCategory no existe en tu versión del SDK, fallback a asignar la propiedad Categories
                                if (msg.Categories == null) msg.Categories = new List<string>();
                                if (!msg.Categories.Contains(cat)) msg.Categories.Add(cat);
                            }
                            catch (Exception)
                            {
                                // Si AddCategory lanza cualquier otra excepción, intentamos el fallback igualmente
                                if (msg.Categories == null) msg.Categories = new List<string>();
                                if (!msg.Categories.Contains(cat)) msg.Categories.Add(cat);
                            }
                        }
                    }
                }

                // También puedes usar custom_args si necesitas metadata adicional:
                // msg.CustomArgs = new Dictionary<string, string> { { "source", "dashboard" } };

                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

                if (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    // Try to read response body for debugging
                    string body = string.Empty;
                    try { body = await response.Body.ReadAsStringAsync().ConfigureAwait(false); } catch { body = "<no-body>"; }
                    _logger.LogError("SendGrid send failed. Status: {Status}. Body: {Body}", response.StatusCode, body);
                    throw new InvalidOperationException($"SendGrid failed to send email: {response.StatusCode} - {body}");
                }

                _logger.LogInformation("SendGrid email sent to {Email} (subject: {Subject}) Categories: {Cats}", email, subject, msg.Categories != null ? string.Join(",", msg.Categories) : "<none>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending email via SendGrid to {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Sends an email using a SendGrid Dynamic Template.
        /// </summary>
        public async Task SendDynamicTemplateAsync(string email, string templateId, object templateData, IEnumerable<string> categories = null)
        {
            ArgumentNullException.ThrowIfNull(templateData);
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("SendGridEmailSender: email is null/empty - skipping dynamic template send.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_sendGridApiKey))
            {
                _logger.LogWarning("SendGrid API key not configured. Dynamic template email was not sent. TemplateId: {TemplateId}", templateId);
                return;
            }

            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(email);
                var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);

                var allCategories = new List<string>(_defaultCategories);
                if (categories != null)
                {
                    allCategories.AddRange(categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()));
                }

                foreach (var cat in allCategories.Distinct())
                {
                    msg.AddCategory(cat);
                }

                var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

                if (response.StatusCode != System.Net.HttpStatusCode.Accepted && response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    string body = string.Empty;
                    try { body = await response.Body.ReadAsStringAsync().ConfigureAwait(false); } catch { body = "<no-body>"; }
                    _logger.LogError("SendGrid dynamic template send failed. Status: {Status}. Body: {Body}", response.StatusCode, body);
                    throw new InvalidOperationException($"SendGrid failed to send dynamic template email: {response.StatusCode} - {body}");
                }

                _logger.LogInformation("SendGrid dynamic template sent to {Email} (templateId: {TemplateId})", email, templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending dynamic template email via SendGrid to {Email}", email);
                throw;
            }
        }

        // Simple helper to build a plain-text preview from html (naive)
        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            try
            {
                var txt = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
                txt = WebUtility.HtmlDecode(txt);
                return txt;
            }
            catch
            {
                return html;
            }
        }

        private static List<string> ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return null;
            return csv.Split(',')
                      .Select(s => s?.Trim())
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .Distinct()
                      .ToList();
        }
    }
}