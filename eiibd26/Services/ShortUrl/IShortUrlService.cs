using System.Threading.Tasks;

namespace eiibd26.Services.ShortUrl
{
    public interface IShortUrlService
    {
        Task<string> CrearAsync(string urlDestino, string? origen = null);
        Task<(string? Url, int? Id)> ResolverAsync(string codigo);
        Task ContarClickAsync(int shortUrlId);
    }
}
