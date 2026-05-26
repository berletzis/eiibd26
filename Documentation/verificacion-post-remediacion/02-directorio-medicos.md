# 02 – Directorio Médicos: Fuente de Verdad para Confirmaciones

**Fecha:** 2025-07-10  
**Issue previo:** FUNC-023  

---

## Problema Pre-Remediación

Existían dos tablas paralelas que ambas pretendían representar confirmaciones de médicos:

- `DirectorioMedicoConfirmaciones` – tabla estructurada de confirmaciones con campos EII especializados
- `ConfirmacionesComunitarias` – tabla comunitaria genérica de tipos de confirmación

No estaba definido cuál era canónica para calcular `NivelConfianza`.

---

## Matriz de Entidades

| Entidad | Estado | Uso actual | ¿Canónica? | Eliminar | Migrar |
|---------|--------|------------|------------|----------|--------|
| `DirectorioMedicoConfirmaciones` | ✅ Activa | Admin, Detalle, Activar, recalc confianza | **SÍ** | No | N/A |
| `ConfirmacionesComunitarias` | ⚠️ Activa | Flujo comunitario (tipos de confirmación genérico, `Detalle.cshtml.cs`) | No (secundaria) | No todavía | Evaluar en sprint siguiente |

---

## ¿Quién calcula `NivelConfianza`?

### Ruta canónica (post-remediación) — `MedicoDirectorioService.RecalcularNivelConfianzaAsync`

```csharp
// MedicoDirectorioService.cs línea 245
public async Task RecalcularNivelConfianzaAsync(int medicoId)
```

Calcula desde `DirectorioMedicoConfirmaciones`:

```csharp
var total   = await _db.DirectorioMedicoConfirmaciones.CountAsync(c => c.MedicoId == medicoId && !c.Eliminado);
var tieneEII = await _db.DirectorioMedicoConfirmaciones.AnyAsync(...tieneExperienciaEII...);
```

### Ruta admin local — `Admin/DirectorioMedicos/Index.cshtml.cs.RecalcularNivelAsync`

```csharp
// Línea 351
private async Task RecalcularNivelAsync(MedicoDirectorio medico, int id)
```

También usa `DirectorioMedicoConfirmaciones` (✅ consistente con canónica).

**Hallazgo:** La ruta admin tiene su propia función privada `RecalcularNivelAsync` en lugar de delegar al servicio. Es duplicación de lógica, pero el resultado es funcionalmente idéntico porque ambas leen de `DirectorioMedicoConfirmaciones`. **No introduce inconsistencia de datos.**

---

## ¿Qué tabla persiste cada flujo?

| Flujo | Tabla que persiste |
|-------|--------------------|
| `Detalle.cshtml.cs.OnPostConfirmarSimpleAsync()` | `DirectorioMedicoConfirmaciones` ✅ |
| `Detalle.cshtml.cs.OnPostConfirmarAsync()` | `DirectorioMedicoConfirmaciones` ✅ |
| `Activar.cshtml.cs.VincularAsync()` | `MedicosDirectorio` (claim status) + llama `RecalcularNivelConfianzaAsync` ✅ |
| Admin `Index.cshtml.cs` (verificación cedula) | `MedicosDirectorio` + `RecalcularNivelAsync` → `DirectorioMedicoConfirmaciones` ✅ |
| Flujo comunitario (tipo confirmación) | `ConfirmacionesComunitarias` ⚠️ (secundario) |

---

## ¿Qué tabla consulta cada servicio?

| Servicio / Página | Tabla leída para confianza | Consistente |
|-------------------|---------------------------|-------------|
| `MedicoDirectorioService.GetListadoAsync()` | `DirectorioMedicoConfirmaciones` | ✅ |
| `MedicoDirectorioService.GetDetalleAsync()` | `DirectorioMedicoConfirmaciones` | ✅ |
| `MedicoDirectorioService.RecalcularNivelConfianzaAsync()` | `DirectorioMedicoConfirmaciones` | ✅ |
| Admin `RecalcularNivelAsync()` | `DirectorioMedicoConfirmaciones` | ✅ |
| `Detalle.cshtml.cs` (confirmaciones ya hechas por usuario) | `ConfirmacionesComunitarias` | ⚠️ (flujo paralelo) |

---

## Tablas / ViewModels / Queries muertas detectadas

| Artefacto | Estado |
|-----------|--------|
| `ConfirmacionesComunitarias` DbSet | ⚠️ Presente y en uso (flujo comunitario distinto). No es huérfana. |
| ViewModels de confirmación comunitaria | Presentes y usados en `Detalle.cshtml`. |
| Queries antiguas que usaban `ConfirmacionesComunitarias` para `NivelConfianza` | ✅ Eliminadas en remediación. |

---

## Riesgos Residuales

| ID | Riesgo | Severidad |
|----|--------|-----------|
| R-DM-01 | `Admin/DirectorioMedicos/Index.cshtml.cs` duplica la lógica de recálculo en lugar de llamar al servicio | 🟡 Medio (manteniblidad, no bug) |
| R-DM-02 | `ConfirmacionesComunitarias` sigue existiendo con semántica diferente; sin documentación de cuándo usar cada tabla | 🟡 Medio (confusión futura) |
| R-DM-03 | No se verificó si hay queries en otros puntos no rastreados que calculen confianza desde `ConfirmacionesComunitarias` | 🟠 Bajo-Medio |

---

## Veredicto Fase 2

| Criterio | Estado |
|----------|--------|
| Fuente única de verdad para NivelConfianza | ✅ `DirectorioMedicoConfirmaciones` |
| Duplicidad de cálculo eliminada en flujo usuario | ✅ |
| Duplicidad en admin (privada local) | ⚠️ WARN – No es bloqueante |
| Tablas muertas / huérfanas | No encontradas |
| **VEREDICTO** | ✅ PASS con observaciones |
