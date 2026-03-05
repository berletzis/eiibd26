# 📋 AI FIRST ANSWER SYSTEM - ARCHITECTURE SUMMARY

## 🎯 OVERVIEW

Sistema de respuestas automáticas con IA para plataforma médica de EII (Enfermedad Inflamatoria Intestinal).

**Objetivo:** Que ningún paciente recién diagnosticado vea una pregunta vacía.

**Principio:** La IA genera UNA respuesta educativa inicial SOLO cuando no existen respuestas humanas.

---

## 📐 ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────────────┐
│                        USUARIO                              │
│                          ↓                                  │
│               [Crea Pregunta]                               │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│              CONTROLLER: PreguntasApiController             │
│                                                             │
│  1. Valida datos                                           │
│  2. Guarda pregunta en DB                                  │
│  3. Enqueue background job ← NO BLOQUEA REQUEST           │
│  4. Retorna OK inmediatamente                              │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                  HANGFIRE QUEUE                             │
│                   (Asíncrono)                               │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│               BACKGROUND JOB: AiAnswerJob                   │
│                                                             │
│  1. Verifica si pregunta tiene respuesta IA                │
│  2. Verifica si hay respuestas humanas                     │
│  3. Si ambas son NO → Genera respuesta                     │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│            AI SERVICES LAYER                                │
│                                                             │
│  ┌──────────────────────────────────────────────┐          │
│  │ AiPromptBuilder                             │          │
│  │ - BuildSystemPrompt()                       │          │
│  │ - BuildUserPrompt(pregunta, context?)       │          │
│  └──────────────────────────────────────────────┘          │
│                       ↓                                     │
│  ┌──────────────────────────────────────────────┐          │
│  │ AiAnswerService                             │          │
│  │ - GenerarRespuestaAsync()                   │          │
│  │ - Llama Claude Sonnet 4.5 API              │          │
│  │ - MaxTokens: 600                           │          │
│  │ - Temperature: 0.3                         │          │
│  └──────────────────────────────────────────────┘          │
│                       ↓                                     │
│  ┌──────────────────────────────────────────────┐          │
│  │ AiSafetyService                             │          │
│  │ - ValidarContenido()                        │          │
│  │ - ForbiddenPhrases check                   │          │
│  │ - Regex patterns check                     │          │
│  │ - AgregarDisclaimer()                       │          │
│  └──────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    DATABASE                                 │
│                                                             │
│  INSERT INTO Respuestas                                     │
│  {                                                          │
│    EsIA = true,                                            │
│    ModeloIA = "claude-sonnet-4.5",                         │
│    Cuerpo = "respuesta + disclaimer"                       │
│  }                                                          │
│                                                             │
│  UPDATE Preguntas                                           │
│  SET TieneRespuestaIA = true                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗂️ PROJECT STRUCTURE

```
eiibd26/
├── Configuration/
│   └── AiAnswerConfiguration.cs        # Configuración de IA
│
├── Services/
│   └── AI/
│       ├── IAiAnswerService.cs         # Interface servicio principal
│       ├── AiAnswerService.cs          # Implementación Claude API
│       ├── IAiSafetyService.cs         # Interface seguridad
│       ├── AiSafetyService.cs          # Filtros de contenido
│       ├── IAiPromptBuilder.cs         # Interface builder
│       └── AiPromptBuilder.cs          # Constructor de prompts
│
├── Jobs/
│   └── AiAnswerJob.cs                  # Background job Hangfire
│
├── Controllers/
│   ├── PreguntasApiController.cs       # Crear pregunta + enqueue
│   └── PreguntasApiController.Respuestas.cs  # GET respuestas
│
├── Models/
│   ├── Pregunta.cs                     # + TieneRespuestaIA
│   └── Respuesta.cs                    # + EsIA, ModeloIA, etc.
│
└── DTOs/
    └── CrearPreguntaDto.cs             # Existing
```

---

## 🔑 KEY FEATURES

### 1. **Cost Optimization**
- ✅ Genera respuesta SOLO UNA VEZ por pregunta
- ✅ Almacenado en DB (cached)
- ✅ MaxTokens limitado (600)
- ✅ No regenera a menos que se solicite explícitamente
- ✅ Temperature baja (0.3) = respuestas consistentes

**Costo estimado:** ~$0.01 por respuesta

### 2. **Medical Safety**
- ✅ Filtro de frases prohibidas
- ✅ Regex patterns para detectar consejos peligrosos
- ✅ Fallback response si falla validación
- ✅ Disclaimer automático SIEMPRE agregado
- ✅ No diagnósticos
- ✅ No modificación de tratamientos

### 3. **Performance**
- ✅ Procesamiento asíncrono (no bloquea usuario)
- ✅ Hangfire background jobs
- ✅ Retry automático (2 intentos)
- ✅ Timeout configurado (30s)
- ✅ Índices de DB para queries rápidas

### 4. **User Experience**
- ✅ Respuesta inmediata al crear pregunta (no espera IA)
- ✅ IA genera en 5-10 segundos en segundo plano
- ✅ Frontend puede mostrar "Preparando respuesta..."
- ✅ Orden de respuestas: Aceptadas → Humanas → IA

### 5. **Future Ready**
- ✅ RAG-ready: `BuildUserPrompt(pregunta, context?)`
- ✅ Extensible para múltiples modelos
- ✅ Logging completo
- ✅ Monitoring con Hangfire dashboard

---

## 📊 DATA MODEL CHANGES

### Tabla: `Respuestas`

| Campo         | Tipo              | Descripción                      |
|---------------|-------------------|----------------------------------|
| `EsIA`        | `bit` (bool)      | True si fue generada por IA     |
| `ModeloIA`    | `nvarchar(100)?`  | Ej: "claude-sonnet-4.5"         |
| `EsColapsada` | `bit` (bool)      | Para UI (mostrar colapsada)     |
| `Puntuacion`  | `int`             | Votos (positivos - negativos)   |

### Tabla: `Preguntas`

| Campo                | Tipo              | Descripción                      |
|----------------------|-------------------|----------------------------------|
| `TieneRespuestaIA`   | `bit` (bool)      | True si ya se generó IA         |
| `FechaGeneracionIA`  | `datetimeoffset?` | Cuándo se generó                |

---

## 🔐 SECURITY LAYERS

### Layer 1: Prompt Engineering
```
SYSTEM PROMPT enforces:
- No diagnosis
- No treatment modifications
- No medication dosage
- Educational only
- Empathetic tone
```

### Layer 2: Safety Service
```csharp
ForbiddenPhrases = [
  "aumenta la dosis",
  "suspende el medicamento",
  "tienes cáncer",
  // ...
]

+ Regex patterns for:
- Dosage instructions
- Treatment discontinuation
- Diagnosis statements
```

### Layer 3: Fallback Response
```
Si contenido falla validación →
  Usar respuesta pre-escrita segura
```

### Layer 4: Mandatory Disclaimer
```
"⚠️ Esta respuesta es informativa y educativa.
No reemplaza consulta con profesional médico..."
```

---

## 🔄 FLOW SEQUENCE

### Happy Path (Sin respuestas previas)

```
1. Usuario POST /api/preguntas
   ↓
2. Controller guarda en DB
   ↓
3. Controller enqueue AiAnswerJob
   ↓
4. Response 200 OK (inmediato)
   ↓
5. [Async] Hangfire procesa job
   ↓
6. AiPromptBuilder crea prompts
   ↓
7. AiAnswerService llama Claude API
   ↓
8. AiSafetyService valida respuesta
   ↓
9. Agrega disclaimer
   ↓
10. Guarda Respuesta en DB (EsIA=true)
    ↓
11. Marca Pregunta.TieneRespuestaIA=true
    ↓
12. Job completa exitosamente
```

### Skip Path 1 (Ya tiene respuesta IA)

```
1. Usuario POST /api/preguntas
   ↓
2. Job verifica: TieneRespuestaIA == true
   ↓
3. Job termina (no genera otra)
```

### Skip Path 2 (Ya tiene respuestas humanas)

```
1. Usuario POST /api/preguntas
   ↓
2. Job verifica: COUNT(Respuestas.Where(EsIA=false)) > 0
   ↓
3. Job termina (no genera)
```

---

## 🎨 PROMPT STRUCTURE

### System Prompt (~400 tokens)

```
Eres un asistente educativo especializado en EII.

REGLAS ESTRICTAS:
1. NUNCA proporciones diagnósticos
2. NUNCA sugieras modificar dosis
3. NUNCA aconsejes suspender tratamientos
4. SIEMPRE recomienda consultar médico

ESTRUCTURA DE RESPUESTA:
1. Empatía inicial (1-2 líneas)
2. Información educativa general
3. Cuándo buscar atención médica
4. Sugerencias de autocuidado general
5. Referencias breves

TONO: Empático, profesional, educativo
```

### User Prompt (~150 tokens)

```
Un paciente con EII ha preguntado:

Título: [Título de pregunta]
Descripción: [Cuerpo de pregunta]

[FUTURO: Contexto RAG aquí]

Proporciona respuesta educativa siguiendo reglas.
```

### AI Response (~600 tokens)

```
[Respuesta generada]

+ Disclaimer automático (agregado por AiSafetyService)
```

---

## 🎛️ CONFIGURATION

### Critical Settings (appsettings.json)

```json
{
  "AiAnswer": {
    "Enabled": true,              // Master switch
    "AnthropicApiKey": "sk-...",  // REQUIRED
    "Model": "claude-sonnet-4.5-20250514",
    "Temperature": 0.3,           // Low = consistent
    "MaxTokens": 600,             // Cost control
    "TimeoutSeconds": 30,
    "SystemUserId": "guid",       // REQUIRED: System user
    "ForbiddenPhrases": [...]     // Safety filters
  }
}
```

### Hangfire Settings (Program.cs)

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2,           // Scale as needed
    options.Queues = new[] { "default", "ai" }
});
```

---

## 📈 MONITORING

### Hangfire Dashboard
```
URL: https://localhost:7002/hangfire

Métricas:
- Enqueued: Jobs esperando
- Processing: Jobs corriendo
- Succeeded: Completados OK
- Failed: Errores (revisar stack trace)
```

### SQL Queries

```sql
-- Total respuestas IA generadas
SELECT COUNT(*) FROM Respuestas WHERE EsIA = 1;

-- Preguntas sin respuestas humanas
SELECT p.Id, p.Titulo, p.TieneRespuestaIA
FROM Preguntas p
LEFT JOIN Respuestas r ON p.Id = r.PreguntaId AND r.EsIA = 0
WHERE r.Id IS NULL AND p.TieneRespuestaIA = 1;

-- Costo estimado total
SELECT 
  COUNT(*) AS TotalRespuestas,
  COUNT(*) * 0.011 AS CostoEstimadoUSD
FROM Respuestas
WHERE EsIA = 1;
```

---

## 🚀 DEPLOYMENT CHECKLIST

### Pre-deployment

- [ ] NuGet packages instalados
- [ ] DB migrations aplicadas
- [ ] System user creado
- [ ] appsettings.json configurado
- [ ] API key de Anthropic válida
- [ ] Program.cs modificado
- [ ] Compilation exitosa

### Post-deployment

- [ ] Hangfire dashboard accesible
- [ ] Test: Crear pregunta dummy
- [ ] Verificar job en Hangfire
- [ ] Verificar respuesta IA en DB
- [ ] Test: GET respuestas endpoint
- [ ] Monitoring setup

### Production

- [ ] SystemUserId correcto en prod
- [ ] Connection string prod correcta
- [ ] Hangfire dashboard protegido (auth)
- [ ] Logs configurados (Serilog/NLog)
- [ ] Alertas configuradas (failed jobs)
- [ ] Backup DB regular

---

## 📚 FILES CREATED

### Core Implementation (8 files)
1. `Configuration/AiAnswerConfiguration.cs`
2. `Services/AI/IAiAnswerService.cs`
3. `Services/AI/AiAnswerService.cs`
4. `Services/AI/IAiSafetyService.cs`
5. `Services/AI/AiSafetyService.cs`
6. `Services/AI/IAiPromptBuilder.cs`
7. `Services/AI/AiPromptBuilder.cs`
8. `Jobs/AiAnswerJob.cs`

### Controllers (2 files)
9. `Controllers/PreguntasApiController.cs` (modified)
10. `Controllers/PreguntasApiController.Respuestas.cs`

### Models (2 files)
11. `Models/Pregunta.cs` (modified)
12. `Models/Respuesta.cs` (modified)

### Installation (5 files)
13. `INSTALLATION-GUIDE.md`
14. `INSTALL-AI-PROGRAM-CS.txt`
15. `INSTALL-APPSETTINGS-AI.json`
16. `INSTALL-NUGET-PACKAGES.sh`
17. `MIGRATION-AI-FIELDS.sql`
18. `SETUP-SYSTEM-USER.sql`

### Documentation (1 file)
19. `AI-ARCHITECTURE-SUMMARY.md` (this file)

---

## 🎯 SUCCESS CRITERIA

✅ **Functional Requirements:**
- [x] AI generates answer only once per question
- [x] Background processing (non-blocking)
- [x] Safety filters implemented
- [x] Disclaimer always present
- [x] Stored in database
- [x] Ordered correctly (humans before AI)

✅ **Non-Functional Requirements:**
- [x] Cost optimized (<$0.02 per answer)
- [x] Low latency (user doesn't wait)
- [x] Retry logic (Hangfire)
- [x] Logging comprehensive
- [x] Monitoring dashboard
- [x] Scalable architecture

✅ **Security Requirements:**
- [x] No medical diagnosis
- [x] No treatment modification advice
- [x] Content validation
- [x] Fallback response
- [x] Disclaimer mandatory

---

## 🔮 FUTURE ENHANCEMENTS

### Phase 2: RAG Implementation
```csharp
// Preparado en AiPromptBuilder
var contextoDinamico = await VectorSearch(pregunta);
var prompt = BuildUserPrompt(pregunta, contextoDinamico);
```

### Phase 3: Multi-Model Support
```json
{
  "Models": [
    { "Name": "claude", "Priority": 1 },
    { "Name": "gpt-4", "Priority": 2 }
  ]
}
```

### Phase 4: Feedback Loop
```sql
ALTER TABLE Respuestas ADD FeedbackScore INT;
-- Train on accepted/rejected AI answers
```

### Phase 5: A/B Testing
```csharp
// Different prompts/temperatures
// Measure acceptance rate
```

---

## 📞 SUPPORT & MAINTENANCE

### Regular Tasks

**Daily:**
- Monitor Hangfire failed jobs
- Check error logs

**Weekly:**
- Review cost reports (Anthropic dashboard)
- Analyze AI answer acceptance rate
- Update forbidden phrases if needed

**Monthly:**
- Review and update prompts
- Analyze most common questions
- Optimize MaxTokens if needed

### Key Metrics to Track

1. **Cost Metrics:**
   - Total API calls
   - Average tokens per response
   - Monthly spend

2. **Quality Metrics:**
   - AI answers generated
   - Human answers after AI
   - AI answers voted positively

3. **Performance Metrics:**
   - Job processing time
   - Failed job rate
   - Retry rate

---

## ✅ CONCLUSION

Sistema completo de AI First Answer implementado con:

- ✅ Clean Architecture
- ✅ Cost optimization
- ✅ Medical safety
- ✅ Background processing
- ✅ Comprehensive logging
- ✅ Future-ready design

**Ready for production with proper configuration.**

---

**Version:** 1.0  
**Last Updated:** 2025-01-04  
**Author:** Senior Systems Analyst & .NET Architect  
**Platform:** .NET 8/10 + ASP.NET Core + Hangfire + Claude Sonnet 4.5
