using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano (Motor de Cobertura Fase 3) que calcula la similitud entre firmas.
    /// Resuelve dependencias scoped en su propio scope (patrón CampanaEnvioJob/FirmaContenidoJob).
    /// Reanudable e idempotente: el servicio procesa por lotes y salta pares sin cambios.
    /// </summary>
    public class SimilitudJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimilitudJob> _logger;

        public SimilitudJob(IServiceScopeFactory scopeFactory, ILogger<SimilitudJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task CalcularAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISimilitudService>();

            var guardados = await svc.CalcularAsync();
            _logger.LogInformation("[SimilitudJob] Corrida finalizada. Pares guardados/refrescados: {Count}.", guardados);
        }
    }
}
