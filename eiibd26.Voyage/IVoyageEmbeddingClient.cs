namespace eiibd26.Voyage
{
    /// <summary>Resultado de un batch de embeddings: vectores (en el ORDEN de entrada) + tokens usados.</summary>
    public sealed record VoyageBatch(IReadOnlyList<float[]> Vectores, int TotalTokens);

    /// <summary>
    /// Cliente de embeddings de Voyage (POST /v1/embeddings). Compartido por el Web (propios)
    /// y el Worker (externos) para garantizar el MISMO modelo, params y serialización — igual
    /// que eiibd26.Firma garantiza la misma firma. Sin EF; solo HttpClient + System.Text.Json.
    /// </summary>
    public interface IVoyageEmbeddingClient
    {
        /// <summary>True si hay API key configurada (si no, no se embebe: el llamador lo omite/loguea).</summary>
        bool Habilitado { get; }

        /// <summary>Nombre del modelo activo (para persistir EmbeddingModelo junto al vector).</summary>
        string Modelo { get; }

        /// <summary>
        /// Embebe un batch de textos en UNA request. Devuelve los vectores en el mismo orden de
        /// entrada, o null si está deshabilitado o la llamada falla tras los reintentos.
        /// Voyage admite hasta 1000 inputs por request (y un tope de tokens por request); el
        /// llamador es responsable de trocear lotes grandes.
        /// </summary>
        Task<VoyageBatch?> EmbedAsync(IReadOnlyList<string> textos, CancellationToken ct = default);

        /// <summary>Conveniencia: embebe un solo texto. Devuelve el vector o null.</summary>
        Task<float[]?> EmbedUnoAsync(string texto, CancellationToken ct = default);
    }
}
