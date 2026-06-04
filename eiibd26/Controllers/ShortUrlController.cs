using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using eiibd26.Services.ShortUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace eiibd26.Controllers
{
    [Route("r")]
    [AllowAnonymous]
    [EnableRateLimiting("shorturl")]
    public class ShortUrlController : Controller
    {
        private readonly IShortUrlService _service;
        private readonly IMemoryCache _cache;

        // Sal efímera — en memoria, no persiste. Privacidad-first (sitio de salud).
        private const string DedupSalt = "eiibd-shorturl-dedup-2025";
        private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(30);

        public ShortUrlController(IShortUrlService service, IMemoryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        // Orden de evaluación:
        // 1. Rate limit (middleware — devuelve 429 si excede)
        // 2. Resolver código → URL
        // 3. Filtro de bots → redirige SIN contar
        // 4. Dedup 30s → redirige SIN contar
        // 5. Caso normal → contar + redirigir
        [HttpGet("{codigo}")]
        public async Task<IActionResult> Go(string codigo)
        {
            var (url, id) = await _service.ResolverAsync(codigo);
            if (url is null) return Redirect("/");

            var ua = Request.Headers.UserAgent.ToString();
            if (BotDetector.IsBot(ua))
                return Redirect(url);

            var clientIp = GetClientIp(HttpContext);
            var hash = ComputeHash(clientIp + ua + DedupSalt);
            var cacheKey = $"shorturl-dedup:{codigo}:{hash}";

            if (_cache.TryGetValue(cacheKey, out _))
                return Redirect(url);

            await _service.ContarClickAsync(id!.Value);
            _cache.Set(cacheKey, true, DedupWindow);

            return Redirect(url);
        }

        private static string GetClientIp(HttpContext ctx)
        {
            var xff = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xff))
                return xff.Split(',')[0].Trim();
            return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..16];
        }
    }
}
