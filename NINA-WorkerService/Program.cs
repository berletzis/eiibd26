using Microsoft.EntityFrameworkCore;
using NINA_WorkerService;
using NINA_WorkerService.Data;

var builder = Host.CreateApplicationBuilder(args);

// Leer cadena de conexi�n desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrar DbContext con SqlServer
builder.Services.AddDbContext<Eiibd26Context>(options =>
    options.UseSqlServer(connectionString));

// Traducción EN→ES (Anthropic) para firmar externos en inglés (Fase 2C).
// Key en config del Worker (user-secrets/env), NO hardcodeada.
builder.Services.AddScoped<NINA_WorkerService.Services.ITranslationService,
    NINA_WorkerService.Services.AnthropicTranslationService>();

// Motor de Cobertura — Fase 5 (embeddings): cliente Voyage compartido (eiibd26.Voyage).
// Mismo cliente que el Web; embebe el texto del externo YA en memoria durante el crawl (Fase 4).
// Key en config del Worker (sección "Voyage": user-secrets/env), NUNCA hardcodeada. Sin key → Habilitado=false.
builder.Services.AddSingleton<eiibd26.Voyage.IVoyageEmbeddingClient>(sp =>
{
    var cfg = builder.Configuration.GetSection("Voyage");
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Voyage");
    var opts = new eiibd26.Voyage.VoyageOptions
    {
        ApiKey = cfg["ApiKey"],
        Model = cfg["Model"] ?? "voyage-4-large",
        BaseUrl = cfg["BaseUrl"] ?? "https://api.voyageai.com/v1",
        InputType = cfg["InputType"] ?? "document",
        OutputDimension = cfg.GetValue<int?>("OutputDimension"),
        TimeoutSeconds = cfg.GetValue<int?>("TimeoutSeconds") ?? 60,
        MaxRetries = cfg.GetValue<int?>("MaxRetries") ?? 5,
        Warn = msg => logger.LogWarning("{VoyageMsg}", msg)
    };
    return new eiibd26.Voyage.VoyageEmbeddingClient(opts);
});

// Registrar el Worker
builder.Services.AddHostedService<ScrapingWorker>();

var host = builder.Build();
host.Run();