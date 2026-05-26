# 05 - Infraestructura

## FUNC-017: Jobs de IA con fire-and-forget (Task.Factory.StartNew)

### Problema
`PreguntasApiController.CrearPregunta()` usaba `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning).Unwrap()` para disparar el procesamiento IA. Este patrón no tiene:
- Persistencia: reinicio del proceso pierde el job.
- Retry: error silenciado, pregunta sin respuesta indefinidamente.
- Observabilidad: no hay estado visible.
- Cancelación: no se puede cancelar ni monitorear.

### Causa raíz
El job fue implementado antes de que Hangfire estuviese completamente configurado, y nunca fue migrado.

### Solución
- Inyectado `IBackgroundJobClient` (Hangfire) en `PreguntasApiController`.
- `Task.Factory.StartNew(...)` reemplazado por `_backgroundJobs.Enqueue<AiAnswerJob>(job => job.ProcesarPreguntaAsync(preguntaId))`.
- Hangfire provee automáticamente: persistencia SQL, retry con backoff exponencial (10 intentos), estado visible en `/hangfire`, stack trace completo en jobs fallidos.
- `AiAnswerJob` ya era compatible con Hangfire (DI en constructor, `Guid` serializable como parámetro).
- ADR documentado en `docs/adr/ADR-ai-job-processing.md`.

### Impacto
- Cero jobs huérfanos en reinicio.
- Jobs fallidos recuperables desde el dashboard de Hangfire.
- Sin presión adicional en el thread pool del OS.

### Archivos modificados
- `eiibd26/Controllers/PreguntasApiController.cs`
- `docs/adr/ADR-ai-job-processing.md` (creado)

---

## UI-015: Brace imbalance en _Layout.cshtml

### Problema
El segundo bloque `<script defer>` en `_Layout.cshtml` abrÃ­a `window.addEventListener('DOMContentLoaded', function() {` pero le faltaba el `});` de cierre antes de `</script>`. El callback del evento de body padding-right correction nunca cerraba su función.

### Causa raíz
Error de edición manual al agregar el MutationObserver de body padding.

### Solución
Agregado `});` para cerrar correctamente el callback de `DOMContentLoaded` antes de `</script>`.

### Impacto
- El script de prevención de layout shift funciona correctamente.
- No hay errores de JavaScript en consola por brace desbalanceado.
- Los eventos de offcanvas (show/shown/hide/hidden) limpian correctamente el `paddingRight` del body.

### Archivos modificados
- `eiibd26/Pages/Shared/_Layout.cshtml`
