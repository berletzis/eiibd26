using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using eiibd26.Models.ShortUrl;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.ShortUrl
{
    public class ShortUrlService : IShortUrlService
    {
        private readonly ApplicationDbContext _db;
        private const string Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private const int MaxRetries = 5;

        public ShortUrlService(ApplicationDbContext db) => _db = db;

        public async Task<string> CrearAsync(string urlDestino, string? origen = null)
        {
            if (string.IsNullOrWhiteSpace(urlDestino))
                throw new ArgumentException("La URL destino no puede estar vacía.", nameof(urlDestino));

            for (int i = 0; i < MaxRetries; i++)
            {
                var codigo = GenerarCodigo();
                var existe = await _db.ShortUrls.AnyAsync(s => s.Codigo == codigo);
                if (existe) continue;

                var entry = new Models.ShortUrl.ShortUrl
                {
                    Codigo      = codigo,
                    UrlDestino  = urlDestino,
                    Origen      = origen,
                    FechaCreado = DateTime.UtcNow
                };
                _db.ShortUrls.Add(entry);
                await _db.SaveChangesAsync();
                return codigo;
            }

            throw new InvalidOperationException("No se pudo generar un código único tras varios intentos.");
        }

        public async Task<(string? Url, int? Id)> ResolverAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return (null, null);

            var entry = await _db.ShortUrls
                .FirstOrDefaultAsync(s => s.Codigo == codigo && !s.Eliminado);

            return entry is null ? (null, null) : (entry.UrlDestino, entry.Id);
        }

        public async Task ContarClickAsync(int shortUrlId)
        {
            var entry = await _db.ShortUrls.FindAsync(shortUrlId);
            if (entry is null) return;

            entry.ClickCount++;
            _db.ShortUrlClicks.Add(new ShortUrlClick
            {
                ShortUrlId = shortUrlId,
                FechaClick = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private static string GenerarCodigo()
        {
            var bytes = RandomNumberGenerator.GetBytes(CodeLength);
            return new string(bytes.Select(b => Charset[b % Charset.Length]).ToArray());
        }
    }
}
