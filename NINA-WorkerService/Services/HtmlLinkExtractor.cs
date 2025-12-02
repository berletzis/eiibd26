using HtmlAgilityPack;
using System.Net;

namespace NINA_WorkerService.Services;

public static class HtmlLinkExtractor
{
    public static IEnumerable<string> ExtractLinks(string html, Uri baseUri)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(a => a.GetAttributeValue("href", null))
            ?? Enumerable.Empty<string>();

        foreach (var href in links)
        {
            if (string.IsNullOrWhiteSpace(href))
                continue;

            // Ignorar anchors y javascript:
            if (href.StartsWith("#") || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                continue;

            // Des-escapar entidades HTML (&amp; -> &, etc.)
            var decodedHref = WebUtility.HtmlDecode(href);

            Uri? absoluteUri = null;

            if (Uri.TryCreate(decodedHref, UriKind.Absolute, out var abs))
            {
                absoluteUri = abs;
            }
            else if (Uri.TryCreate(baseUri, decodedHref, out var relToAbs))
            {
                absoluteUri = relToAbs;
            }

            if (absoluteUri == null)
                continue;

            // Quitar fragmento (#...)
            var builder = new UriBuilder(absoluteUri)
            {
                Fragment = string.Empty
            };

            yield return builder.Uri.ToString();
        }
    }
}