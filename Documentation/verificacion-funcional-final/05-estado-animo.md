# Verificación FUNC-002 · FUNC-003 — Estado de Ánimo

**Fecha:** 2026-05-25  
**Archivos verificados:**
- `Controllers/EstadoAnimoUsuarioController.cs`
- `Models/EstadoAnimoUsuario.cs`

---

## FUNC-002 — Fecha de registro aceptable (±24 h, no futura)

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | Acepta `fechaRegistro` como `[FromForm] DateTime?` parameter (no solo body JSON) | ✅ PASS | Firma: `public async Task<IActionResult> Crear([FromForm] DateTime? fechaRegistro, ...)` |
| 2 | Fecha válida (≤ UtcNow && ≥ UtcNow - 24h) → se usa con `SpecifyKind(DateTimeKind.Utc)` | ✅ PASS | `DateTime.SpecifyKind(fechaRegistro.Value, DateTimeKind.Utc)` cuando condición se cumple |
| 3 | Fecha futura o más de 24h atrás → se usa `DateTime.UtcNow` como fallback | ✅ PASS | `else FechaRegistro = DateTime.UtcNow` |
| 4 | Fecha null → se usa `DateTime.UtcNow` | ✅ PASS | condición `fechaRegistro.HasValue` previo al check de rango |

**Subtotal FUNC-002: 4/4 ✅ PASS**

---

## FUNC-003 — Límite de longitud en campo Texto

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 5 | Modelo `EstadoAnimoUsuario` tiene `[MaxLength(2000)]` en propiedad `Texto` | ✅ PASS | `Models/EstadoAnimoUsuario.cs` — `[MaxLength(2000)] public string? Texto { get; set; }` |
| 6 | Controller valida `texto?.Length > 2000` → BadRequest antes de INSERT | ✅ PASS | `if (texto?.Length > 2000) return BadRequest(new { error = "..." })` en `Crear` action |

**Subtotal FUNC-003: 2/2 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-002 | 4 | 4 | 0 | 0 |
| FUNC-003 | 2 | 2 | 0 | 0 |
| **TOTAL** | **6** | **6** | **0** | **0** |

**Veredicto: ✅ APTO**
