using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano (Motor de Cobertura Fase 5) que embebe los contenidos publicados
    /// pendientes con Voyage. Resuelve dependencias scoped en su propio scope (patrón
    /// FirmaContenidoJob/SimilitudJob). Reanudable: EmbedPendientesAsync solo procesa los que
    /// tienen Embedding IS NULL, así que re-encolar retoma únicamente lo que falta.
    /// </summary>
    public class EmbeddingContenidoJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmbeddingContenidoJob> _logger;

        public EmbeddingContenidoJob(IServiceScopeFactory scopeFactory, ILogger<EmbeddingContenidoJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task EmbedPendientesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var embebidos = await svc.EmbedPendientesAsync();
            _logger.LogInformation("[EmbeddingJob] Corrida finalizada. Contenidos embebidos: {Count}.", embebidos);
        }
    }
}
