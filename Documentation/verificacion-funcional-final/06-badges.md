# Verificación FUNC-025 — Badges Médicos

**Fecha:** 2026-05-25  
**Archivo verificado:** `Services/Medico/MedicoBadgeService.cs`

---

## FUNC-025 — Badge `participante_qa` excluye respuestas de IA

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | Query de `participante_qa` filtra `&& !r.EsIA` (excluye respuestas generadas por IA) | ✅ PASS | `MedicoBadgeService.cs:116` — `CountAsync(r => r.UsuarioId == perfil.UserId.Value && !r.Eliminado && !r.EsIA)` |
| 2 | Query filtra también `&& !r.Eliminado` (excluye respuestas borradas) | ✅ PASS | mismo predicado |
| 3 | Umbral correcto: `>= 3` respuestas para otorgar badge | ✅ PASS | `if (respuestas >= 3) await OtorgarBadgeAsync(...)` |
| 4 | Solo se evalúa si el médico tiene `UserId` vinculado (`perfil?.UserId != null`) | ✅ PASS | bloque condicional `if (perfil?.UserId != null)` |

**Subtotal FUNC-025: 4/4 ✅ PASS**

---

## Nota adicional — Badge `activo_comunidad` (fuera de scope FUNC-025)

El badge `activo_comunidad` (línea 102) aún lee de `_db.DirectorioMedicoConfirmaciones` en lugar de `ConfirmacionesComunitarias`. Esta condición es **pre-existente** y está fuera del alcance de FUNC-025, que solo cubría el badge `participante_qa`. Se documenta como deuda técnica en el reporte de regresiones (08-regresiones.md).

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-025 | 4 | 4 | 0 | 0 |
| **TOTAL** | **4** | **4** | **0** | **0** |

**Veredicto: ✅ APTO**
