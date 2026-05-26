# Matriz de Referencias — DirectorioMedicoConfirmaciones

**Fecha:** 2026-05-25  
**Objetivo:** Inventario completo de referencias activas a `_db.DirectorioMedicoConfirmaciones` previo a migración.

---

## Modelo antiguo vs. nuevo

| Campo (DirectorioMedicoConfirmacion) | Campo equivalente (ConfirmacionComunitaria) | Nota |
|--------------------------------------|---------------------------------------------|------|
| `MedicoId` | `MedicoDirectorioId` | Renombrado |
| `UsuarioId` | `UsuarioId` | Idéntico |
| `FechaConfirmacion` (DateTime) | `FechaCreacion` (DateTimeOffset) | Tipo distinto |
| `Eliminado` | `Eliminado` | Idéntico |
| `TieneExperienciaEII`, `ExpCUCI`, … (10 bool) | `TipoConfirmacionId` (FK) | Sin equivalente directo |
| — | `TipoConfirmacion.Nombre` | Nueva granularidad |

**Regla semántica:** EIIBD es una plataforma EII-específica. Cualquier confirmación implica experiencia EII. `tieneEII = total > 0`.

---

## Referencias activas (8 instancias · 5 archivos)

### 1. `Services/Directorio/MedicoDirectorioService.cs`

| Método | Líneas | Tipo | Uso | Reemplazo |
|--------|--------|------|-----|-----------|
| `GetListadoAsync` | 61-65 | Lectura | `TotalConfirmaciones`, `TotalPacientesUnicos` en proyección | `ConfirmacionesComunitarias` + `MedicoDirectorioId` |
| `GetDetalleAsync` | 122-126 | Lectura | Ídem para vista de detalle | Ídem |

### 2. `Services/Medico/MedicoBadgeService.cs`

| Método | Líneas | Tipo | Uso | Reemplazo |
|--------|--------|------|-----|-----------|
| `EvaluarBadgesAutomaticosAsync` | 102-103 | Lectura | `CountAsync` para badge `activo_comunidad` (umbral ≥ 5) | `ConfirmacionesComunitarias` + `MedicoDirectorioId` |

### 3. `Pages/DirectorioMedicos/Index.cshtml.cs`

| Método | Líneas | Tipo | Uso | Reemplazo |
|--------|--------|------|-----|-----------|
| `OnGetAsync` | 42-51 | Lectura | `MedicosConEII` — médicos con al menos una confirmación EII | `ConfirmacionesComunitarias` — cualquier confirmación = EII |

### 4. `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`

| Método | Líneas | Tipo | Uso | Reemplazo |
|--------|--------|------|-----|-----------|
| `OnGetAsync` | 82-83 | Lectura | `TotalRecomendaciones` — conteo simple | `ConfirmacionesComunitarias` + `MedicoDirectorioId` |
| `OnGetAsync` | 87-92 | Lectura | Lista de 20 últimas confirmaciones para `Recomendaciones` | Ídem; `FechaConfirmacion` ← `FechaCreacion.DateTime`; `Exp*` ← `false` |

### 5. `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs`

| Método | Líneas | Tipo | Uso | Reemplazo |
|--------|--------|------|-----|-----------|
| `OnGetGridDataAsync` | 70-74 | Lectura | `Confirmaciones` (count) + `TieneConfEII` (any) en grid | `ConfirmacionesComunitarias`; `tieneEII = Any()` |
| `OnGetMedicoAsync` | 128-157 | Lectura | `expContadores` por área EII + lista `confirmadores` | `ConfirmacionesComunitarias`; `expContadores` → zeros; `exps` ← `TipoConfirmacion.Nombre` |
| `RecalcularNivelAsync` | 352-357 | Lectura | `total` + `tieneEII` para recalcular `NivelConfianza` | `ConfirmacionesComunitarias`; `tieneEII = total > 0` |

---

## Referencias NO activas (no requieren cambio)

| Archivo | Línea | Tipo | Motivo |
|---------|-------|------|--------|
| `Data/ApplicationDbContext.cs` | 87 | DbSet | Se mantiene para no romper EF model — la tabla existe en BD con datos históricos |
| `Models/Directorio/DirectorioMedicoConfirmacion.cs` | — | Modelo | Ídem |
| `Pages/DirectorioMedicos/Activar.cshtml.cs` | 196 | Comentario | Solo texto, no query |
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | 32 | Comentario | Solo texto, no query |

---

## Impacto de la migración

| Funcionalidad | Impacto |
|---------------|---------|
| Contadores en tarjetas de listado | Lee datos correctos (nuevas confirmaciones) |
| Badge `activo_comunidad` | Se activa correctamente con nuevas confirmaciones |
| Dashboard médico — total | Correcto |
| Dashboard médico — lista EII areas (`ExpCUCI`, etc.) | Vacía (siempre `false`); datos de nueva tabla no tienen granularidad por área |
| Admin grid — total confirmaciones | Correcto |
| Admin grid — `tieneConfirmacionEII` | `true` si hay cualquier confirmación |
| Admin panel médico — `expContadores` | Todos en 0 (sin datos per-área en nueva tabla) |
| Admin panel médico — `confirmadores.exps` | Muestra `TipoConfirmacion.Nombre` en lugar de áreas boolean |
| `RecalcularNivelAsync` admin | Usa conteos correctos |
