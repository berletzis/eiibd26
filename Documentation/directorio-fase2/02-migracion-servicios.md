# Migraciones Aplicadas — DirectorioMedicoConfirmaciones → ConfirmacionesComunitarias

**Fecha:** 2026-05-25  
**Build:** ✅ 0 errores · 733 warnings pre-existentes

---

## Archivos modificados (5 archivos · 9 cambios)

---

### 1. `Services/Directorio/MedicoDirectorioService.cs`

**Cambio A — `GetListadoAsync` (líneas 61-65)**

```csharp
// ANTES
TotalConfirmaciones  = _db.DirectorioMedicoConfirmaciones
    .Count(c => c.MedicoId == m.Id && !c.Eliminado),
TotalPacientesUnicos = _db.DirectorioMedicoConfirmaciones
    .Where(c => c.MedicoId == m.Id && !c.Eliminado)
    .Select(c => c.UsuarioId).Distinct().Count(),

// DESPUÉS
TotalConfirmaciones  = _db.ConfirmacionesComunitarias
    .Count(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
TotalPacientesUnicos = _db.ConfirmacionesComunitarias
    .Where(c => c.MedicoDirectorioId == m.Id && !c.Eliminado)
    .Select(c => c.UsuarioId).Distinct().Count(),
```

**Cambio B — `GetDetalleAsync` (líneas 122-126)** — mismo patrón.

---

### 2. `Services/Medico/MedicoBadgeService.cs`

**Cambio C — `EvaluarBadgesAutomaticosAsync` (líneas 102-103)**

```csharp
// ANTES
var totalConfirmaciones = await _db.DirectorioMedicoConfirmaciones
    .CountAsync(c => c.MedicoId == medicoId && !c.Eliminado);

// DESPUÉS
var totalConfirmaciones = await _db.ConfirmacionesComunitarias
    .CountAsync(c => c.MedicoDirectorioId == medicoId && !c.Eliminado);
```

---

### 3. `Pages/DirectorioMedicos/Index.cshtml.cs`

**Cambio D — `OnGetAsync` (líneas 42-51)**

```csharp
// ANTES — filtraba por campos booleanos de áreas EII (modelo antiguo)
var conEII = await _db.DirectorioMedicoConfirmaciones
    .AsNoTracking()
    .Where(c => ids.Contains(c.MedicoId) && !c.Eliminado &&
                (c.TieneExperienciaEII || c.ExpCUCI || ... || c.ExpSeguimientoProlongado))
    .Select(c => c.MedicoId).Distinct().ToListAsync();

// DESPUÉS — plataforma EII: cualquier confirmación = experiencia EII
var conEII = await _db.ConfirmacionesComunitarias
    .AsNoTracking()
    .Where(c => ids.Contains(c.MedicoDirectorioId) && !c.Eliminado)
    .Select(c => c.MedicoDirectorioId).Distinct().ToListAsync();
```

---

### 4. `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`

**Cambio E — `TotalRecomendaciones` (línea 82)**

```csharp
// ANTES
TotalRecomendaciones = await _db.DirectorioMedicoConfirmaciones
    .CountAsync(c => c.MedicoId == perfil.MedicoId.Value && !c.Eliminado);

// DESPUÉS
TotalRecomendaciones = await _db.ConfirmacionesComunitarias
    .CountAsync(c => c.MedicoDirectorioId == perfil.MedicoId.Value && !c.Eliminado);
```

**Cambio F — Lista de confirmaciones (líneas 87-115)**

```csharp
// ANTES — cargaba DirectorioMedicoConfirmaciones con Exp* booleans
var confirmaciones = await _db.DirectorioMedicoConfirmaciones
    .Where(c => c.MedicoId == ... && !c.Eliminado)
    .OrderByDescending(c => c.FechaConfirmacion).Take(20).ToListAsync();

// mapping: FechaConfirmacion = c.FechaConfirmacion, ExpCUCI = c.ExpCUCI, ...

// DESPUÉS
var confirmaciones = await _db.ConfirmacionesComunitarias
    .Where(c => c.MedicoDirectorioId == ... && !c.Eliminado)
    .OrderByDescending(c => c.FechaCreacion).Take(20).ToListAsync();

// mapping: FechaConfirmacion = c.FechaCreacion.DateTime, ExpCUCI = false, ...
// (Exp* → false: nueva tabla no tiene granularidad por área EII)
```

---

### 5. `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs`

**Cambio G — `OnGetGridDataAsync` (líneas 70-74)**

```csharp
// ANTES
Confirmaciones = _db.DirectorioMedicoConfirmaciones.Count(c => c.MedicoId == m.Id && !c.Eliminado),
TieneConfEII   = _db.DirectorioMedicoConfirmaciones.Any(c => c.MedicoId == m.Id && !c.Eliminado &&
                    (c.TieneExperienciaEII || c.ExpCUCI || ...)),

// DESPUÉS
Confirmaciones = _db.ConfirmacionesComunitarias.Count(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
TieneConfEII   = _db.ConfirmacionesComunitarias.Any(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
```

**Cambio H — `OnGetMedicoAsync` (líneas 128-157)**

```csharp
// ANTES — cargaba Exp* boolean selectors para expContadores
var confs = await _db.DirectorioMedicoConfirmaciones.Where(c => c.MedicoId == id && ...).ToListAsync();
var expContadores = areas.Select((a, i) => new { nombre = a, total = confs.Count(expSelectors[i]) }).ToList();
// exps array: ["CUCI", "Crohn", ...]

// DESPUÉS — carga TipoConfirmacion via Include
var confs = await _db.ConfirmacionesComunitarias
    .Include(c => c.TipoConfirmacion)
    .Where(c => c.MedicoDirectorioId == id && ...).ToListAsync();
var expContadores = areas.Select(a => new { nombre = a, total = 0 }).ToList();
// exps array: ["<TipoConfirmacion.Nombre>"]
tieneConfirmacionEII = confs.Any(),  // en lugar de confs.Any(c => c.ExpCUCI || ...)
```

**Cambio I — `RecalcularNivelAsync` (líneas 352-357)**

```csharp
// ANTES
var total    = await _db.DirectorioMedicoConfirmaciones.CountAsync(c => c.MedicoId == id && !c.Eliminado);
var tieneEII = await _db.DirectorioMedicoConfirmaciones.AnyAsync(c => c.MedicoId == id && !c.Eliminado &&
    (c.TieneExperienciaEII || c.ExpCUCI || ...));

// DESPUÉS
var total    = await _db.ConfirmacionesComunitarias.CountAsync(c => c.MedicoDirectorioId == id && !c.Eliminado);
var tieneEII = total > 0;  // plataforma EII: cualquier confirmación = experiencia EII
```

---

## Archivos sin cambio (intencional)

| Archivo | Motivo |
|---------|--------|
| `Data/ApplicationDbContext.cs` | DbSet mantenido — tabla existe en BD con datos históricos |
| `Models/Directorio/DirectorioMedicoConfirmacion.cs` | Modelo mantenido — no se eliminan tablas sin SQL script explícito |
| `Pages/DirectorioMedicos/Activar.cshtml.cs` | Solo comentario, no query |
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | Solo comentario, no query |
