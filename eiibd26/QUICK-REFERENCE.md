# 🚀 AI FIRST ANSWER - QUICK REFERENCE CARD

## 📦 Installation (15 minutes)

```bash
# 1. Install Hangfire
cd eiibd26
dotnet add package Hangfire.Core --version 1.8.12
dotnet add package Hangfire.SqlServer --version 1.8.12
dotnet add package Hangfire.AspNetCore --version 1.8.12

# 2. Run migration
dotnet ef migrations add AddAiAnswerFields
dotnet ef database update

# 3. Build & Run
dotnet build
dotnet run
```

## ⚙️ Configuration (appsettings.json)

```json
{
  "AiAnswer": {
    "Enabled": true,
    "AnthropicApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,
    "MaxTokens": 600,
    "SystemUserId": "GUID-from-SETUP-SYSTEM-USER.sql"
  }
}
```

## 🔧 Code to Uncomment (After Hangfire Install)

### Jobs/AiAnswerJob.cs (Line 5)
```csharp
using Hangfire;
```

### Jobs/AiAnswerJob.cs (Line 42)
```csharp
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
```

### Controllers/PreguntasApiController.cs (Top)
```csharp
using Hangfire;
private readonly IBackgroundJobClient _backgroundJobClient;
```

### Controllers/PreguntasApiController.cs (Constructor)
```csharp
IBackgroundJobClient backgroundJobClient
_backgroundJobClient = backgroundJobClient;
```

### Controllers/PreguntasApiController.cs (CrearPregunta)
```csharp
_backgroundJobClient.Enqueue<eiibd26.Jobs.AiAnswerJob>(
    job => job.ProcesarPreguntaAsync(pregunta.Id));
```

## 📊 Monitoring

```
Dashboard: https://localhost:7002/hangfire
```

## 💰 Costs

| Volume | Cost/Month |
|--------|------------|
| 100 | $1.07 |
| 1,000 | $10.65 |
| 10,000 | $106.50 |

## 🔍 Troubleshooting

```sql
-- Ver respuestas IA
SELECT * FROM Respuestas WHERE EsIA = 1;

-- Ver jobs fallidos
SELECT * FROM [Hangfire].[State] WHERE Name = 'Failed';

-- Verificar system user
SELECT * FROM AspNetUsers WHERE Email = 'sistema-ia@eiibd.com';
```

## 📚 Full Docs

1. `README-NEXTSTEPS.md` - Start here
2. `INSTALLATION-GUIDE.md` - Complete guide
3. `AI-ARCHITECTURE-SUMMARY.md` - Technical details
4. `IMPLEMENTATION-COMPLETE.md` - Full summary

## ✅ Checklist

- [ ] Hangfire packages installed
- [ ] Code uncommented
- [ ] Program.cs configured
- [ ] DB migration run
- [ ] System user created
- [ ] appsettings.json configured
- [ ] Application runs
- [ ] Hangfire dashboard accessible
- [ ] Test question created
- [ ] AI answer generated

---

**Get Anthropic API Key:** https://console.anthropic.com/

**Support:** See `INSTALLATION-GUIDE.md` troubleshooting section
