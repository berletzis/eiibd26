using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano (Motor de Cobertura Fase 5) que calcula la similitud de coseno entre
    /// embeddings densos. Resuelve dependencias scoped en su propio scope (patrón SimilitudJob).
    /// Reanudable e idempotente: el servicio procesa por lotes y salta pares sin cambios.
    /// </summary>
    public class SimilitudEmbeddingJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimilitudEmbeddingJob> _logger;

        public SimilitudEmbeddingJob(IServiceScopeFactory scopeFactory, ILogger<SimilitudEmbeddingJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task CalcularAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISimilitudEmbeddingService>();

            var guardados = await svc.CalcularAsync();
            _logger.LogInformation("[SimEmbeddingJob] Corrida finalizada. Pares guardados/refrescados: {Count}.", guardados);
        }
    }
}
