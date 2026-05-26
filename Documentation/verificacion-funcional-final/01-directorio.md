# Verificación FUNC-023 · FUNC-024 · FUNC-026 — Directorio Médicos

**Fecha:** 2026-05-25  
**Archivos verificados:**
- `Pages/DirectorioMedicos/Detalle.cshtml.cs`
- `Services/Directorio/MedicoDirectorioService.cs`
- `Pages/DirectorioMedicos/Proponer.cshtml.cs`
- `Pages/DirectorioMedicos/ReclamarPerfil.cshtml.cs`

---

## FUNC-023 — Confirmación comunitaria escribe en tabla correcta

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 1 | `OnPostConfirmarSimpleAsync` escribe en `ConfirmacionesComunitarias` (no en `DirectorioMedicoConfirmaciones`) | ✅ PASS | `Detalle.cshtml.cs:135` — `_db.ConfirmacionesComunitarias.Add(...)` |
| 2 | Duplicate check usa misma tabla (`ConfirmacionesComunitarias`) | ✅ PASS | `Detalle.cshtml.cs:117` — `_db.ConfirmacionesComunitarias.AnyAsync(...)` |
| 3 | `TipoConfirmacion` se obtiene de DB con null guard antes de usarse | ✅ PASS | `Detalle.cshtml.cs:125-131` — `if (tipoConfirmacion is null)` → TempData["Error"] + return |
| 4 | `TotalConfirmaciones` en `OnGetAsync` lee de `ConfirmacionesComunitarias` | ✅ PASS | `Detalle.cshtml.cs:67` — `_db.ConfirmacionesComunitarias.CountAsync(c => c.MedicoDirectorioId == id && !c.Eliminado)` |
| 5 | `YaConfirme` en `OnGetAsync` lee de `ConfirmacionesComunitarias` | ✅ PASS | `Detalle.cshtml.cs:74` — `_db.ConfirmacionesComunitarias.AnyAsync(c => c.MedicoDirectorioId == id && c.UsuarioId == usuarioId.Value && !c.Eliminado)` |
| 6 | `RecalcularNivelConfianzaAsync` usa `ConfirmacionesComunitarias` | ✅ PASS | `MedicoDirectorioService.cs` — `_db.ConfirmacionesComunitarias.CountAsync(c => c.MedicoDirectorioId == medicoId)` |
| 7 | `EvaluarBadgesAutomaticosAsync` envuelto en try-catch | ✅ PASS | `Detalle.cshtml.cs:146-153` — bloque `try { await _badgeService.EvaluarBadgesAutomaticosAsync(...) } catch (Exception ex) { _ = ex; }` |
| 8 | Parámetros `exp*` eliminados de firma `OnPostConfirmarSimpleAsync` | ✅ PASS | Firma contiene solo `int medicoId` — sin `expCUCI`, `expCrohn`, etc. |

**Subtotal FUNC-023: 8/8 ✅ PASS**

---

## FUNC-024 — Proponer médico verifica duplicados

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 9 | `ProponerMedicoAsync` usa variable local para `Especialidad?.Trim()` (EF Core expression tree) | ✅ PASS | `MedicoDirectorioService.cs:176` — `var especialidadTrim = vm.Especialidad?.Trim();` luego `m.Especialidad == especialidadTrim` |
| 10 | Duplicate check compara `NombreCompleto + Especialidad` antes de INSERT | ✅ PASS | `MedicoDirectorioService.cs:177-181` — `AnyAsync(m => m.NombreCompleto == ... && m.Especialidad == especialidadTrim && !m.Eliminado)` |
| 11 | Médico propuesto se crea con `VisiblePublicamente = false, Activo = false` | ✅ PASS | `MedicoDirectorioService.cs:199-200` — ambos `false` en constructor |
| 12 | `Proponer.cshtml.cs` captura `InvalidOperationException` y muestra error via ModelState | ✅ PASS | `Proponer.cshtml.cs` — `try { await _directorioService.ProponerMedicoAsync(...) } catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }` |

**Subtotal FUNC-024: 4/4 ✅ PASS**

---

## FUNC-026 — Email de reclamación proviene del ClaimsPrincipal

| # | Escenario | Resultado | Evidencia |
|---|-----------|-----------|-----------|
| 13 | `Medico.EmailSolicitudClaim` se asigna desde `User.FindFirstValue(ClaimTypes.Email)` (no del form) | ✅ PASS | `ReclamarPerfil.cshtml.cs` — `Medico.EmailSolicitudClaim = User.FindFirstValue(ClaimTypes.Email)` |
| 14 | Null guard presente: si `emailClaim` es null se retorna error antes del INSERT | ✅ PASS | Guard presente antes de asignar |
| 15 | Mensaje de éxito no revela campo `EmailContacto` — dice "email de tu cuenta" | ✅ PASS | TempData["Success"] = "... email de tu cuenta ..." |

**Subtotal FUNC-026: 3/3 ✅ PASS**

---

## Resumen del módulo

| Función | Escenarios | PASS | FAIL | WARN |
|---------|-----------|------|------|------|
| FUNC-023 | 8 | 8 | 0 | 0 |
| FUNC-024 | 4 | 4 | 0 | 0 |
| FUNC-026 | 3 | 3 | 0 | 0 |
| **TOTAL** | **15** | **15** | **0** | **0** |

**Veredicto: ✅ APTO**
