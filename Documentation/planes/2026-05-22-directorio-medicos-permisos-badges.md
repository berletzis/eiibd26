# Directorio Médicos — Permisos, Ubicaciones, Badges y Términos

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separar visibilidad por rol en `/DirectorioMedicos/Detalle/`, mostrar ubicaciones combinadas, corregir bug de badge `validador_contenido`, crear partial de badges reutilizable, mostrar comentarios médicos en términos del glosario, y actualizar dashboard de médico.

**Architecture:** Modificaciones quirúrgicas sobre Razor Pages existentes. Sin nuevas tablas. El bug principal (badge no se otorga al validar un término) se corrige añadiendo un hook post-save en `GlossaryService.AddValidationAsync`. El partial `_MedicoBadges.cshtml` usa `IEnumerable<MedicoBadgeDto>` para reutilizarse tanto en Detalle como en Dashboard.

**Tech Stack:** ASP.NET Core 8, Razor Pages, EF Core 8, SQL Server, Bootstrap 5, ASP.NET Identity

---

## Mapa de archivos afectados

| Archivo | Acción |
|---------|--------|
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | Modificar: añadir 5 propiedades bool de rol |
| `Pages/DirectorioMedicos/Detalle.cshtml` | Modificar: condicionales por rol, sección ubicaciones extendida |
| `Services/Medico/MedicoBadgeService.cs` | Modificar: bajar umbral `validador_contenido` de 5→1 |
| `Services/Medico/IMedicoBadgeService.cs` | Modificar: añadir `EvaluarBadgesPorUserIdAsync` |
| `Services/Glossary/GlossaryService.cs` | Modificar: hook badge en `AddValidationAsync`, `ComentariosMedicos` en `GetValidationCountsAsync` |
| `Services/Glossary/DTOs/GlossaryValidationCountsDto.cs` | Modificar: añadir `ComentariosMedicos` |
| `Pages/Shared/_MedicoBadges.cshtml` | Crear: partial reutilizable con `IEnumerable<MedicoBadgeDto>` |
| `Pages/Shared/_MedicoCard.cshtml` | Modificar: añadir badges compactos |
| `Pages/DirectorioMedicos/Index.cshtml.cs` | Modificar: cargar badges por médico en Index |
| `Pages/Glosario/Termino.cshtml` | Modificar: sección "Validado por médicos" |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml` | Modificar: añadir botón "Re-evaluar badges" en panel |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` | Modificar: añadir handler `OnPostReEvaluarBadgesAsync` |
| `Areas/Identity/Pages/Medico/Dashboard.cshtml` | Modificar: usar partial `_MedicoBadges` |

---

## FASE 1 — Permisos por rol en Detalle (PageModel)

**Archivos:**
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml.cs`
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml`

### Contexto real del código actual
`DetalleModel.OnGetAsync` usa `_db.MedicosPerfilExtendido` con campo `UserId` (Guid?). El `ObtenerUsuarioId()` retorna `Guid?`. El `UserManager` NO está inyectado — se determina el rol mediante `User.IsInRole()`.

---

- [ ] **1.1 — Añadir propiedades bool al PageModel**

En `Detalle.cshtml.cs`, añadir las 5 propiedades después de `public bool PerfilYaVinculado { get; set; }`:

```csharp
public bool IsPaciente { get; private set; }
public bool IsMedico { get; private set; }
public bool IsAdmin { get; private set; }
public bool IsOwnerMedico { get; private set; }
public bool CanInteractAsPaciente { get; private set; }
```

- [ ] **1.2 — Calcular las propiedades en OnGetAsync**

Dentro de `OnGetAsync`, después de `if (Medico is null) return NotFound();`, añadir:

```csharp
IsMedico  = User.IsInRole("Medico");
IsPaciente = User.IsInRole("Paciente");
IsAdmin   = User.IsInRole("Administrador");

// ¿El médico autenticado está viendo SU propio perfil?
if (IsMedico && usuarioId.HasValue)
{
    IsOwnerMedico = await _db.MedicosPerfilExtendido
        .AnyAsync(p => p.MedicoId == id && p.UserId == usuarioId.Value);
}

CanInteractAsPaciente = (IsPaciente || IsAdmin) && !IsMedico;
```

- [ ] **1.3 — Ajustar "¿Recibiste atención?" en la vista**

En `Detalle.cshtml`, reemplazar `@if (User.Identity?.IsAuthenticated == true)` que rodea el bloque de confirmación (líneas 153–202) por:

```cshtml
@if (Model.CanInteractAsPaciente)
```

El bloque `else { vote-auth-notice }` (líneas 197-202) cambia a:

```cshtml
@if (!User.Identity?.IsAuthenticated ?? true)
{
    <div class="vote-auth-notice mb-4">
        <span>¿Recibiste atención de este médico?</span>
        <a asp-area="Identity" asp-page="/Account/Login">Inicia sesión para confirmarlo</a>
    </div>
}
```

- [ ] **1.4 — Ajustar "¿Conoces o fuiste paciente?" (ConfirmarSimple)**

El segundo bloque autenticado (líneas 205–243) también cambia:

```cshtml
@if (Model.CanInteractAsPaciente)
```

- [ ] **1.5 — Ajustar "Reclamar perfil" — solo para Médicos**

El bloque actual (líneas 246–253) muestra el botón a cualquier usuario si `!PerfilYaVinculado`. Reemplazar por:

```cshtml
@if (!Model.PerfilYaVinculado && (Model.IsMedico || Model.IsAdmin))
{
    <div class="mt-3">
        <a href="/directorio/reclamar/@Model.Medico.Id" class="btn btn-outline-primary">
            <i class="bi bi-person-check me-1"></i> ¿Eres este médico? Reclamar perfil
        </a>
    </div>
}
```

- [ ] **1.6 — Añadir link "Mi dashboard" para el dueño del perfil**

Después del bloque de Reclamar, añadir:

```cshtml
@if (Model.IsOwnerMedico)
{
    <div class="mt-3">
        <a asp-area="Identity" asp-page="/Medico/Dashboard" class="btn btn-primary">
            <i class="bi bi-speedometer2 me-1"></i> Ir a mi dashboard
        </a>
    </div>
}
```

- [ ] **1.7 — Verificar build**

```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

Esperado: sin errores nuevos.

- [ ] **1.8 — Commit Fase 1**

```powershell
git add Pages/DirectorioMedicos/Detalle.cshtml Pages/DirectorioMedicos/Detalle.cshtml.cs
git commit -m "feat(directorio): separar visibilidad por rol en Detalle del médico"
```

---

## FASE 2 — Ubicaciones extendidas en Detalle

**Archivos:**
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml.cs`
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml`

### Contexto real
`MedicoPerfilExtendido.Hospitales` es un campo `string?` libre (no una colección). No existe tabla separada de ubicaciones reportadas por pacientes — `DirectorioMedicoConfirmacion` no tiene campo hospital. Las "dos fuentes" disponibles son:
1. `MedicoDirectorio.HospitalClinica` + `Estado` + `Ciudad` (datos originales del directorio, aportados por la comunidad)
2. `MedicoPerfilExtendido.Hospitales` + `Estado` + `Ciudad` (datos ingresados por el propio médico en su perfil)

---

- [ ] **2.1 — Crear ViewModel de ubicación**

Al final de `Models/Directorio/DirectorioViewModels.cs`, añadir:

```csharp
public class UbicacionMedicoVm
{
    public string? Hospital { get; set; }
    public string? Ciudad { get; set; }
    public string? Estado { get; set; }
    public string Fuente { get; set; } = "comunidad"; // "medico" | "comunidad"
}
```

- [ ] **2.2 — Añadir propiedad y lógica en PageModel**

En `Detalle.cshtml.cs`, añadir propiedad después de `CanInteractAsPaciente`:

```csharp
public List<UbicacionMedicoVm> UbicacionesCombinadas { get; private set; } = new();
```

En `OnGetAsync`, después de calcular `CanInteractAsPaciente`, añadir:

```csharp
// Ubicaciones del médico (perfil extendido vinculado)
var perfilExtendido = await _db.MedicosPerfilExtendido
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.MedicoId == id && p.UserId != null);

if (perfilExtendido is not null && (!string.IsNullOrWhiteSpace(perfilExtendido.Hospitales)
    || !string.IsNullOrWhiteSpace(perfilExtendido.Estado)
    || !string.IsNullOrWhiteSpace(perfilExtendido.Ciudad)))
{
    UbicacionesCombinadas.Add(new UbicacionMedicoVm
    {
        Hospital = perfilExtendido.Hospitales,
        Ciudad   = perfilExtendido.Ciudad ?? Medico.Ciudad,
        Estado   = perfilExtendido.Estado ?? Medico.Estado,
        Fuente   = "medico"
    });
}

// Ubicación reportada por la comunidad (datos originales del directorio)
var hospitalComunidad = Medico.HospitalClinica;
var ciudadComunidad   = Medico.Ciudad;
var estadoComunidad   = Medico.Estado;

var yaEnLista = UbicacionesCombinadas.Any(u =>
    string.Equals(u.Hospital, hospitalComunidad, StringComparison.OrdinalIgnoreCase));

if (!yaEnLista && (!string.IsNullOrWhiteSpace(hospitalComunidad)
    || !string.IsNullOrWhiteSpace(ciudadComunidad)))
{
    UbicacionesCombinadas.Add(new UbicacionMedicoVm
    {
        Hospital = hospitalComunidad,
        Ciudad   = ciudadComunidad,
        Estado   = estadoComunidad,
        Fuente   = "comunidad"
    });
}
```

- [ ] **2.3 — UI en la vista**

En `Detalle.cshtml`, reemplazar la card de Ubicación (columna col-md-6 con `<h6>Ubicación</h6>`, líneas 102–114) por:

```cshtml
<div class="col-md-6">
    <div class="crm-card h-100">
        <h6 class="crm-label mb-3"><i class="bi bi-geo-alt me-1"></i>Consultorios / Hospitales</h6>
        @if (Model.UbicacionesCombinadas.Any())
        {
            @foreach (var ub in Model.UbicacionesCombinadas)
            {
                <div class="d-flex align-items-start gap-2 mb-2">
                    <i class="bi bi-geo-alt-fill text-primary mt-1"></i>
                    <div>
                        @if (!string.IsNullOrWhiteSpace(ub.Hospital))
                        { <div class="fw-semibold">@ub.Hospital</div> }
                        @if (!string.IsNullOrWhiteSpace(ub.Ciudad) || !string.IsNullOrWhiteSpace(ub.Estado))
                        { <div class="text-muted small">@string.Join(", ", new[]{ ub.Ciudad, ub.Estado }.Where(x => !string.IsNullOrWhiteSpace(x)))</div> }
                        @if (ub.Fuente == "medico")
                        { <span class="badge bg-success-subtle text-success" style="font-size:.65rem;">✓ Confirmado por médico</span> }
                        else
                        { <span class="badge bg-secondary-subtle text-secondary" style="font-size:.65rem;">Reportado por la comunidad</span> }
                    </div>
                </div>
            }
        }
        else
        {
            @if (!string.IsNullOrWhiteSpace(Model.Medico!.Estado))
            { <p class="mb-1"><strong>Estado:</strong> @Model.Medico.Estado</p> }
            @if (!string.IsNullOrWhiteSpace(Model.Medico.Ciudad))
            { <p class="mb-1"><strong>Ciudad:</strong> @Model.Medico.Ciudad</p> }
            @if (!string.IsNullOrWhiteSpace(Model.Medico.MunicipioAlcaldia))
            { <p class="mb-1"><strong>Municipio/Alcaldía:</strong> @Model.Medico.MunicipioAlcaldia</p> }
            @if (!string.IsNullOrWhiteSpace(Model.Medico.HospitalClinica))
            { <p class="mb-0"><strong>Hospital/Clínica:</strong> @Model.Medico.HospitalClinica</p> }
        }
    </div>
</div>
```

- [ ] **2.4 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **2.5 — Commit Fase 2**

```powershell
git add Pages/DirectorioMedicos/Detalle.cshtml Pages/DirectorioMedicos/Detalle.cshtml.cs Models/Directorio/DirectorioViewModels.cs
git commit -m "feat(directorio): mostrar ubicaciones combinadas (médico + comunidad) en Detalle"
```

---

## FASE 3 — Fix Badge validador_contenido

**Archivos:**
- Modify: `Services/Medico/MedicoBadgeService.cs` — umbral 5→1
- Modify: `Services/Medico/IMedicoBadgeService.cs` — añadir método `EvaluarBadgesPorUserIdAsync`
- Modify: `Services/Glossary/GlossaryService.cs` — hook post-save + inyectar servicio

### Contexto real
- `GlossaryValidation.UserId` es `string` (ASP.NET Identity, hasta 450 chars)
- `EvaluarBadgesAutomaticosAsync(int medicoId)` requiere el ID del directorio, no el UserId
- `MedicosPerfilExtendido.UserId` es `Guid?` — el puente entre Identity userId y medicoId
- `GlossaryService` ya tiene `_db` inyectado — puede hacer el lookup sin nueva dependencia

---

- [ ] **3.1 — Bajar umbral validador_contenido de 5 a 1**

En `MedicoBadgeService.cs`, línea donde cuenta validaciones y compara con 5:

```csharp
// ANTES:
var validaciones = await _db.GlossaryValidations
    .CountAsync(v => v.UserId == userIdStr);
if (validaciones >= 5)
    await OtorgarBadgeAsync(medicoId, "validador_contenido", "sistema");

// DESPUÉS:
var validaciones = await _db.GlossaryValidations
    .CountAsync(v => v.UserId == userIdStr && v.Approved);
if (validaciones >= 1)
    await OtorgarBadgeAsync(medicoId, "validador_contenido", "sistema");
```

- [ ] **3.2 — Añadir método por userId en la interfaz**

En `IMedicoBadgeService.cs`, añadir:

```csharp
Task EvaluarBadgesPorUserIdAsync(string userId);
```

- [ ] **3.3 — Implementar EvaluarBadgesPorUserIdAsync en el servicio**

En `MedicoBadgeService.cs`, añadir método al final de la clase (antes del `}`):

```csharp
public async Task EvaluarBadgesPorUserIdAsync(string userId)
{
    if (!Guid.TryParse(userId, out var userGuid)) return;

    var perfil = await _db.MedicosPerfilExtendido
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.UserId == userGuid && p.MedicoId != null);

    if (perfil?.MedicoId is null) return;

    await EvaluarBadgesAutomaticosAsync(perfil.MedicoId.Value);
}
```

- [ ] **3.4 — Inyectar IMedicoBadgeService en GlossaryService**

En `GlossaryService.cs`, añadir campo y parámetro al constructor:

```csharp
// Campo (junto a los demás):
private readonly IMedicoBadgeService _medicoBadgeService;

// En el constructor, añadir parámetro:
IMedicoBadgeService medicoBadgeService,

// En el cuerpo del constructor, asignar:
_medicoBadgeService = medicoBadgeService;
```

El constructor queda con 7 parámetros. Verificar que DI en `Program.cs` ya registra `IMedicoBadgeService` — existe el registro `builder.Services.AddScoped<IMedicoBadgeService, MedicoBadgeService>()`.

- [ ] **3.5 — Añadir hook badge en AddValidationAsync**

En `GlossaryService.AddValidationAsync`, inmediatamente después de `await _db.SaveChangesAsync()` (línea 290) y antes del bloque de cache invalidation, añadir:

```csharp
// Hook: otorgar badge validador_contenido si el usuario es médico
try
{
    await _medicoBadgeService.EvaluarBadgesPorUserIdAsync(userId);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "No se pudo evaluar badges para usuario {UserId}", userId);
}
```

- [ ] **3.6 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **3.7 — Commit Fase 3**

```powershell
git add Services/Medico/MedicoBadgeService.cs Services/Medico/IMedicoBadgeService.cs Services/Glossary/GlossaryService.cs
git commit -m "fix(badges): corregir umbral validador_contenido (5->1) y hook post-validacion glosario"
```

---

## FASE 4 — Partial _MedicoBadges y display en Detalle + Cards

**Archivos:**
- Create: `Pages/Shared/_MedicoBadges.cshtml`
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml.cs` — cargar badges
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml` — incluir partial
- Modify: `Pages/Shared/_MedicoCard.cshtml` — badges compactos

### Contexto real
- El Dashboard usa `IEnumerable<MedicoBadgeDto>` (el DTO del servicio con flag `Obtenido`)
- Detalle actualmente NO carga badges — hay que añadir la query en `OnGetAsync`
- `_MedicoCard.cshtml` ya recibe `MedicoCardVm` que no incluye badges — no se puede pasar badges desde ahí sin modificar el ViewModel y el query del Index

---

- [ ] **4.1 — Crear Pages/Shared/_MedicoBadges.cshtml**

```cshtml
@model IEnumerable<eiibd26.Models.Medico.MedicoBadgeDto>
@{
    var definiciones = new[]
    {
        new { Codigo = "perfil_reclamado",    Icono = "bi-shield-check",      Label = "Perfil Reclamado",    Color = "#4f46e5" },
        new { Codigo = "verificado",          Icono = "bi-patch-check-fill",  Label = "Verificado",          Color = "#0ea5e9" },
        new { Codigo = "activo_comunidad",    Icono = "bi-people-fill",       Label = "Activo en Comunidad", Color = "#22c55e" },
        new { Codigo = "participante_qa",     Icono = "bi-chat-dots-fill",    Label = "Participa en Q&A",    Color = "#f59e0b" },
        new { Codigo = "validador_contenido", Icono = "bi-check2-circle",     Label = "Valida Contenido",    Color = "#8b5cf6" },
        new { Codigo = "creador_contenido",   Icono = "bi-pencil-square",     Label = "Crea Contenido",      Color = "#ec4899" },
    };
    var obtenidos = Model?.Where(b => b.Obtenido).Select(b => b.Codigo).ToHashSet()
        ?? new HashSet<string>();
}

<div class="medico-badges-row d-flex flex-wrap gap-2">
    @foreach (var def in definiciones)
    {
        var tiene = obtenidos.Contains(def.Codigo);
        var style = tiene
            ? $"background:{def.Color}20; border:1.5px solid {def.Color}; color:{def.Color};"
            : "background:#f3f4f6; border:1.5px solid #d1d5db; color:#9ca3af;";
        <span class="badge-medico-pill" style="@style"
              title="@(tiene ? def.Label : $"{def.Label} (no obtenido)")"
              data-bs-toggle="tooltip" data-bs-placement="top">
            <i class="bi @def.Icono me-1"></i>
            <span class="badge-label d-none d-md-inline">@def.Label</span>
        </span>
    }
</div>
```

- [ ] **4.2 — CSS de badges (añadir en wwwroot/css/directorio-medicos.css)**

Leer el archivo primero para añadir al final:

```css
.badge-medico-pill {
    display: inline-flex;
    align-items: center;
    padding: 4px 10px;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: default;
    transition: transform 0.1s;
    white-space: nowrap;
}
.badge-medico-pill:hover { transform: scale(1.05); }
```

- [ ] **4.3 — Cargar badges en Detalle.cshtml.cs**

Añadir propiedad en `DetalleModel`:

```csharp
public List<MedicoBadgeDto> Badges { get; private set; } = new();
```

En `OnGetAsync`, después de `PerfilYaVinculado = ...`, añadir:

```csharp
Badges = await _badgeService.GetTodosLosBadgesAsync(id);
```

Añadir using si falta: `using eiibd26.Models.Medico;`

- [ ] **4.4 — Incluir partial en Detalle.cshtml**

Después de los badges de nivel de confianza actuales (div con badges comunidad/verificado/reclamado, cierre del `div class="flex-grow-1"`, línea ~96), añadir antes del cierre de `.crm-card`:

```cshtml
<div class="mt-3">
    <partial name="_MedicoBadges" model="Model.Badges" />
</div>
```

- [ ] **4.5 — Badges compactos en _MedicoCard.cshtml**

`MedicoCardVm` no tiene badges. La solución sin tocar el ViewModel es omitir los 6 badges del sistema de la card (que ya tiene los 3 status-badges existentes de comunidad/verificado/médico). El partial completo va solo en Detalle. **No modificar `_MedicoCard.cshtml` para badges de sistema** — ya hay info suficiente de nivel. Marcar esta sub-tarea como N/A por decisión de alcance: los 3 status badges existentes ya cumplen la función en la card.

- [ ] **4.6 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **4.7 — Commit Fase 4**

```powershell
git add Pages/Shared/_MedicoBadges.cshtml Pages/DirectorioMedicos/Detalle.cshtml Pages/DirectorioMedicos/Detalle.cshtml.cs wwwroot/css/directorio-medicos.css
git commit -m "feat(badges): partial _MedicoBadges y display en Detalle del médico"
```

---

## FASE 5 — Admin Panel: Re-evaluar badges

**Archivos:**
- Modify: `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` — handler re-evaluar
- Modify: `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml` — botón JS + llamada

### Contexto real
El Admin ya tiene `OnPostOtorgarBadgeAsync` y `OnPostRevocarBadgeAsync`. Los botones de otorgar badges manuales (verificado, creador_contenido) ya existen en el JS (`otorgarBadge`/`revocarBadge` functions). Falta: botón "Re-evaluar" que llame a `EvaluarBadgesAutomaticosAsync`.

---

- [ ] **5.1 — Añadir handler OnPostReEvaluarBadgesAsync**

En `Index.cshtml.cs` (Admin), añadir después de `OnPostOtorgarBadgeAsync`:

```csharp
public async Task<IActionResult> OnPostReEvaluarBadgesAsync(int medicoId)
{
    await _badgeService.EvaluarBadgesAutomaticosAsync(medicoId);
    return new JsonResult(new { success = true });
}
```

- [ ] **5.2 — Añadir función JS reEvaluarBadges en el panel admin**

En `Index.cshtml`, dentro del bloque `<script>`, añadir después de la función `revocarBadge`:

```javascript
async function reEvaluarBadges(medicoId) {
    const r = await postJson('@Url.Page(null, "ReEvaluarBadges")', { medicoId });
    if (r.success) { abrirEditar(medicoId); }
    else alert('Error al re-evaluar badges.');
}
```

- [ ] **5.3 — Añadir botón "Re-evaluar" en la sección de badges del panel**

Buscar en `Index.cshtml` el bloque donde se renderizan los badges (buscar `badges.forEach` o la sección de badges). Añadir un botón al final de esa sección:

```javascript
// En la función que renderiza los badges del panel de edición (junto al listado de badges):
const btnReEval = document.createElement('button');
btnReEval.className = 'btn btn-sm btn-outline-secondary mt-2';
btnReEval.textContent = '↺ Re-evaluar badges automáticos';
btnReEval.onclick = () => reEvaluarBadges(medicoId);
badgesContainer.appendChild(btnReEval);
```

> **Nota:** Localizar exactamente el contenedor de badges en el JS del Admin (`abrirEditar` function) e insertar el botón ahí. El nombre del contenedor varía — buscar `id="badges"` o similar en el HTML del modal/panel.

- [ ] **5.4 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **5.5 — Commit Fase 5**

```powershell
git add Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs
git commit -m "feat(admin): añadir botón Re-evaluar badges automáticos en panel admin"
```

---

## FASE 6 — Comentarios médicos visibles en /Termino/

**Archivos:**
- Modify: `Services/Glossary/DTOs/GlossaryValidationCountsDto.cs` — añadir `ComentariosMedicos`
- Modify: `Services/Glossary/GlossaryService.cs` — poblar `ComentariosMedicos` en `GetValidationCountsAsync`
- Modify: `Pages/Glosario/Termino.cshtml` — sección "Validado por médicos"

### Contexto real
- `GlossaryValidation.UserId` es `string` (Identity)
- Para saber si el médico tiene `perfil_reclamado` o `verificado`, hay que join con `MedicosPerfilExtendido` y `MedicosPerfilBadge`
- `MedicoDirectorio.NombreCompleto` es el nombre público del médico

---

- [ ] **6.1 — Añadir DTO ValidationCommentDto y propiedad en GlossaryValidationCountsDto**

En `GlossaryValidationCountsDto.cs`, añadir al final del archivo:

```csharp
public class ValidationCommentDto
{
    public string UserDisplay { get; set; } = "Médico verificado";
    public GlossaryValidationType ValidationType { get; set; }
    public MedicalRelationType? RelationType { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Y en la clase `GlossaryValidationCountsDto`, añadir propiedad:

```csharp
public List<ValidationCommentDto> ComentariosMedicos { get; set; } = new();
```

- [ ] **6.2 — Poblar ComentariosMedicos en GetValidationCountsAsync**

En `GlossaryService.GetValidationCountsAsync`, después de construir el objeto `return new GlossaryValidationCountsDto { ... }` (antes del return), añadir la query de comentarios:

```csharp
// Cargar comentarios de validadores médicos con nombre público
var validacionesConComentario = await _db.GlossaryValidations
    .AsNoTracking()
    .Where(v => v.GlossaryTermId == termId && v.Approved
        && !string.IsNullOrEmpty(v.Comment))
    .Select(v => new { v.UserId, v.ValidationType, v.MedicalRelationTypeId, v.Comment, v.CreatedAt })
    .ToListAsync();

var comentarios = new List<ValidationCommentDto>();
foreach (var val in validacionesConComentario)
{
    if (!Guid.TryParse(val.UserId, out var userGuid)) continue;

    // Buscar perfil médico vinculado
    var perfilMedico = await _db.MedicosPerfilExtendido
        .AsNoTracking()
        .Include(p => p.Medico)
        .FirstOrDefaultAsync(p => p.UserId == userGuid && p.MedicoId != null);

    string display = "Médico verificado";
    if (perfilMedico?.Medico != null)
    {
        // Mostrar nombre solo si tiene badge perfil_reclamado o verificado
        var tieneNombrePublico = await _db.MedicosPerfilBadge
            .AnyAsync(pb => pb.MedicoId == perfilMedico.MedicoId
                && _db.MedicosBadge.Any(b => b.Id == pb.BadgeId
                    && (b.Codigo == "perfil_reclamado" || b.Codigo == "verificado")));

        if (tieneNombrePublico)
            display = $"Dr. {perfilMedico.Medico.NombreCompleto}";
    }

    comentarios.Add(new ValidationCommentDto
    {
        UserDisplay  = display,
        ValidationType = val.ValidationType,
        RelationType   = val.MedicalRelationTypeId,
        Comment        = val.Comment,
        CreatedAt      = val.CreatedAt
    });
}
```

Y en el objeto retornado, añadir:

```csharp
ComentariosMedicos = comentarios
```

- [ ] **6.3 — Mostrar sección en Termino.cshtml**

Localizar en `Termino.cshtml` dónde se muestra `Term.ValidationCounts` (buscar `counts` o `ValidationCounts`). Añadir al final de la sección de validaciones:

```cshtml
@if (Model.Term?.ValidationCounts?.ComentariosMedicos?.Any(c => !string.IsNullOrWhiteSpace(c.Comment)) == true)
{
    <div class="mt-4">
        <h3 style="font-size:1rem;font-weight:700;color:var(--color-text-primary);margin-bottom:1rem;">
            <i class="bi bi-patch-check text-primary me-1"></i> Validado por médicos
        </h3>
        @foreach (var val in Model.Term.ValidationCounts.ComentariosMedicos.Where(c => !string.IsNullOrWhiteSpace(c.Comment)))
        {
            <div class="crm-card mb-2 p-3">
                <div class="d-flex gap-2 align-items-start">
                    <i class="bi bi-person-badge-fill text-primary mt-1"></i>
                    <div>
                        <span class="fw-semibold small">@val.UserDisplay</span>
                        <span class="badge bg-primary-subtle text-primary ms-1" style="font-size:.65rem;">
                            @(val.ValidationType == eiibd26.Models.Glossary.GlossaryValidationType.MeaningValidation ? "Descripción" : "Relación EII")
                        </span>
                        @if (val.RelationType.HasValue)
                        {
                            <span class="badge bg-secondary-subtle ms-1" style="font-size:.65rem;">@val.RelationType.Value.ToString()</span>
                        }
                        <p class="mb-0 mt-1 small text-dark fst-italic">"@val.Comment"</p>
                        <small class="text-muted">@val.CreatedAt.ToString("MMMM yyyy")</small>
                    </div>
                </div>
            </div>
        }
    </div>
}
```

> **Nota:** Verificar el nombre exacto de la propiedad en `GlossaryTermDetailDto` que expone `ValidationCounts`. Podría ser `Term.Counts`, `Term.ValidationCounts`, o similar — leer `GlossaryTermDetailDto.cs` y ajustar.

- [ ] **6.4 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **6.5 — Commit Fase 6**

```powershell
git add Services/Glossary/DTOs/GlossaryValidationCountsDto.cs Services/Glossary/GlossaryService.cs Pages/Glosario/Termino.cshtml
git commit -m "feat(glosario): mostrar comentarios de médicos validadores en termino"
```

---

## FASE 7 — Dashboard del Médico: usar partial _MedicoBadges

**Archivos:**
- Modify: `Areas/Identity/Pages/Medico/Dashboard.cshtml`

### Contexto real
`DashboardModel` ya carga `TodosLosBadges` (tipo `List<MedicoBadgeDto>`) via `GetTodosLosBadgesAsync`. El partial `_MedicoBadges` acepta `IEnumerable<MedicoBadgeDto>`. Solo hay que usar el partial en la vista.

---

- [ ] **7.1 — Leer Dashboard.cshtml para localizar la sección de badges actual**

```powershell
Get-Content "Areas\Identity\Pages\Medico\Dashboard.cshtml" | Select-String -Pattern "badge|Badge|nivel" -CaseSensitive:$false | Select-Object -First 20
```

- [ ] **7.2 — Reemplazar o añadir sección de badges en Dashboard.cshtml**

Localizar donde el Dashboard muestra los badges actuales (buscar la sección con `TodosLosBadges` o `badge-container`). Reemplazar ese bloque por:

```cshtml
<div class="manage-card">
    <div class="card-body">
        <h2 class="card-section-title">Mis Badges</h2>
        <partial name="_MedicoBadges" model="Model.TodosLosBadges" />
        <p class="text-muted small mt-2">
            Los badges se actualizan automáticamente según tu actividad en la plataforma.
        </p>
    </div>
</div>
```

- [ ] **7.3 — Verificar build**

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "error|Error" | Where-Object { $_ -notmatch "0 Error" }
```

- [ ] **7.4 — Commit Fase 7**

```powershell
git add Areas/Identity/Pages/Medico/Dashboard.cshtml
git commit -m "feat(dashboard): usar partial _MedicoBadges en dashboard del médico"
```

---

## Checklist Final

```powershell
dotnet build --no-incremental 2>&1 | tail -5
```

- [ ] Build limpio sin errores ni warnings nuevos
- [ ] Médico NO ve "¿Recibiste atención?" en Detalle (IsMedico && !IsAdmin)
- [ ] Paciente SÍ ve ese bloque
- [ ] Reclamar perfil solo aparece para médicos (y admin)
- [ ] "Ir a mi dashboard" aparece solo si `IsOwnerMedico`
- [ ] Ubicaciones combinadas muestran fuente correcta
- [ ] Los 6 badges aparecen en Detalle (en color si obtenido, gris si no)
- [ ] Validar término en /Termino/ → badge `validador_contenido` se otorga (umbral ≥1)
- [ ] Comentarios de médicos visibles en /Termino/ si existen
- [ ] Dashboard muestra badges actualizados con partial
- [ ] Admin puede re-evaluar badges automáticos desde el panel
- [ ] Ninguna ruta pública cambió
