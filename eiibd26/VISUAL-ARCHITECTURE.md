# 🎨 AI FIRST ANSWER SYSTEM - VISUAL ARCHITECTURE

## 📐 SYSTEM ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              USER LAYER                                      │
│                                                                              │
│  [Patient Browser] ────────────► POST /api/preguntas                       │
│  {Titulo, Cuerpo}                                                           │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                   │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  PreguntasApiController                                        │        │
│  │                                                                │        │
│  │  1. Validate input                                            │        │
│  │  2. Create Pregunta entity                                    │        │
│  │  3. Save to database                                          │        │
│  │  4. Enqueue background job ← Non-blocking!                    │        │
│  │  5. Return 200 OK immediately                                 │        │
│  └────────────────────────────────────────────────────────────────┘        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        BACKGROUND QUEUE                                      │
│                                                                              │
│  ╔═══════════════════════════════════════════════════════════╗             │
│  ║             HANGFIRE JOB QUEUE                           ║             │
│  ║                                                           ║             │
│  ║  [Job 1: AI Answer for Question A] ← Enqueued           ║             │
│  ║  [Job 2: AI Answer for Question B] ← Processing         ║             │
│  ║  [Job 3: AI Answer for Question C] ← Succeeded          ║             │
│  ╚═══════════════════════════════════════════════════════════╝             │
│                                                                              │
│  Workers: 2 (configurable)                                                  │
│  Queues: ["default", "ai"]                                                  │
│  Retry: 2 attempts (60s, 300s delays)                                      │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        BUSINESS LOGIC LAYER                                  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  AiAnswerJob.ProcesarPreguntaAsync()                          │        │
│  │                                                                │        │
│  │  IF pregunta.TieneRespuestaIA == true                         │        │
│  │    → SKIP (already has AI answer)                             │        │
│  │                                                                │        │
│  │  IF COUNT(human answers) > 0                                  │        │
│  │    → SKIP (humans already answered)                           │        │
│  │                                                                │        │
│  │  ELSE                                                          │        │
│  │    → GENERATE AI ANSWER                                       │        │
│  └────────────────────────────────────────────────────────────────┘        │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          AI SERVICES LAYER                                   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │ 1. AiPromptBuilder                                               │      │
│  │    ├─ BuildSystemPrompt() → Medical guidelines                   │      │
│  │    └─ BuildUserPrompt() → Question context + RAG placeholder    │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                              ↓                                               │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │ 2. AiAnswerService                                               │      │
│  │    ├─ HTTP Client → Anthropic API                               │      │
│  │    ├─ POST /v1/messages                                          │      │
│  │    ├─ Model: claude-sonnet-4.5                                   │      │
│  │    ├─ MaxTokens: 600                                             │      │
│  │    ├─ Temperature: 0.3                                           │      │
│  │    └─ Timeout: 30s                                               │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                              ↓                                               │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │ 3. AiSafetyService                                               │      │
│  │    ├─ ValidarContenido()                                         │      │
│  │    │   ├─ Check ForbiddenPhrases                                 │      │
│  │    │   ├─ Check Regex patterns                                   │      │
│  │    │   └─ Return true/false                                      │      │
│  │    │                                                              │      │
│  │    ├─ IF valid:                                                  │      │
│  │    │   └─ AgregarDisclaimer() → Add mandatory warning            │      │
│  │    │                                                              │      │
│  │    └─ IF invalid:                                                │      │
│  │        └─ ObtenerRespuestaFallback() → Safe pre-written answer   │      │
│  └──────────────────────────────────────────────────────────────────┘      │
└──────────────────────────────────┬──────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DATA LAYER                                        │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────┐        │
│  │  SQL Server Database                                           │        │
│  │                                                                │        │
│  │  INSERT INTO Respuestas                                        │        │
│  │  {                                                             │        │
│  │    Id: new GUID                                                │        │
│  │    PreguntaId: pregunta.Id                                     │        │
│  │    UsuarioId: SystemUserId                                     │        │
│  │    Cuerpo: "AI answer + disclaimer"                            │        │
│  │    EsIA: true                                                  │        │
│  │    ModeloIA: "claude-sonnet-4.5-20250514"                      │        │
│  │    EsAceptada: false                                           │        │
│  │    EsColapsada: false                                          │        │
│  │    Puntuacion: 0                                               │        │
│  │    FechaCreacion: NOW                                          │        │
│  │  }                                                             │        │
│  │                                                                │        │
│  │  UPDATE Preguntas                                              │        │
│  │  SET TieneRespuestaIA = true,                                  │        │
│  │      FechaGeneracionIA = NOW                                   │        │
│  │  WHERE Id = pregunta.Id                                        │        │
│  └────────────────────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────────────────┘


                              ✅ JOB COMPLETE

                                    │
                                    ▼

┌─────────────────────────────────────────────────────────────────────────────┐
│                          USER SEES RESULT                                    │
│                                                                              │
│  [Patient Browser] ────────────► GET /api/preguntas/{id}/respuestas        │
│                                                                              │
│  Response:                                                                   │
│  {                                                                          │
│    ok: true,                                                                │
│    tieneRespuestaIA: true,                                                  │
│    respuestas: [                                                            │
│      {                                                                      │
│        esIA: true,                                                          │
│        cuerpo: "Respuesta educativa...\n\n⚠️ Disclaimer..."                │
│      }                                                                      │
│    ]                                                                        │
│  }                                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 ANSWER ORDERING LOGIC

```
┌─────────────────────────────────────────────────────────────┐
│              GET /api/preguntas/{id}/respuestas             │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  Load all Respuestas  │
              │  WHERE Eliminado = 0   │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │   ORDER BY:            │
              │   1. EsAceptada DESC   │──► Accepted answers first
              │   2. EsIA ASC          │──► Humans before AI
              │   3. Puntuacion DESC   │──► Higher score first
              │   4. FechaCreacion ASC │──► Older first
              └───────────┬────────────┘
                          │
                          ▼
        ┌─────────────────────────────────────────┐
        │         RESULT ORDER:                   │
        │                                         │
        │  [1] ✅ Human Answer (accepted)         │
        │  [2] 👤 Human Answer (highest score)    │
        │  [3] 👤 Human Answer (older)            │
        │  [4] 🤖 AI Answer (last)                │
        └─────────────────────────────────────────┘

        ℹ️ AI answers always appear last when humans exist
```

---

## 🛡️ SAFETY LAYERS DIAGRAM

```
┌──────────────────────────────────────────────────────────────┐
│               AI Generated Content                           │
│   "Puedes aumentar la dosis de tu medicamento..."            │
└─────────────────────────┬────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────────────┐
        │      LAYER 1: Prompt Engineering        │
        │                                         │
        │  System prompt enforces:                │
        │  - No diagnosis                         │
        │  - No treatment changes                 │
        │  - Educational only                     │
        └───────────────┬─────────────────────────┘
                        │ PASSED (but may slip)
                        ▼
        ┌─────────────────────────────────────────┐
        │    LAYER 2: Forbidden Phrases Check     │
        │                                         │
        │  "aumentar dosis" → DETECTED! ❌        │
        └───────────────┬─────────────────────────┘
                        │ FAILED
                        ▼
        ┌─────────────────────────────────────────┐
        │    LAYER 3: Fallback Response           │
        │                                         │
        │  Replace with safe pre-written answer   │
        └───────────────┬─────────────────────────┘
                        │
                        ▼
        ┌─────────────────────────────────────────┐
        │    LAYER 4: Mandatory Disclaimer        │
        │                                         │
        │  Append: "⚠️ No reemplaza consulta..."  │
        └───────────────┬─────────────────────────┘
                        │
                        ▼
        ┌─────────────────────────────────────────┐
        │         SAFE CONTENT DELIVERED          │
        │                                         │
        │  "Información general sobre EII...      │
        │   ⚠️ Esta respuesta es informativa..."  │
        └─────────────────────────────────────────┘
```

---

## 💰 COST FLOW DIAGRAM

```
                    QUESTION CREATED
                           │
                           ▼
               ┌───────────────────────┐
               │   Check if needed?    │
               └───────┬───────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
   Has AI?        Has humans?     Enabled?
     YES              YES             NO
     │                │              │
     │                │              │
     └────────────────┴──────────────┘
                      │
                   SKIP ✅
               (Cost: $0.00)


                NO AI / NO HUMANS / ENABLED
                           │
                           ▼
                  ┌────────────────┐
                  │ Generate AI    │
                  │ Answer         │
                  └────────┬───────┘
                           │
                           ▼
            ┌──────────────────────────────┐
            │   ANTHROPIC API CALL          │
            │                               │
            │   Input:  ~550 tokens         │
            │   Cost:   $0.00165            │
            │                               │
            │   Output: ~600 tokens         │
            │   Cost:   $0.00900            │
            │                               │
            │   TOTAL:  $0.01065            │
            └──────────────┬────────────────┘
                           │
                           ▼
                  ┌────────────────┐
                  │  Save to DB    │
                  │  (Cached) ✅   │
                  └────────┬───────┘
                           │
                           ▼
                    NO MORE COSTS
              (Answer served from DB)


      If 1000 questions/month with no human answers:
      1000 × $0.01065 = $10.65/month

      If humans answer 90% of questions:
      100 × $0.01065 = $1.07/month
```

---

## 📊 DATA FLOW DIAGRAM

```
┌────────────────────────────────────────────────────────────┐
│                    TABLES INVOLVED                         │
└────────────────────────────────────────────────────────────┘

┌──────────────────┐       ┌──────────────────┐
│   Preguntas      │       │   Respuestas     │
├──────────────────┤       ├──────────────────┤
│ Id (PK)          │◄──────┤ PreguntaId (FK)  │
│ Titulo           │       │ Id (PK)          │
│ Cuerpo           │       │ Cuerpo           │
│ UsuarioId        │       │ UsuarioId        │
│ TieneRespuestaIA │◄─┐    │ EsIA ◄────────┐  │
│ FechaGeneracionIA│  │    │ ModeloIA       │  │
└──────────────────┘  │    │ Puntuacion     │  │
                      │    │ EsAceptada     │  │
                      │    └──────────────────┘  │
                      │                          │
                      └──────────────────────────┘
                         Updated when AI
                         answer is saved


┌──────────────────┐       ┌──────────────────┐
│   AspNetUsers    │       │   Hangfire       │
├──────────────────┤       │   Tables         │
│ Id (PK)          │       ├──────────────────┤
│ Email            │       │ Job              │
│ UserName         │       │ State            │
│ ...              │       │ Set              │
│                  │       │ Server           │
│ "Sistema IA" ◄───┼───────┤ ...              │
│ SystemUserId     │       │                  │
└──────────────────┘       └──────────────────┘
  Used as author          Manages background
  of AI answers            job execution
```

---

## 🔍 MONITORING DASHBOARD LAYOUT

```
┌─────────────────────────────────────────────────────────────┐
│            HANGFIRE DASHBOARD                               │
│            https://localhost:7002/hangfire                  │
└─────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  JOBS                                                         │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Enqueued: 5 jobs                                       │ │
│  │ ├─ AiAnswerJob (Question: abc-123)                     │ │
│  │ ├─ AiAnswerJob (Question: def-456)                     │ │
│  │ └─ ...                                                 │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Processing: 2 jobs (Workers: 2)                        │ │
│  │ ├─ AiAnswerJob (Question: ghi-789) [Running 5s]       │ │
│  │ └─ AiAnswerJob (Question: jkl-012) [Running 3s]       │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Succeeded: 147 jobs (Last 24h)                         │ │
│  │ Average execution time: 7.2 seconds                    │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Failed: 3 jobs                                         │ │
│  │ ├─ AiAnswerJob → Timeout (retry scheduled)            │ │
│  │ ├─ AiAnswerJob → API Error 429 (retry scheduled)      │ │
│  │ └─ AiAnswerJob → Network error (retry scheduled)      │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  SERVERS                                                      │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Server: LAPTOP-ABC123                                  │ │
│  │ Started: 2 hours ago                                   │ │
│  │ Workers: 2                                             │ │
│  │ Queues: default, ai                                    │ │
│  │ Status: Active ✅                                      │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

## 🎯 SUCCESS FLOW (Happy Path)

```
 MINUTE 0:00   User creates question
                      │
                      ▼
 MINUTE 0:00   Saved to DB + Job enqueued
                      │
                      ▼
 MINUTE 0:00   User sees: 200 OK response
                      │
                      ▼
 MINUTE 0:05   Hangfire picks up job
                      │
                      ▼
 MINUTE 0:07   Calls Claude API
                      │
                      ▼
 MINUTE 0:09   Response received
                      │
                      ▼
 MINUTE 0:09   Safety validation passes
                      │
                      ▼
 MINUTE 0:09   Disclaimer added
                      │
                      ▼
 MINUTE 0:09   Saved to database
                      │
                      ▼
 MINUTE 0:10   User refreshes page
                      │
                      ▼
 MINUTE 0:10   Sees AI answer! ✅


 Total time: ~10 seconds (background)
 User wait time: 0 seconds (non-blocking)
```

---

**This visual architecture provides a comprehensive overview of the entire AI First Answer system.**

**For detailed implementation, see:**
- `AI-ARCHITECTURE-SUMMARY.md` - Technical details
- `INSTALLATION-GUIDE.md` - Setup instructions
- `IMPLEMENTATION-COMPLETE.md` - Full summary
