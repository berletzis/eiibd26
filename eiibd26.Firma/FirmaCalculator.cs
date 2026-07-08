using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace eiibd26.Firma
{
    /// <summary>
    /// Lógica PURA de firma de cobertura (sin EF ni acceso a datos). Compartida entre el
    /// Web (contenido propio) y el Worker (externos) para que ambas firmas sean IDÉNTICAS
    /// en método: misma normalización, mismo conteo, misma serialización.
    ///
    /// Entrada: texto (título + cuerpo) + vocabulario ya compilado. Salida: JSON de firma.
    /// El vocabulario se pasa como parámetro (cada app lo lee de su BD y lo compila con
    /// <see cref="CompilarVocabulario"/>).
    /// </summary>
    public static class FirmaCalculator
    {
        /// <summary>Versión del formato de firma.</summary>
        public const int FirmaVersion = 1;

        // Mismo enfoque que los StripHtml del repo (regex de tags).
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        // Deja solo [a-z0-9] y espacios; el resto (signos, tildes ya removidas) → espacio.
        private static readonly Regex NoAlfaNumRegex = new("[^a-z0-9\\s]", RegexOptions.Compiled);
        private static readonly Regex EspaciosRegex = new("\\s+", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Normaliza los nombres crudos del vocabulario, elimina duplicados (tras normalizar)
        /// y precompila un regex de frase por término. Preserva el orden de entrada.
        /// </summary>
        public static IReadOnlyList<VocabularioTermino> CompilarVocabulario(IEnumerable<string> nombres)
        {
            var vocab = new List<VocabularioTermino>();
            var vistos = new HashSet<string>(StringComparer.Ordinal);

            foreach (var nombre in nombres)
            {
                var norm = Normalizar(nombre);
                if (norm.Length == 0) continue;
                if (!vistos.Add(norm)) continue;

                var rx = new Regex($@"\b{Regex.Escape(norm)}\b", RegexOptions.Compiled);
                vocab.Add(new VocabularioTermino(norm, rx));
            }

            return vocab;
        }

        /// <summary>
        /// Calcula la firma JSON de un contenido a partir de su título + cuerpo (HTML o texto)
        /// y el vocabulario compilado. Cuenta cada término como frase; guarda solo conteos &gt; 0.
        /// </summary>
        public static string Calcular(string? titulo, string? cuerpo, IReadOnlyList<VocabularioTermino> vocab)
        {
            var crudo = $"{titulo} {cuerpo}";
            var plano = WebUtility.HtmlDecode(StripHtml(crudo));
            var norm = Normalizar(plano);

            var totalTokens = norm.Length == 0
                ? 0
                : norm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Disperso: solo términos con conteo > 0.
            var counts = new Dictionary<string, int>();
            foreach (var t in vocab)
            {
                var n = t.Patron.Matches(norm).Count;
                if (n > 0) counts[t.Termino] = n;
            }

            var dto = new FirmaDto { V = FirmaVersion, TotalTokens = totalTokens, Counts = counts };
            return JsonSerializer.Serialize(dto, JsonOpts);
        }

        /// <summary>Quita tags HTML (deja el texto). No elimina el contenido de &lt;script&gt;/&lt;style&gt;.</summary>
        public static string StripHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return HtmlTagRegex.Replace(html, " ");
        }

        /// <summary>minúsculas + sin acentos (FormD) + solo [a-z0-9] y espacios colapsados.</summary>
        public static string Normalizar(string? input)
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
