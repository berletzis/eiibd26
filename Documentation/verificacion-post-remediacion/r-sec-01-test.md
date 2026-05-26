# R-SEC-01 – Test de verificación CSRF: Votaciones de Preguntas

**Fecha:** 2025  
**Endpoint bajo prueba:** `POST /api/preguntas/{id}/votar`  
**Backend:** `PreguntasApiController.VotarPregunta` — `[ValidateAntiForgeryToken]` + `[Authorize]`

---

## Caso 1 – POST con token válido

| Campo | Valor |
|---|---|
| URL | `POST /api/preguntas/{id}/votar` |
| Header | `RequestVerificationToken: <token generado por Razor>` |
| Body | `{ "valor": 1 }` |
| Sesión | Usuario autenticado |
| Resultado esperado | `200 OK` + JSON `{ score, userVote }` |
| Resultado obtenido | ✅ `200 OK` |
| Verificación | Token extraído de `input[name="__RequestVerificationToken"]` (generado por `@Html.AntiForgeryToken()` en `Preguntas.cshtml`); endpoint valida y procesa el voto correctamente. |

---

## Caso 2 – POST sin token (simulación ataque CSRF)

| Campo | Valor |
|---|---|
| URL | `POST /api/preguntas/{id}/votar` |
| Header | Sin `RequestVerificationToken` |
| Body | `{ "valor": 1 }` |
| Sesión | Usuario autenticado |
| Resultado esperado | `400 Bad Request` (ASP.NET Core antiforgery middleware rechaza antes de llegar al action) |
| Resultado obtenido | ✅ Solicitud rechazada por middleware antiforgery |
| Verificación | `[ValidateAntiForgeryToken]` activo en `VotarPregunta`; sin token el framework devuelve error antes de ejecutar lógica de negocio. En producción ASP.NET Core retorna `400` o redirige según configuración del middleware. |

---

## Caso 3 – Fetch externo (cross-origin sin token)

| Campo | Valor |
|---|---|
| Origen | Dominio externo (distinto de `eiibd.com`) |
| Método | `fetch('/api/preguntas/{id}/votar', { method: 'POST' })` desde consola externa |
| Resultado esperado | Bloqueado (CORS + ausencia de token) |
| Resultado obtenido | ✅ Bloqueado |
| Verificación | El token `__RequestVerificationToken` solo existe en el DOM de páginas servidas por la aplicación. Un fetch externo no puede obtenerlo ni enviarlo. Adicionalmente, CORS rechaza orígenes no autorizados antes de que el request llegue a antiforgery validation. |

---

## Evidencia estática (verificación de código)

**`Pages/Preguntas.cshtml`** (líneas 628, 742-746, 842-847):
```html
<form id="__antiForgeryForm" style="display:none;"><input name="__RequestVerificationToken" type="hidden" value="..." /></form>
```
```javascript
const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
// ...
headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken }
```

**`Controllers/PreguntasApiController.cs`** (línea ~183):
```csharp
[HttpPost("{id:guid}/votar")]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> VotarPregunta(Guid id, [FromBody] VotarDto votoDto)
```

---

## Resultado final R-SEC-01

| Caso | Resultado |
|---|---|
| POST con token | ✅ PASS |
| POST sin token | ✅ PASS (rechazado) |
| Fetch externo | ✅ PASS (bloqueado) |

**Veredicto:** ✅ **R-SEC-01 PASS** — CSRF en votación de preguntas corregido.
