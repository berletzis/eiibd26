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

// Registrar el Worker
builder.Services.AddHostedService<ScrapingWorker>();

var host = builder.Build();
host.Run();