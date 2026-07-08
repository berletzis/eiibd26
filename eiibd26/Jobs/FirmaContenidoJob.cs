using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eiibd26.Jobs
{
    /// <summary>
    /// Job en segundo plano (Motor de Cobertura Fase 1) que firma los contenidos
    /// publicados pendientes. Resuelve sus dependencias scoped en un scope propio
    /// porque el job vive fuera del request (mismo patrón que CampanaEnvioJob).
    /// Reanudable: FirmarPendientesAsync solo procesa los que tienen Firma IS NULL,
    /// así que re-encolar retoma únicamente lo que falta.
    /// </summary>
    public class FirmaContenidoJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FirmaContenidoJob> _logger;

        public FirmaContenidoJob(IServiceScopeFactory scopeFactory, ILogger<FirmaContenidoJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task FirmarPendientesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var firmaService = scope.ServiceProvider.GetRequiredService<IFirmaService>();

            var firmados = await firmaService.FirmarPendientesAsync();
            _logger.LogInformation("[FirmaJob] Corrida finalizada. Contenidos firmados: {Count}.", firmados);
        }
    }
}
