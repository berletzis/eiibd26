# Verificación FUNC-004 · FUNC-005 · FUNC-006 · FUNC-007 · FUNC-008 — Usuario Síntomas

**Fecha:** 2026-05-25  
**Archivo verificado:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs`

---

## FUNC-004 — Eliminar síntoma verifica registros de tracking activos

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | `OnPostEliminarSintomaAsync` consulta `TrackingSintomaUsuario` antes de eliminar | ✅ PASS | `AnyAsync(t => t.SintomaUsuarioId == rel.id)` presente |
| 2 | Si existen trackings → retorna BadRequest 400 con mensaje descriptivo | ✅ PASS | `return BadRequest(new { error = "..." })` cuando `tieneTracking == true` |

**Subtotal FUNC-004: 2/2 ✅ PASS**

---

## FUNC-005 — FechaCreacion usa UTC en UsuarioSintomas

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 3 | `FechaCreacion` asignada con `DateTime.UtcNow` (no `DateTime.Now`) | ✅ PASS | `sintomasUsuario` initializer usa `DateTime.UtcNow` |
| 4 | `FechaInicio` asignada con `DateTime.UtcNow` | ✅ PASS | mismo initializer |
| 5 | `UltimaActividad` asignada con `DateTime.UtcNow` | ✅ PASS | mismo initializer |

**Subtotal FUNC-005: 3/3 ✅ PASS**

---

## FUNC-006 — Validación de rango de fecha de inicio

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 6 | `OnPostEditarFechaInicioAsync` rechaza fechas anteriores a 1900-01-01 | ✅ PASS | `if (fecha < new DateTime(1900,1,1)) return BadRequest(...)` |
| 7 | Rechaza fechas futuras (> `DateTime.UtcNow`) | ✅ PASS | `if (fecha > DateTime.UtcNow) return BadRequest(...)` |

**Subtotal FUNC-006: 2/2 ✅ PASS**

---

## FUNC-007 — Asociar condiciones verifica propiedad del usuario

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 8 | `OnPostAsociarCondicionesAsync` filtra IDs recibidos contra `condicionesUsuario` WHERE `idUsuario == userId` antes de INSERT | ✅ PASS | Query de ownership check presente: `var idsValidos = await _db.CondicionesUsuario.Where(c => c.idUsuario == userId && condicionIds.Contains(c.id)).Select(c => c.id).ToListAsync()` |

**Subtotal FUNC-007: 1/1 ✅ PASS**

---

## FUNC-008 — Asociar tratamientos verifica propiedad del usuario

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 9 | `OnPostAsociarTratamientosAsync` filtra IDs recibidos contra `tratamientoUsuario` WHERE `idUsuario == userId` antes de INSERT | ✅ PASS | Query de ownership check presente: similar a FUNC-007 |

**Subtotal FUNC-008: 1/1 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-004 | 2 | 2 | 0 | 0 |
| FUNC-005 | 3 | 3 | 0 | 0 |
| FUNC-006 | 2 | 2 | 0 | 0 |
| FUNC-007 | 1 | 1 | 0 | 0 |
| FUNC-008 | 1 | 1 | 0 | 0 |
| **TOTAL** | **9** | **9** | **0** | **0** |

**Veredicto: ✅ APTO**
