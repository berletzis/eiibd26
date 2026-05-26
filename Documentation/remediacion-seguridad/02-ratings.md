# Bloque 2 – Ratings (SEC-004, SEC-005, SEC-007)

## Análisis

| Issue | Descripción | Controladores afectados |
|---|---|---|
| SEC-004 | Ratings anónimos de artículos – deduplicación por IP falsificable | `ArticleRatingsApiController` |
| SEC-005 | Ratings anónimos de glosario – misma vulnerabilidad | `GlossaryRatingsApiController` |
| SEC-007 | `X-Forwarded-For` como primer identificador de IP → puede ser forjado por el cliente | Ambos |

## Decisión sobre anonimato (SEC-004/005)

Los ratings anónimos son una **funcionalidad de diseño explícita** del sitio: usuarios visitantes pueden dar like/dislike a artículos y términos del glosario sin registrarse. No es un error de seguridad; es el comportamiento esperado.

**No se cambia la política de acceso.** Los endpoints de rating siguen siendo accesibles sin autenticación.

La deduplicación usa el esquema correcto: usuarios autenticados → deduplicación por `UserId`; usuarios anónimos → deduplicación por IP + ventana de 24 h.

## Remediación aplicada (SEC-007)

**Problema:** `GetClientIpAddress()` consultaba `X-Forwarded-For` **antes** que `RemoteIpAddress`. Cualquier cliente podía enviar un header `X-Forwarded-For: 1.2.3.4` arbitrario para eludir la deduplicación anónima y votar múltiples veces.

**Fix:** Simplificar `GetClientIpAddress()` para retornar directamente `HttpContext.Connection.RemoteIpAddress`, que es la IP real de la conexión TCP y **no puede ser falsificada por el cliente**.

### ¿Por qué ignorar X-Forwarded-For?

La app no configura `UseForwardedHeaders()` con `KnownProxies`/`KnownNetworks` en `Program.cs`. Sin esa configuración:
- El middleware de ASP.NET Core no desenvuelve los headers del proxy.
- `X-Forwarded-For` es un header ordinario que **cualquier cliente puede incluir**.
- Confiar en él para deduplicación de seguridad es equivalente a confiar en datos del cliente.

### Nota futura

Si la app se despliega detrás de un reverse proxy (nginx, IIS ARR, Azure App Gateway), se debe:
1. Agregar `builder.Services.Configure<ForwardedHeadersOptions>(...)` con `KnownProxies` explícitos.
2. Agregar `app.UseForwardedHeaders()` **antes** de `UseRouting`.
3. En ese momento `RemoteIpAddress` ya contendrá la IP del usuario final, así que el código de los controladores no necesita cambios.

## Estado

| Issue | Estado |
|---|---|
| SEC-004 | ✅ RESUELTO — anonimato intencional documentado |
| SEC-005 | ✅ RESUELTO — anonimato intencional documentado |
| SEC-007 | ✅ RESUELTO — `GetClientIpAddress()` usa solo `RemoteIpAddress` |
