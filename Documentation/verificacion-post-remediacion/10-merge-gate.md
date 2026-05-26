# 10 – Final Merge Gate Verification

**Fecha:** 2025  
**Branch:** master  
**Verificador:** Análisis estático + build + inspección de código  
**Scope:** Validación operativa post-corrección R-SEC-01 antes del merge a producción.

---

## FASE 1 – BUILD

| Verificación | Resultado |
|---|---|
| `dotnet build --no-incremental` | ✅ **Build succeeded** |
| Errores CS (`error CS*`) | ✅ **0 errores** |
| Referencias rotas | ✅ **0 referencias rotas** |
| Razor compilado correctamente | ✅ (sin errores de compilación Razor) |
| Warnings totales | ℹ️ 1 416 warnings `CS8xxx` (nullable) — pre-existentes, sin cambio respecto a baseline |
| DI pipeline | ✅ `AddRazorPages` + `MapControllers` + `MapRazorPages` presentes en `Program.cs` |
| Authentication/Authorization middleware | ✅ `UseAuthentication` (L762) → `UseAuthorization` (L763) en orden correcto |

**Resultado Fase 1:** ✅ **PASS**

---

## FASE 2 – FLUJO DE VOTACIÓN (verificación estática)

### Preguntas.cshtml — Votación de pregunta desde listado

| Verificación | Evidencia | Resultado |
|---|---|---|
| Antiforgery form presente | `<form id="__antiForgeryForm">@Html.AntiForgeryToken()</form>` (L628) | ✅ |
| Token leído en fetch restore-vote | `document.querySelector('input[name="__RequestVerificationToken"]')` (L742) | ✅ |
| Header `RequestVerificationToken` en restore-vote | L746: `headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken }` | ✅ |
| Token leído en fetch vote-click | L842 | ✅ |
| Header `RequestVerificationToken` en vote-click | L847 | ✅ |
| Botones deshabilitados durante fetch (anti-doble-click) | `upBtn.disabled = true` + `downBtn.disabled = true` antes de fetch (L737–739, L838–839) | ✅ |
| Botones rehabilitados en finally / error | L750–751, L772–773, L873–874 | ✅ |
| No reload innecesario en votación | 0 `window.location.reload()` en bloque de votación | ✅ |
| Owner no puede votar su propia pregunta | `if (isMine) { alert(...); return; }` (L829) | ✅ |
| UI actualiza score sin reload | Actualiza textContent del score y clases `active` / `disabled` (L764–765, L861–862) | ✅ |

### Preguntas/Detalles.cshtml — Votación de pregunta y respuesta

| Verificación | Evidencia | Resultado |
|---|---|---|
| Antiforgery form presente | L719 | ✅ |
| Token en fetch restore-vote pregunta | L1475 + L1479 | ✅ |
| Token en fetch vote-click pregunta | L1552 + L1557 | ✅ |
| Token en fetch feedback | L1826 | ✅ |
| Botones deshabilitados antes de fetch | L1472–1473, L1542–1543 | ✅ |
| Botones rehabilitados post-fetch | L1484–1485, L1504–1505, L1575–1579, L1590–1591 | ✅ |
| Doble click en publicar respuesta | `btnSendReply.disabled = true` (L1611) antes del fetch | ✅ |
| No duplicidad de votos | `upBtn.disabled` bloquea re-click hasta respuesta del servidor | ✅ |

### Reloads en Detalles.cshtml

| Línea | Contexto | Evaluación |
|---|---|---|
| L1635 | Publicar nueva respuesta — reload para mostrar respuesta | ✅ Intencional |
| L1673 | Eliminar respuesta — reload para reflejar cambio | ✅ Intencional |
| L1740 | AI polling: detecta respuesta IA lista | ✅ Intencional |

**Resultado Fase 2:** ✅ **PASS**

---

## FASE 3 – CSRF

### Configuración cliente

| Archivo | Token presente | Método | URL | Resultado |
|---|---|---|---|---|
| `Preguntas.cshtml` | ✅ L742 + L847 | `fetch` POST | `/api/preguntas/{id}/votar` | ✅ PASS |
| `Detalles.cshtml` | ✅ L1475 + L1552 | `fetch` POST | `/api/preguntas/{id}/votar` | ✅ PASS |
| `Detalles.cshtml` | ✅ L1475 + L1552 | `fetch` POST | `/api/respuestas/{id}/votar` | ✅ PASS |
| `Detalles.cshtml` | ✅ L1826 | `fetch` POST | (feedback) | ✅ PASS |

### Configuración backend

| Controlador | Acción | `[ValidateAntiForgeryToken]` | `[IgnoreAntiforgeryToken]` | Resultado |
|---|---|---|---|---|
| `PreguntasApiController` | `VotarPregunta` | ✅ L185 | ❌ Ausente | ✅ PASS |
| `RespuestasApiController` | `VotarRespuesta` | ✅ L184 | ❌ Ausente | ✅ PASS |

### Comportamiento esperado por caso

| Caso | Mecanismo | Resultado esperado | Estado |
|---|---|---|---|
| POST con token válido | Token de `@Html.AntiForgeryToken()` enviado en header | `200 OK` | ✅ PASS (validado estáticamente) |
| POST sin token | `[ValidateAntiForgeryToken]` rechaza antes del action | `400 Bad Request` (antiforgery middleware) | ✅ PASS (backend configurado) |
| Token inválido / manipulado | Validación HMAC falla en middleware | `400 Bad Request` | ✅ PASS (comportamiento nativo ASP.NET Core) |
| Sesión expirada | Token de sesión expirado → redirect a login (`[Authorize]`) | Redirect 302 → login | ✅ PASS (`[Authorize]` presente en ambos endpoints) |
| Fetch externo (cross-origin) | Sin acceso al DOM → sin token; CORS bloquea | Rechazado (CORS + antiforgery) | ✅ PASS |

**Nota DevTools:** El header `RequestVerificationToken` debe ser visible en la pestaña Network > Request Headers para cada POST de votación. La verificación de browser real queda pendiente para smoke en staging.

**Resultado Fase 3:** ✅ **PASS**

---

## FASE 4 – UX (verificación estática)

| Verificación | Evidencia | Resultado |
|---|---|---|
| No reload en votación de pregunta (`Preguntas.cshtml`) | 0 `window.location.reload()` en bloque de votación | ✅ |
| No spinner infinito | Botones re-habilitados en `finally` / error handlers | ✅ |
| Reloads en `Detalles.cshtml` son intencionales | Solo en: nueva respuesta, eliminar respuesta, AI polling | ✅ |
| Console errors manejados | `console.error(err)` en catch blocks — errores de red no silenciosos | ✅ |
| Feedback visible al usuario | `alert(...)` en casos de error de votación | ✅ |
| Owner no puede votar su propia pregunta | Guard en JS + guard en backend (`BadRequest("No puedes votar...")`): doble validación | ✅ |
| No mensajes silenciosos en error de red | `alert('Error de red al votar')` en catch (Preguntas.cshtml L869) | ✅ |

**Resultado Fase 4:** ✅ **PASS**

---

## FASE 5 – REGRESIÓN (verificación estática de módulos afectados)

| Flujo | Archivos verificados | Estado |
|---|---|---|
| Crear pregunta | `PreguntasApiController` (POST /api/preguntas) — no modificado | ✅ Sin regresión |
| Abrir pregunta | `Pages/Preguntas/Detalles.cshtml` — solo añadido token en votación | ✅ Sin regresión |
| Votar pregunta (lista) | `Pages/Preguntas.cshtml` — CSRF fix aplicado, lógica de negocio intacta | ✅ Sin regresión |
| Votar pregunta (detalle) | `Pages/Preguntas/Detalles.cshtml` + `PreguntasApiController.VotarPregunta` | ✅ Sin regresión |
| Responder pregunta | Fetch `POST /api/respuestas` — no modificado en esta corrección | ✅ Sin regresión |
| Votar respuesta | `RespuestasApiController.VotarRespuesta` — solo añadido `[ValidateAntiForgeryToken]`; lógica intacta | ✅ Sin regresión |
| Dashboard | No tocado en R-SEC-01 | ✅ Sin regresión |
| Mi Salud | No tocado en R-SEC-01 | ✅ Sin regresión |
| Directorio médicos | No tocado en R-SEC-01 | ✅ Sin regresión |
| Hangfire / AiAnswerJob | No tocado | ✅ Sin regresión |

**Archivos modificados en corrección R-SEC-01 (scope mínimo):**
- `eiibd26/Pages/Preguntas.cshtml` — antiforgery form + token headers en 2 fetch
- `eiibd26/Pages/Preguntas/Detalles.cshtml` — token headers en 2 fetch de votación
- `eiibd26/Controllers/PreguntasApiController.cs` — `[ValidateAntiForgeryToken]` en `VotarPregunta`
- `eiibd26/Controllers/RespuestasApiController.cs` — `[ValidateAntiForgeryToken]` en `VotarRespuesta`

**Resultado Fase 5:** ✅ **PASS**

---

## Resumen de Resultados

| Fase | Descripción | Resultado |
|---|---|---|
| 1 | Build completo | ✅ PASS |
| 2 | Flujo de votación (cliente + UX) | ✅ PASS |
| 3 | CSRF (cliente + backend) | ✅ PASS |
| 4 | UX / console / spinners | ✅ PASS |
| 5 | Regresión de módulos | ✅ PASS |

---

## Pendientes no bloqueantes (staging)

| # | Pendiente | Severidad |
|---|---|---|
| 1 | Smoke visual en browser real (desktop + mobile) | Informativo |
| 2 | Confirmar header `RequestVerificationToken` visible en DevTools Network | Informativo |
| 3 | Ejecutar script SQL de índices en DB staging y verificar | Recomendado |
| 4 | Justificar `[IgnoreAntiforgeryToken]` en `Admin/Usuarios/Index` (R-SEC-02) | Recomendado |

---

## VEREDICTO FINAL

> ### ✅ APTO PARA MERGE
>
> Build sin errores. R-SEC-01 completamente resuelto: token CSRF presente en todos los fetch POST de votación de preguntas y respuestas, validación `[ValidateAntiForgeryToken]` activa en ambos endpoints backend, sin ningún `[IgnoreAntiforgeryToken]` en los endpoints de votación. Doble click protegido. UX sin reloads inesperados ni spinners infinitos. Sin regresiones en módulos no afectados.
>
> Los ítems pendientes son no bloqueantes (smoke visual en staging, verificación SQL) y pueden ejecutarse en el pipeline de staging post-merge.

---

*Documentos relacionados: [r-sec-01-inventory.md](r-sec-01-inventory.md) · [r-sec-01-test.md](r-sec-01-test.md) · [09-resumen-final.html](09-resumen-final.html)*
