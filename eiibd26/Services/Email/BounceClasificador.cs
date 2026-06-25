using eiibd26.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Services.Email
{
    /// <summary>
    /// Clasifica rebotes (bounces) de SendGrid en HARD (permanente) o SOFT (temporal)
    /// y determina, calculándolo AL VUELO desde SendGridEventLog, qué direcciones deben
    /// excluirse de los ENVÍOS de campañas para proteger la reputación de remitente.
    ///
    /// REGLAS (decididas con el equipo):
    ///  - HARD bounce → se excluye SIEMPRE (mientras no haya entrega posterior que lo recupere).
    ///  - SOFT bounce (buzón lleno, timeout) → se excluye solo si rebotó 2+ veces
    ///    y no hubo una entrega exitosa posterior al último rebote.
    ///  - AUTO-CORRECCIÓN: si tras los rebotes hay un 'delivered' más reciente que todos
    ///    ellos, la dirección se considera recuperada y NO se excluye. No hay marca
    ///    persistente en BD — todo se recalcula desde los eventos cada vez.
    ///
    /// La clasificación se hace por el TEXTO del Reason (código SMTP + frase), NO por
    /// BounceType (que SendGrid reporta de forma ambigua: 'blocked'/'bounce').
    ///
    /// IMPORTANTE: este servicio SOLO afecta a los envíos. NO toca UsuarioValidez,
    /// ni el dashboard, ni las estadísticas de campaña.
    /// </summary>
    public class BounceClasificador
    {
        private readonly ApplicationDbContext _db;

        public BounceClasificador(ApplicationDbContext db)
        {
            _db = db;
        }

        public enum BounceClase { Hard, Soft }

        // Mínimo de soft bounces (posteriores a la última entrega) para excluir.
        private const int SoftBouncesParaExcluir = 2;

        // ──────────────────────────────────────────────────────────────────────
        // CLASIFICACIÓN DE UN REASON
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Marcadores PERMANENTES (HARD). Si el Reason contiene cualquiera de estos,
        /// es un rebote permanente: buzón inexistente/deshabilitado, dominio inválido,
        /// o "buzón lleno PERMANENTE/inactivo" (OverQuotaPerm, "permanent exception").
        /// Se revisan ANTES que los marcadores soft para que un "buzón lleno permanente"
        /// no se confunda con el temporal recuperable.
        /// </summary>
        private static readonly string[] MarcadoresHard =
        {
            // Buzón lleno PERMANENTE / cuenta inactiva (Google OverQuotaPerm, Microsoft permanent)
            "overquotaperm", "permanent", "inactive",
            // Buzón inexistente / usuario desconocido
            "does not exist", "no such user", "unknown user", "user does not exist",
            "not a valid mailbox", "mailbox not found",
            // Buzón no disponible / deshabilitado / acceso denegado
            "mailbox unavailable", "disabled", "access denied",
            // Dominio / DNS inválido
            "unable to get mx", "bogon", "unrecognized address",
        };

        /// <summary>
        /// Marcadores TEMPORALES (SOFT). Buzón lleno temporal (recuperable si lo vacían)
        /// y problemas transitorios de red.
        /// </summary>
        private static readonly string[] MarcadoresSoft =
        {
            "overquotatemp", "out of storage", "mailbox full", "quotaexceeded", "quota",
            "i/o timeout", "connection refused", "connection timed out", "timed out",
        };

        /// <summary>
        /// Clasifica un texto de Reason como HARD o SOFT.
        /// Prioridad: marcadores permanentes → marcadores temporales → código SMTP
        /// (5.x.x permanente = HARD, 4.x.x temporal = SOFT) → desconocido = SOFT (conservador).
        /// Clasificar mal un permanente como soft es "seguro" (solo necesita 2 rebotes para
        /// excluirse); clasificar un temporal como hard excluiría de forma permanente algo
        /// recuperable, así que ante la duda se devuelve SOFT.
        /// </summary>
        public static BounceClase Clasificar(string? reason)
        {
            var r = (reason ?? string.Empty).Trim().ToLowerInvariant();

            if (r.Length == 0)
                return BounceClase.Soft; // sin info → conservador

            foreach (var m in MarcadoresHard)
                if (r.Contains(m))
                    return BounceClase.Hard;

            foreach (var m in MarcadoresSoft)
                if (r.Contains(m))
                    return BounceClase.Soft;

            // Sin frase reconocida: clasificar por el código SMTP al inicio del texto.
            // 5.x.x = error permanente → HARD; 4.x.x = temporal → SOFT.
            if (r.StartsWith("5"))
                return BounceClase.Hard;
            if (r.StartsWith("4"))
                return BounceClase.Soft;

            return BounceClase.Soft; // desconocido → conservador
        }

        // ──────────────────────────────────────────────────────────────────────
        // EVENTOS NORMALIZADOS (uso interno)
        // ──────────────────────────────────────────────────────────────────────

        private sealed record EventoRebote(string Email, string EventType, string? Reason, DateTime Timestamp);

        /// <summary>
        /// Trae de SendGridEventLog todos los eventos bounce y delivered, normaliza el email
        /// (lower/trim) y los agrupa por email para evaluación.
        /// </summary>
        private async Task<Dictionary<string, List<EventoRebote>>> CargarEventosAsync()
        {
            var eventos = await _db.SendGridEventLogs
                .Where(l => l.EventType == "bounce" || l.EventType == "delivered")
                .Select(l => new { l.Email, l.EventType, l.Reason, l.Timestamp })
                .ToListAsync();

            return eventos
                .Where(e => !string.IsNullOrWhiteSpace(e.Email))
                .Select(e => new EventoRebote(
                    e.Email.Trim().ToLowerInvariant(),
                    e.EventType,
                    e.Reason,
                    e.Timestamp))
                .GroupBy(e => e.Email)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Dado el historial de eventos de UN email, decide si debe excluirse y por qué.
        /// Devuelve null si NO debe excluirse (sin rebotes pendientes, o recuperado por
        /// entrega posterior, o solo 1 soft bounce).
        /// </summary>
        private static DetalleExclusion? EvaluarEmail(string email, List<EventoRebote> eventos)
        {
            // Última entrega exitosa (si existe).
            DateTime? ultimaEntrega = eventos
                .Where(e => e.EventType == "delivered")
                .Select(e => (DateTime?)e.Timestamp)
                .DefaultIfEmpty(null)
                .Max();

            // Rebotes POSTERIORES a la última entrega (o todos, si nunca hubo entrega).
            var bouncesPendientes = eventos
                .Where(e => e.EventType == "bounce"
                            && (ultimaEntrega == null || e.Timestamp > ultimaEntrega.Value))
                .ToList();

            if (bouncesPendientes.Count == 0)
                return null; // sin rebotes, o todos anteriores a una entrega → recuperado

            var clasificados = bouncesPendientes
                .Select(b => new { b, clase = Clasificar(b.Reason) })
                .ToList();

            var hardCount = clasificados.Count(x => x.clase == BounceClase.Hard);
            var softCount = clasificados.Count(x => x.clase == BounceClase.Soft);

            bool excluirPorHard = hardCount >= 1;
            bool excluirPorSoft = softCount >= SoftBouncesParaExcluir;

            if (!excluirPorHard && !excluirPorSoft)
                return null; // p.ej. un único soft bounce → todavía no se excluye

            // El último rebote (para mostrar fecha + razón legible).
            var ultimoBounce = bouncesPendientes
                .OrderByDescending(b => b.Timestamp)
                .First();

            return new DetalleExclusion
            {
                Email = email,
                Motivo = excluirPorHard ? MotivoExclusion.Hard : MotivoExclusion.SoftReincidente,
                NumeroRebotes = bouncesPendientes.Count,
                UltimoRebote = ultimoBounce.Timestamp,
                RazonLegible = ResumirRazon(ultimoBounce.Reason),
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Conjunto de emails (lower/trim) que deben excluirse de los envíos HOY.
        /// Se cruza por Email (no por UserId, que puede ser null en bounces).
        /// </summary>
        public async Task<HashSet<string>> ObtenerEmailsExcluidosAsync()
        {
            var porEmail = await CargarEventosAsync();
            var excluidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (email, eventos) in porEmail)
            {
                if (EvaluarEmail(email, eventos) != null)
                    excluidos.Add(email);
            }

            return excluidos;
        }

        /// <summary>
        /// Lista detallada (para el panel de transparencia): email, motivo (hard / soft
        /// reincidente), nº de rebotes pendientes, fecha del último rebote y razón legible.
        /// Ordenada: primero los HARD (permanentes), luego por fecha de último rebote desc.
        /// </summary>
        public async Task<List<DetalleExclusion>> ObtenerDetalleExcluidosAsync()
        {
            var porEmail = await CargarEventosAsync();
            var detalle = new List<DetalleExclusion>();

            foreach (var (email, eventos) in porEmail)
            {
                var d = EvaluarEmail(email, eventos);
                if (d != null)
                    detalle.Add(d);
            }

            return detalle
                .OrderBy(d => d.Motivo == MotivoExclusion.Hard ? 0 : 1)
                .ThenByDescending(d => d.UltimoRebote)
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Recorta la razón SMTP a algo legible para la UI (quita los identificadores
        /// de servidor tipo "... - gsmtp" y trunca a longitud razonable).
        /// </summary>
        private static string ResumirRazon(string? reason)
        {
            var r = (reason ?? string.Empty).Trim();
            if (r.Length == 0)
                return "(sin detalle)";

            // Cortar antes del id de mensaje del servidor (ruido para el admin).
            var corte = r.IndexOf(" - gsmtp", StringComparison.OrdinalIgnoreCase);
            if (corte > 0) r = r[..corte];

            corte = r.IndexOf("Please direct the recipient", StringComparison.OrdinalIgnoreCase);
            if (corte > 0) r = r[..corte].TrimEnd();

            return r.Length > 160 ? r[..160].TrimEnd() + "…" : r;
        }

        public enum MotivoExclusion { Hard, SoftReincidente }

        public class DetalleExclusion
        {
            public string Email { get; set; } = string.Empty;
            public MotivoExclusion Motivo { get; set; }
            public int NumeroRebotes { get; set; }
            public DateTime UltimoRebote { get; set; }
            public string RazonLegible { get; set; } = string.Empty;

            /// <summary>Etiqueta corta para la UI.</summary>
            public string MotivoTexto => Motivo == MotivoExclusion.Hard
                ? "Permanente (Hard)"
                : "Temporal reincidente (Soft)";
        }
    }
}
