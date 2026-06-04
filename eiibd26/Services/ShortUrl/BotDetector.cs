using System;
using System.Text.RegularExpressions;

namespace eiibd26.Services.ShortUrl
{
    internal static class BotDetector
    {
        private static readonly string[] KnownBots =
        {
            "facebookexternalhit",
            "WhatsApp",
            "Twitterbot",
            "Slackbot",
            "TelegramBot",
            "LinkedInBot",
            "Discordbot",
            "Googlebot",
            "bingbot",
            "Applebot",
            "redditbot",
            "Pinterest",
            "SkypeUriPreview",
            "vkShare",
            "W3C_Validator",
            "Embedly",
        };

        private static readonly Regex GenericBotPattern = new(
            @"\b(bot|crawler|spider|preview)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsBot(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return true;

            foreach (var known in KnownBots)
            {
                if (userAgent.Contains(known, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return GenericBotPattern.IsMatch(userAgent);
        }
    }
}
