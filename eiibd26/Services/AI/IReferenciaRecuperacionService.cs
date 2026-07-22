using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Una referencia CANDIDATA recuperada del índice del crawler: link REAL (nunca inventado),
    /// de un dominio de confianza. "Parecido por significado" ≠ "respalda la afirmación" → el humano
    /// la valida antes de publicar. El modelo no participa: esto sale de embeddings + coseno.
    /// </summary>
    public record ReferenciaCandidataDto(string Titulo, string Url, string Sitio, double Score, int Porcentaje);

    /// <summary>
    /// Nivel 1 del REQ de referencias por recuperación: dado el tema de una nota, embebe la consulta
    /// y busca en el índice crawleado (ScrapedPage.Embedding) las páginas REALES de dominios de
    /// confianza más similares. Devuelve candidatas (título + URL reales) para que el editor confirme.
    /// Best-effort: si Voyage no tiene key, no hay dominios, o no hay nada por encima del umbral,
    /// devuelve lista vacía (sin inventar nada).
    /// </summary>
    public interface IReferenciaRecuperacionService
    {
        Task<List<ReferenciaCandidataDto>> RecuperarAsync(string consulta, CancellationToken cancellationToken = default);
    }
}
