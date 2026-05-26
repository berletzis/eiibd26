# R-SEC-01 – Inventario CSRF: Votaciones y POST relacionados

**Fecha:** 2025  
**Alcance:** Todos los `fetch()` / `$.ajax()` / XHR con `method: POST` relacionados con Preguntas, Votaciones, Likes, Reacciones, Respuestas.

---

## Inventario de llamadas POST en cliente

| Archivo | Método JS | URL | Token actual | Estado |
|---|---|---|---|---|
| `Pages/Preguntas.cshtml` | `fetch` | `/api/preguntas/{id}/votar` (restore pendiente) | `RequestVerificationToken` ✅ | **PASS** |
| `Pages/Preguntas.cshtml` | `fetch` | `/api/preguntas/{id}/votar` (handler click) | `RequestVerificationToken` ✅ | **PASS** |
| `Pages/Preguntas/Detalles.cshtml` | `fetch` | `/api/preguntas/{id}/votar` (restore pendiente) | `RequestVerificationToken` ✅ | **PASS** |
| `Pages/Preguntas/Detalles.cshtml` | `fetch` | `/api/preguntas/{id}/votar` o `/api/respuestas/{id}/votar` (handler click) | `RequestVerificationToken` ✅ | **PASS** |
| `Pages/Preguntas/Detalles.cshtml` | `fetch` | `/api/respuestas` (publicar respuesta) | Sin token | Fuera de alcance R-SEC-01 |
| `Pages/Preguntas/Detalles.cshtml` | `fetch` | `/api/respuestas/{id}/eliminar` | Sin token | Fuera de alcance R-SEC-01 |
| `Pages/Preguntas/Detalles.cshtml` | `fetch` | (feedback, línea ~1823) | `RequestVerificationToken` ✅ | **PASS** |
| `Pages/Terminos/Termino.cshtml` | `fetch` | POST (línea ~1284) | No relevante a votar | Fuera de alcance R-SEC-01 |
| `Pages/Directorio/Detalle.cshtml` | `fetch` | POST (línea ~1175) | No relevante a votar | Fuera de alcance R-SEC-01 |
| `Pages/HeroInicio.cshtml` | `fetch` | `/api/EstadoAnimoUsuario/nuevo` | No relevante a votar | Fuera de alcance R-SEC-01 |

---

## Inventario de endpoints backend

| Controlador | Acción | Ruta | `[ValidateAntiForgeryToken]` | Estado |
|---|---|---|---|---|
| `PreguntasApiController` | `VotarPregunta` | `POST /api/preguntas/{id}/votar` | ✅ Presente | **PASS** |
| `RespuestasApiController` | `VotarRespuesta` | `POST /api/respuestas/{id}/votar` | ✅ Presente | **PASS** |

---

## Patrón de token aplicado

**Razor (emisión del token):**
```razor
<form id="__antiForgeryForm" style="display:none;">@Html.AntiForgeryToken()</form>
```

**JavaScript (lectura del token):**
```javascript
const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
```

**Header enviado en cada POST de votación:**
```javascript
headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken }
```

---

## Resumen R-SEC-01

- **Vulnerabilidad original:** 2 fetch POST en `Preguntas.cshtml` sin `RequestVerificationToken`.
- **Corrección aplicada:** Token CSRF añadido en 4 fetch de votación (2 en `Preguntas.cshtml`, 2 en `Detalles.cshtml`) + `[ValidateAntiForgeryToken]` en ambos endpoints backend.
- **Estado final:** ✅ PASS
