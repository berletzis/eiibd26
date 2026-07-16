using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eiibd26.Security;

/// <summary>
/// Validates that all required application secrets are present at startup.
/// In Production: throws if any critical secret is missing → app does not start.
/// In Development: logs a warning, allows startup to continue.
/// </summary>
internal static class SecretsValidator
{
    /// <summary>
    /// Critical secrets that must be set before the app can operate.
    /// Key = configuration path, Description = human-readable name for error messages.
    /// </summary>
    private static readonly (string Key, string Description)[] CriticalSecrets =
    [
        ("ConnectionStrings:DefaultConnection",  "SQL Server connection string"),
        ("SendGrid:ApiKey",                      "SendGrid API key (email sending)"),
        ("AiAnswer:AnthropicApiKey",             "Anthropic Claude API key (AI answers)"),
        ("VapidKeys:PublicKey",                  "VAPID public key (push notifications)"),
        ("VapidKeys:PrivateKey",                 "VAPID private key (push notifications)"),
        ("Twilio:AccountSid",                    "Twilio Account SID (SMS)"),
        ("Twilio:AuthToken",                     "Twilio Auth Token (SMS)"),
    ];

    /// <summary>
    /// Validates the presence of all critical secrets.
    /// Call this immediately after builder.Build() in Program.cs.
    /// Always writes to Console.Error so the message appears in IIS stdout logs
    /// regardless of whether the ASP.NET Core logging pipeline is ready.
    /// </summary>
    public static void ValidateOrThrow(IConfiguration configuration, IWebHostEnvironment env, ILogger logger)
    {
        var missing = CriticalSecrets
            .Where(s => string.IsNullOrWhiteSpace(configuration[s.Key]))
            .Select(s => $"  - {s.Key}: {s.Description}")
            .ToList();

        if (missing.Count == 0)
        {
            logger.LogInformation("[SecretsValidator] All critical secrets are present.");
            return;
        }

        var messageBody = string.Join(Environment.NewLine, missing);
        var fullMessage =
            $"[SecretsValidator] Missing required configuration secrets:{Environment.NewLine}" +
            $"{messageBody}{Environment.NewLine}" +
            $"Set them as environment variables on the server, using double-underscore " +
            $"separators (e.g. SendGrid__ApiKey). See SECRETS.md.";

        // Always write to stderr/stdout so the message is visible in IIS stdout logs
        // even before the ASP.NET Core logging pipeline is fully initialized.
        Console.Error.WriteLine(fullMessage);

        if (env.IsProduction())
        {
            // Hard fail: production must never run without its secrets.
            logger.LogCritical("[SecretsValidator] FATAL — app cannot start. {Message}", fullMessage);
            throw new InvalidOperationException(
                $"[SecretsValidator] Application cannot start: required secrets are missing.{Environment.NewLine}{messageBody}");
        }

        // Development / Staging: warn but allow startup so developers can onboard incrementally.
        logger.LogWarning("[SecretsValidator] Some secrets are not configured. Affected services will be degraded:{NewLine}{Message}",
            Environment.NewLine, fullMessage);
    }

    /// <summary>
    /// Returns a masked representation of a secret value safe for structured logging.
    /// e.g. "SG.xxxx...xxxx" → "SG.****(masked)****"
    /// </summary>
    public static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "(not set)";
        if (value.Length <= 6) return "***";
        return value[..3] + "****(masked)****" + value[^3..];
    }
}

