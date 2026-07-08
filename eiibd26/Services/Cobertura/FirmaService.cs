using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Glossary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Cobertura
{
    /// <inheritdoc cref="IFirmaService"/>
    public class FirmaService : IFirmaService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FirmaService> _logger;

        // Versión del formato de firma (para migrar/comparar en fases posteriores).
        private const int FirmaVersion = 1;

        // Publicado + Destacado + Archivado (Domain.Constants.EstadoPublicacion.Visibles).
        private static readonly int[] EstadosVisibles = { 1, 2, 3 };

        // Reutiliza el mismo enfoque que los StripHtml del repo (regex de tags).
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        // Deja solo [a-z0-9] y espacios; el resto (signos, tildes ya removidas) → espacio.
        private static readonly Regex NoAlfaNumRegex = new("[^a-z0-9\\s]", RegexOptions.Compiled);
        private static readonly Regex EspaciosRegex = new("\\s+", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Lotes chicos: los cuerpos HTML pueden ser grandes (imágenes base64, etc.).
        private const int BatchSize = 25;

        public FirmaService(ApplicationDbContext db, ILogger<FirmaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Firma serializada
        // ─────────────────────────────────────────────────────────────────────

        private sealed class FirmaDto
        {
            public int V { get; set; }
            public int TotalTokens { get; set; }
            public Dictionary<string, int> Counts { get; set; } = new();
        }

        // ─────────────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────────────

        public async Task<int> FirmarPendientesAsync(CancellationToken ct = default)
        {
            // FASE 1A — vocabulario cargado UNA vez por corrida (cache en memoria local).
            var vocab = await CargarVocabularioAsync(ct);
            if (vocab.Count == 0)
            {
                _logger.LogWarning("[Firma] Vocabulario EII vacío (0 términos Directa activos). Nada que firmar.");
                return 0;
            }

            _logger.LogInformation("[Firma] Vocabulario cargado: {Count} términos EII (relación Directa).", vocab.Count);

            int firmados = 0;

            // Reanudable: cada lote re-consulta Firma == null. Los recién firmados (ya guardados)
            // salen del filtro, así que el while converge sin repetir ni entrar en bucle infinito.
            while (!ct.IsCancellationRequested)
            {
                var lote = await _db.Contenidos
                    .Where(c => !c.Eliminado
                                && c.EstadoPublicacion.HasValue
                                && EstadosVisibles.Contains(c.EstadoPublicacion.Value)
                                && c.Firma == null)
                    .OrderBy(c => c.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (lote.Count == 0) break;

                foreach (var c in lote)
                {
                    c.Firma = CalcularFirmaJson(c.ContenidoTitulo, c.ContenidoTextoL, vocab);
                    c.FirmaCalculadaEn = DateTime.Now;
                    firmados++;
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("[Firma] Corrida completa: {Count} contenidos firmados.", firmados);
            return firmados;
        }

        public async Task<(int total, int firmados)> ObtenerProgresoAsync(CancellationToken ct = default)
        {
            var baseQuery = _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value));

            var total = await baseQuery.CountAsync(ct);
            var firmados = await baseQuery.CountAsync(c => c.Firma != null, ct);
            return (total, firmados);
        }

        public async Task<int> ResetearFirmasAsync(CancellationToken ct = default)
        {
            var afectados = await _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Firma, (string?)null)
                    .SetProperty(c => c.FirmaCalculadaEn, (DateTime?)null), ct);

            _logger.LogInformation("[Firma] Reset total: {Count} firmas puestas a NULL.", afectados);
            return afectados;
        }

        // ─────────────────────────────────────────────────────────────────────
        // FASE 1A — Vocabulario EII
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Vocabulario = GlossaryTerm.Nombre con Activo=1 y relación EII DIRECTA
        /// (MedicalRelationSuggestedId = Directa). Normaliza cada término igual que el texto
        /// y precompila un regex de frase (soporta términos multi-palabra como "colitis ulcerosa").
        /// </summary>
        private async Task<List<(string term, Regex rx)>> CargarVocabularioAsync(CancellationToken ct)
        {
            var nombres = await _db.GlossaryTerms
                .AsNoTracking()
                .Where(t => t.Activo && t.MedicalRelationSuggestedId == MedicalRelationType.Directa)
                .Select(t => t.Nombre)
                .ToListAsync(ct);

            var vocab = new List<(string, Regex)>(nombres.Count);
            var vistos = new HashSet<string>(StringComparer.Ordinal);

            foreach (var nombre in nombres)
            {
                var norm = Normalizar(nombre);
                if (norm.Length == 0) continue;
                if (!vistos.Add(norm)) continue; // evitar duplicados tras normalizar

                // \b...\b sobre texto ya normalizado ([a-z0-9 ]) cuenta la frase completa
                // como unidad, no palabras sueltas.
                var rx = new Regex($@"\b{Regex.Escape(norm)}\b", RegexOptions.Compiled);
                vocab.Add((norm, rx));
            }

            return vocab;
        }

        // ─────────────────────────────────────────────────────────────────────
        // FASE 1B — Cálculo de la firma
        // ─────────────────────────────────────────────────────────────────────

        private static string CalcularFirmaJson(string? titulo, string? cuerpoHtml, List<(string term, Regex rx)> vocab)
        {
            var crudo = $"{titulo} {cuerpoHtml}";
            var plano = WebUtility.HtmlDecode(StripHtml(crudo));
            var norm = Normalizar(plano);

            var totalTokens = norm.Length == 0
                ? 0
                : norm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Disperso: solo términos con conteo > 0 (no guardar 120 ceros).
            var counts = new Dictionary<string, int>();
            foreach (var (term, rx) in vocab)
            {
                var n = rx.Matches(norm).Count;
                if (n > 0) counts[term] = n;
            }

            var dto = new FirmaDto { V = FirmaVersion, TotalTokens = totalTokens, Counts = counts };
            return JsonSerializer.Serialize(dto, JsonOpts);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers de limpieza (mismo enfoque que StripHtml + Normalize(FormD) del repo)
        // ─────────────────────────────────────────────────────────────────────

        private static string StripHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return HtmlTagRegex.Replace(html, " ");
        }

        /// <summary>minúsculas + sin acentos (FormD) + solo [a-z0-9] y espacios colapsados.</summary>
        private static string Normalizar(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var descompuesto = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            foreach (var ch in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var limpio = sb.ToString().Normalize(NormalizationForm.FormC);
            limpio = NoAlfaNumRegex.Replace(limpio, " ");
            limpio = EspaciosRegex.Replace(limpio, " ").Trim();
            return limpio;
        }
    }
}
