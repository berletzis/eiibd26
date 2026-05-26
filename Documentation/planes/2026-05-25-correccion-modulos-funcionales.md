# Corrección de Módulos Funcionales — EIIBD Platform

> **Para workers agénticos:** Usar superpowers:executing-plans o superpowers:subagent-driven-development para ejecutar este plan tarea por tarea con build check entre bloques.

**Goal:** Corregir 25 defectos funcionales de seguridad, integridad y lógica en la plataforma EIIBD sin alterar arquitectura ni rutas públicas.

**Architecture:** Correcciones quirúrgicas archivo por archivo. Build check después de cada bloque. SQL directo para cambios de schema. Sin migraciones EF Core.

**Tech Stack:** ASP.NET Core 8 · Razor Pages · EF Core 8 · SQL Server · Hangfire · C# 12

---

## Pre-condición: Build inicial

```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet build --no-restore
```
Debe dar 0 errores. Si falla, no continuar.

---

## Items ya corregidos (skip — no tocar)

- **FUNC-022** — `VincularAsync` ya setea `EstatusReclamacion = Reclamado` y `FechaReclamacion` (`Activar.cshtml.cs` líneas 178-180).
- **FUNC-017** — `PreguntasApiController` ya usa Hangfire (`_backgroundJobs.Enqueue`), no `Task.Factory.StartNew`.
- **FUNC-001** — `EstadoAnimoUsuarioController` ya valida ownership con `_ownership.ValidateEstadoAnimoRelationsAsync`.

---

## BLOQUE 1 — Directorio Médicos

### Tarea 1A: FUNC-023 — Unificar flujos de confirmación

**Problema:** `OnPostConfirmarAsync` → escribe a `ConfirmacionesComunitarias`. `OnPostConfirmarSimpleAsync` → escribe a `DirectorioMedicoConfirmaciones`. `RecalcularNivelConfianzaAsync` solo lee de `DirectorioMedicoConfirmaciones`. Las confirmaciones del flujo "confirmar" no afectan el NivelConfianza.

**Archivos:**
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml.cs` líneas 119-157
- Modify: `Services/Directorio/MedicoDirectorioService.cs` líneas 245-264

**Archivo 1 — `Detalle.cshtml.cs`:**

Cambiar `OnPostConfirmarSimpleAsync` (línea 120): el duplicate-check actual usa `DirectorioMedicoConfirmaciones`. Cambiarlo a `ConfirmacionesComunitarias` y crear `ConfirmacionComunitaria` en lugar de `DirectorioMedicoConfirmacion`.

```csharp
// OnPostConfirmarSimpleAsync — reemplazar desde línea 120 hasta el SaveChanges (línea 150)

// ANTES:
var existe = await _db.DirectorioMedicoConfirmaciones
    .AnyAsync(c => c.MedicoId == medicoId && c.UsuarioId == usuarioId && !c.Eliminado);
...
_db.DirectorioMedicoConfirmaciones.Add(new DirectorioMedicoConfirmacion { ... });

// DESPUÉS:
var existe = await _db.ConfirmacionesComunitarias
    .AnyAsync(c => c.MedicoDirectorioId == medicoId && c.UsuarioId == usuarioId && !c.Eliminado);

if (existe)
{
    TempData["Error"] = "Ya confirmaste a este médico anteriormente.";
    return RedirectToPage(new { id = medicoId });
}

var tipoConfirmacion = await _db.TiposConfirmacion
    .OrderBy(t => t.Orden)
    .FirstOrDefaultAsync(t => t.Activo);

if (tipoConfirmacion is null)
{
    TempData["Error"] = "No hay tipos de confirmación disponibles.";
    return RedirectToPage(new { id = medicoId });
}

_db.ConfirmacionesComunitarias.Add(new ConfirmacionComunitaria
{
    MedicoDirectorioId = medicoId,
    UsuarioId          = usuarioId,
    TipoConfirmacionId = tipoConfirmacion.Id,
    FechaCreacion      = DateTimeOffset.UtcNow
});
await _db.SaveChangesAsync();
```

**Archivo 2 — `MedicoDirectorioService.cs`:**

Cambiar `RecalcularNivelConfianzaAsync` para leer de `ConfirmacionesComunitarias`:

```csharp
// Reemplazar líneas 250-263 (total y tieneEII)

var total = await _db.ConfirmacionesComunitarias
    .CountAsync(c => c.MedicoDirectorioId == medicoId && !c.Eliminado);

// La plataforma es EII-específica: cualquier confirmación implica contexto EII
var tieneEII = total > 0;

medico.NivelConfianza = (NivelConfianzaEnum)CalcularNivelVerificacion(
    total, tieneEII, medico.CedulaVerificada, medico.PerfilReclamado);
medico.FechaModificacion = DateTimeOffset.UtcNow;
await _db.SaveChangesAsync();
```

- [ ] Aplicar cambios en `Detalle.cshtml.cs`
- [ ] Aplicar cambios en `MedicoDirectorioService.cs`

---

### Tarea 1B: FUNC-024 — ProponerMedicoAsync no visible por defecto + check duplicado

**Archivo:** `Services/Directorio/MedicoDirectorioService.cs` líneas 174-215

**Cambio 1 — duplicate check antes del insert (añadir antes de línea 176):**

```csharp
// Verificar duplicado por NombreCompleto + Especialidad
var yaExiste = await _db.MedicosDirectorio
    .AnyAsync(m =>
        m.NombreCompleto == vm.NombreCompleto.Trim() &&
        m.Especialidad == vm.Especialidad!.Trim() &&
        !m.Eliminado);

if (yaExiste)
    throw new InvalidOperationException(
        $"Ya existe un médico registrado con el nombre '{vm.NombreCompleto}' y especialidad '{vm.Especialidad}'.");
```

**Cambio 2 — cambiar líneas 192-193:**

```csharp
// ANTES:
VisiblePublicamente   = true,
Activo                = true,

// DESPUÉS:
VisiblePublicamente   = false,
Activo                = false,
```

**Nota:** El caller (`Pages/DirectorioMedicos/Proponer.cshtml.cs`) debe capturar `InvalidOperationException` y mostrar el mensaje al usuario. Verificar que ya lo hace o agregar un try-catch.

- [ ] Agregar duplicate check
- [ ] Cambiar `VisiblePublicamente = false, Activo = false`
- [ ] Verificar que el caller maneja `InvalidOperationException`

---

### Tarea 1C: FUNC-026 — ReclamarPerfil acepta email arbitrario

**Archivo:** `Pages/DirectorioMedicos/ReclamarPerfil.cshtml.cs`

**Línea 75 actual:**
```csharp
Medico.EmailSolicitudClaim = EmailContacto?.Trim();
```

**Fix:** ignorar el campo del form y usar el email del usuario autenticado:

```csharp
// ANTES (línea 75):
Medico.EmailSolicitudClaim = EmailContacto?.Trim();

// DESPUÉS: leer email directamente de la sesión autenticada
var userEmail = User.FindFirstValue(ClaimTypes.Email);
if (string.IsNullOrWhiteSpace(userEmail))
{
    ModelState.AddModelError(string.Empty, "No se pudo verificar tu email. Inicia sesión nuevamente.");
    return Page();
}
Medico.EmailSolicitudClaim = userEmail;
```

También eliminar o ignorar el `[BindProperty] EmailContacto` (puede dejarse para mostrar en la vista pero no debe usarse al guardar).

- [ ] Cambiar la asignación de `EmailSolicitudClaim` para usar `User.FindFirstValue(ClaimTypes.Email)`

---

**Build check BLOQUE 1:**
```powershell
dotnet build --no-restore
```

---

## BLOQUE 2 — Ownership Checks

Patrón común: antes de insertar/usar FK enviado por cliente, verificar que pertenece al usuario autenticado.

### Tarea 2A: FUNC-007 — UsuarioSintomas: condicionUsuarioIds sin validar

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs` línea 286

En `OnPostAsociarCondicionesAsync`, el loop `foreach (var condId in condicionUsuarioIds)` inserta sin verificar ownership:

```csharp
// Reemplazar el foreach de condicionUsuarioIds (líneas 286-299)

// Filtrar solo IDs que pertenecen al usuario autenticado
var idsValidos = await _db.condicionUsuario
    .Where(x => condicionUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
    .Select(x => x.id)
    .ToListAsync();

foreach (var condId in idsValidos)
{
    if (!existentes.Any(x => x.IdCondicionUsuario == condId))
    {
        _db.SintomaCondicionUsuario.Add(new SintomaCondicionUsuario
        {
            IdUsuario = Guid.Parse(userId),
            IdSintomaUsuario = sintomaId,
            IdCondicionUsuario = condId,
            FechaCreado = DateTime.Now,
            Notas = null
        });
    }
}
```

- [ ] Aplicar filtro de ownership en `OnPostAsociarCondicionesAsync`

---

### Tarea 2B: FUNC-008 — UsuarioSintomas: tratamientoUsuarioIds sin validar

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs` línea 335

En `OnPostAsociarTratamientosAsync`:

```csharp
// Reemplazar el foreach de tratamientoUsuarioIds (líneas 335-349)

var idsValidos = await _db.tratamientoUsuario
    .Where(x => tratamientoUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
    .Select(x => x.id)
    .ToListAsync();

foreach (var tratId in idsValidos)
{
    if (!existentes.Any(x => (x.IdTratamientoUsuario ?? 0) == tratId))
    {
        _db.TratamientoSintomaUsuario.Add(new TratamientoSintomaUsuario
        {
            IdUsuario = Guid.Parse(userId),
            IdSintomaUsuario = sintomaId,
            IdTratamientoUsuario = tratId,
            FechaCreado = DateTime.Now,
            Notas = null
        });
    }
}
```

- [ ] Aplicar filtro de ownership en `OnPostAsociarTratamientosAsync`

---

### Tarea 2C: FUNC-009 — UsuarioTratamientos: sintomaUsuarioIds sin validar

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs` línea 252

En `OnPostAsociarSintomasAsync`:

```csharp
// Reemplazar el foreach de sintomaUsuarioIds (líneas 252-265)

var idsValidos = await _db.sintomasUsuario
    .Where(x => sintomaUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
    .Select(x => x.id)
    .ToListAsync();

foreach (var sId in idsValidos)
{
    if (!relsActuales.Any(x => x.IdSintomaUsuario == sId))
    {
        _db.TratamientoSintomaUsuario.Add(new TratamientoSintomaUsuario
        {
            IdUsuario = Guid.Parse(userId),
            IdTratamientoUsuario = tratamientoId,
            IdSintomaUsuario = sId,
            FechaCreado = DateTime.Now,
            Notas = null
        });
    }
}
```

- [ ] Aplicar filtro de ownership en `OnPostAsociarSintomasAsync`

---

### Tarea 2D: FUNC-010 — UsuarioTratamientos: condicionUsuarioIds sin validar

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs` línea 281

En `OnPostAsociarCondicionesAsync`:

```csharp
// Reemplazar el foreach de condicionUsuarioIds (líneas 281-295)

var idsValidos = await _db.condicionUsuario
    .Where(x => condicionUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
    .Select(x => x.id)
    .ToListAsync();

foreach (var cId in idsValidos)
{
    if (!relsActuales.Any(x => x.IdCondicionUsuario == cId))
    {
        _db.TratamientoCondicionUsuario.Add(new TratamientoCondicionUsuario
        {
            IdUsuario = Guid.Parse(userId),
            IdTratamientoUsuario = tratamientoId,
            IdCondicionUsuario = cId,
            FechaCreado = DateTime.Now,
            Notas = null
        });
    }
}
```

- [ ] Aplicar filtro de ownership en `OnPostAsociarCondicionesAsync`

---

### Tarea 2E: FUNC-013 — UsuarioLaboratorios: duplicados al crear resultado

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs` línea 182

En `OnPostAgregarResultadoAsync`, antes de crear el nuevo registro:

```csharp
// Añadir DESPUÉS de verificar que tipoExiste (línea 180) y ANTES de crear 'nuevo'

var yaExiste = await _db.PatientLaboratoryResults
    .AnyAsync(r => r.PatientId == Guid.Parse(userId)
                && r.LaboratoryTypeId == laboratoryTypeId
                && !r.Eliminado);

if (yaExiste)
    return new JsonResult(new { ok = false, mensaje = "Ya tienes un resultado activo para este tipo de laboratorio." }) { StatusCode = 400 };
```

- [ ] Agregar check de duplicado antes del insert

---

### Tarea 2F: FUNC-014 — UsuarioLaboratorios: FK opcionales sin ownership check

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs` línea 219

En `OnPostActualizarResultadoAsync`, antes de asignar `condicionUsuarioId`, `sintomaUsuarioId`, `tratamientoUsuarioId`:

```csharp
// Añadir ANTES de las asignaciones (línea 219), después de obtener 'r':

var userGuid = Guid.Parse(userId);

if (condicionUsuarioId.HasValue)
{
    var validCond = await _db.condicionUsuario.AnyAsync(
        x => x.id == condicionUsuarioId.Value && x.idUsuario == userGuid && !x.Eliminado);
    if (!validCond) condicionUsuarioId = null;
}

if (sintomaUsuarioId.HasValue)
{
    var validSint = await _db.sintomasUsuario.AnyAsync(
        x => x.id == sintomaUsuarioId.Value && x.idUsuario == userGuid && !x.Eliminado);
    if (!validSint) sintomaUsuarioId = null;
}

if (tratamientoUsuarioId.HasValue)
{
    var validTrat = await _db.tratamientoUsuario.AnyAsync(
        x => x.id == tratamientoUsuarioId.Value && x.idUsuario == userGuid && !x.Eliminado);
    if (!validTrat) tratamientoUsuarioId = null;
}
```

- [ ] Agregar ownership validation para FK opcionales

---

**Build check BLOQUE 2:**
```powershell
dotnet build --no-restore
```

---

## BLOQUE 3 — Integridad de datos

### Tarea 3A: FUNC-004 — Soft-delete de síntoma deja TrackingSintomaUsuario huérfano

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs` línea 384

En `OnPostEliminarSintomaAsync`, después de los checks de condiciones y tratamientos (línea 388) y antes del soft-delete (línea 390):

```csharp
// Añadir check de tracking (después de la línea 388 — check de tieneTratamientos)

var trackingsActivos = await _db.TrackingSintomaUsuario
    .Where(t => t.IdSintomaUsuario == sintId)
    .ToListAsync();

if (trackingsActivos.Any())
{
    // Soft-delete cascada de los tracking records
    foreach (var tracking in trackingsActivos)
    {
        // TrackingSintomaUsuario no tiene campo Eliminado — eliminar físicamente
        // o comentar esta sección si se prefiere bloquear
        _db.TrackingSintomaUsuario.Remove(tracking);
    }
}
```

**Nota:** `TrackingSintomaUsuario` no tiene campo `Eliminado`. Verificar si el proyecto prefiere:
- A) Eliminar físicamente los tracking (opción arriba)
- B) Bloquear eliminación del síntoma si tiene trackings activos (retornar error como se hace con condiciones/tratamientos)

Opción B (más conservadora):
```csharp
var tieneTracking = await _db.TrackingSintomaUsuario
    .AnyAsync(t => t.IdSintomaUsuario == sintId);
if (tieneTracking)
    return new JsonResult(new { ok = false, mensaje = "No se puede eliminar el síntoma porque tiene registros de seguimiento. Contacta soporte." }) { StatusCode = 400 };
```

**Usar opción B por defecto** (no se pierden datos de tracking).

- [ ] Agregar check de TrackingSintomaUsuario en `OnPostEliminarSintomaAsync`

---

### Tarea 3B: FUNC-018 — PreguntasApiController: idempotencia job IA

**Archivo:** `Controllers/PreguntasApiController.cs` línea 93

Antes de `_backgroundJobs.Enqueue(...)` (línea 94), agregar:

```csharp
// Idempotencia: solo encolar si no existe respuesta IA activa
var yaExisteRespuestaIA = await _db.Respuestas
    .AnyAsync(r => r.PreguntaId == pregunta.Id && r.EsIA && !r.Eliminado);

if (!yaExisteRespuestaIA)
{
    var preguntaIdCapture = pregunta.Id;
    _backgroundJobs.Enqueue<eiibd26.Jobs.AiAnswerJob>(
        job => job.ProcesarPreguntaAsync(preguntaIdCapture));
    _logger.LogInformation("AI job enqueued via Hangfire for pregunta {PreguntaId}", pregunta.Id);
}
```

Eliminar el `var preguntaIdCapture` y el `Enqueue` que ya están (líneas 93-97 actuales) — quedan dentro del `if`.

- [ ] Agregar check de idempotencia antes del Enqueue

---

**Build check BLOQUE 3:**
```powershell
dotnet build --no-restore
```

---

## BLOQUE 4 — Validaciones y lógica

### Tarea 4A: FUNC-005 — UsuarioSintomas: DateTime.Now → DateTime.UtcNow

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs`

Cambiar en `OnPostAgregarSintomaAsync` (línea 229):

```csharp
// ANTES:
fechaInicio = DateTime.Now,
fechaCreado = DateTime.Now,
fechaModificado = DateTime.Now,

// DESPUÉS:
fechaInicio = DateTime.UtcNow,
fechaCreado = DateTime.UtcNow,
fechaModificado = DateTime.UtcNow,
```

- [ ] Cambiar `DateTime.Now` → `DateTime.UtcNow` en creación de `sintomasUsuario`

---

### Tarea 4B: FUNC-006 — UsuarioSintomas: EditarFechaInicio sin validación de rango

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs` línea 408

En `OnPostEditarFechaInicioAsync`, antes de asignar `fechaInicio`:

```csharp
// Añadir validación de rango (antes de línea 408)

var fechaMin = new DateTime(1900, 1, 1);
if (nuevaFechaInicio < fechaMin || nuevaFechaInicio > DateTime.Today)
    return new JsonResult(new { ok = false, mensaje = "La fecha debe estar entre 1900 y hoy." }) { StatusCode = 400 };
```

- [ ] Agregar validación de rango en `OnPostEditarFechaInicioAsync`

---

### Tarea 4C: FUNC-011 — UsuarioTratamientos: FechaFin puede ser anterior a FechaInicio

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs` línea 185

En `OnPostEditarFechaFinAsync`, después de obtener `rel` (línea 185) y antes de asignar `FechaFin`:

```csharp
// Añadir validación (antes de rel.FechaFin = fechaFin)

if (fechaFin.HasValue && rel.fechaInicio.HasValue && fechaFin.Value < rel.fechaInicio.Value)
    return new JsonResult(new { ok = false, mensaje = "La fecha de fin no puede ser anterior a la fecha de inicio." }) { StatusCode = 400 };
```

- [ ] Agregar validación fecha fin >= fecha inicio

---

### Tarea 4D: FUNC-012 — TratamientosAdminController: IA sobreescribe nombre sin aprobación

**Paso 1 — SQL directo** (ejecutar antes de modificar código):

```sql
-- Ejecutar con connection string de user secrets
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'NombreSugeridoIA'
)
    ALTER TABLE dbo.tratamientos ADD NombreSugeridoIA NVARCHAR(500) NULL;
```

**Paso 2 — Modelo** `Models/tratamientos.cs` — agregar propiedad después de `ValidadoHumano`:

```csharp
public string? NombreSugeridoIA { get; set; }
```

**Paso 3 — Controller** `Controllers/TratamientosAdminController.cs` líneas 55-61:

```csharp
// ANTES:
if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogInformation("Traduciendo nombre de '{NombreOriginal}' a '{NombreTraducido}'",
        tratamiento.nombre, nombreTraducido);
    tratamiento.nombre = nombreTraducido;
}

// DESPUÉS:
if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogInformation(
        "IA sugirió nombre '{NombreTraducido}' para tratamiento '{NombreOriginal}' — pendiente aprobación admin.",
        nombreTraducido, tratamiento.nombre);
    tratamiento.NombreSugeridoIA = nombreTraducido;
    tratamiento.ValidadoHumano = false;
}
```

- [ ] Ejecutar SQL para agregar columna `NombreSugeridoIA`
- [ ] Agregar propiedad al modelo `tratamientos.cs`
- [ ] Cambiar asignación en controller

---

### Tarea 4E: FUNC-015 — UsuarioLaboratorios: ResultValue sin sanitizar

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs` línea 215

Reemplazar la asignación de `ResultValue`:

```csharp
// ANTES:
r.ResultValue = string.IsNullOrWhiteSpace(resultValue) ? null : resultValue.Trim();

// DESPUÉS:
if (!string.IsNullOrWhiteSpace(resultValue))
{
    var trimmed = resultValue.Trim();
    if (trimmed.Length > 500)
        return new JsonResult(new { ok = false, mensaje = "El valor del resultado no puede superar 500 caracteres." }) { StatusCode = 400 };
    r.ResultValue = trimmed;
}
else
{
    r.ResultValue = null;
}
```

- [ ] Agregar validación de longitud en `ResultValue`

---

### Tarea 4F: FUNC-019 — RespuestasApiController: eliminar expone registros ajenos

**Archivo:** `Controllers/RespuestasApiController.cs` línea 62

```csharp
// ANTES:
var r = await _db.Respuestas.FirstOrDefaultAsync(x => x.Id == id);
if (r == null) return NotFound(new { ok = false, error = "Respuesta no encontrada" });

if (r.UsuarioId != userId.Value)
    return Forbid();

// DESPUÉS (unifica en una sola query — expone 404 para ambos casos):
var r = await _db.Respuestas.FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);
if (r == null) return NotFound(new { ok = false, error = "Respuesta no encontrada" });

if (r.UsuarioId != userId.Value)
    return NotFound(new { ok = false, error = "Respuesta no encontrada" });
```

- [ ] Agregar `!x.Eliminado` en query + retornar `NotFound` en lugar de `Forbid`

---

### Tarea 4G: FUNC-020 — RespuestasApiController: Cuerpo sin validación de longitud

**Archivo:** `Controllers/RespuestasApiController.cs` línea 123

Antes de crear el objeto `Respuesta` (línea 118), agregar:

```csharp
// Añadir validación de dto.Cuerpo antes de crear la Respuesta

if (string.IsNullOrWhiteSpace(dto.Cuerpo) || dto.Cuerpo.Trim().Length < 10)
    return BadRequest(new { ok = false, error = "La respuesta debe tener al menos 10 caracteres." });

if (dto.Cuerpo.Length > 10000)
    return BadRequest(new { ok = false, error = "La respuesta no puede superar 10.000 caracteres." });
```

- [ ] Agregar validación de longitud de `dto.Cuerpo`

---

### Tarea 4H: FUNC-025 — MedicoBadgeService: badge participante_qa cuenta respuestas IA y eliminadas

**Archivo:** `Services/Medico/MedicoBadgeService.cs` línea 116

```csharp
// ANTES:
var respuestas = await _db.Respuestas
    .CountAsync(r => r.UsuarioId == perfil.UserId.Value);

// DESPUÉS:
var respuestas = await _db.Respuestas
    .CountAsync(r => r.UsuarioId == perfil.UserId.Value && !r.Eliminado && !r.EsIA);
```

- [ ] Agregar filtros `!r.Eliminado && !r.EsIA` en query de badge

---

### Tarea 4I: FUNC-027 — UsuarioCondiciones: EditarFechaInicio busca por FK catálogo en lugar de PK

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs` línea 168-183

```csharp
// ANTES — parámetro y filtro usan idCondicion (FK catálogo):
public async Task<IActionResult> OnPostEditarFechaInicioAsync(int condId, DateTime nuevaFechaInicio)
...
.FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.idCondicion == condId && !x.Eliminado);

// DESPUÉS — usar PK 'id' del registro condicionUsuario:
public async Task<IActionResult> OnPostEditarFechaInicioAsync(int condUsuarioId, DateTime nuevaFechaInicio)
...
.FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == condUsuarioId && !x.Eliminado);
```

**Importante:** verificar que la vista `UsuarioCondiciones.cshtml` envía el PK (`id`) en el campo `condUsuarioId`, no el `idCondicion`. Si la vista envía `condId`, renombrar el campo del form en la vista.

- [ ] Cambiar parámetro a `condUsuarioId` y filtro a `x.id == condUsuarioId`
- [ ] Verificar que la vista envía el PK correcto

---

### Tarea 4J: FUNC-028 — UsuarioCondiciones: EliminarCondicion lookup incorrecto + no limpia relaciones

**Archivo:** `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs` líneas 185-201

```csharp
// ANTES:
public async Task<IActionResult> OnPostEliminarCondicionAsync(int condId)
...
.FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.idCondicion == condId && !x.Eliminado);

// DESPUÉS — fix 1: usar PK
public async Task<IActionResult> OnPostEliminarCondicionAsync(int condUsuarioId)
...
.FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == condUsuarioId && !x.Eliminado);

// fix 2: verificar relaciones antes de eliminar
if (rel != null)
{
    // Verificar síntomas relacionados
    var tieneSintomas = await _db.SintomaCondicionUsuario
        .AnyAsync(x => x.IdCondicionUsuario == rel.id);
    if (tieneSintomas)
        return new JsonResult(new { ok = false, mensaje = "No se puede eliminar la condición porque tiene síntomas relacionados. Primero quítalos." }) { StatusCode = 400 };

    // Verificar tratamientos relacionados
    var tieneTratamientos = await _db.TratamientoCondicionUsuario
        .AnyAsync(x => x.IdCondicionUsuario == rel.id);
    if (tieneTratamientos)
        return new JsonResult(new { ok = false, mensaje = "No se puede eliminar la condición porque tiene tratamientos relacionados. Primero quítalos." }) { StatusCode = 400 };

    rel.Eliminado = true;
    rel.fechaEliminado = DateTime.Now;
    await _db.SaveChangesAsync();
}

return RedirectToPage();
```

- [ ] Cambiar parámetro a `condUsuarioId` y filtro a `x.id == condUsuarioId`
- [ ] Agregar checks de relaciones antes de soft-delete

---

**Build check BLOQUE 4:**
```powershell
dotnet build --no-restore
```

---

## BLOQUE 5 — Low Priority

### Tarea 5A: FUNC-002 — EstadoAnimoUsuarioController: soporte fecha retroactiva (hasta 24h)

**Archivo:** `Controllers/EstadoAnimoUsuarioController.cs` línea 107

Agregar parámetro opcional `fechaRegistro` al método `Nuevo`:

```csharp
// Cambiar firma del método:
[HttpPost("nuevo")]
public async Task<ActionResult<object>> Nuevo(
    [FromForm] string mood,
    [FromForm] string? texto,
    [FromForm] int? condicionUsuarioId,
    [FromForm] int? sintomaUsuarioId,
    [FromForm] int? tratamientoUsuarioId,
    [FromForm] DateTime? fechaRegistro)   // ← nuevo parámetro
```

Cambiar línea 154 (asignación de `FechaRegistro`):

```csharp
// ANTES:
FechaRegistro = DateTime.UtcNow,

// DESPUÉS:
FechaRegistro = (fechaRegistro.HasValue
    && fechaRegistro.Value <= DateTime.UtcNow
    && fechaRegistro.Value >= DateTime.UtcNow.AddHours(-24))
    ? DateTime.SpecifyKind(fechaRegistro.Value, DateTimeKind.Utc)
    : DateTime.UtcNow,
```

- [ ] Agregar parámetro `fechaRegistro` y lógica de validación

---

### Tarea 5B: FUNC-003 — EstadoAnimoUsuario.Texto sin MaxLength

**Archivo:** `Models/EstadoAnimoUsuario.cs` línea 28

```csharp
// ANTES:
public string? Texto { get; set; }

// DESPUÉS:
[MaxLength(2000)]
public string? Texto { get; set; }
```

También en `Controllers/EstadoAnimoUsuarioController.cs`, después de `if (string.IsNullOrWhiteSpace(texto)) texto = null;` (línea 140):

```csharp
if (texto?.Length > 2000)
    return BadRequest(new { ok = false, error = "El texto no puede superar 2000 caracteres." });
```

- [ ] Agregar `[MaxLength(2000)]` al modelo
- [ ] Agregar validación en controller

---

### Tarea 5C: FUNC-029 — Autocomplete endpoints sin [AllowAnonymous] explícito

**Archivos:**
- `Controllers/CondicionesApiController.cs` — método `Autocomplete` línea 24
- `Controllers/SintomasApiController.cs` — método `Autocomplete` línea 28
- `Controllers/TratamientosApiController.cs` — método `Autocomplete` línea 26

Agregar atributo antes de cada método:

```csharp
[AllowAnonymous] // Catálogo público — consultado por usuarios no autenticados en registro
public async Task<IActionResult> Autocomplete(...)
```

- [ ] Agregar `[AllowAnonymous]` en los 3 controllers

---

### Tarea 5D: FUNC-011b — TrackingSintomaUsuario: frecuencia null sin default

Buscar en `Services/` el servicio de tracking (`_trackingService.GuardarTrackingAsync`). En `UsuarioSintomas.cshtml.cs` línea 263 ya se hace: `FrecuenciaId: frecuenciaId > 0 ? frecuenciaId : null`. El default null es correcto; el modelo `TrackingSintomaUsuario.FrecuenciaId` es nullable.

Si el requerimiento es asignar un ID default cuando es null, agregar en `UsuarioSintomas.cshtml.cs` línea 263:

```csharp
// Solo si hay una frecuencia "Ocasional" con Id conocido (ej. 1):
FrecuenciaId: frecuenciaId > 0
    ? frecuenciaId
    : await _db.FrecuenciaSintomaCatalog
          .Where(f => f.Nombre == "Ocasional")
          .Select(f => (int?)f.Id)
          .FirstOrDefaultAsync(),
```

**Nota:** Solo implementar si `FrecuenciaSintomaCatalog` tiene un registro "Ocasional". Verificar primero con `SELECT * FROM FrecuenciaSintomaCatalog`.

- [ ] Verificar en BD si existe registro "Ocasional" y aplicar si corresponde

---

**Build check FINAL:**
```powershell
dotnet build --no-restore
```
Debe dar 0 errores, 0 warnings críticos.

---

## Resumen de archivos modificados

| Archivo | Items |
|---|---|
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | FUNC-023 |
| `Services/Directorio/MedicoDirectorioService.cs` | FUNC-023, FUNC-024 |
| `Pages/DirectorioMedicos/ReclamarPerfil.cshtml.cs` | FUNC-026 |
| `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs` | FUNC-004, FUNC-005, FUNC-006, FUNC-007, FUNC-008 |
| `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs` | FUNC-009, FUNC-010, FUNC-011 |
| `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs` | FUNC-013, FUNC-014, FUNC-015 |
| `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs` | FUNC-027, FUNC-028 |
| `Controllers/PreguntasApiController.cs` | FUNC-018 |
| `Controllers/RespuestasApiController.cs` | FUNC-019, FUNC-020 |
| `Controllers/TratamientosAdminController.cs` | FUNC-012 |
| `Services/Medico/MedicoBadgeService.cs` | FUNC-025 |
| `Models/tratamientos.cs` | FUNC-012 |
| `Models/EstadoAnimoUsuario.cs` | FUNC-003 |
| `Controllers/EstadoAnimoUsuarioController.cs` | FUNC-002, FUNC-003 |
| `Controllers/CondicionesApiController.cs` | FUNC-029 |
| `Controllers/SintomasApiController.cs` | FUNC-029 |
| `Controllers/TratamientosApiController.cs` | FUNC-029 |
| SQL directo | FUNC-012 (columna NombreSugeridoIA) |
