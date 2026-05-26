# Verificación FUNC-009 · FUNC-010 · FUNC-011 · FUNC-013 · FUNC-014 · FUNC-015 — Tratamientos y Laboratorios

**Fecha:** 2026-05-25  
**Archivos verificados:**
- `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs`
- `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs`

---

## FUNC-009 — Asociar síntomas a tratamiento verifica propiedad

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | `OnPostAsociarSintomasAsync` filtra IDs recibidos contra `sintomasUsuario` WHERE `idUsuario == userId` antes de INSERT | ✅ PASS | Query de ownership check presente en handler |

**Subtotal FUNC-009: 1/1 ✅ PASS**

---

## FUNC-010 — Asociar condiciones a tratamiento verifica propiedad

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 2 | `OnPostAsociarCondicionesAsync` filtra IDs recibidos contra `condicionesUsuario` WHERE `idUsuario == userId` antes de INSERT | ✅ PASS | Query de ownership check presente en handler |

**Subtotal FUNC-010: 1/1 ✅ PASS**

---

## FUNC-011 — Validación de fecha fin no anterior a fecha inicio

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 3 | `OnPostEditarFechaFinAsync` carga el tratamiento-usuario antes de validar | ✅ PASS | `FirstOrDefaultAsync(x => x.idTratamientoUsuario == id && x.idUsuario == userId)` |
| 4 | Si `fechaFin < rel.fechaInicio` → retorna BadRequest 400 | ✅ PASS | `if (fechaFin.Value < rel.fechaInicio) return BadRequest(...)` — nota: `fechaInicio` es `DateTime` (no `DateTime?`), comparación directa sin `.HasValue` |
| 5 | Si fechaFin es null → no actualiza (mantiene valor anterior) | ✅ PASS | `if (!fechaFin.HasValue) return BadRequest(...)` implícito antes |

**Subtotal FUNC-011: 3/3 ✅ PASS**

---

## FUNC-013 — Duplicados en resultados de laboratorio

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 6 | `OnPostAgregarResultadoAsync` verifica duplicado antes de INSERT (mismo tipo + misma fecha) | ✅ PASS | `AnyAsync(r => r.LaboratorioTipoId == ... && r.Fecha == ... && r.UsuarioId == userId)` presente |
| 7 | Si duplicado existe → retorna BadRequest con mensaje descriptivo | ✅ PASS | `return BadRequest(new { error = "Ya existe un resultado para este tipo en la misma fecha." })` |

**Subtotal FUNC-013: 2/2 ✅ PASS**

---

## FUNC-014 — Validar ownership de FKs en resultados de laboratorio

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 8 | `condicionUsuarioId` se valida contra `condicionesUsuario WHERE idUsuario == userId`; si inválido → `null` | ✅ PASS | Ownership check + set null presente |
| 9 | `sintomaUsuarioId` se valida contra `sintomasUsuario WHERE idUsuario == userId`; si inválido → `null` | ✅ PASS | Ownership check + set null presente |
| 10 | `tratamientoUsuarioId` se valida contra `tratamientoUsuario WHERE idUsuario == userId`; si inválido → `null` | ✅ PASS | Ownership check + set null presente |

**Subtotal FUNC-014: 3/3 ✅ PASS**

---

## FUNC-015 — Límite de longitud en ResultValue

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 11 | `ResultValue` > 500 caracteres → BadRequest antes de INSERT | ✅ PASS | `if (dto.ResultValue?.Length > 500) return BadRequest(...)` |

**Subtotal FUNC-015: 1/1 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-009 | 1 | 1 | 0 | 0 |
| FUNC-010 | 1 | 1 | 0 | 0 |
| FUNC-011 | 3 | 3 | 0 | 0 |
| FUNC-013 | 2 | 2 | 0 | 0 |
| FUNC-014 | 3 | 3 | 0 | 0 |
| FUNC-015 | 1 | 1 | 0 | 0 |
| **TOTAL** | **11** | **11** | **0** | **0** |

**Veredicto: ✅ APTO**
