using System.Threading.Tasks;

namespace eiibd26.Services.ShortUrl
{
    public interface IShortUrlService
    {
        Task<string> CrearAsync(string urlDestino, string? origen = null);
        Task<string?> ResolverYContarAsync(string codigo);
    }
}
