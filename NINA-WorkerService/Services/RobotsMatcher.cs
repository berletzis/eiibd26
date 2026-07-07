using System.Text;
using System.Text.RegularExpressions;

namespace NINA_WorkerService.Services;

/// <summary>
/// Matcher de robots.txt propio y correcto. Reemplaza a TurnerSoftware.RobotsExclusionTools 0.9.1,
/// que tenía dos bugs sobre robots.txt reales: (1) parseaba mal user-agents numéricos (ej. "008"),
/// contaminando el grupo "*"; (2) no hacía longest-match, dejando pasar rutas que un Disallow
/// específico prohíbe.
///
/// Implementa el estándar (estilo Google): selección de grupo por user-agent con fallback a "*",
/// longest-match entre Allow/Disallow (gana el patrón más específico; empate a favor de Allow),
/// y comodines "*" (cualquier secuencia) y "$" (fin de URL). Validado con banco de pruebas contra
/// el robots.txt real de mycrohnsandcolitisteam.com antes de cablearse (9/9 casos).
/// </summary>
public sealed class RobotsMatcher
{
    private sealed class Group
    {
        public List<string> UserAgents { get; } = new();
        public List<(bool Allow, string Pattern)> Rules { get; } = new();
        public int? CrawlDelay { get; set; }
    }

    private readonly List<Group> _groups = new();

    /// <summary>Matcher vacío que permite todo (para robots.txt ausente o inaccesible).</summary>
    public static RobotsMatcher AllowAll() => new();

    public static RobotsMatcher Parse(string robotsText)
    {
        var matcher = new RobotsMatcher();
        Group? current = null;
        var lastWasRule = false;

        foreach (var rawLine in robotsText.Replace("\r", "").Split('\n'))
        {
            var line = rawLine;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            line = line.Trim();
            if (line.Length == 0) continue;

            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            var field = line.Substring(0, idx).Trim().ToLowerInvariant();
            var value = line.Substring(idx + 1).Trim();

            switch (field)
            {
                case "user-agent":
                    // Un User-agent que sigue a una regla inicia un grupo nuevo.
                    if (current == null || lastWasRule)
                    {
                        current = new Group();
                        matcher._groups.Add(current);
                    }
                    current.UserAgents.Add(value.ToLowerInvariant());
                    lastWasRule = false;
                    break;
                case "allow":
                    current?.Rules.Add((true, value));
                    lastWasRule = true;
                    break;
                case "disallow":
                    current?.Rules.Add((false, value));
                    lastWasRule = true;
                    break;
                case "crawl-delay":
                    if (current != null && int.TryParse(value, out var cd)) current.CrawlDelay = cd;
                    lastWasRule = true;
                    break;
                default:
                    // sitemap u otros: se ignoran sin romper el grupo actual.
                    break;
            }
        }
        return matcher;
    }

    // Selecciona el grupo aplicable: match más específico (token más largo) por prefijo del UA; si no, "*".
    private Group? SelectGroup(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        Group? best = null;
        var bestLen = -1;
        Group? star = null;
        foreach (var g in _groups)
        {
            foreach (var token in g.UserAgents)
            {
                if (token == "*") { star ??= g; continue; }
                if (ua.StartsWith(token, StringComparison.Ordinal) && token.Length > bestLen)
                {
                    best = g;
                    bestLen = token.Length;
                }
            }
        }
        return best ?? star;
    }

    public bool IsAllowed(string userAgent, string path)
    {
        var group = SelectGroup(userAgent);
        if (group == null) return true; // sin grupo aplicable => permitido

        int allowLen = -1, disallowLen = -1;
        foreach (var (allow, pattern) in group.Rules)
        {
            if (pattern.Length == 0) continue; // "Disallow:" vacío = sin restricción
            if (!PatternMatches(pattern, path)) continue;
            var len = Specificity(pattern);
            if (allow) { if (len > allowLen) allowLen = len; }
            else { if (len > disallowLen) disallowLen = len; }
        }
        if (allowLen < 0 && disallowLen < 0) return true; // ninguna regla matchea
        return allowLen >= disallowLen;                   // longest-match; empate => Allow gana
    }

    public int? GetCrawlDelay(string userAgent) => SelectGroup(userAgent)?.CrawlDelay;

    // Especificidad = longitud del patrón sin contar el "$" de fin.
    private static int Specificity(string pattern) => pattern.EndsWith("$") ? pattern.Length - 1 : pattern.Length;

    // "*" = cualquier secuencia, "$" final = fin de cadena; anclado al inicio del path.
    private static bool PatternMatches(string pattern, string path)
    {
        var sb = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*') sb.Append(".*");
            else if (c == '$' && i == pattern.Length - 1) sb.Append('$');
            else sb.Append(Regex.Escape(c.ToString()));
        }
        return Regex.IsMatch(path, sb.ToString());
    }
}
