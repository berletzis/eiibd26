namespace eiibd26.Voyage
{
    /// <summary>
    /// Configuración del cliente de embeddings de Voyage. Cada app (Web / Worker) la construye
    /// desde su propia <c>IConfiguration</c> (sección <c>Voyage</c>) y la pasa al cliente. La
    /// biblioteca NO depende de Microsoft.Extensions.* (se mantiene mínima, como eiibd26.Firma).
    /// </summary>
    public sealed class VoyageOptions
    {
        /// <summary>Valor centinela: si la key es esto (o vacía), el cliente queda deshabilitado.</summary>
        public const string Placeholder = "SET_IN_ENVIRONMENT_OR_SECRETS";

        /// <summary>API key de Voyage. NUNCA hardcodeada — viene de user-secrets / variable de entorno.</summary>
        public string? ApiKey { get; init; }

        /// <summary>Modelo multilingüe. Se persiste junto al vector (EmbeddingModelo) para detectar vectores obsoletos.</summary>
        public string Model { get; init; } = "voyage-4-large";

        /// <summary>Base del endpoint. Sin barra final.</summary>
        public string BaseUrl { get; init; } = "https://api.voyageai.com/v1";

        /// <summary>
        /// input_type de Voyage. "document" para TODOS (propios y externos) → similitud simétrica
        /// artículo↔artículo (lo que validó el experimento: gemelo #1 a 0.90).
        /// </summary>
        public string InputType { get; init; } = "document";

        /// <summary>Dimensiones de salida. null = default del modelo (1024 en voyage-4-large).</summary>
        public int? OutputDimension { get; init; }

        /// <summary>Timeout por request HTTP.</summary>
        public int TimeoutSeconds { get; init; } = 60;

        /// <summary>Reintentos ante 429 / 5xx (backoff exponencial).</summary>
        public int MaxRetries { get; init; } = 5;

        /// <summary>Backoff base (segundos) para el primer reintento; luego crece exponencial.</summary>
        public int RetryBaseSeconds { get; init; } = 2;

        /// <summary>
        /// Callback opcional de diagnóstico (el llamador enchufa su ILogger). Evita que la
        /// biblioteca dependa de Microsoft.Extensions.Logging.
        /// </summary>
        public Action<string>? Warn { get; init; }

        internal string BaseUrlTrimmed => BaseUrl.TrimEnd('/');

        /// <summary>True si hay una API key real (no vacía ni placeholder).</summary>
        public bool TieneKey => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != Placeholder;
    }
}
