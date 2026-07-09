using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NINA_WorkerService.Services;

/// <summary>
/// Filtro de exclusión de URLs del crawler (denylist) y coincidencia de la allowlist de sub-sitemaps.
/// Semántica predecible y segura:
///   - case-insensitive;
///   - prefijo "re:" =&gt; expresión regular (regex inválida =&gt; NO excluye, fail-open);
///   - si no, substring por defecto, con glob '*' (cualquier secuencia);
///   - se evalúa contra el path+query de la URL (o la URL del sub-sitemap en la allowlist).
/// No introduce dependencias nuevas (solo System.Text.RegularExpressions). El RobotsMatcher queda
/// aparte y autoritativo: su semántica está anclada a robots.txt y no se relaja aquí.
/// </summary>
public static class UrlExclusion
{
    /// <summary>Primer patrón de <paramref name="patrones"/> que excluye <paramref name="objetivo"/>, o null si ninguno.</summary>
    public static string? PatronQueExcluye(string objetivo, IReadOnlyList<string> patrones)
    {
        if (patrones == null || patrones.Count == 0 || string.IsNullOrEmpty(objetivo)) return null;
        foreach (var p in patrones)
        {
            var patron = p?.Trim();
            if (string.IsNullOrEmpty(patron)) continue;
            if (Coincide(objetivo, patron!)) return patron;
        }
        return null;
    }

    /// <summary>¿<paramref name="objetivo"/> coincide con <paramref name="patron"/> (substring / glob '*' / regex 're:')?</summary>
    public static bool Coincide(string objetivo, string patron)
    {
        if (patron.StartsWith("re:", StringComparison.OrdinalIgnoreCase))
        {
            var rx = patron.Substring(3);
            try { return Regex.IsMatch(objetivo, rx, RegexOptions.IgnoreCase); }
            catch (ArgumentException) { return false; } // regex inválida => fail-open (nunca excluye por accidente)
        }

        if (patron.IndexOf('*') >= 0)
        {
            // glob: escapar literal y convertir '*' -> '.*'; sin anclar => coincide en cualquier parte.
            var rx = Regex.Escape(patron).Replace("\\*", ".*");
            try { return Regex.IsMatch(objetivo, rx, RegexOptions.IgnoreCase); }
            catch (ArgumentException) { return false; }
        }

        // substring por defecto (contains, case-insensitive).
        return objetivo.Contains(patron, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Valida un patrón. Solo los 're:' pueden ser inválidos (regex mal formada). Devuelve false y
    /// el mensaje de error para que el llamador lo loguee (fail-open: el patrón inválido se ignora).
    /// </summary>
    public static bool PatronValido(string? patron, out string? error)
    {
        error = null;
        if (patron != null && patron.Trim().StartsWith("re:", StringComparison.OrdinalIgnoreCase))
        {
            try { _ = Regex.Match(string.Empty, patron.Trim().Substring(3)); }
            catch (ArgumentException ex) { error = ex.Message; return false; }
        }
        return true;
    }
}
