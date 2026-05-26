# 04 – Hangfire: Migración Fire-and-Forget → Job Persistido

**Fecha:** 2025-07-10  
**Issue:** FUNC-017

---

## Estado Pre-Remediación

```csharp
// ANTES: fire-and-forget sin retry ni persistencia
Task.Factory.StartNew(() => ProcesarPreguntaIA(preguntaId));
```

**Problemas:**
- Sin retry en error de red / OpenAI timeout
- Sin visibilidad de estado (éxito, fallo, duración)
- Sin persistencia — si el proceso moría, el job se perdía
- Sin cancelación cooperativa

---

## Estado Post-Remediación

```csharp
// AHORA: Hangfire enqueue
_backgroundJobs.Enqueue<AiAnswerJob>(job => job.ProcesarPreguntaAsync(preguntaIdCapture));
```

---

## Pipeline Verificado

```
POST /api/preguntas  (CrearPregunta)
		│
		▼
_backgroundJobs.Enqueue<AiAnswerJob>(ProcesarPreguntaAsync)
		│
		▼  [Hangfire SQL Server Storage]
Job en cola "default"
		│
		▼  [HangfireServer, WorkerCount=2]
AiAnswerJob.ProcesarPreguntaAsync(preguntaId)
		│
		├─ [Enabled == false]  → skip, no error
		├─ [Pregunta not found] → skip, no error
		├─ [Ya tiene respuesta IA] → skip, no error
		├─ [Respuestas humanas > 0] → skip (no sobreescribe)
		├─ [Pregunta similar 80%] → reutiliza respuesta existente
		├─ [Genera respuesta] → OpenAI / NINA
		│        │
		│        ├─ [OperationCanceledException] → throw (Hangfire retry)
		│        ├─ [HttpRequestException] → throw (Hangfire retry)
		│        └─ [Exception genérica] → log + NO throw (evita retry infinito)
		│
		▼
Respuesta persistida en DB + TieneRespuestaIA = true
```

---

## Configuración Hangfire Verificada

| Configuración | Valor | Archivo |
|---------------|-------|---------|
| Storage | SQL Server (`DefaultConnection`) | `Program.cs` línea 316 |
| WorkerCount | 2 | `Program.cs` línea 321 |
| Dashboard URL | `/hangfire` | `Program.cs` línea 850 |
| Dashboard Auth | `HangfireAdminAuthFilter` | `Program.cs` línea 852 |
| AiAnswerJob registrado en DI | ✅ `AddScoped<AiAnswerJob>()` | `Program.cs` |
| IBackgroundJobClient inyectado en Controller | ✅ | `PreguntasApiController.cs` línea 27 |

---

## Retry Strategy

| Tipo de Excepción | Comportamiento | Hangfire retry |
|-------------------|----------------|----------------|
| `OperationCanceledException` | `throw` | ✅ Sí — Hangfire reintenta |
| `HttpRequestException` | `throw` | ✅ Sí — Hangfire reintenta |
| `Exception` genérica | Log + **no throw** | ❌ No reintenta (intencional — evita loop infinito en errores de lógica) |

Hangfire por defecto aplica retry con backoff exponencial (10 intentos de serie Fibonacci).

---

## Cancelación Cooperativa

`CancellationToken` es aceptado y propagado:

```csharp
cancellationToken.ThrowIfCancellationRequested(); // 3 puntos en el flujo
```

Hangfire provee `CancellationToken` cuando el servidor está siendo detenido (graceful shutdown).

---

## Jobs Huérfanos / Dead Jobs — Estado Estático

No se puede verificar estado runtime sin acceso a la base de datos. Sin embargo:

- La lógica de idempotencia está presente: si una pregunta ya tiene `TieneRespuestaIA = true`, el job termina sin error sin reintentar
- El job es idempotente si se ejecuta más de una vez para la misma `preguntaId`

---

## Riesgos Residuales

| ID | Riesgo | Severidad |
|----|--------|-----------|
| R-HF-01 | Excepciones genéricas (e.g., `DbException` transitoria) no causan retry. Si la DB falla por 1 segundo, la IA no se genera. | 🟡 Medio |
| R-HF-02 | Sin `[AutomaticRetry(Attempts = N)]` explícito en el job — depende del default de Hangfire (10 intentos). No hay control fino. | 🟡 Bajo-Medio |
| R-HF-03 | `WorkerCount = 2` — con volumen alto de preguntas, puede haber cola. No bloqueante para MVP. | 🟡 Bajo |

---

## Veredicto Fase 4

| Criterio | Estado |
|----------|--------|
| Fire-and-forget eliminado | ✅ PASS |
| Job enqueued via Hangfire | ✅ PASS |
| Persistencia en SQL Server | ✅ PASS |
| Retry en errores de red | ✅ PASS |
| Retry en errores genéricos (intencional off) | ⚠️ WARN — Decisión arquitectural documentada |
| Dashboard accesible y protegido | ✅ PASS |
| Cancelación cooperativa | ✅ PASS |
| **VEREDICTO** | ✅ **PASS** |
