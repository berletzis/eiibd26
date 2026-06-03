using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace eiibd26.Controllers;

/// <summary>
/// Recibe eventos del Event Webhook de SendGrid.
/// URL a configurar en SendGrid: https://eiibd.com/api/sendgrid/events
///
/// Decisión sobre firma ausente: si WebhookPublicKey no está configurada,
/// se procesa igualmente con un warning — permite recibir eventos durante
/// el setup inicial sin perderlos. En producción siempre debe estar configurada.
///
/// Custom args esperados (inyectados en F1):
///   "userId"     → UserId del usuario
///   "campana"    → CampanaCodigo ("reactivacion", "general")
///   "fase"       → FaseLog como string
///   "templateId" → ID del template (solo campaña general)
///   "envio_ts"   → ISO 8601 del momento del envío
/// </summary>
[ApiController]
[Route("api/sendgrid")]
public class SendGridWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridWebhookController> _logger;

    public SendGridWebhookController(ApplicationDbContext db, IConfiguration config, ILogger<SendGridWebhookController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/sendgrid/events — punto de entrada del Event Webhook.
    /// SendGrid envía un array JSON de hasta ~1000 eventos por request.
    /// Responde 200 siempre que llegue a parsear (SendGrid reintenta si no recibe 2xx).
    /// </summary>
    [HttpPost("events")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReceiveEvents()
    {
        // 1. Leer body RAW antes de cualquier model binding.
        //    Se lee directamente del stream — no se usa [FromBody] para poder
        //    tener el string crudo que necesita la verificación de firma ECDSA.
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false))
        {
            rawBody = await reader.ReadToEndAsync();
        }

        // 2. Validar firma ECDSA
        var publicKey = _config["SendGrid:WebhookPublicKey"];

        if (!string.IsNullOrWhiteSpace(publicKey))
        {
            var signature = Request.Headers["X-Twilio-Email-Event-Webhook-Signature"].ToString();
            var timestamp = Request.Headers["X-Twilio-Email-Event-Webhook-Timestamp"].ToString();

            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
            {
                _logger.LogWarning("[Webhook] Headers de firma ausentes (X-Twilio-Email-Event-Webhook-Signature / Timestamp). Rechazando.");
                return Unauthorized();
            }

            if (!VerifyEcdsaSignature(publicKey, rawBody, signature, timestamp))
            {
                _logger.LogWarning("[Webhook] Firma ECDSA inválida. Posible falsificación desde {IP}.", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }

            _logger.LogDebug("[Webhook] Firma ECDSA verificada.");
        }
        else
        {
            // Sin key configurada: procesar con warning para no perder eventos en setup inicial.
            // IMPORTANTE: configurar SendGrid:WebhookPublicKey antes de ir a producción.
            _logger.LogWarning("[Webhook] SendGrid:WebhookPublicKey vacía — procesando sin validar firma. Configure la clave pública en producción.");
        }

        // 3. Parsear y guardar eventos
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            _logger.LogDebug("[Webhook] Body vacío, ignorando.");
            return Ok();
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("[Webhook] Payload no es un array JSON. ValueKind={Kind}", doc.RootElement.ValueKind);
                return Ok(); // 200 de todas formas para no generar retries
            }

            var logs = new List<SendGridEventLog>();

            foreach (var evt in doc.RootElement.EnumerateArray())
            {
                var eventType = Str(evt, "event") ?? "unknown";
                var email = Str(evt, "email") ?? string.Empty;

                // Timestamp unix → DateTime UTC
                var unixTs = evt.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == JsonValueKind.Number
                    ? tsProp.GetInt64() : 0L;
                var eventTs = unixTs > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(unixTs).UtcDateTime
                    : DateTime.UtcNow;

                // Custom args inyectados por F1 — llegan como campos al nivel raíz del evento
                var faseStr = Str(evt, "fase");
                int? faseLog = int.TryParse(faseStr, out var fp) ? fp : null;

                logs.Add(new SendGridEventLog
                {
                    EventType = eventType,
                    Timestamp = eventTs,
                    SgMessageId = Str(evt, "sg_message_id"),
                    Email = email,

                    UserId = Str(evt, "userId"),
                    FaseLog = faseLog,
                    CampanaCodigo = Str(evt, "campana"),
                    TemplateId = Str(evt, "templateId"),
                    EnvioTimestamp = Str(evt, "envio_ts"),

                    Url = Str(evt, "url"),
                    Reason = Str(evt, "reason"),
                    BounceType = Str(evt, "type"),   // solo bounce events lo tienen; null para el resto

                    FechaRegistro = DateTime.UtcNow,
                    RawJson = evt.GetRawText()
                });
            }

            _db.SendGridEventLogs.AddRange(logs);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Webhook] {Count} eventos guardados.", logs.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[Webhook] Error parseando JSON del body.");
            return Ok(); // 200 de todas formas — evitar retries de SendGrid
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Webhook] Error guardando eventos en BD.");
            return Ok(); // 200 de todas formas
        }

        return Ok();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Verificación ECDSA P-256 SHA-256 (SendGrid Signed Event Webhook)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica la firma ECDSA del webhook usando System.Security.Cryptography.ECDsa.
    /// SendGrid firma: ECDSA-P256-SHA256(timestamp + rawBody).
    /// Formato de firma: detectado por longitud — 64 bytes = IEEE P1363 (raw r||s),
    /// otro tamaño = DER (SEQUENCE ASN.1, típicamente 70-72 bytes para P-256).
    /// La public key llega como Base64 de SubjectPublicKeyInfo (SPKI/DER).
    /// </summary>
    private bool VerifyEcdsaSignature(string publicKeyBase64, string rawBody, string signatureBase64, string timestamp)
    {
        try
        {
            // ECDSA signed data: UTF-8(timestamp) + UTF-8(rawBody)
            var payload = Encoding.UTF8.GetBytes(timestamp + rawBody);
            var sigBytes = Convert.FromBase64String(signatureBase64);
            var pubKeyBytes = Convert.FromBase64String(publicKeyBase64);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(pubKeyBytes, out _);

            // Auto-detección de formato de firma:
            // P-256 IEEE P1363 → exactamente 64 bytes (r=32 + s=32)
            // P-256 DER        → 70-72 bytes (SEQUENCE { INTEGER r, INTEGER s })
            var format = sigBytes.Length == 64
                ? DSASignatureFormat.IeeeP1363FixedFieldConcatenation
                : DSASignatureFormat.Rfc3279DerSequence;

            return ecdsa.VerifyData(payload, sigBytes, HashAlgorithmName.SHA256, format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Webhook] Excepción verificando firma ECDSA.");
            return false;
        }
    }

    // ── Helper: leer string de JsonElement o null si no existe / no es string ──
    private static string? Str(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }
}
