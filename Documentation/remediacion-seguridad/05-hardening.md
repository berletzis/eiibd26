# Bloque 5 – Hardening (SEC-013, SEC-014, SEC-015, SEC-016, SEC-017, SEC-018)

## SEC-013: Token de mood push — audit trail

**Hallazgo:** `POST /api/mood/quick` no logueaba tokens inválidos o expirados.

**Verificación previa:** `PushMoodTokenService` usa ASP.NET Core Data Protection como mecanismo de cifrado/firma. Data Protection usa CSPRNG internamente — no hay problema de entropía.

**Fix aplicado:** Agregado `ILogger<MoodApiController>` con `LogWarning` cuando el token falla validación, incluyendo la IP del solicitante:
```csharp
_logger.LogWarning("[SEC-013] Token de mood inválido o expirado. IP: {IP}", ...);
```
Esto permite detectar abusos (ej: barrido de tokens, replay attacks) en los logs estructurados.

---

## SEC-014: HTML generado sin HtmlEncoder en admin de contenidos

**Hallazgo:** `ContenidosAdminController.GetGridData()` construía strings HTML con `p.id` sin encodar.

**Contexto:** `p.id` es `int`, no XSS real hoy. Sin embargo, el patrón es peligroso si se replica con strings como títulos o autores.

**Fix aplicado:** Agregado `using System.Text.Encodings.Web;` y:
```csharp
var safeId = HtmlEncoder.Default.Encode(p.id.ToString());
```
Todos los IDs embebidos en el HTML del grid ahora pasan por HtmlEncoder.

---

## SEC-015: Stack traces en APIs si ASPNETCORE_ENVIRONMENT no está configurado

**Hallazgo:** Varios endpoints retornan `ex.ToString()` condicionalmente en Development.

**Fix aplicado:** Agregado un warning en startup cuando el ambiente no es Production:
```csharp
if (!app.Environment.IsProduction())
	startupLogger.LogWarning("[SEC-015] ASPNETCORE_ENVIRONMENT = '{Env}'...", ...);
```
En producción el warning nunca aparece. En Development/Staging aparece en los logs de inicio para recordar configurar la variable de entorno antes de deploy.

---

## SEC-016: Task.Factory.StartNew para jobs de IA

**Hallazgo:** `PreguntasApiController.cs:95-111` usa `Task.Factory.StartNew(LongRunning)` en lugar de Hangfire.

**Decisión:** Este issue es de confiabilidad/resiliencia (FUNC-017 en el reporte de funcionalidad), no solo de seguridad. La remediación completa requiere refactorizar el endpoint para delegar a `BackgroundJob.Enqueue<AiAnswerJob>(...)`. **No se modifica en este bloque** para evitar scope creep — se registra como tarea pendiente en el backlog funcional.

**Estado:** ⚠️ PENDIENTE — Ver FUNC-017. Alcance superado para este bloque de seguridad.

---

## SEC-017: HangfireAdminAuthFilter

**Verificación:** `HangfireAdminAuthFilter.Authorize()` retorna:
```csharp
http.User.Identity?.IsAuthenticated == true && http.User.IsInRole("Administrador")
```
✅ El filtro checa autenticación **y** rol `"Administrador"` correctamente. No hay bug. Con el fix de SEC-012 (seed de rol `"Administrador"`) el filtro ahora puede funcionar end-to-end.

**Acción adicional:** Verificar manualmente en deploy que `/hangfire` retorna 302 para anónimos y 403 para usuarios no-admin.

**Estado:** ✅ VERIFICADO — No se requieren cambios de código.

---

## SEC-018: CSP con 'unsafe-inline' y 'unsafe-eval'

**Hallazgo:** El CSP en `Program.cs` incluye `'unsafe-inline'` y `'unsafe-eval'` en `script-src`.

**Análisis:** El reporte ya identifica la causa raíz: más de 200 líneas de JS inline en `_Layout.cshtml`. La eliminación de `'unsafe-inline'` requiere extraer todo ese JS a archivos externos y agregar nonces para el JS inline restante — trabajo de semanas, no de un bloque de seguridad.

**Decisión:** Este es un riesgo aceptado documentado. La mitigación primaria es el conjunto completo de controles ya en lugar (autenticación, CSRF, ownership validation) que reducen la superficie de ataque XSS. La migración de JS inline es una tarea de largo plazo (UI-018).

**Estado:** ⚠️ ACEPTADO — Riesgo documentado. Tarea de largo plazo en backlog UI.

---

## Estado resumen

| Issue | Estado |
|---|---|
| SEC-013 | ✅ RESUELTO — Logging de tokens inválidos en `MoodApiController` |
| SEC-014 | ✅ RESUELTO — `HtmlEncoder.Default.Encode()` en `ContenidosAdminController` |
| SEC-015 | ✅ RESUELTO — Warning en startup si `ASPNETCORE_ENVIRONMENT != Production` |
| SEC-016 | ⚠️ PENDIENTE — Refactor a Hangfire (FUNC-017, fuera de scope de seguridad) |
| SEC-017 | ✅ VERIFICADO — `HangfireAdminAuthFilter` correctamente implementado |
| SEC-018 | ⚠️ ACEPTADO — Requiere migración de JS inline a archivos externos (UI-018) |
