using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace eiibd26.Areas.Identity.Pages.Admin.ApiKeys
{
    /// <summary>
    /// Vista de administrador SOLO LECTURA que lista las API keys de servicios
    /// externos leyendo la configuración EFECTIVA (IConfiguration ya resuelve
    /// env vars + web.config sobre appsettings).
    ///
    /// SEGURIDAD: el valor completo de la key NUNCA sale del backend. Se calcula
    /// aquí un prefijo enmascarado y solo eso viaja al HTML. No se loguean las keys
    /// ni se pasan por URL/querystring.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>Fila de la tabla — solo contiene datos ya seguros para el cliente.</summary>
        public record ApiKeyRow(
            string Servicio,
            string ConfigPath,
            string Prefijo,
            bool Configurada,
            int Longitud,
            string? Nota);

        public List<ApiKeyRow> Rows { get; } = new();

        public void OnGet()
        {
            // Anthropic (NINA) — clave real usada por AnthropicClient
            Rows.Add(Build("Anthropic (NINA)", "AiAnswer:AnthropicApiKey", prefixLen: 6));

            // Anthropic (GRIS) — GRIS reutiliza el mismo AnthropicClient; solo
            // sobrescribe el modelo vía Gris:Model. NO tiene key propia.
            Rows.Add(Build("Anthropic (GRIS)", "AiAnswer:AnthropicApiKey", prefixLen: 6,
                nota: "Misma key que NINA (GRIS solo cambia el modelo con Gris:Model)."));

            // Voyage — embeddings del Motor de Cobertura (Web + Worker). Vive fuera de appsettings
            // (user-secrets/env); por eso se lee de la config efectiva igual que las demás.
            Rows.Add(Build("Voyage (embeddings)", "Voyage:ApiKey", prefixLen: 6));

            // SendGrid — envío de email
            Rows.Add(Build("SendGrid", "SendGrid:ApiKey", prefixLen: 6));

            // Twilio — SMS. El Account SID es menos sensible pero se enmascara por consistencia.
            Rows.Add(Build("Twilio (Account SID)", "Twilio:AccountSid", prefixLen: 6));
            Rows.Add(Build("Twilio (Auth Token)", "Twilio:AuthToken", prefixLen: 6));

            // Google Maps — API key de Maps
            Rows.Add(Build("Google Maps", "GoogleMaps:ApiKey", prefixLen: 6));

            // VAPID Push — más cortas: prefijo de 4. La pública es publicable por diseño,
            // pero se enmascara igual por consistencia visual.
            Rows.Add(Build("VAPID Push (pública)", "VapidKeys:PublicKey", prefixLen: 4));
            Rows.Add(Build("VAPID Push (privada)", "VapidKeys:PrivateKey", prefixLen: 4));
        }

        /// <summary>
        /// Lee la config, calcula prefijo enmascarado + longitud, y descarta el valor completo.
        /// El valor completo jamás se almacena en el modelo ni se envía al cliente.
        /// </summary>
        private ApiKeyRow Build(string servicio, string configPath, int prefixLen, string? nota = null)
        {
            var value = _config[configPath];
            var present = !string.IsNullOrWhiteSpace(value);

            string prefijo;
            int longitud;

            if (!present)
            {
                prefijo = "—";
                longitud = 0;
            }
            else
            {
                var v = value!;
                longitud = v.Length;
                // Si la key es más corta que el prefijo pedido, no revelar más de lo prudente.
                var take = v.Length <= prefixLen ? 1 : prefixLen;
                prefijo = v.Substring(0, take) + "••••••••••";
            }

            return new ApiKeyRow(servicio, configPath, prefijo, present, longitud, nota);
        }
    }
}
