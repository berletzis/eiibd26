using Microsoft.EntityFrameworkCore;
using NINA_WorkerService;
using NINA_WorkerService.Data;

var builder = Host.CreateApplicationBuilder(args);

// Leer cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrar DbContext con SqlServer
builder.Services.AddDbContext<Eiibd26Context>(options =>
    options.UseSqlServer(connectionString));

// Registrar el Worker
builder.Services.AddHostedService<ScrapingWorker>();

var host = builder.Build();
host.Run();