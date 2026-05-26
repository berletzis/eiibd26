# Verificación FUNC-012 · FUNC-027 · FUNC-028 — Integridad BD

**Fecha:** 2026-05-25  
**Archivos verificados:**
- `Controllers/TratamientosAdminController.cs`
- `Models/tratamientos.cs`
- `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs`
- `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml`

---

## FUNC-012 — NombreSugeridoIA protege el nombre original del tratamiento

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | Modelo `tratamientos` tiene `public string? NombreSugeridoIA { get; set; }` | ✅ PASS | `Models/tratamientos.cs:31` |
| 2 | `GenerateIaDescription` NO asigna `tratamiento.nombre = nombreTraducido` | ✅ PASS | `TratamientosAdminController.cs:54-62` — solo asigna `NombreSugeridoIA`, el campo `nombre` queda intacto |
| 3 | Cuando IA sugiere nombre distinto → asigna `tratamiento.NombreSugeridoIA = nombreTraducido` | ✅ PASS | `TratamientosAdminController.cs:60` |
| 4 | Junto a sugerencia → marca `tratamiento.ValidadoHumano = false` | ✅ PASS | `TratamientosAdminController.cs:61` |
| 5 | `BatchGenerateIaDescriptions` replica mismo patrón (no sobreescribe `nombre`) | ✅ PASS | `TratamientosAdminController.cs:312-321` — misma lógica, `nombre` intacto |

**Subtotal FUNC-012: 5/5 ✅ PASS**

---

## FUNC-027 — EditarFechaInicio de condición usa PK (`id`), no FK (`idCondicion`)

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 6 | Handler `OnPostEditarFechaInicioAsync` recibe `int condUsuarioId` (PK de `condicionesUsuario`) | ✅ PASS | `UsuarioCondiciones.cshtml.cs:168` — parámetro `condUsuarioId` |
| 7 | Query filtra `x.idUsuario == userId && x.id == condUsuarioId` (no `x.idCondicion`) | ✅ PASS | `UsuarioCondiciones.cshtml.cs:173-174` |
| 8 | Vista envía el PK correcto con `name="condUsuarioId"` | ✅ PASS | `UsuarioCondiciones.cshtml:96` — input oculto envía PK |

**Subtotal FUNC-027: 3/3 ✅ PASS**

---

## FUNC-028 — EliminarCondicion usa PK + verifica relaciones antes de soft-delete

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 9 | Handler `OnPostEliminarCondicionAsync` recibe `int condUsuarioId` (PK) | ✅ PASS | `UsuarioCondiciones.cshtml.cs:185` — parámetro `condUsuarioId` |
| 10 | Query filtra `x.idUsuario == userId && x.id == condUsuarioId` | ✅ PASS | `UsuarioCondiciones.cshtml.cs:190-191` |
| 11 | Verifica `SintomaCondicionUsuario.AnyAsync(x.IdCondicionUsuario == rel.id)` → BadRequest si existe | ✅ PASS | `UsuarioCondiciones.cshtml.cs:195-198` |
| 12 | Verifica también `TratamientoCondicionUsuario.AnyAsync(x.IdCondicionUsuario == rel.id)` → BadRequest si existe | ✅ PASS | `UsuarioCondiciones.cshtml.cs:200-203` |
| 13 | Vista envía PK correcto en formulario de eliminar (`name="condUsuarioId"`) | ✅ PASS | `UsuarioCondiciones.cshtml:248` — JS construye input con nombre correcto |

**Subtotal FUNC-028: 5/5 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-012 | 5 | 5 | 0 | 0 |
| FUNC-027 | 3 | 3 | 0 | 0 |
| FUNC-028 | 5 | 5 | 0 | 0 |
| **TOTAL** | **13** | **13** | **0** | **0** |

**Veredicto: ✅ APTO**
