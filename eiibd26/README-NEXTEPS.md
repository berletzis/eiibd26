# ⚠️ IMPORTANT: NEXT STEPS TO COMPLETE INSTALLATION

## Current Status

✅ **COMPLETED:**
- All service interfaces created
- All service implementations created
- Models updated with AI fields
- Configuration classes created
- Background job class created
- Controller modifications prepared
- Documentation complete

❌ **PENDING:**
- Install Hangfire NuGet packages
- Uncomment Hangfire-dependent code
- Configure Program.cs
- Run database migration
- Create system user
- Configure appsettings.json

---

## 🚀 INSTALLATION STEPS (IN ORDER)

### STEP 1: Install Hangfire Packages

Open PowerShell/Terminal in project directory and run:

```powershell
cd eiibd26
dotnet add package Hangfire.Core --version 1.8.12
dotnet add package Hangfire.SqlServer --version 1.8.12
dotnet add package Hangfire.AspNetCore --version 1.8.12
```

### STEP 2: Uncomment Hangfire Code

After packages are installed, make these changes:

#### File: `Jobs/AiAnswerJob.cs`
Line 5: Uncomment
```csharp
using Hangfire;
```

Line 42: Uncomment
```csharp
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
```

#### File: `Controllers/PreguntasApiController.cs`

**Add at top:**
```csharp
using Hangfire;
```

**In constructor parameters, add:**
```csharp
IBackgroundJobClient backgroundJobClient
```

**In constructor body, add:**
```csharp
_backgroundJobClient = backgroundJobClient;
```

**In CrearPregunta method, uncomment lines 84-86:**
```csharp
_backgroundJobClient.Enqueue<eiibd26.Jobs.AiAnswerJob>(
    job => job.ProcesarPreguntaAsync(pregunta.Id));
```

### STEP 3: Configure Program.cs

Open `Program.cs` and add BEFORE `var app = builder.Build();`:

```csharp
// ===== AI SERVICES CONFIGURATION =====
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.SqlServer;

// 1. Configurar opciones de IA
builder.Services.Configure<eiibd26.Configuration.AiAnswerConfiguration>(
    builder.Configuration.GetSection("AiAnswer"));

// 2. Registrar HttpClient para Anthropic
builder.Services.AddHttpClient("AnthropicClient", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IOptions<eiibd26.Configuration.AiAnswerConfiguration>>().Value;
    client.BaseAddress = new Uri(config.ApiBaseUrl);
    client.DefaultRequestHeaders.Add("x-api-key", config.AnthropicApiKey);
    client.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion);
    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
});

// 3. Registrar servicios de IA
builder.Services.AddScoped<eiibd26.Services.AI.IAiAnswerService, eiibd26.Services.AI.AiAnswerService>();
builder.Services.AddScoped<eiibd26.Services.AI.IAiSafetyService, eiibd26.Services.AI.AiSafetyService>();
builder.Services.AddScoped<eiibd26.Services.AI.IAiPromptBuilder, eiibd26.Services.AI.AiPromptBuilder>();
builder.Services.AddScoped<eiibd26.Jobs.AiAnswerJob>();

// 4. Configurar Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.Queues = new[] { "default", "ai" };
});
// ===== FIN AI CONFIGURATION =====
```

Then AFTER `app.UseAuthorization();` add:

```csharp
// Hangfire Dashboard (solo desarrollo o con auth en producción)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
```

### STEP 4: Database Migration

Run EF Core migration:

```powershell
dotnet ef migrations add AddAiAnswerFields
dotnet ef database update
```

Or execute SQL manually from `MIGRATION-AI-FIELDS.sql`

### STEP 5: Create System User

Execute SQL script from `SETUP-SYSTEM-USER.sql`

Copy the generated GUID.

### STEP 6: Configure appsettings.json

Add to `appsettings.json`:

```json
{
  "AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "YOUR-ANTHROPIC-API-KEY-HERE",
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "TimeoutSeconds": 30,
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ApiVersion": "2023-06-01",
    "SystemUserId": "PASTE-GUID-FROM-STEP-5-HERE",
    "ForbiddenPhrases": [
      "aumenta la dosis",
      "suspende el medicamento",
      "tienes cáncer"
    ]
  }
}
```

### STEP 7: Build and Test

```powershell
dotnet build
dotnet run
```

Access:
- Application: https://localhost:7002
- Hangfire Dashboard: https://localhost:7002/hangfire

---

## 📚 DOCUMENTATION

Complete guides available:

1. **`INSTALLATION-GUIDE.md`** - Comprehensive installation guide
2. **`AI-ARCHITECTURE-SUMMARY.md`** - Full architecture documentation
3. **`MIGRATION-AI-FIELDS.sql`** - Database migration script
4. **`SETUP-SYSTEM-USER.sql`** - System user creation script
5. **`INSTALL-AI-PROGRAM-CS.txt`** - Program.cs configuration snippet
6. **`INSTALL-APPSETTINGS-AI.json`** - Configuration template

---

## ⚡ QUICK START (After Hangfire Installation)

1. Install packages (Step 1)
2. Uncomment code (Step 2)
3. Configure Program.cs (Step 3)
4. Run migration (Step 4)
5. Create system user (Step 5)
6. Configure appsettings.json (Step 6)
7. Build & run (Step 7)

Total time: ~15 minutes

---

## ✅ WHAT'S WORKING NOW (Without Hangfire)

- ✅ All models updated
- ✅ All AI services ready
- ✅ Safety filters implemented
- ✅ Prompt builder ready
- ✅ Configuration classes ready
- ✅ GET respuestas endpoint working

## ⏳ WHAT NEEDS HANGFIRE

- ⏳ Background job processing
- ⏳ Automatic AI answer generation
- ⏳ Job retry logic
- ⏳ Hangfire dashboard

---

**NOTE:** The application will compile and run now, but AI answer generation won't work until you complete all installation steps above.

Follow the detailed guide in `INSTALLATION-GUIDE.md` for complete instructions.
