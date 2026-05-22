# Médico: Áreas EII persistentes + Dashboard — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persistir la selección de áreas EII en el perfil médico y crear un dashboard progresivo para doctores con 6 secciones desbloqueables por nivel de badge.

**Architecture:** Tabla `MedicoAreaEii` (MedicoPerfilExtendido.Id ↔ condiciones.id) para las áreas. Dashboard en `Areas/Identity/Pages/Medico/Dashboard.cshtml` con contenido condicional por `IMedicoBadgeService.GetNivelActualAsync`. Redirect post-login via middleware en Program.cs que intercepta `"/"` para usuarios con rol Medico.

**Tech Stack:** ASP.NET Core 8 Razor Pages, EF Core 8 (SQL-first schema), ASP.NET Identity, Bootstrap 5, IMedicoBadgeService (ya en DI)

---

## Mapa de archivos

| Acción | Ruta |
|--------|------|
| Crear | `Migrations/2026-05-21_MedicoAreaEii.sql` |
| Crear | `Models/Medico/MedicoAreaEii.cs` |
| Modificar | `Data/ApplicationDbContext.cs` |
| Modificar | `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs` |
| Modificar | `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml` |
| Crear | `Areas/Identity/Pages/Medico/Dashboard.cshtml` |
| Crear | `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs` |
| Modificar | `Program.cs` (middleware redirect + `AllowAnonymousToAreaPage`) |

---

## Task 1: Tabla MedicoAreaEii — SQL + Entidad + ApplicationDbContext

**Files:**
- Create: `Migrations/2026-05-21_MedicoAreaEii.sql`
- Create: `Models/Medico/MedicoAreaEii.cs`
- Modify: `Data/ApplicationDbContext.cs`

- [ ] **Step 1.1: Crear script SQL**

`Migrations/2026-05-21_MedicoAreaEii.sql`:
```sql
USE eiibd26;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoAreaEii')
BEGIN
    CREATE TABLE MedicoAreaEii (
        MedicoPerfilId INT NOT NULL,
        CondicionId    INT NOT NULL,
        CONSTRAINT PK_MedicoAreaEii PRIMARY KEY (MedicoPerfilId, CondicionId),
        CONSTRAINT FK_MedicoAreaEii_Perfil
            FOREIGN KEY (MedicoPerfilId) REFERENCES MedicoPerfilExtendido(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoAreaEii_Condicion
            FOREIGN KEY (CondicionId) REFERENCES condiciones(id) ON DELETE CASCADE
    );
    PRINT 'Tabla MedicoAreaEii creada.';
END
ELSE PRINT 'Tabla MedicoAreaEii ya existe.';
GO
```

- [ ] **Step 1.2: Aplicar a la BD**

Usando la connection string del proyecto (`Server=132.148.74.136\\ybridio;Database=eiibd26;user id=sa;password=U3xc3pt!0n!22;TrustServerCertificate=True;MultipleActiveResultSets=true`), ejecutar con sqlcmd o PowerShell+SqlClient:

```powershell
sqlcmd -S "132.148.74.136\ybridio" -d eiibd26 -U sa -P "U3xc3pt!0n!22" -i "Migrations\2026-05-21_MedicoAreaEii.sql"
```

Verificar: `SELECT name FROM sys.tables WHERE name = 'MedicoAreaEii'` → debe devolver 1 fila.

- [ ] **Step 1.3: Crear entidad C#**

`Models/Medico/MedicoAreaEii.cs`:
```csharp
namespace eiibd26.Models.Medico;

public class MedicoAreaEii
{
    public int MedicoPerfilId { get; set; }
    public int CondicionId { get; set; }

    public virtual MedicoPerfilExtendido? MedicoPerfil { get; set; }
}
```

- [ ] **Step 1.4: Agregar DbSet y Fluent config en ApplicationDbContext.cs**

Agregar el DbSet después de `MedicosReclamacionToken`:
```csharp
public DbSet<eiibd26.Models.Medico.MedicoAreaEii> MedicoAreasEii { get; set; }
```

Agregar al final de `OnModelCreating`, antes del cierre `}`:
```csharp
builder.Entity<eiibd26.Models.Medico.MedicoAreaEii>(b =>
{
    b.ToTable("MedicoAreaEii");
    b.HasKey(x => new { x.MedicoPerfilId, x.CondicionId });
    b.HasOne(x => x.MedicoPerfil)
     .WithMany()
     .HasForeignKey(x => x.MedicoPerfilId)
     .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 1.5: Verificar compilación**

```powershell
cd "D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26"
dotnet build --no-restore 2>&1 | Where-Object { $_ -match ": error (CS|RZ)" }
```

Esperado: 0 errores.

---

## Task 2: Persistir Áreas EII en PerfilMedico

**Files:**
- Modify: `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs`
- Modify: `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml`

### Cambios en PerfilMedico.cshtml.cs

- [ ] **Step 2.1: Actualizar propiedades del PageModel**

En `PerfilMedicoModel`, reemplazar `public List<int> AreasVinculadas { get; set; } = new();` por:
```csharp
public HashSet<int> AreasSeleccionadas { get; set; } = new();
```

Agregar el `BindProperty` para recibir los checkboxes del POST:
```csharp
[BindProperty]
public List<int> AreasEiiSeleccionadas { get; set; } = new();
```

- [ ] **Step 2.2: Actualizar OnGetAsync — cargar áreas**

En `OnGetAsync`, reemplazar el bloque que carga `AreasVinculadas` (dentro del `if (perfil.MedicoId.HasValue)`) con:

```csharp
// Cargar áreas EII seleccionadas desde MedicoAreaEii (usa perfil.Id, no medicoId)
AreasSeleccionadas = (await _db.MedicoAreasEii
    .AsNoTracking()
    .Where(a => a.MedicoPerfilId == perfil.Id)
    .Select(a => a.CondicionId)
    .ToListAsync()).ToHashSet();
```

> Nota: `perfil.Id` es el PK de `MedicoPerfilExtendido`, no `MedicoId`. Necesitas que `perfil` no sea `AsNoTracking` para tener el `Id` — o carga el `perfil.Id` explícitamente. Como `OnGetAsync` ya tiene `perfil` sin `AsNoTracking`, usa `perfil.Id` directamente.

- [ ] **Step 2.3: Actualizar OnPostAsync — guardar áreas**

Al final de `OnPostAsync`, antes del `if (ModelState.IsValid)` redirect, agregar el delete+insert:

```csharp
// Persistir áreas EII: delete existentes + insert nuevas
if (perfil.Id > 0)
{
    var existentes = await _db.MedicoAreasEii
        .Where(a => a.MedicoPerfilId == perfil.Id)
        .ToListAsync();
    _db.MedicoAreasEii.RemoveRange(existentes);

    foreach (var condicionId in AreasEiiSeleccionadas.Distinct())
    {
        _db.MedicoAreasEii.Add(new eiibd26.Models.Medico.MedicoAreaEii
        {
            MedicoPerfilId = perfil.Id,
            CondicionId    = condicionId
        });
    }
    await _db.SaveChangesAsync();
}
```

> Nota: en el caso en que `perfil` fue `Add`-eado en el mismo POST, `perfil.Id` estará disponible después del primer `SaveChangesAsync()` ya existente. Asegúrate de que el bloque de áreas va DESPUÉS del `SaveChangesAsync()` del perfil.

### Cambios en PerfilMedico.cshtml

- [ ] **Step 2.4: Actualizar checkboxes en la vista**

En `PerfilMedico.cshtml`, en el Bloque 2 (Información profesional), reemplazar el `foreach` de áreas que actualmente usa `name="areasEii"` y `Model.AreasVinculadas` por:

```cshtml
@foreach (var area in Model.AreasEiiDisponibles)
{
    <div class="col-6 col-md-4">
        <div class="form-check">
            <input class="form-check-input" type="checkbox"
                   name="AreasEiiSeleccionadas"
                   value="@area.Value"
                   id="area-@area.Value"
                   @(Model.AreasSeleccionadas.Contains(int.Parse(area.Value)) ? "checked" : "") />
            <label class="form-check-label" for="area-@area.Value">@area.Text</label>
        </div>
    </div>
}
```

- [ ] **Step 2.5: Verificar compilación**

```powershell
dotnet build --no-restore 2>&1 | Where-Object { $_ -match ": error (CS|RZ)" }
```

Esperado: 0 errores.

---

## Task 3: Dashboard del Médico

**Files:**
- Create: `Areas/Identity/Pages/Medico/Dashboard.cshtml`
- Create: `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`
- Modify: `Program.cs` (middleware redirect + allow anonymous configuration)

### Dashboard.cshtml.cs

- [ ] **Step 3.1: Crear el PageModel**

`Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`:

```csharp
using System.Security.Claims;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Medico;

[Authorize(Roles = "Medico")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IMedicoBadgeService _badgeService;

    public DashboardModel(ApplicationDbContext db, IMedicoBadgeService badgeService)
    {
        _db = db;
        _badgeService = badgeService;
    }

    // Datos del médico
    public int NivelActual { get; set; }
    public List<MedicoBadgeDto> TodosLosBadges { get; set; } = new();
    public string? NombreMedico { get; set; }
    public string? FotoUrl { get; set; }
    public int? MedicoDirectorioId { get; set; }

    // Sección: Pacientes que recomendaron
    public int TotalRecomendaciones { get; set; }
    public List<RecomendacionDashboardVm> Recomendaciones { get; set; } = new();

    // ID del perfil extendido (para el link a PerfilMedico)
    public bool TienePerfilVinculado { get; set; }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is not null)
        {
            FotoUrl              = perfil.Foto;
            TienePerfilVinculado = perfil.MedicoId.HasValue;
            MedicoDirectorioId   = perfil.MedicoId;
            NombreMedico         = perfil.Medico?.NombreCompleto;

            if (perfil.MedicoId.HasValue)
            {
                NivelActual    = await _badgeService.GetNivelActualAsync(perfil.MedicoId.Value);
                TodosLosBadges = await _badgeService.GetTodosLosBadgesAsync(perfil.MedicoId.Value);

                // Cargar recomendaciones de pacientes
                TotalRecomendaciones = await _db.DirectorioMedicoConfirmaciones
                    .CountAsync(c => c.MedicoId == perfil.MedicoId.Value && !c.Eliminado);

                if (NivelActual >= 2)
                {
                    var confirmaciones = await _db.DirectorioMedicoConfirmaciones
                        .AsNoTracking()
                        .Where(c => c.MedicoId == perfil.MedicoId.Value && !c.Eliminado)
                        .OrderByDescending(c => c.FechaConfirmacion)
                        .Take(20)
                        .ToListAsync();

                    // Para nivel 3: cargar nombres si el paciente autorizó
                    Dictionary<Guid, string?> nombresPacientes = new();
                    if (NivelActual >= 3)
                    {
                        var userIds = confirmaciones.Select(c => c.UsuarioId).Distinct().ToList();
                        nombresPacientes = await _db.Perfil
                            .AsNoTracking()
                            .Where(p => userIds.Contains(p.idUser) && p.PermitirCompartirDatosMedicos == true)
                            .ToDictionaryAsync(p => p.idUser, p => $"{p.Nombre} {p.Apellidos}".Trim());
                    }

                    Recomendaciones = confirmaciones.Select(c => new RecomendacionDashboardVm
                    {
                        FechaConfirmacion = c.FechaConfirmacion,
                        ExpCUCI           = c.ExpCUCI,
                        ExpCrohn          = c.ExpCrohn,
                        ExpPediatrico     = c.ExpPediatrico,
                        ExpBiologicos     = c.ExpBiologicos,
                        NombrePaciente    = NivelActual >= 3 && nombresPacientes.TryGetValue(c.UsuarioId, out var n) ? n : null,
                        UsuarioId         = NivelActual >= 3 ? c.UsuarioId : null
                    }).ToList();
                }
            }
        }
        else
        {
            // Médico sin perfil extendido aún
            NivelActual = 0;
        }

        return Page();
    }
}

public class RecomendacionDashboardVm
{
    public DateTime FechaConfirmacion { get; set; }
    public bool ExpCUCI { get; set; }
    public bool ExpCrohn { get; set; }
    public bool ExpPediatrico { get; set; }
    public bool ExpBiologicos { get; set; }
    public string? NombrePaciente { get; set; }
    public Guid? UsuarioId { get; set; }
}
```

### Dashboard.cshtml

- [ ] **Step 3.2: Crear la vista del Dashboard**

`Areas/Identity/Pages/Medico/Dashboard.cshtml`:

```cshtml
@page
@model eiibd26.Areas.Identity.Pages.Medico.DashboardModel
@{
    ViewData["Title"] = "Dashboard Médico";
    Layout = "_Layout";
}

@section Styles {
    <link rel="stylesheet" href="~/css/directorio-medicos.css" asp-append-version="true" />
    <style>
        .dash-card {
            background: #fff;
            border: 1px solid #e5e7eb;
            border-radius: 12px;
            padding: 1.25rem;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
            height: 100%;
        }
        .dash-card-locked {
            background: #f9fafb;
            border-color: #e5e7eb;
            opacity: .85;
        }
        .dash-card-title {
            font-size: 1rem;
            font-weight: 600;
            color: #1f2937;
            display: flex;
            align-items: center;
            gap: .5rem;
            margin-bottom: .75rem;
        }
        .dash-lock-badge {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            background: #f3f4f6;
            color: #6b7280;
            border-radius: 999px;
            padding: 2px 10px;
            font-size: .78rem;
            font-weight: 600;
        }
        .nivel-indicator {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: #f5f3ff;
            color: #7c3aed;
            border: 1px solid #e9d5ff;
            border-radius: 999px;
            padding: 4px 14px;
            font-size: .85rem;
            font-weight: 600;
        }
        .badge-item {
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 6px 0;
            border-bottom: 1px solid #f3f4f6;
        }
        .badge-item:last-child { border-bottom: none; }
        .rec-row {
            display: flex;
            align-items: flex-start;
            gap: 10px;
            padding: 8px 0;
            border-bottom: 1px solid #f3f4f6;
            font-size: .88rem;
        }
        .rec-row:last-child { border-bottom: none; }
    </style>
}

<div class="container py-4">

    @* Header *@
    <div class="d-flex align-items-center gap-3 mb-4">
        <img src="@(string.IsNullOrWhiteSpace(Model.FotoUrl) ? "https://ui-avatars.com/api/?name=M&size=56" : Model.FotoUrl)"
             class="rounded-circle" width="56" height="56" style="object-fit:cover;" />
        <div>
            <h1 style="font-size:1.6rem;font-weight:300;color:#172849;margin:0;">
                @(string.IsNullOrWhiteSpace(Model.NombreMedico) ? "Mi Dashboard" : $"Hola, {Model.NombreMedico}")
            </h1>
            <span class="nivel-indicator">
                <i class="bi bi-award-fill"></i> Nivel @Model.NivelActual de 6
            </span>
        </div>
    </div>

    @if (!Model.TienePerfilVinculado)
    {
        <div class="alert alert-info mb-4">
            <i class="bi bi-info-circle me-2"></i>
            Tu cuenta no está vinculada a un perfil del directorio todavía.
            <a asp-page="/DirectorioMedicos/Index" class="alert-link ms-1">Busca tu perfil</a>
            y haz clic en "Reclamar perfil" para desbloquear todas las secciones.
        </div>
    }

    <div class="row g-3">

        @* ── Sección 1: Mi Perfil (Nivel ≥ 1) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 1)
            {
                <div class="dash-card">
                    <div class="dash-card-title"><i class="bi bi-person-circle text-primary"></i> Mi Perfil</div>
                    <div class="d-flex gap-3 align-items-center mb-3">
                        <img src="@(string.IsNullOrWhiteSpace(Model.FotoUrl) ? "https://ui-avatars.com/api/?name=M&size=48" : Model.FotoUrl)"
                             class="rounded-circle" width="48" height="48" style="object-fit:cover;" />
                        <div>
                            <div class="fw-semibold">@(Model.NombreMedico ?? "Sin nombre")</div>
                            <div class="text-muted small">Médico EII</div>
                        </div>
                    </div>
                    <a asp-area="Identity" asp-page="/Account/Manage/PerfilMedico"
                       class="btn btn-outline-primary btn-sm w-100">
                        <i class="bi bi-pencil me-1"></i> Editar mi perfil
                    </a>
                    @if (Model.TienePerfilVinculado && Model.MedicoDirectorioId.HasValue)
                    {
                        <a asp-page="/DirectorioMedicos/Detalle" asp-route-id="@Model.MedicoDirectorioId"
                           class="btn btn-outline-secondary btn-sm w-100 mt-2">
                            <i class="bi bi-eye me-1"></i> Ver perfil público
                        </a>
                    }
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Mi Perfil
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 1</span>
                    </div>
                    <p class="text-muted small mb-0">Reclama tu perfil del directorio para desbloquear esta sección.</p>
                </div>
            }
        </div>

        @* ── Sección 2: Mis Badges (Nivel ≥ 1) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 1 && Model.TodosLosBadges.Any())
            {
                <div class="dash-card">
                    <div class="dash-card-title"><i class="bi bi-award" style="color:#7c3aed;"></i> Mis Badges</div>
                    @foreach (var badge in Model.TodosLosBadges)
                    {
                        <div class="badge-item">
                            <i class="@badge.Icono" style="font-size:1.2rem;color:@(badge.Obtenido ? "#7c3aed" : "#9ca3af");width:22px;text-align:center;"></i>
                            <div>
                                <div style="font-size:.85rem;font-weight:@(badge.Obtenido ? "600" : "400");color:@(badge.Obtenido ? "#1f2937" : "#9ca3af");">
                                    @badge.Nombre
                                </div>
                                @if (!badge.Obtenido)
                                {
                                    <div class="text-muted" style="font-size:.72rem;">@badge.ComoObtenerlo</div>
                                }
                            </div>
                        </div>
                    }
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Mis Badges
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 1</span>
                    </div>
                    <p class="text-muted small mb-0">Disponible al reclamar y completar tu perfil.</p>
                </div>
            }
        </div>

        @* ── Sección 3: Pacientes que me recomendaron (Nivel ≥ 1 teaser / ≥ 2 full) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 2)
            {
                <div class="dash-card">
                    <div class="dash-card-title">
                        <i class="bi bi-people-fill" style="color:#7c3aed;"></i>
                        Pacientes que me recomendaron
                    </div>
                    <div class="mb-2">
                        <span class="fw-bold" style="font-size:1.5rem;color:#7c3aed;">@Model.TotalRecomendaciones</span>
                        <span class="text-muted small ms-1">confirmaciones</span>
                    </div>
                    @if (Model.Recomendaciones.Any())
                    {
                        @foreach (var rec in Model.Recomendaciones)
                        {
                            <div class="rec-row">
                                <i class="bi bi-person-circle text-secondary" style="font-size:1.2rem;flex-shrink:0;"></i>
                                <div>
                                    @if (!string.IsNullOrWhiteSpace(rec.NombrePaciente))
                                    {
                                        <div class="fw-semibold">@rec.NombrePaciente</div>
                                    }
                                    else
                                    {
                                        <div class="text-muted">Paciente anónimo</div>
                                    }
                                    <div class="text-muted" style="font-size:.75rem;">@rec.FechaConfirmacion.ToString("dd MMM yyyy")</div>
                                    @{
                                        var areas = new List<string>();
                                        if (rec.ExpCUCI) areas.Add("CUCI");
                                        if (rec.ExpCrohn) areas.Add("Crohn");
                                        if (rec.ExpPediatrico) areas.Add("Pediátrico");
                                        if (rec.ExpBiologicos) areas.Add("Biológicos");
                                    }
                                    @if (areas.Any())
                                    {
                                        <div class="text-muted" style="font-size:.75rem;">@string.Join(", ", areas)</div>
                                    }
                                </div>
                                @if (rec.UsuarioId.HasValue && Model.NivelActual >= 3)
                                {
                                    <a asp-page="/Preguntas/Index"
                                       asp-route-usuario="@rec.UsuarioId"
                                       class="btn btn-outline-secondary btn-sm ms-auto flex-shrink-0">
                                        Responder
                                    </a>
                                }
                            </div>
                        }
                    }
                    else
                    {
                        <p class="text-muted small mb-0">Aún no tienes confirmaciones de pacientes.</p>
                    }
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Pacientes que me recomendaron
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 2</span>
                    </div>
                    <div class="mb-2">
                        <span class="fw-bold" style="font-size:1.5rem;color:#9ca3af;">@Model.TotalRecomendaciones</span>
                        <span class="text-muted small ms-1">confirmaciones</span>
                    </div>
                    <p class="text-muted small mb-0">Obtén el badge <strong>Verificado</strong> para ver quiénes te recomendaron.</p>
                </div>
            }
        </div>

        @* ── Sección 4: Q&A (Nivel ≥ 4) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 4)
            {
                <div class="dash-card">
                    <div class="dash-card-title"><i class="bi bi-chat-dots-fill" style="color:#7c3aed;"></i> Q&A Comunidad</div>
                    <p class="text-muted small mb-3">Responde preguntas de pacientes con EII en tu área de especialidad.</p>
                    <a asp-page="/Preguntas/Index" class="btn btn-primary btn-sm w-100">
                        <i class="bi bi-chat-text me-1"></i> Ver preguntas
                    </a>
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Q&A Comunidad
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 4</span>
                    </div>
                    <p class="text-muted small mb-0">Disponible en Nivel 4: Participante Q&A.</p>
                </div>
            }
        </div>

        @* ── Sección 5: Validar Contenido (Nivel ≥ 5) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 5)
            {
                <div class="dash-card">
                    <div class="dash-card-title"><i class="bi bi-check-circle-fill" style="color:#7c3aed;"></i> Validar Contenido</div>
                    <p class="text-muted small mb-3">Revisa y valida términos médicos del glosario EII.</p>
                    <a asp-page="/Glosario/Index" class="btn btn-primary btn-sm w-100">
                        <i class="bi bi-book me-1"></i> Ir al glosario
                    </a>
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Validar Contenido
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 5</span>
                    </div>
                    <p class="text-muted small mb-0">Disponible en Nivel 5: Validador de Contenido.</p>
                </div>
            }
        </div>

        @* ── Sección 6: Crear Contenido (Nivel ≥ 6) ── *@
        <div class="col-md-6 col-lg-4">
            @if (Model.NivelActual >= 6)
            {
                <div class="dash-card">
                    <div class="dash-card-title"><i class="bi bi-star-fill" style="color:#7c3aed;"></i> Crear Contenido</div>
                    <p class="text-muted small mb-3">Propón artículos y contenido educativo sobre EII.</p>
                    <form method="post" asp-page-handler="SolicitarContenido">
                        @Html.AntiForgeryToken()
                        <div class="form-floating mb-2">
                            <input type="text" name="TituloSolicitud" class="form-control form-control-sm"
                                   placeholder="Título del artículo" id="titulo-solicitud" required />
                            <label for="titulo-solicitud">Título del artículo propuesto</label>
                        </div>
                        <button type="submit" class="btn btn-primary btn-sm w-100">
                            <i class="bi bi-send me-1"></i> Enviar solicitud
                        </button>
                    </form>
                </div>
            }
            else
            {
                <div class="dash-card dash-card-locked">
                    <div class="dash-card-title">
                        <i class="bi bi-lock text-secondary"></i> Crear Contenido
                        <span class="dash-lock-badge ms-auto"><i class="bi bi-lock-fill"></i> Nivel 6</span>
                    </div>
                    <p class="text-muted small mb-0">Disponible en Nivel 6: Creador de Contenido.</p>
                </div>
            }
        </div>

    </div>
</div>
```

- [ ] **Step 3.3: Agregar handler OnPostSolicitarContenido al PageModel**

En `Dashboard.cshtml.cs`, agregar dentro de `DashboardModel`:

```csharp
public async Task<IActionResult> OnPostSolicitarContenidoAsync(string tituloSolicitud)
{
    if (string.IsNullOrWhiteSpace(tituloSolicitud))
    {
        TempData["Error"] = "El título de la solicitud es requerido.";
        return RedirectToPage();
    }

    var userId = GetUserId();
    _logger.LogInformation("Médico {UserId} solicita crear contenido: {Titulo}", userId, tituloSolicitud);

    // TODO-FUTURO: guardar en tabla ContentSolicitud cuando exista
    TempData["Success"] = "Solicitud enviada. El equipo EIIBD la revisará pronto.";
    return RedirectToPage();
}
```

Agregar `ILogger<DashboardModel>` al constructor:
```csharp
private readonly ILogger<DashboardModel> _logger;

public DashboardModel(ApplicationDbContext db, IMedicoBadgeService badgeService, ILogger<DashboardModel> logger)
{
    _db = db;
    _badgeService = badgeService;
    _logger = logger;
}
```

### Redirect post-login en Program.cs

- [ ] **Step 3.4: Agregar middleware de redirect para médicos en Program.cs**

En `Program.cs`, después de `app.UseAuthentication();` y `app.UseAuthorization();` (pero ANTES de `app.MapRazorPages()`), agregar:

```csharp
// Redirigir médicos a su dashboard cuando aterrizan en "/"
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        && context.User.IsInRole("Medico")
        && context.Request.Path == "/")
    {
        context.Response.Redirect("/Identity/Medico/Dashboard");
        return;
    }
    await next();
});
```

- [ ] **Step 3.5: Asegurar que la ruta del dashboard no requiere login para el redirect**

En Program.cs, busca el bloque de `AddRazorPages()` y las convenciones de páginas que permiten acceso anónimo. Agrega (junto a los existentes `AllowAnonymousToAreaPage`):

```csharp
options.Conventions.AllowAnonymousToAreaPage("Identity", "/Medico/Dashboard");
```

Luego asegúrate que `[Authorize(Roles = "Medico")]` en el PageModel manejará el acceso no autorizado correctamente (redirige al login con ReturnUrl).

- [ ] **Step 3.6: Verificar compilación final**

```powershell
cd "D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26"
dotnet build --no-restore 2>&1 | Where-Object { $_ -match ": error (CS|RZ)" }
```

Esperado: 0 errores.

---

## Notas de Implementación

### Orden de ejecución
Task 1 → Task 2 → Task 3. Task 2 depende del DbSet creado en Task 1. Task 3 es independiente una vez compilado Task 1.

### perfil.Id vs perfil.MedicoId en Task 2
`MedicoAreaEii.MedicoPerfilId` apunta a `MedicoPerfilExtendido.Id` (el PK del perfil extendido). En el OnGetAsync de Task 2, `perfil` es el registro de `MedicosPerfilExtendido`; usa `perfil.Id` (no `perfil.MedicoId`). En el OnPostAsync, el `perfil` podría ser recién insertado — asegúrate que el `SaveChangesAsync()` del perfil va ANTES del bloque de áreas para que `perfil.Id` sea válido.

### Redirect middleware vs. filtro
El middleware de redirect en Step 3.4 intercepta peticiones GET a `/` para médicos autenticados. Esto funciona porque Login redirige a `ReturnUrl ?? "~/"` — los médicos caen en `"/"`, el middleware los redirige al Dashboard. No afecta pacientes ni admin.

### Link "Responder" en Pacientes
El botón "Responder" (Step 3.2, Sección 3) usa `asp-route-usuario="@rec.UsuarioId"` apuntando a `/Preguntas/Index`. Verifica que esa página tenga un `BindProperty(SupportsGet=true)` para `usuario` o usa la ruta de preguntas que sea correcta en el proyecto. Si no existe ese filtro, cambia el href a solo `asp-page="/Preguntas/Index"`.

### AllowAnonymousToAreaPage
El método `AllowAnonymousToAreaPage` sobrescribe el `[Authorize]` en la clase. Como el PageModel tiene `[Authorize(Roles="Medico")]`, removerlo de Step 3.5 es opcional — el `[Authorize]` maneja el acceso directamente. Solo necesitas garantizar que el área `Identity/Medico` esté mapeada correctamente por Razor Pages.
