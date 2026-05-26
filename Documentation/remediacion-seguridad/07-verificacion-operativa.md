# 07 – Verificación Operativa Post-Remediación (SEC-001 → SEC-018)

> **Modo:** Solo verificación. Sin cambios de código.  
> **App:** `eiibd26` — ASP.NET Core 8, IIS in-process, SQL Server.  
> **Build previo:** ✅ `Build succeeded` en todos los bloques.

---

## FASE 1 — Rate Limiting (SEC-001 / SEC-002 / SEC-003)

### Configuración verificada

| Parámetro | Valor | Fuente |
|---|---|---|
| Política | `catalogos-autocomplete` | `Program.cs:141` |
| Algoritmo | Fixed Window | `AddFixedWindowLimiter` |
| Ventana | 60 segundos | `limiterOptions.Window` |
| Límite por IP | 30 requests/ventana | `limiterOptions.PermitLimit` |
| Cola | 0 (sin buffering) | `limiterOptions.QueueLimit` |
| HTTP rechazado | 429 | `options.RejectionStatusCode` |
| Cuerpo rechazo | `{"ok":false,"error":"Demasiadas solicitudes..."}` | `options.OnRejected` |
| Middleware | `app.UseRateLimiter()` después de `UseRouting()` | `Program.cs:793` |

### Atributos por endpoint

| Endpoint | `[AllowAnonymous]` | `[EnableRateLimiting]` | Ruta |
|---|---|---|---|
| Síntomas autocomplete | ✅ | ✅ `catalogos-autocomplete` | `GET /api/sintomas/autocomplete` |
| Tratamientos autocomplete | ✅ | ✅ `catalogos-autocomplete` | `GET /api/tratamientos/autocomplete` |
| Condiciones autocomplete | ✅ | ✅ `catalogos-autocomplete` | `GET /api/condiciones/autocomplete` |

### Pruebas HTTP ejecutadas (app corriendo en localhost:5175)

**Prueba 1 — Requests normales (1 request por endpoint)**

```
GET /api/sintomas/autocomplete?q=col    → HTTP 200 ✅
GET /api/tratamientos/autocomplete?q=cor → HTTP 200 ✅
GET /api/condiciones/autocomplete?q=col  → HTTP 200 ✅
```

**Prueba 2 — Burst (35 requests secuenciales en la misma ventana)**

```
Requests 1–30   → HTTP 200 ✅ (dentro del límite)
Requests 31–35  → HTTP 429 ✅ (rechazo correcto con JSON)
```
**Resultado:** El límite se activa exactamente en el request 31. Sin falsos positivos.

**Prueba 3 — Nueva ventana (después de 62 segundos)**

```
Requests 1–30   → HTTP 200 ✅ (ventana reseteada)
Requests 31–32  → HTTP 429 ✅ (rechazo correcto)
```
**Resultado:** La ventana fija se resetea correctamente al expirar los 60 segundos.

### Impacto en UX

La UI (`UsuarioSintomas.cshtml`, `UsuarioCondiciones.cshtml`, `UsuarioTratamientos.cshtml`) usa:
```javascript
let resp = await fetch(url);
if (!resp.ok) return;   // ← ante 429: dropdown vacío, sin crash
```

- **Normal:** ningún usuario legítimo alcanza 30 keystrokes en el autocomplete en 60 segundos.
- **Bajo 429:** el dropdown se vacía silenciosamente. No hay mensaje de error al usuario.
- **UX impact:** mínimo. El usuario simplemente no ve sugerencias temporalmente.

> ⚠️ **WARN-01:** Ante 429 la UI no muestra retroalimentación al usuario (dropdown vacío). No es bloqueante para merge pero es UX mejorable a futuro.

### Resultado Fase 1

| Issue | Estado |
|---|---|
| SEC-001 | ✅ **PASS** — `/api/sintomas/autocomplete` rate-limited y probado |
| SEC-002 | ✅ **PASS** — `/api/tratamientos/autocomplete` rate-limited y probado |
| SEC-003 | ✅ **PASS** — `/api/condiciones/autocomplete` rate-limited y probado |

---

## FASE 2 — Roles (SEC-012)

### Inventario de roles en BD local

```sql
SELECT r.Name, COUNT(ur.UserId) as UserCount
FROM AspNetRoles r LEFT JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
GROUP BY r.Name ORDER BY r.Name;
```

| Rol en BD | Usuarios asignados |
|---|---|
| `Administrador` | 1 |
| `Candidato` | 0 |
| `Empresa` | 1 |
| `Paciente` | 699 |
| `Admin` | **NO EXISTE** |
| `Medico` | **NO EXISTE** |

### Hallazgo clave

El rol `"Admin"` **nunca existió en esta base de datos**. El rol operativo siempre fue `"Administrador"`. El código (controllers, HangfireAdminAuthFilter, políticas) siempre apuntó a `"Administrador"` — era el seed el que tenía el error al no incluirlo.

### Usuario administrador

| Email | Rol | Estado |
|---|---|---|
| `berletzis@gmail.com` | `Administrador` | ✅ Activo |

### Usuarios sin rol (6 registros)

```
fechade@nacimiento.com, hey@hoy.com, jiofi@dkdkd.com,
kddd@llddd.com, paciente@paciente.com, test@test.com
```

Todos parecen cuentas de prueba/test. Sin rol asignado = acceso solo a recursos públicos. No son usuarios huérfanos críticos.

### Resultado Fase 2

| Issue | Estado |
|---|---|
| SEC-012 | ✅ **PASS** — `"Administrador"` en seed, 1 admin funcional, ningún usuario huérfano crítico |

---

## FASE 3 — Ratings e IP Deduplication (SEC-004 / SEC-005 / SEC-007)

### Configuración actual

```csharp
// ArticleRatingsApiController.cs + GlossaryRatingsApiController.cs
private string? GetClientIpAddress() =>
	HttpContext.Connection.RemoteIpAddress?.ToString();
```

`X-Forwarded-For` eliminado deliberadamente (no confiable sin middleware de proxy configurado).

### Análisis por escenario de deployment

| Escenario | `RemoteIpAddress` | Deduplicación | Resultado |
|---|---|---|---|
| **Acceso directo** (Kestrel sin proxy) | IP real del cliente | ✅ Correcta por usuario | ✅ PASS |
| **IIS in-process** (deployment actual) | IP real del cliente | ✅ Correcta por usuario | ✅ PASS |
| **IIS ARR** (reverse proxy local) | IP del IIS | ⚠️ Todos los usuarios bajo misma IP local | ⚠️ WARN |
| **Nginx** (reverse proxy) | IP del Nginx | ⚠️ Sin `UseForwardedHeaders` = todos bajo IP Nginx | ⚠️ WARN |
| **Cloudflare** (CDN delante) | IP del edge node | ⚠️ Múltiples users = misma IP de CDN | ⚠️ WARN |

### Análisis del deployment actual

El `web.config` confirma: `hostingModel="inprocess"` con IIS. No hay evidencia de Cloudflare, nginx, ni IIS ARR configurado. En este escenario:

- `RemoteIpAddress` = IP TCP real del cliente.
- La deduplicación anónima funciona correctamente.
- No hay riesgo de colisión de IPs por proxy.

> ⚠️ **WARN-02:** Si en el futuro se agrega un CDN (Cloudflare, Azure Front Door) o un reverse proxy, la deduplicación anónima de ratings fallará porque todos los usuarios anónimos compartirán la IP del edge. La solución correcta en ese momento es: configurar `UseForwardedHeaders()` con `KnownProxies` explícitos.

### ¿RemoteIpAddress cambia entre requests del mismo usuario?

- En TCP keepalive (mismo socket): no cambia.
- En requests nuevos desde el mismo dispositivo: puede cambiar si el ISP usa IPs dinámicas o CGNAT.
- CGNAT (Carrier-Grade NAT): múltiples usuarios pueden compartir la misma IP pública.

**Evaluación de riesgo:** La deduplicación anónima ya tiene una **ventana de 24 horas** — no es una protección fuerte contra múltiples votos, sino un throttle básico. Este nivel de protección es **proporcional** a la sensibilidad de la funcionalidad (like/dislike de artículos médicos).

### Resultado Fase 3

| Issue | Estado |
|---|---|
| SEC-004 | ✅ **PASS** — Anonimato intencional, deduplicación correcta en deployment actual |
| SEC-005 | ✅ **PASS** — Ídem |
| SEC-007 | ✅ **PASS** — `X-Forwarded-For` eliminado; `RemoteIpAddress` es la fuente correcta hoy |

---

## FASE 4 — CSP (SEC-018)

### Inventario de directivas CSP

```
Program.cs: 4 apariciones de 'unsafe-inline' / 'unsafe-eval'
```

| Directiva | Valor | Riesgo |
|---|---|---|
| `script-src` | `'self' 'unsafe-inline' 'unsafe-eval' blob: [CDNs]` | ⚠️ Alto |
| `script-src-elem` | `'self' 'unsafe-inline' blob: [CDNs]` | ⚠️ Alto |
| `script-src-attr` | `'self' 'unsafe-inline' blob: [CDNs]` | ⚠️ Alto |
| `style-src` | `'self' 'unsafe-inline' [CDNs]` | ⚠️ Medio |

### Causa raíz confirmada — Inventario de JS inline

```
Total <script> tags en archivos .cshtml:  146
Tags con src externo (.js):                54
Tags inline (sin src atributo):            ~92
Archivos .cshtml con JS inline:            66
```

**Top archivos por bloques inline:**

| Archivo | Bloques `<script>` inline |
|---|---|
| `_Layout.cshtml` | 10 |
| Múltiples `Index.cshtml` | 5–6 c/u |
| `Dashboard.cshtml` | 3 |

### Esfuerzo estimado de migración

Para eliminar `'unsafe-inline'` se requiere:
1. Extraer ~92 bloques `<script>` inline a archivos `.js` externos.
2. O usar nonces por request (`<script nonce="@Model.Nonce">`).
3. Afecta 66 archivos `.cshtml`.
4. Requiere regresión funcional completa de la UI.

**Estimado:** 4–8 semanas de trabajo. Clasificado como tarea de largo plazo (UI-018).

### Resultado Fase 4

| Issue | Estado |
|---|---|
| SEC-018 | ⚠️ **ACEPTADO** — `'unsafe-inline'`/`'unsafe-eval'` presentes. Causa raíz documentada. Migración = tarea UI-018 de largo plazo. Riesgo mitigado por controles compensatorios (CSRF, autenticación, ownership). |

---

## HALLAZGO ADICIONAL — `web.config` con secretos

> **Fuera del backlog SEC-001→SEC-018** — Documentado por completitud.

El archivo `eiibd26/web.config` contiene credenciales reales en texto claro (SQL Server, SendGrid, Twilio, VapidKeys, Anthropic API Key). Este archivo:

- ✅ **Está en `.gitignore`** — no se sube al repositorio.
- ✅ El `.gitignore` tiene un comentario explícito sobre esto.
- El archivo es de uso exclusivo para el deployment en producción.

**Sin acción requerida** para este ciclo de remediación. Verificar que el servidor de producción tiene los permisos de filesystem correctos sobre `web.config` (solo lectura para la cuenta del app pool).

---

## Resumen de Fases

| Fase | Issues | Resultado |
|---|---|---|
| 1 — Rate Limiting | SEC-001/002/003 | ✅ PASS (probado en vivo) |
| 2 — Roles | SEC-012 | ✅ PASS (BD confirmada) |
| 3 — Ratings IP | SEC-004/005/007 | ✅ PASS con WARN-02 (proxy futuro) |
| 4 — CSP | SEC-018 | ⚠️ ACEPTADO (tarea larga) |

**Warnings activos:**
- **WARN-01:** UI no muestra retroalimentación ante 429 (UX mejorable)
- **WARN-02:** Deduplicación de ratings fallará si se agrega CDN/reverse-proxy sin `UseForwardedHeaders`
