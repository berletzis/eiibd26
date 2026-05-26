# ADR-001: Procesamiento de Jobs IA con Hangfire

**Estado:** Aceptado  
**Fecha:** 2025-07  
**Issues relacionados:** FUNC-017

---

## Contexto

`PreguntasApiController.CrearPregunta()` usaba `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning).Unwrap()` para disparar el procesamiento de IA tras crear una pregunta. Riesgos identificados:

- Sin persistencia: reinicio del proceso pierde el job y la pregunta queda sin respuesta IA.
- Sin retry: error de red/LLM silenciado; pregunta sin responder indefinidamente.
- Sin observabilidad: no hay estado visible de jobs pendientes/fallidos.
- Jobs huérfanos: `Task.Delay(3000)` amplía la ventana de pérdida en reinicio.
- Thread pool pressure: `LongRunning` crea OS threads fuera del pool administrado.

Hangfire ya estaba instalado (`Hangfire.Core`, `Hangfire.AspNetCore`, `Hangfire.SqlServer`) y configurado en `Program.cs`.

---

## Decisión

**Migrar jobs de IA a `IBackgroundJobClient.Enqueue<AiAnswerJob>()`.**

Hangfire provee automáticamente:

1. **Persistencia** en SQL Server antes de responder al cliente; sobrevive reinicios.
2. **Retry automático** con backoff exponencial (10 intentos por defecto).
3. **Estado visible:** Enqueued → Processing → Succeeded | Failed en `/hangfire`.
4. **Stack trace completo** en la cola Failed para diagnóstico.
5. **Controller determinístico:** una sola línea sin hilos adicionales.

---

## Alternativas descartadas

| Alternativa | Razón |
|---|---|
| Mantener `Task.Factory.StartNew` | Sin persistencia ni retry; riesgo real por pérdida de datos |
| `Channel<T>` / `IHostedService` interno | Requeriría nueva infraestructura; Hangfire ya disponible |
| Azure Service Bus / SQS | Dependencia cloud extra; fuera del stack actual |
| Tabla propia de cola | Reinventar Hangfire; mayor costo de mantenimiento |

---

## Consecuencias

### Positivas
- Jobs de IA auditables, reiniciables y monitoreables desde `/hangfire`.
- Controller devuelve respuesta al cliente sin esperar el job.
- Sin nuevas dependencias al proyecto.
- Patrón extensible a futuros jobs (emails, badges, recálculos).

### Atención
- El delay artificial de 3 segundos fue eliminado; la pregunta ya está persistida antes del `Enqueue`.
- El dashboard `/hangfire` debe estar protegido con `[Authorize(Roles="Administrador")]` en producción.

---

## Archivos modificados

- `eiibd26/Controllers/PreguntasApiController.cs` — eliminado `Task.Factory.StartNew`; inyectado `IBackgroundJobClient`.
