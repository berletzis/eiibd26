# Verificación FUNC-018 · FUNC-019 · FUNC-020 — Preguntas y Respuestas

**Fecha:** 2026-05-25  
**Archivos verificados:**
- `Controllers/PreguntasApiController.cs`
- `Controllers/RespuestasApiController.cs`

---

## FUNC-018 — Idempotency en job de IA para preguntas

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | Antes de encolar `AiAnswerJob` via Hangfire, verifica `AnyAsync(r => r.PreguntaId == pregunta.Id && r.EsIA && !r.Eliminado)` | ✅ PASS | `PreguntasApiController.cs:94-101` — `var yaExisteRespuestaIA = await _db.Respuestas.AnyAsync(...)` |
| 2 | Si ya existe respuesta IA → NO encola (no duplica jobs) | ✅ PASS | `if (!yaExisteRespuestaIA) { _backgroundJobs.Enqueue<...>(...); }` |
| 3 | Si no existe → encola normalmente | ✅ PASS | mismo bloque condicional |

**Subtotal FUNC-018: 3/3 ✅ PASS**

---

## FUNC-019 — Eliminar respuesta verifica propiedad y estado

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 4 | Query en `Eliminar` incluye `&& !x.Eliminado` (no revive respuestas ya borradas) | ✅ PASS | `RespuestasApiController.cs:62` — `FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado)` |
| 5 | Si `UsuarioId != userId` → retorna `NotFound` (no revela existencia del recurso ajeno) | ✅ PASS | `RespuestasApiController.cs:65-66` — `return NotFound(new { ... })` en lugar de `Forbid` |

**Subtotal FUNC-019: 2/2 ✅ PASS**

---

## FUNC-020 — Validación de longitud en cuerpo de respuesta

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 6 | Cuerpo < 10 caracteres (tras Trim) → BadRequest con mensaje descriptivo | ✅ PASS | `RespuestasApiController.cs:116-117` — `if (dto.Cuerpo.Trim().Length < 10) return BadRequest(...)` |
| 7 | Cuerpo > 10 000 caracteres → BadRequest con mensaje descriptivo | ✅ PASS | `RespuestasApiController.cs:119-120` — `if (dto.Cuerpo.Length > 10000) return BadRequest(...)` |

**Subtotal FUNC-020: 2/2 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-018 | 3 | 3 | 0 | 0 |
| FUNC-019 | 2 | 2 | 0 | 0 |
| FUNC-020 | 2 | 2 | 0 | 0 |
| **TOTAL** | **7** | **7** | **0** | **0** |

**Veredicto: ✅ APTO**
