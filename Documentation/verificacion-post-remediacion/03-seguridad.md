# 03 – Seguridad: CSRF + Ownership

**Fecha:** 2025-07-10  
**Issues cubiertos:** SEC-008, SEC-010, SEC-011

---

## A. CSRF — Inventario Global `[IgnoreAntiforgeryToken]`

### Búsqueda ejecutada
```
Select-String -Pattern "IgnoreAntiforgeryToken|ValidateAntiForgeryToken|ValidateAntiforgery"
```

### Resultados

| Archivo | Línea | Tipo | Justificado |
|---------|-------|------|-------------|
| `Controllers/MoodApiController.cs` | 35 | `[IgnoreAntiforgeryToken]` | ✅ **Justificado** — endpoint público de push notification con token de corta duración propio (`IPushMoodTokenService`). No maneja sesión. |
| `Areas/Identity/Pages/Admin/Usuarios/Index.cshtml.cs` | 662 | `[IgnoreAntiforgeryToken]` | ⚠️ **Revisar** — Acción de admin con datos de usuario. Requiere evaluación. |
| `Areas/Identity/Pages/Admin/Usuarios/Index.cshtml.cs` | 754 | `[IgnoreAntiforgeryToken]` | ⚠️ **Revisar** — Segunda acción en misma página admin. |
| `Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml.cs` | 15 | `[IgnoreAntiforgeryToken]` | ⚠️ **Revisar** — Page de admin de contenidos. AJAX DataTable handler. |
| `Pages/Error.cshtml.cs` | 8 | `[IgnoreAntiforgeryToken]` | ✅ **Justificado** — Página de error estática. Sin mutaciones. |

### Páginas clínicas remediadas (SEC-008)

Las siguientes páginas clínicas **ya no tienen** supresión de antiforgery y fueron corregidas en remediación:

| Página | Estado |
|--------|--------|
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | ✅ Antiforgery activo |
| `Pages/Directorio/Activar.cshtml.cs` | ✅ Antiforgery activo |
| Formularios Razor clínicos (`<form method="post">`) | ✅ Token incluido via Tag Helpers asp-antiforgery por defecto |

### AJAX / fetch POST — Verificación tokens

| Archivo | Endpoint | Token presente |
|---------|----------|----------------|
| `Pages/Preguntas/Detalles.cshtml` | `/api/preguntas/{id}/votar`, `/api/respuestas`, `/api/respuestas/{id}/eliminar`, `/api/respuestas/{id}/feedback` | ✅ Hidden form `__antiForgeryForm` + `RequestVerificationToken` en headers (línea 1824) |
| `Pages/Preguntas/Preguntas.cshtml` | `/api/preguntas/{id}/votar` | ⚠️ No se encontró token en headers — solo `Content-Type: application/json`. Requiere revisión. |
| `Pages/DirectorioMedicos/Detalle.cshtml` | `<form asp-page-handler>` | ✅ Tag Helper incluye token automáticamente |
| `Pages/Directorio/Activar.cshtml` | `<form method="post">` | ✅ Token incluido |
| `Controllers/DashboardController.cs` | `add-mood`, `add-sintoma` | ✅ `[ValidateAntiForgeryToken]` en líneas 195 y 217 |

---

## B. Ownership — Validación de Entidades Clínicas

### `ClinicalOwnershipValidator` — Cobertura

Registrado en DI: ✅ `AddScoped<ClinicalOwnershipValidator>()`

Métodos disponibles:

| Método | Entidad |
|--------|---------|
| `OwnsCondicionAsync` | condicionUsuario |
| `OwnsSintomaAsync` | sintomasUsuario |
| `OwnsTratamientoAsync` | tratamientoUsuario |
| `OwnsEstadoAnimoAsync` | EstadoAnimoUsuario |
| `ValidateEstadoAnimoRelationsAsync` | FK opcionales de estado ánimo |

### Uso verificado en controladores

| Controlador | Método | Validación |
|-------------|--------|------------|
| `EstadoAnimoUsuarioController.Registrar()` | SEC-010 | ✅ `_ownership.ValidateEstadoAnimoRelationsAsync(condicionUsuarioId, sintomaUsuarioId, tratamientoUsuarioId, guid)` |
| `EstadoAnimoUsuarioController.Eliminar()` | | ✅ `FirstOrDefaultAsync(e => e.Id == id && e.IdUsuario == guid)` (filtro directo) |
| `DashboardController.AddSymptom()` | SEC-011 | ✅ `_ownership.OwnsSintomaAsync(sintomaUsuarioId, userGuid)` → Forbid() |
| `DashboardController.AddMood()` | | ⚠️ No llama validator. Solo filtra `userGuid` en insert. Aceptable para creación, no para update/delete. |
| `PreguntasApiController.Eliminar()` | Ownership propio | ✅ `p.UsuarioId != userId.Value` → Forbid |
| `PreguntasApiController.ActualizarPregunta()` | Ownership propio | ✅ `pregunta.UsuarioId != userId.Value` → Forbid |
| `PreguntasApiController.VotarPregunta()` | | ✅ `_userManager.GetUserAsync` + validación de usuario |

### Riesgos de Ownership Residuales

| ID | Riesgo | Severidad |
|----|--------|-----------|
| R-SEC-01 | `Preguntas.cshtml` fetch POST a `/api/preguntas/{id}/votar` no incluye `RequestVerificationToken` en headers | 🔴 **FAIL** – CSRF en voting de lista |
| R-SEC-02 | `Areas/Identity/Pages/Admin/Usuarios/Index.cshtml.cs` líneas 662, 754: acciones admin con `IgnoreAntiforgeryToken` sin justificación documentada | 🟠 Medio |
| R-SEC-03 | `Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml.cs` suprime antiforgery clase entera para DataTable AJAX | 🟠 Medio (admin-only, but best practice violation) |
| R-SEC-04 | `DashboardController.AddMood()` no valida ownership de `relacionId` (condicionUsuarioId opcional) | 🟡 Bajo — sólo creación, usuario autenticado, ID se asigna directo |

---

## Veredicto Fase 3

| Criterio | Estado |
|----------|--------|
| Páginas clínicas críticas sin IgnoreAntiforgery | ✅ PASS |
| Ownership en estado ánimo, síntomas | ✅ PASS |
| Ownership en P&R (Preguntas / Respuestas) | ✅ PASS |
| Token CSRF en fetch POSTs de Detalles.cshtml | ✅ PASS |
| Token CSRF en fetch POSTs de Preguntas.cshtml (listado) | ❌ **FAIL** |
| Admin Usuarios acciones con IgnoreAntiforgeryToken | ⚠️ WARN |
| **VEREDICTO GLOBAL** | ⚠️ **PASS CONDICIONADO** — Un FAIL no bloqueante (funcionalidad de voto, sin escalada de privilegios) + 2 WARN admin |

> El FAIL en `Preguntas.cshtml` es de bajo riesgo de impacto (voting público), pero debe documentarse para corrección antes de producción.
