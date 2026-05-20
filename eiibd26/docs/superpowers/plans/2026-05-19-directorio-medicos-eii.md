# Directorio Comunitario de Médicos EII — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar la primera fase de un directorio comunitario de médicos con experiencia reportada en EII, separado de AspNetUsers, con confirmaciones anónimas agregadas, niveles de confianza y ubicación lista para mapa.

**Architecture:** Se crean 5 entidades nuevas (`MedicoDirectorio`, `AreaExperienciaEii`, `MedicoExperienciaEii`, `TipoConfirmacion`, `ConfirmacionComunitaria`) bajo `Models/Directorio/`. El servicio `MedicoDirectorioService` encapsula la lógica; el controlador `DirectorioMedicosController` expone vistas públicas (listado, ficha) y una acción de propuesta de médico. Las confirmaciones se almacenan ligadas al paciente autenticado pero se exponen solo de forma agregada y anónima.

**Tech Stack:** ASP.NET Core MVC (.NET 8), EF Core 8, SQL Server, Bootstrap 5, Bootstrap Icons, HTML/CSS/JS vanilla.

---

## Convenciones del proyecto confirmadas por análisis

- **PK**: `int` con Identity (patrón de módulos nuevos como Glossary, FrecuenciaSintomaCatalog)
- **Timestamps**: `DateTimeOffset` para entidades nuevas (`FechaCreacion`, `FechaModificacion?`)
- **Soft-delete**: `bool Eliminado = false` con `HasQueryFilter(x => !x.Eliminado)` en OnModelCreating
- **FK a ApplicationUser**: tipo `Guid` (ApplicationUser hereda de `IdentityUser<Guid>`)
- **ForeignKey attribute**: `[ForeignKey(nameof(PropId))]` sobre la navigation property
- **Naming modelos**: PascalCase para módulos nuevos
- **Table names**: PascalCase en OnModelCreating (`b.ToTable("MedicosDirectorio")`)
- **Índices únicos**: composite para prevenir duplicados (ej. un paciente confirma un tipo una sola vez por médico)
- **Catálogos**: seeded en `OnModelCreating` con `b.HasData(...)`
- **Servicio**: interface `IMedicoDirectorioService` + `MedicoDirectorioService`
- **Views**: Razor Pages en `/Pages/` (el proyecto usa Pages para contenido público, Controllers para API y admin)
- **Mapa**: patrón de carga dinámica via fetch + div contenedor
- **Color brand**: `--brand-color: #7c3aed` (violeta), variables CSS custom, Bootstrap 5

---

## Mapa de archivos

### Crear — Modelos
| Archivo | Responsabilidad |
|---------|----------------|
| `Models/Directorio/Enums/EstatusValidacionCedula.cs` | Enum para estatus de validación de cédula profesional |
| `Models/Directorio/Enums/EstatusReclamacion.cs` | Enum para estatus de reclamo del perfil por el médico |
| `Models/Directorio/Enums/NivelConfianzaEnum.cs` | Enum 0-3 para nivel de confianza comunitaria |
| `Models/Directorio/AreaExperienciaEii.cs` | Catálogo de áreas/focos de atención EII (seeded) |
| `Models/Directorio/TipoConfirmacion.cs` | Catálogo de tipos de confirmación comunitaria (seeded) |
| `Models/Directorio/MedicoDirectorio.cs` | Entidad principal del directorio de médicos |
| `Models/Directorio/MedicoExperienciaEii.cs` | Tabla puente MedicoDirectorio ↔ AreaExperienciaEii |
| `Models/Directorio/ConfirmacionComunitaria.cs` | Confirmación de un paciente sobre un médico |
| `Models/Directorio/DirectorioViewModels.cs` | ViewModels para listado, ficha, propuesta y confirmar |

### Crear — Servicios
| Archivo | Responsabilidad |
|---------|----------------|
| `Services/Directorio/IMedicoDirectorioService.cs` | Interface del servicio principal |
| `Services/Directorio/MedicoDirectorioService.cs` | Implementación: listado, ficha, proponer, confirmar, recalcular nivel |

### Crear — Controller + Views
| Archivo | Responsabilidad |
|---------|----------------|
| `Controllers/DirectorioMedicosController.cs` | Acciones MVC: Index, Detalle, Proponer, Confirmar |
| `Pages/DirectorioMedicos/Index.cshtml` + `.cs` | Listado público con búsqueda/filtros |
| `Pages/DirectorioMedicos/Detalle.cshtml` + `.cs` | Ficha pública del médico |
| `Pages/DirectorioMedicos/Proponer.cshtml` + `.cs` | Formulario para que un paciente proponga un médico |
| `Views/Shared/_MedicoCard.cshtml` | Partial reutilizable para card de médico |

### Modificar
| Archivo | Cambio |
|---------|--------|
| `Data/ApplicationDbContext.cs` | Agregar 5 DbSets + configuración OnModelCreating |
| `Program.cs` | Registrar `IMedicoDirectorioService` → `MedicoDirectorioService` |

---

## Task 1: Enums del módulo Directorio

**Files:**
- Create: `Models/Directorio/Enums/EstatusValidacionCedula.cs`
- Create: `Models/Directorio/Enums/EstatusReclamacion.cs`
- Create: `Models/Directorio/Enums/NivelConfianzaEnum.cs`

- [ ] **Step 1.1: Crear EstatusValidacionCedula**

```csharp
// Models/Directorio/Enums/EstatusValidacionCedula.cs
namespace eiibd26.Models.Directorio.Enums;

public enum EstatusValidacionCedula
{
    PendienteValidacion = 0,
    Validado = 1,
    NoVerificado = 2,
    Rechazado = 3
}
```

- [ ] **Step 1.2: Crear EstatusReclamacion**

```csharp
// Models/Directorio/Enums/EstatusReclamacion.cs
namespace eiibd26.Models.Directorio.Enums;

public enum EstatusReclamacion
{
    NoReclamado = 0,
    EnProceso = 1,
    Reclamado = 2,
    Rechazado = 3
}
```

- [ ] **Step 1.3: Crear NivelConfianzaEnum**

```csharp
// Models/Directorio/Enums/NivelConfianzaEnum.cs
namespace eiibd26.Models.Directorio.Enums;

public enum NivelConfianzaEnum
{
    Identificado = 0,   // 1 paciente lo registró
    Confirmado = 1,     // 3 pacientes distintos lo confirmaron
    Reconocido = 2,     // 5 pacientes con experiencia EII reportada
    Establecido = 3     // 10+ pacientes con actividad sostenida
}
```

- [ ] **Step 1.4: Commit**

```
git add Models/Directorio/Enums/
git commit -m "feat(directorio): agregar enums EstatusValidacionCedula, EstatusReclamacion, NivelConfianza"
```

---

## Task 2: Catálogos — AreaExperienciaEii y TipoConfirmacion

**Files:**
- Create: `Models/Directorio/AreaExperienciaEii.cs`
- Create: `Models/Directorio/TipoConfirmacion.cs`

- [ ] **Step 2.1: Crear AreaExperienciaEii**

```csharp
// Models/Directorio/AreaExperienciaEii.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("AreaExperienciaEii")]
public class AreaExperienciaEii
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<MedicoExperienciaEii> MedicosExperiencia { get; set; } = new List<MedicoExperienciaEii>();
}
```

- [ ] **Step 2.2: Crear TipoConfirmacion**

```csharp
// Models/Directorio/TipoConfirmacion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("TipoConfirmacion")]
public class TipoConfirmacion
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<ConfirmacionComunitaria> Confirmaciones { get; set; } = new List<ConfirmacionComunitaria>();
}
```

- [ ] **Step 2.3: Commit**

```
git add Models/Directorio/AreaExperienciaEii.cs Models/Directorio/TipoConfirmacion.cs
git commit -m "feat(directorio): agregar catálogos AreaExperienciaEii y TipoConfirmacion"
```

---

## Task 3: Entidades principales — MedicoDirectorio, MedicoExperienciaEii, ConfirmacionComunitaria

**Files:**
- Create: `Models/Directorio/MedicoDirectorio.cs`
- Create: `Models/Directorio/MedicoExperienciaEii.cs`
- Create: `Models/Directorio/ConfirmacionComunitaria.cs`

- [ ] **Step 3.1: Crear MedicoDirectorio**

```csharp
// Models/Directorio/MedicoDirectorio.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Models.Directorio;

[Table("MedicosDirectorio")]
public class MedicoDirectorio
{
    [Key]
    public int Id { get; set; }

    // Datos profesionales
    [Required, MaxLength(300)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Cédula profesional")]
    public string? CedulaProfesional { get; set; }

    [MaxLength(200)]
    [Display(Name = "Especialidad")]
    public string? Especialidad { get; set; }

    [MaxLength(200)]
    [Display(Name = "Subespecialidad")]
    public string? Subespecialidad { get; set; }

    // Ubicación
    [MaxLength(100)]
    [Display(Name = "Estado")]
    public string? Estado { get; set; }

    [MaxLength(100)]
    [Display(Name = "Ciudad")]
    public string? Ciudad { get; set; }

    [MaxLength(100)]
    [Display(Name = "Municipio / Alcaldía")]
    public string? MunicipioAlcaldia { get; set; }

    [MaxLength(300)]
    [Display(Name = "Hospital o Clínica")]
    public string? HospitalClinica { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    [Display(Name = "Latitud")]
    public decimal? Latitud { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    [Display(Name = "Longitud")]
    public decimal? Longitud { get; set; }

    // Validación y confianza
    [Display(Name = "Estatus de validación de cédula")]
    public EstatusValidacionCedula EstatusValidacion { get; set; } = EstatusValidacionCedula.PendienteValidacion;

    [Display(Name = "Nivel de confianza comunitaria")]
    public NivelConfianzaEnum NivelConfianza { get; set; } = NivelConfianzaEnum.Identificado;

    // Reclamo futuro del perfil por el médico (nullable — no obligatorio en fase 1)
    [Display(Name = "Estatus de reclamación")]
    public EstatusReclamacion EstatusReclamacion { get; set; } = EstatusReclamacion.NoReclamado;

    public Guid? AspNetUserId { get; set; }  // nullable — para vinculación futura

    public DateTimeOffset? FechaReclamacion { get; set; }

    // Visibilidad y estado
    [Display(Name = "Visible públicamente")]
    public bool VisiblePublicamente { get; set; } = true;

    public bool Activo { get; set; } = true;

    // Auditoría
    public bool Eliminado { get; set; } = false;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FechaModificacion { get; set; }

    // FK al paciente que propuso este perfil
    public Guid? PropuestoPorUsuarioId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AspNetUserId))]
    public virtual ApplicationUser? UsuarioVinculado { get; set; }

    public virtual ICollection<MedicoExperienciaEii> AreasExperiencia { get; set; } = new List<MedicoExperienciaEii>();

    public virtual ICollection<ConfirmacionComunitaria> Confirmaciones { get; set; } = new List<ConfirmacionComunitaria>();
}
```

- [ ] **Step 3.2: Crear MedicoExperienciaEii (tabla puente)**

```csharp
// Models/Directorio/MedicoExperienciaEii.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("MedicoExperienciaEii")]
public class MedicoExperienciaEii
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required]
    public int AreaExperienciaEiiId { get; set; }

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public bool Eliminado { get; set; } = false;

    [ForeignKey(nameof(MedicoDirectorioId))]
    public virtual MedicoDirectorio MedicoDirectorio { get; set; } = null!;

    [ForeignKey(nameof(AreaExperienciaEiiId))]
    public virtual AreaExperienciaEii AreaExperienciaEii { get; set; } = null!;
}
```

- [ ] **Step 3.3: Crear ConfirmacionComunitaria**

```csharp
// Models/Directorio/ConfirmacionComunitaria.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("ConfirmacionComunitaria")]
public class ConfirmacionComunitaria
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }  // paciente autenticado — anónimo en UI pública

    [Required]
    public int TipoConfirmacionId { get; set; }

    public bool Eliminado { get; set; } = false;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(MedicoDirectorioId))]
    public virtual MedicoDirectorio MedicoDirectorio { get; set; } = null!;

    [ForeignKey(nameof(UsuarioId))]
    public virtual ApplicationUser Usuario { get; set; } = null!;

    [ForeignKey(nameof(TipoConfirmacionId))]
    public virtual TipoConfirmacion TipoConfirmacion { get; set; } = null!;
}
```

- [ ] **Step 3.4: Commit**

```
git add Models/Directorio/
git commit -m "feat(directorio): agregar entidades MedicoDirectorio, MedicoExperienciaEii, ConfirmacionComunitaria"
```

---

## Task 4: DbContext — DbSets + OnModelCreating + Seeds

**Files:**
- Modify: `Data/ApplicationDbContext.cs`

- [ ] **Step 4.1: Agregar DbSets al final de los DbSets existentes**

Localizar en `ApplicationDbContext.cs` el último DbSet registrado y agregar después:

```csharp
// Directorio comunitario de médicos EII
public DbSet<eiibd26.Models.Directorio.MedicoDirectorio> MedicosDirectorio { get; set; }
public DbSet<eiibd26.Models.Directorio.AreaExperienciaEii> AreasExperienciaEii { get; set; }
public DbSet<eiibd26.Models.Directorio.MedicoExperienciaEii> MedicoExperienciaEii { get; set; }
public DbSet<eiibd26.Models.Directorio.TipoConfirmacion> TiposConfirmacion { get; set; }
public DbSet<eiibd26.Models.Directorio.ConfirmacionComunitaria> ConfirmacionesComunitarias { get; set; }
```

- [ ] **Step 4.2: Agregar configuración OnModelCreating al final del método (antes del cierre)**

Localizar el cierre de `OnModelCreating` en `ApplicationDbContext.cs` y agregar antes del último `}`:

```csharp
// ── DIRECTORIO MÉDICOS EII ──────────────────────────────────────────────

builder.Entity<eiibd26.Models.Directorio.AreaExperienciaEii>(b =>
{
    b.ToTable("AreaExperienciaEii");
    b.HasKey(a => a.Id);
    b.Property(a => a.Nombre).HasMaxLength(100).IsRequired();
    b.Property(a => a.Descripcion).HasMaxLength(300);
    b.HasData(
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 1,  Nombre = "CUCI",                    Orden = 1,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 2,  Nombre = "Crohn",                   Orden = 2,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 3,  Nombre = "Pediátrico",              Orden = 3,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 4,  Nombre = "Ostomías",                Orden = 4,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 5,  Nombre = "Biológicos",              Orden = 5,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 6,  Nombre = "Embarazo + EII",          Orden = 6,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 7,  Nombre = "Manejo de brotes",        Orden = 7,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 8,  Nombre = "Segunda opinión",         Orden = 8,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 9,  Nombre = "Cirugía",                 Orden = 9,  Activo = true },
        new eiibd26.Models.Directorio.AreaExperienciaEii { Id = 10, Nombre = "Seguimiento prolongado",  Orden = 10, Activo = true }
    );
});

builder.Entity<eiibd26.Models.Directorio.TipoConfirmacion>(b =>
{
    b.ToTable("TipoConfirmacion");
    b.HasKey(t => t.Id);
    b.Property(t => t.Nombre).HasMaxLength(100).IsRequired();
    b.Property(t => t.Descripcion).HasMaxLength(300);
    b.HasData(
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 1, Nombre = "Me diagnosticó",                          Orden = 1, Activo = true },
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 2, Nombre = "Me ayudó con tratamiento biológico",      Orden = 2, Activo = true },
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 3, Nombre = "Manejo de brotes",                        Orden = 3, Activo = true },
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 4, Nombre = "Segunda opinión",                         Orden = 4, Activo = true },
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 5, Nombre = "Cirugía",                                 Orden = 5, Activo = true },
        new eiibd26.Models.Directorio.TipoConfirmacion { Id = 6, Nombre = "Seguimiento prolongado",                  Orden = 6, Activo = true }
    );
});

builder.Entity<eiibd26.Models.Directorio.MedicoDirectorio>(b =>
{
    b.ToTable("MedicosDirectorio");
    b.HasKey(m => m.Id);
    b.Property(m => m.NombreCompleto).HasMaxLength(300).IsRequired();
    b.Property(m => m.CedulaProfesional).HasMaxLength(20);
    b.Property(m => m.Especialidad).HasMaxLength(200);
    b.Property(m => m.Subespecialidad).HasMaxLength(200);
    b.Property(m => m.Estado).HasMaxLength(100);
    b.Property(m => m.Ciudad).HasMaxLength(100);
    b.Property(m => m.MunicipioAlcaldia).HasMaxLength(100);
    b.Property(m => m.HospitalClinica).HasMaxLength(300);
    b.Property(m => m.Latitud).HasColumnType("decimal(9,6)");
    b.Property(m => m.Longitud).HasColumnType("decimal(9,6)");
    b.Property(m => m.EstatusValidacion).HasConversion<int>();
    b.Property(m => m.NivelConfianza).HasConversion<int>();
    b.Property(m => m.EstatusReclamacion).HasConversion<int>();
    b.HasQueryFilter(m => !m.Eliminado);
    b.HasIndex(m => m.CedulaProfesional);
    b.HasIndex(m => new { m.Estado, m.Ciudad });
    b.HasOne(m => m.UsuarioVinculado)
        .WithMany()
        .HasForeignKey(m => m.AspNetUserId)
        .OnDelete(DeleteBehavior.SetNull);
});

builder.Entity<eiibd26.Models.Directorio.MedicoExperienciaEii>(b =>
{
    b.ToTable("MedicoExperienciaEii");
    b.HasKey(me => me.Id);
    b.HasIndex(me => new { me.MedicoDirectorioId, me.AreaExperienciaEiiId })
        .IsUnique()
        .HasFilter("[Eliminado] = 0");
    b.HasQueryFilter(me => !me.Eliminado);
    b.HasOne(me => me.MedicoDirectorio)
        .WithMany(m => m.AreasExperiencia)
        .HasForeignKey(me => me.MedicoDirectorioId)
        .OnDelete(DeleteBehavior.Cascade);
    b.HasOne(me => me.AreaExperienciaEii)
        .WithMany(a => a.MedicosExperiencia)
        .HasForeignKey(me => me.AreaExperienciaEiiId)
        .OnDelete(DeleteBehavior.Restrict);
});

builder.Entity<eiibd26.Models.Directorio.ConfirmacionComunitaria>(b =>
{
    b.ToTable("ConfirmacionComunitaria");
    b.HasKey(c => c.Id);
    // Un paciente solo puede confirmar cada tipo una vez por médico
    b.HasIndex(c => new { c.MedicoDirectorioId, c.UsuarioId, c.TipoConfirmacionId })
        .IsUnique()
        .HasFilter("[Eliminado] = 0");
    b.HasQueryFilter(c => !c.Eliminado);
    b.HasOne(c => c.MedicoDirectorio)
        .WithMany(m => m.Confirmaciones)
        .HasForeignKey(c => c.MedicoDirectorioId)
        .OnDelete(DeleteBehavior.Cascade);
    b.HasOne(c => c.TipoConfirmacion)
        .WithMany(t => t.Confirmaciones)
        .HasForeignKey(c => c.TipoConfirmacionId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasOne(c => c.Usuario)
        .WithMany()
        .HasForeignKey(c => c.UsuarioId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 4.3: Verificar que el proyecto compila**

```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet build
```

Esperado: Build succeeded, 0 errors.

- [ ] **Step 4.4: Commit**

```
git add Data/ApplicationDbContext.cs
git commit -m "feat(directorio): registrar entidades directorio médicos en DbContext con seeds y QueryFilters"
```

---

## Task 5: Migración EF Core

**Files:**
- Auto-generated: `Migrations/[timestamp]_AddDirectorioMedicos.cs`

- [ ] **Step 5.1: Crear la migración**

```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet ef migrations add AddDirectorioMedicos
```

Esperado: migración generada en `Migrations/`. Verificar que incluye las 5 tablas nuevas y los datos seed.

- [ ] **Step 5.2: Aplicar la migración**

```powershell
dotnet ef database update
```

Esperado: `Done.` sin errores.

- [ ] **Step 5.3: Verificar en SQL Server que existen las tablas**

```powershell
# Verificar que dotnet ef vea el schema actualizado
dotnet ef dbcontext info
```

- [ ] **Step 5.4: Commit**

```
git add Migrations/
git commit -m "feat(directorio): migración AddDirectorioMedicos — 5 tablas + seeds catálogos"
```

---

## Task 6: ViewModels del módulo Directorio

**Files:**
- Create: `Models/Directorio/DirectorioViewModels.cs`

- [ ] **Step 6.1: Crear el archivo de ViewModels**

```csharp
// Models/Directorio/DirectorioViewModels.cs
using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Models.Directorio;

// ── LISTADO ──────────────────────────────────────────────────────────────

public class DirectorioIndexVm
{
    public List<MedicoCardVm> Medicos { get; set; } = new();
    public string? FiltroBusqueda { get; set; }
    public string? FiltroEstado { get; set; }
    public string? FiltroEspecialidad { get; set; }
    public int? FiltroAreaId { get; set; }
    public List<AreaExperienciaEii> AreasDisponibles { get; set; } = new();
    public List<string> EstadosDisponibles { get; set; } = new();
    public int TotalResultados { get; set; }
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; }
}

public class MedicoCardVm
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
    public string? Subespecialidad { get; set; }
    public string? Estado { get; set; }
    public string? Ciudad { get; set; }
    public string? HospitalClinica { get; set; }
    public NivelConfianzaEnum NivelConfianza { get; set; }
    public string NivelConfianzaLabel => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "Identificado por pacientes",
        NivelConfianzaEnum.Confirmado   => "Confirmado por la comunidad",
        NivelConfianzaEnum.Reconocido   => "Reconocido en EII",
        NivelConfianzaEnum.Establecido  => "Experiencia establecida en EII",
        _                               => "Identificado por pacientes"
    };
    public string NivelConfianzaBadgeClass => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "badge-nivel-0",
        NivelConfianzaEnum.Confirmado   => "badge-nivel-1",
        NivelConfianzaEnum.Reconocido   => "badge-nivel-2",
        NivelConfianzaEnum.Establecido  => "badge-nivel-3",
        _                               => "badge-nivel-0"
    };
    public EstatusValidacionCedula EstatusValidacion { get; set; }
    public bool CedulaValidada => EstatusValidacion == EstatusValidacionCedula.Validado;
    public int TotalConfirmaciones { get; set; }
    public int TotalPacientesUnicos { get; set; }
    public List<string> AreasExperiencia { get; set; } = new();
}

// ── FICHA DETALLE ────────────────────────────────────────────────────────

public class MedicoDetalleVm
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? CedulaProfesional { get; set; }
    public string? Especialidad { get; set; }
    public string? Subespecialidad { get; set; }
    public string? Estado { get; set; }
    public string? Ciudad { get; set; }
    public string? MunicipioAlcaldia { get; set; }
    public string? HospitalClinica { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public NivelConfianzaEnum NivelConfianza { get; set; }
    public string NivelConfianzaLabel => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "Identificado por pacientes con EII",
        NivelConfianzaEnum.Confirmado   => "Confirmado por la comunidad EII",
        NivelConfianzaEnum.Reconocido   => "Reconocido con experiencia en EII",
        NivelConfianzaEnum.Establecido  => "Experiencia establecida y sostenida en EII",
        _                               => "Identificado por pacientes con EII"
    };
    public EstatusValidacionCedula EstatusValidacion { get; set; }
    public EstatusReclamacion EstatusReclamacion { get; set; }
    public bool PerfilReclamable => EstatusReclamacion == EstatusReclamacion.NoReclamado;
    public int TotalConfirmaciones { get; set; }
    public int TotalPacientesUnicos { get; set; }
    public List<AreaExperienciaVm> AreasExperiencia { get; set; } = new();
    public List<ConfirmacionAgregadaVm> ConfirmacionesAgregadas { get; set; } = new();
    public bool UsuarioYaConfirmo { get; set; }
    public List<int> TiposConfirmadosPorUsuario { get; set; } = new();
}

public class AreaExperienciaVm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ConfirmacionAgregadaVm
{
    public int TipoConfirmacionId { get; set; }
    public string NombreTipo { get; set; } = string.Empty;
    public int Total { get; set; }
}

// ── FORMULARIO PROPONER MÉDICO ───────────────────────────────────────────

public class ProponerMedicoVm
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(300)]
    [Display(Name = "Nombre completo del médico")]
    public string NombreCompleto { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Cédula profesional (si la conoces)")]
    public string? CedulaProfesional { get; set; }

    [MaxLength(200)]
    [Display(Name = "Especialidad")]
    public string? Especialidad { get; set; }

    [MaxLength(200)]
    [Display(Name = "Subespecialidad")]
    public string? Subespecialidad { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio")]
    [MaxLength(100)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Ciudad")]
    public string? Ciudad { get; set; }

    [MaxLength(100)]
    [Display(Name = "Municipio / Alcaldía")]
    public string? MunicipioAlcaldia { get; set; }

    [MaxLength(300)]
    [Display(Name = "Hospital o Clínica")]
    public string? HospitalClinica { get; set; }

    [Display(Name = "Áreas de experiencia EII que reportas")]
    public List<int> AreasSeleccionadas { get; set; } = new();

    // Datos para poblar los checkboxes de áreas
    public List<AreaExperienciaEii> AreasDisponibles { get; set; } = new();
}

// ── CONFIRMAR ATENCIÓN ───────────────────────────────────────────────────

public class ConfirmarAtencionVm
{
    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required(ErrorMessage = "Selecciona al menos un tipo de atención")]
    [Display(Name = "Tipo de atención recibida")]
    public int TipoConfirmacionId { get; set; }

    // Para poblar el select en la vista
    public List<TipoConfirmacion> TiposDisponibles { get; set; } = new();
    public string NombreMedico { get; set; } = string.Empty;
}
```

- [ ] **Step 6.2: Commit**

```
git add Models/Directorio/DirectorioViewModels.cs
git commit -m "feat(directorio): agregar ViewModels del módulo directorio médicos EII"
```

---

## Task 7: Interface + Servicio IMedicoDirectorioService

**Files:**
- Create: `Services/Directorio/IMedicoDirectorioService.cs`
- Create: `Services/Directorio/MedicoDirectorioService.cs`

- [ ] **Step 7.1: Crear IMedicoDirectorioService**

```csharp
// Services/Directorio/IMedicoDirectorioService.cs
using eiibd26.Models.Directorio;

namespace eiibd26.Services.Directorio;

public interface IMedicoDirectorioService
{
    Task<DirectorioIndexVm> GetListadoAsync(
        string? busqueda,
        string? estado,
        string? especialidad,
        int? areaId,
        int pagina = 1,
        int porPagina = 18);

    Task<MedicoDetalleVm?> GetDetalleAsync(int medicoId, Guid? usuarioId);

    Task<ProponerMedicoVm> GetProponerVmAsync();

    Task<int> ProponerMedicoAsync(ProponerMedicoVm vm, Guid usuarioId);

    Task<bool> ConfirmarAtencionAsync(int medicoId, int tipoConfirmacionId, Guid usuarioId);

    Task RecalcularNivelConfianzaAsync(int medicoId);
}
```

- [ ] **Step 7.2: Crear MedicoDirectorioService**

```csharp
// Services/Directorio/MedicoDirectorioService.cs
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Services.Directorio;

public class MedicoDirectorioService : IMedicoDirectorioService
{
    private readonly ApplicationDbContext _db;

    public MedicoDirectorioService(ApplicationDbContext db)
        => _db = db;

    public async Task<DirectorioIndexVm> GetListadoAsync(
        string? busqueda,
        string? estado,
        string? especialidad,
        int? areaId,
        int pagina = 1,
        int porPagina = 18)
    {
        var query = _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.VisiblePublicamente && m.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(m =>
                m.NombreCompleto.Contains(busqueda) ||
                (m.Especialidad != null && m.Especialidad.Contains(busqueda)) ||
                (m.HospitalClinica != null && m.HospitalClinica.Contains(busqueda)));

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(m => m.Estado == estado);

        if (!string.IsNullOrWhiteSpace(especialidad))
            query = query.Where(m => m.Especialidad == especialidad);

        if (areaId.HasValue)
            query = query.Where(m =>
                m.AreasExperiencia.Any(ae => ae.AreaExperienciaEiiId == areaId.Value));

        var total = await query.CountAsync();

        var medicos = await query
            .OrderByDescending(m => (int)m.NivelConfianza)
            .ThenBy(m => m.NombreCompleto)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(m => new MedicoCardVm
            {
                Id                    = m.Id,
                NombreCompleto        = m.NombreCompleto,
                Especialidad          = m.Especialidad,
                Subespecialidad       = m.Subespecialidad,
                Estado                = m.Estado,
                Ciudad                = m.Ciudad,
                HospitalClinica       = m.HospitalClinica,
                NivelConfianza        = m.NivelConfianza,
                EstatusValidacion     = m.EstatusValidacion,
                TotalConfirmaciones   = m.Confirmaciones.Count(),
                TotalPacientesUnicos  = m.Confirmaciones.Select(c => c.UsuarioId).Distinct().Count(),
                AreasExperiencia      = m.AreasExperiencia
                    .Select(ae => ae.AreaExperienciaEii.Nombre)
                    .ToList()
            })
            .ToListAsync();

        var estados = await _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.VisiblePublicamente && m.Activo && m.Estado != null)
            .Select(m => m.Estado!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        var areas = await _db.AreasExperienciaEii
            .AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();

        return new DirectorioIndexVm
        {
            Medicos              = medicos,
            FiltroBusqueda       = busqueda,
            FiltroEstado         = estado,
            FiltroEspecialidad   = especialidad,
            FiltroAreaId         = areaId,
            AreasDisponibles     = areas,
            EstadosDisponibles   = estados,
            TotalResultados      = total,
            PaginaActual         = pagina,
            TotalPaginas         = (int)Math.Ceiling((double)total / porPagina)
        };
    }

    public async Task<MedicoDetalleVm?> GetDetalleAsync(int medicoId, Guid? usuarioId)
    {
        var medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.Id == medicoId && m.VisiblePublicamente && m.Activo)
            .Select(m => new MedicoDetalleVm
            {
                Id                   = m.Id,
                NombreCompleto       = m.NombreCompleto,
                CedulaProfesional    = m.CedulaProfesional,
                Especialidad         = m.Especialidad,
                Subespecialidad      = m.Subespecialidad,
                Estado               = m.Estado,
                Ciudad               = m.Ciudad,
                MunicipioAlcaldia    = m.MunicipioAlcaldia,
                HospitalClinica      = m.HospitalClinica,
                Latitud              = m.Latitud,
                Longitud             = m.Longitud,
                NivelConfianza       = m.NivelConfianza,
                EstatusValidacion    = m.EstatusValidacion,
                EstatusReclamacion   = m.EstatusReclamacion,
                TotalConfirmaciones  = m.Confirmaciones.Count(),
                TotalPacientesUnicos = m.Confirmaciones.Select(c => c.UsuarioId).Distinct().Count(),
                AreasExperiencia     = m.AreasExperiencia
                    .Select(ae => new AreaExperienciaVm
                    {
                        Id     = ae.AreaExperienciaEiiId,
                        Nombre = ae.AreaExperienciaEii.Nombre
                    }).ToList(),
                ConfirmacionesAgregadas = m.Confirmaciones
                    .GroupBy(c => c.TipoConfirmacionId)
                    .Select(g => new ConfirmacionAgregadaVm
                    {
                        TipoConfirmacionId = g.Key,
                        NombreTipo         = g.First().TipoConfirmacion.Nombre,
                        Total              = g.Count()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (medico is null) return null;

        if (usuarioId.HasValue)
        {
            var tiposConfirmados = await _db.ConfirmacionesComunitarias
                .AsNoTracking()
                .Where(c => c.MedicoDirectorioId == medicoId && c.UsuarioId == usuarioId.Value)
                .Select(c => c.TipoConfirmacionId)
                .ToListAsync();

            medico.UsuarioYaConfirmo = tiposConfirmados.Any();
            medico.TiposConfirmadosPorUsuario = tiposConfirmados;
        }

        return medico;
    }

    public async Task<ProponerMedicoVm> GetProponerVmAsync()
    {
        var areas = await _db.AreasExperienciaEii
            .AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();

        return new ProponerMedicoVm { AreasDisponibles = areas };
    }

    public async Task<int> ProponerMedicoAsync(ProponerMedicoVm vm, Guid usuarioId)
    {
        var medico = new MedicoDirectorio
        {
            NombreCompleto      = vm.NombreCompleto.Trim(),
            CedulaProfesional   = vm.CedulaProfesional?.Trim(),
            Especialidad        = vm.Especialidad?.Trim(),
            Subespecialidad     = vm.Subespecialidad?.Trim(),
            Estado              = vm.Estado.Trim(),
            Ciudad              = vm.Ciudad?.Trim(),
            MunicipioAlcaldia   = vm.MunicipioAlcaldia?.Trim(),
            HospitalClinica     = vm.HospitalClinica?.Trim(),
            EstatusValidacion   = EstatusValidacionCedula.PendienteValidacion,
            NivelConfianza      = NivelConfianzaEnum.Identificado,
            EstatusReclamacion  = EstatusReclamacion.NoReclamado,
            VisiblePublicamente = true,
            Activo              = true,
            PropuestoPorUsuarioId = usuarioId,
            FechaCreacion       = DateTimeOffset.UtcNow
        };

        _db.MedicosDirectorio.Add(medico);
        await _db.SaveChangesAsync();

        if (vm.AreasSeleccionadas.Any())
        {
            var areas = vm.AreasSeleccionadas
                .Select(areaId => new MedicoExperienciaEii
                {
                    MedicoDirectorioId  = medico.Id,
                    AreaExperienciaEiiId = areaId,
                    FechaCreacion       = DateTimeOffset.UtcNow
                });
            _db.MedicoExperienciaEii.AddRange(areas);
            await _db.SaveChangesAsync();
        }

        return medico.Id;
    }

    public async Task<bool> ConfirmarAtencionAsync(int medicoId, int tipoConfirmacionId, Guid usuarioId)
    {
        var yaExiste = await _db.ConfirmacionesComunitarias
            .AnyAsync(c =>
                c.MedicoDirectorioId == medicoId &&
                c.UsuarioId == usuarioId &&
                c.TipoConfirmacionId == tipoConfirmacionId);

        if (yaExiste) return false;

        _db.ConfirmacionesComunitarias.Add(new ConfirmacionComunitaria
        {
            MedicoDirectorioId  = medicoId,
            UsuarioId           = usuarioId,
            TipoConfirmacionId  = tipoConfirmacionId,
            FechaCreacion       = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        await RecalcularNivelConfianzaAsync(medicoId);
        return true;
    }

    public async Task RecalcularNivelConfianzaAsync(int medicoId)
    {
        var medico = await _db.MedicosDirectorio.FindAsync(medicoId);
        if (medico is null) return;

        var pacientesUnicos = await _db.ConfirmacionesComunitarias
            .Where(c => c.MedicoDirectorioId == medicoId)
            .Select(c => c.UsuarioId)
            .Distinct()
            .CountAsync();

        medico.NivelConfianza = pacientesUnicos switch
        {
            >= 10 => NivelConfianzaEnum.Establecido,
            >= 5  => NivelConfianzaEnum.Reconocido,
            >= 3  => NivelConfianzaEnum.Confirmado,
            _     => NivelConfianzaEnum.Identificado
        };

        medico.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 7.3: Verificar que compila**

```powershell
dotnet build
```

Esperado: Build succeeded.

- [ ] **Step 7.4: Commit**

```
git add Services/Directorio/
git commit -m "feat(directorio): agregar IMedicoDirectorioService + MedicoDirectorioService"
```

---

## Task 8: Registrar servicio en DI (Program.cs)

**Files:**
- Modify: `Program.cs`

- [ ] **Step 8.1: Agregar registro del servicio**

Localizar el bloque de `builder.Services.AddScoped` en `Program.cs` (donde están registrados otros servicios como `ILaboratorioService`, `ISintomaService`, etc.) y agregar:

```csharp
// Directorio comunitario de médicos EII
builder.Services.AddScoped<eiibd26.Services.Directorio.IMedicoDirectorioService,
                            eiibd26.Services.Directorio.MedicoDirectorioService>();
```

- [ ] **Step 8.2: Verificar que compila**

```powershell
dotnet build
```

Esperado: Build succeeded.

- [ ] **Step 8.3: Commit**

```
git add Program.cs
git commit -m "feat(directorio): registrar MedicoDirectorioService en DI"
```

---

## Task 9: Controller DirectorioMedicosController

**Files:**
- Create: `Controllers/DirectorioMedicosController.cs`

- [ ] **Step 9.1: Crear el controller**

```csharp
// Controllers/DirectorioMedicosController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Controllers;

public class DirectorioMedicosController : Controller
{
    private readonly IMedicoDirectorioService _service;

    public DirectorioMedicosController(IMedicoDirectorioService service)
        => _service = service;

    // GET /DirectorioMedicos
    [HttpGet]
    public async Task<IActionResult> Index(
        string? busqueda,
        string? estado,
        string? especialidad,
        int? areaId,
        int pagina = 1)
    {
        var vm = await _service.GetListadoAsync(busqueda, estado, especialidad, areaId, pagina);
        return View(vm);
    }

    // GET /DirectorioMedicos/Detalle/5
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        var vm = await _service.GetDetalleAsync(id, usuarioId);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // GET /DirectorioMedicos/Proponer
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Proponer()
    {
        var vm = await _service.GetProponerVmAsync();
        return View(vm);
    }

    // POST /DirectorioMedicos/Proponer
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Proponer(ProponerMedicoVm vm)
    {
        if (!ModelState.IsValid)
        {
            var areas = await _service.GetProponerVmAsync();
            vm.AreasDisponibles = areas.AreasDisponibles;
            return View(vm);
        }

        var usuarioId = ObtenerUsuarioId()!.Value;
        var medicoId = await _service.ProponerMedicoAsync(vm, usuarioId);
        TempData["Success"] = "Gracias por tu aporte. El médico fue registrado en el directorio comunitario.";
        return RedirectToAction(nameof(Detalle), new { id = medicoId });
    }

    // POST /DirectorioMedicos/Confirmar
    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(ConfirmarAtencionVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Selecciona un tipo de atención para confirmar.";
            return RedirectToAction(nameof(Detalle), new { id = vm.MedicoDirectorioId });
        }

        var usuarioId = ObtenerUsuarioId()!.Value;
        var resultado = await _service.ConfirmarAtencionAsync(
            vm.MedicoDirectorioId, vm.TipoConfirmacionId, usuarioId);

        TempData[resultado ? "Success" : "Error"] = resultado
            ? "Tu confirmación fue registrada. Gracias por contribuir al directorio comunitario."
            : "Ya registraste este tipo de confirmación para este médico.";

        return RedirectToAction(nameof(Detalle), new { id = vm.MedicoDirectorioId });
    }

    private Guid? ObtenerUsuarioId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is not null ? Guid.Parse(value) : null;
    }
}
```

- [ ] **Step 9.2: Verificar que compila**

```powershell
dotnet build
```

Esperado: Build succeeded.

- [ ] **Step 9.3: Commit**

```
git add Controllers/DirectorioMedicosController.cs
git commit -m "feat(directorio): agregar DirectorioMedicosController con Index, Detalle, Proponer, Confirmar"
```

---

## Task 10: Vistas — _MedicoCard partial y css

**Files:**
- Create: `Views/DirectorioMedicos/Index.cshtml`
- Create: `Views/DirectorioMedicos/Detalle.cshtml`
- Create: `Views/DirectorioMedicos/Proponer.cshtml`
- Create: `Views/Shared/_MedicoCard.cshtml`

- [ ] **Step 10.1: Crear _MedicoCard.cshtml (partial reutilizable)**

```cshtml
@* Views/Shared/_MedicoCard.cshtml *@
@model eiibd26.Models.Directorio.MedicoCardVm

<article class="medico-card">
    <div class="medico-card__header">
        <div class="medico-card__avatar">
            <i class="bi bi-person-badge-fill"></i>
        </div>
        <div class="medico-card__info">
            <h3 class="medico-card__nombre">
                <a asp-controller="DirectorioMedicos" asp-action="Detalle" asp-route-id="@Model.Id">
                    @Model.NombreCompleto
                </a>
            </h3>
            @if (!string.IsNullOrWhiteSpace(Model.Especialidad))
            {
                <p class="medico-card__especialidad">@Model.Especialidad
                    @if (!string.IsNullOrWhiteSpace(Model.Subespecialidad))
                    {
                        <span class="text-muted"> · @Model.Subespecialidad</span>
                    }
                </p>
            }
        </div>
    </div>

    <div class="medico-card__ubicacion">
        <i class="bi bi-geo-alt text-muted"></i>
        <span>@(string.Join(", ", new[]{ Model.Ciudad, Model.Estado }.Where(x => !string.IsNullOrWhiteSpace(x))))</span>
    </div>

    @if (!string.IsNullOrWhiteSpace(Model.HospitalClinica))
    {
        <div class="medico-card__hospital text-muted">
            <i class="bi bi-hospital"></i> @Model.HospitalClinica
        </div>
    }

    @if (Model.AreasExperiencia.Any())
    {
        <div class="medico-card__areas">
            @foreach (var area in Model.AreasExperiencia.Take(4))
            {
                <span class="se-rel-badge">@area</span>
            }
            @if (Model.AreasExperiencia.Count > 4)
            {
                <span class="se-rel-badge text-muted">+@(Model.AreasExperiencia.Count - 4) más</span>
            }
        </div>
    }

    <div class="medico-card__footer">
        <span class="medico-nivel-badge @Model.NivelConfianzaBadgeClass">
            @Model.NivelConfianzaLabel
        </span>
        @if (Model.TotalPacientesUnicos > 0)
        {
            <span class="medico-card__confirmaciones text-muted">
                <i class="bi bi-people"></i> @Model.TotalPacientesUnicos
                @(Model.TotalPacientesUnicos == 1 ? "paciente" : "pacientes")
            </span>
        }
        @if (Model.CedulaValidada)
        {
            <span class="medico-cedula-validada" title="Cédula verificada">
                <i class="bi bi-patch-check-fill"></i>
            </span>
        }
    </div>
</article>
```

- [ ] **Step 10.2: Agregar CSS de tarjetas médico en el sitio**

Localizar el archivo CSS principal del proyecto (buscar en `wwwroot/css/` el CSS que ya tiene `.se-rel-badge` y variables CSS) y agregar al final:

```css
/* ── DIRECTORIO MÉDICOS EII ─────────────────────────────────────────── */

.medico-card {
    display: flex;
    flex-direction: column;
    gap: var(--space-sm);
    background: var(--color-bg);
    border: 1px solid var(--color-border);
    border-radius: 0.75rem;
    padding: var(--space-lg);
    box-shadow: var(--shadow-sm);
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.medico-card:hover {
    transform: translateY(-3px);
    box-shadow: var(--shadow-lg);
}

.medico-card__header {
    display: flex;
    gap: var(--space-md);
    align-items: flex-start;
}

.medico-card__avatar {
    width: 48px;
    height: 48px;
    min-width: 48px;
    background: var(--color-bg-subtle);
    border: 1px solid var(--color-border);
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.4rem;
    color: var(--color-primary);
}

.medico-card__nombre {
    font-size: var(--font-size-base);
    font-weight: 700;
    margin: 0 0 2px 0;
    line-height: 1.3;
}

.medico-card__nombre a {
    color: var(--color-heading);
    text-decoration: none;
}

.medico-card__nombre a:hover {
    color: var(--color-primary);
    text-decoration: underline;
}

.medico-card__especialidad {
    font-size: var(--font-size-sm);
    color: var(--color-text-secondary);
    margin: 0;
}

.medico-card__ubicacion,
.medico-card__hospital {
    font-size: var(--font-size-sm);
    display: flex;
    align-items: center;
    gap: 6px;
}

.medico-card__areas {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.medico-card__footer {
    display: flex;
    align-items: center;
    gap: var(--space-sm);
    flex-wrap: wrap;
    margin-top: auto;
    padding-top: var(--space-sm);
    border-top: 1px solid var(--color-border);
}

.medico-card__confirmaciones {
    font-size: var(--font-size-xs);
    display: flex;
    align-items: center;
    gap: 4px;
}

/* Badges de nivel de confianza */
.medico-nivel-badge {
    display: inline-flex;
    align-items: center;
    font-size: var(--font-size-xs);
    font-weight: 600;
    padding: 3px 8px;
    border-radius: 12px;
    border: 1px solid transparent;
}

.badge-nivel-0 {
    background: #f3f4f6;
    color: #6b7280;
    border-color: #e5e7eb;
}

.badge-nivel-1 {
    background: #eff6ff;
    color: #3b82f6;
    border-color: #bfdbfe;
}

.badge-nivel-2 {
    background: #f0fdf4;
    color: #16a34a;
    border-color: #bbf7d0;
}

.badge-nivel-3 {
    background: #faf5ff;
    color: #7c3aed;
    border-color: #e9d5ff;
}

.medico-cedula-validada {
    color: #16a34a;
    font-size: 1rem;
}

/* Grid de médicos */
.directorio-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: var(--space-lg);
    align-items: start;
}

@media (max-width: 991.98px) {
    .directorio-grid { grid-template-columns: repeat(2, 1fr); }
}

@media (max-width: 575.98px) {
    .directorio-grid { grid-template-columns: 1fr; }
}
```

- [ ] **Step 10.3: Crear Views/DirectorioMedicos/Index.cshtml**

Primero crear la carpeta `Views/DirectorioMedicos/`.

```cshtml
@* Views/DirectorioMedicos/Index.cshtml *@
@model eiibd26.Models.Directorio.DirectorioIndexVm
@{
    ViewData["Title"] = "Directorio de médicos con experiencia en EII";
}

<div class="conte-detail">
    <div class="page-title">
        <h1>Directorio de médicos</h1>
        <div class="se-subtitle">Médicos identificados por pacientes con EII</div>
    </div>

    @if (TempData["Success"] is string success)
    {
        <div class="alert alert-success alert-dismissible fade show mx-0 mb-3" role="alert">
            <i class="bi bi-check-circle-fill me-2"></i>@success
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }
    @if (TempData["Error"] is string error)
    {
        <div class="alert alert-danger alert-dismissible fade show mx-0 mb-3" role="alert">
            <i class="bi bi-exclamation-triangle-fill me-2"></i>@error
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }

    <div class="alert alert-info mb-4" role="note">
        <i class="bi bi-info-circle me-2"></i>
        Este directorio es construido por la comunidad EII. La información refleja experiencias
        reportadas por pacientes y <strong>no constituye una recomendación médica oficial.</strong>
    </div>

    <form method="get" class="directorio-filtros mb-4">
        <div class="row g-2 align-items-end">
            <div class="col-12 col-md-4">
                <label class="form-label">Buscar</label>
                <div class="input-group">
                    <input type="text" name="busqueda" value="@Model.FiltroBusqueda"
                           class="form-control" placeholder="Nombre, especialidad, hospital…" />
                    <button type="submit" class="btn btn-primary">
                        <i class="bi bi-search"></i>
                    </button>
                </div>
            </div>
            <div class="col-6 col-md-3">
                <label class="form-label">Estado</label>
                <select name="estado" class="form-select" onchange="this.form.submit()">
                    <option value="">Todos los estados</option>
                    @foreach (var e in Model.EstadosDisponibles)
                    {
                        <option value="@e" selected="@(Model.FiltroEstado == e ? "selected" : null)">@e</option>
                    }
                </select>
            </div>
            <div class="col-6 col-md-3">
                <label class="form-label">Área EII</label>
                <select name="areaId" class="form-select" onchange="this.form.submit()">
                    <option value="">Todas las áreas</option>
                    @foreach (var a in Model.AreasDisponibles)
                    {
                        <option value="@a.Id" selected="@(Model.FiltroAreaId == a.Id ? "selected" : null)">@a.Nombre</option>
                    }
                </select>
            </div>
            <div class="col-12 col-md-2">
                <a asp-action="Index" class="btn btn-outline-secondary w-100">
                    <i class="bi bi-x-lg"></i> Limpiar
                </a>
            </div>
        </div>
    </form>

    <div class="d-flex justify-content-between align-items-center mb-3">
        <span class="text-muted" style="font-size: var(--font-size-sm);">
            @Model.TotalResultados médico@(Model.TotalResultados != 1 ? "s" : "") encontrado@(Model.TotalResultados != 1 ? "s" : "")
        </span>
        <a asp-action="Proponer" class="btn btn-primary btn-sm">
            <i class="bi bi-plus-lg me-1"></i> Agregar médico
        </a>
    </div>

    @if (Model.Medicos.Any())
    {
        <div class="directorio-grid">
            @foreach (var medico in Model.Medicos)
            {
                @await Html.PartialAsync("_MedicoCard", medico)
            }
        </div>

        @if (Model.TotalPaginas > 1)
        {
            <div class="preguntas-paginacion mt-4">
                <div class="pag-bar">
                    @if (Model.PaginaActual > 1)
                    {
                        <a class="pag-btn" asp-action="Index"
                           asp-route-busqueda="@Model.FiltroBusqueda"
                           asp-route-estado="@Model.FiltroEstado"
                           asp-route-areaId="@Model.FiltroAreaId"
                           asp-route-pagina="@(Model.PaginaActual - 1)">‹</a>
                    }
                    @for (int p = 1; p <= Model.TotalPaginas; p++)
                    {
                        <a class="pag-num @(p == Model.PaginaActual ? "active" : "")"
                           asp-action="Index"
                           asp-route-busqueda="@Model.FiltroBusqueda"
                           asp-route-estado="@Model.FiltroEstado"
                           asp-route-areaId="@Model.FiltroAreaId"
                           asp-route-pagina="@p">@p</a>
                    }
                    @if (Model.PaginaActual < Model.TotalPaginas)
                    {
                        <a class="pag-btn" asp-action="Index"
                           asp-route-busqueda="@Model.FiltroBusqueda"
                           asp-route-estado="@Model.FiltroEstado"
                           asp-route-areaId="@Model.FiltroAreaId"
                           asp-route-pagina="@(Model.PaginaActual + 1)">›</a>
                    }
                </div>
            </div>
        }
    }
    else
    {
        <div class="text-center py-5">
            <i class="bi bi-search" style="font-size: 3rem; color: var(--color-text-secondary);"></i>
            <p class="mt-3 text-muted">No se encontraron médicos con esos filtros.</p>
            <a asp-action="Proponer" class="btn btn-primary mt-2">
                <i class="bi bi-plus-lg me-1"></i> Agregar el primero
            </a>
        </div>
    }
</div>
```

- [ ] **Step 10.4: Crear Views/DirectorioMedicos/Detalle.cshtml**

```cshtml
@* Views/DirectorioMedicos/Detalle.cshtml *@
@model eiibd26.Models.Directorio.MedicoDetalleVm
@{
    ViewData["Title"] = $"Dr. {Model.NombreCompleto} — Directorio EII";
}

<div class="conte-detail">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index">Directorio</a></li>
            <li class="breadcrumb-item active" aria-current="page">@Model.NombreCompleto</li>
        </ol>
    </nav>

    @if (TempData["Success"] is string success)
    {
        <div class="alert alert-success alert-dismissible fade show mb-3" role="alert">
            <i class="bi bi-check-circle-fill me-2"></i>@success
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }
    @if (TempData["Error"] is string error)
    {
        <div class="alert alert-danger alert-dismissible fade show mb-3" role="alert">
            <i class="bi bi-exclamation-triangle-fill me-2"></i>@error
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }

    <div class="detail-grid">
        <div class="content-panel">

            <div class="card shadow-sm mb-4">
                <div class="card-body">
                    <div class="d-flex gap-3 align-items-start">
                        <div class="medico-card__avatar" style="width:64px;height:64px;font-size:2rem;">
                            <i class="bi bi-person-badge-fill"></i>
                        </div>
                        <div class="flex-grow-1">
                            <h1 class="h3 mb-1">@Model.NombreCompleto</h1>
                            @if (!string.IsNullOrWhiteSpace(Model.Especialidad))
                            {
                                <p class="text-muted mb-1">@Model.Especialidad
                                    @if (!string.IsNullOrWhiteSpace(Model.Subespecialidad))
                                    {
                                        <span> · @Model.Subespecialidad</span>
                                    }
                                </p>
                            }
                            <div class="d-flex flex-wrap gap-2 mt-2">
                                <span class="medico-nivel-badge @(Model.NivelConfianza switch {
                                    eiibd26.Models.Directorio.Enums.NivelConfianzaEnum.Identificado => "badge-nivel-0",
                                    eiibd26.Models.Directorio.Enums.NivelConfianzaEnum.Confirmado   => "badge-nivel-1",
                                    eiibd26.Models.Directorio.Enums.NivelConfianzaEnum.Reconocido   => "badge-nivel-2",
                                    _                                                                 => "badge-nivel-3"
                                })">@Model.NivelConfianzaLabel</span>

                                @if (Model.EstatusValidacion == eiibd26.Models.Directorio.Enums.EstatusValidacionCedula.Validado)
                                {
                                    <span class="badge bg-success-subtle text-success">
                                        <i class="bi bi-patch-check-fill me-1"></i>Cédula verificada
                                    </span>
                                }
                                else
                                {
                                    <span class="badge bg-light text-muted border">
                                        <i class="bi bi-clock me-1"></i>Pendiente de verificación
                                    </span>
                                }
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-3 mb-4">
                <div class="col-md-6">
                    <div class="card shadow-sm h-100">
                        <div class="card-header"><h6 class="mb-0"><i class="bi bi-geo-alt me-2"></i>Ubicación</h6></div>
                        <div class="card-body">
                            @if (!string.IsNullOrWhiteSpace(Model.Estado))
                            {
                                <p class="mb-1"><strong>Estado:</strong> @Model.Estado</p>
                            }
                            @if (!string.IsNullOrWhiteSpace(Model.Ciudad))
                            {
                                <p class="mb-1"><strong>Ciudad:</strong> @Model.Ciudad</p>
                            }
                            @if (!string.IsNullOrWhiteSpace(Model.MunicipioAlcaldia))
                            {
                                <p class="mb-1"><strong>Municipio/Alcaldía:</strong> @Model.MunicipioAlcaldia</p>
                            }
                            @if (!string.IsNullOrWhiteSpace(Model.HospitalClinica))
                            {
                                <p class="mb-1"><strong>Hospital/Clínica:</strong> @Model.HospitalClinica</p>
                            }
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="card shadow-sm h-100">
                        <div class="card-header"><h6 class="mb-0"><i class="bi bi-clipboard2-pulse me-2"></i>Confirmaciones comunitarias</h6></div>
                        <div class="card-body">
                            <p class="mb-2">
                                <strong>@Model.TotalPacientesUnicos</strong>
                                paciente@(Model.TotalPacientesUnicos != 1 ? "s" : "") de la comunidad
                                @(Model.TotalPacientesUnicos != 1 ? "han" : "ha") reportado atención.
                            </p>
                            @if (Model.ConfirmacionesAgregadas.Any())
                            {
                                <ul class="list-unstyled mb-0">
                                    @foreach (var conf in Model.ConfirmacionesAgregadas.OrderByDescending(c => c.Total))
                                    {
                                        <li class="d-flex justify-content-between align-items-center py-1 border-bottom">
                                            <span style="font-size:var(--font-size-sm);">@conf.NombreTipo</span>
                                            <span class="badge bg-primary-subtle text-primary">@conf.Total</span>
                                        </li>
                                    }
                                </ul>
                            }
                        </div>
                    </div>
                </div>
            </div>

            @if (Model.AreasExperiencia.Any())
            {
                <div class="card shadow-sm mb-4">
                    <div class="card-header"><h6 class="mb-0"><i class="bi bi-tags me-2"></i>Áreas de experiencia EII reportadas</h6></div>
                    <div class="card-body">
                        <div class="d-flex flex-wrap gap-2">
                            @foreach (var area in Model.AreasExperiencia)
                            {
                                <span class="se-rel-badge">@area.Nombre</span>
                            }
                        </div>
                        <p class="text-muted mt-2 mb-0" style="font-size:var(--font-size-xs);">
                            Áreas reportadas por pacientes. No implican especialización certificada.
                        </p>
                    </div>
                </div>
            }

            @if (User.Identity?.IsAuthenticated == true)
            {
                <div class="card shadow-sm mb-4 border-primary-subtle">
                    <div class="card-header bg-primary-subtle">
                        <h6 class="mb-0"><i class="bi bi-person-check me-2"></i>¿Recibiste atención de este médico?</h6>
                    </div>
                    <div class="card-body">
                        @if (Model.UsuarioYaConfirmo)
                        {
                            <p class="text-muted mb-0">
                                <i class="bi bi-check-circle-fill text-success me-1"></i>
                                Ya aportaste tu experiencia. Puedes agregar más tipos de atención.
                            </p>
                        }
                        <form asp-action="Confirmar" method="post" class="d-flex gap-2 align-items-end flex-wrap mt-2">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="MedicoDirectorioId" value="@Model.Id" />
                            <div class="flex-grow-1">
                                <label class="form-label mb-1" style="font-size:var(--font-size-sm);">Tipo de atención recibida</label>
                                <select name="TipoConfirmacionId" class="form-select form-select-sm" required>
                                    <option value="">Selecciona…</option>
                                    @* Los tipos se pueden cargar via ViewBag o endpoint — simplificar con hardcode por ahora *@
                                </select>
                            </div>
                            <button type="submit" class="btn btn-primary btn-sm">
                                <i class="bi bi-check-lg me-1"></i>Confirmar
                            </button>
                        </form>
                        <p class="text-muted mt-2 mb-0" style="font-size:var(--font-size-xs);">
                            Tu identidad no será visible públicamente. Solo el total agregado se muestra.
                        </p>
                    </div>
                </div>
            }
            else
            {
                <div class="vote-auth-notice mb-4">
                    <span>¿Recibiste atención de este médico?</span>
                    <a href="/Identity/Account/Login">Inicia sesión para confirmarlo</a>
                </div>
            }

            @if (Model.PerfilReclamable)
            {
                <div class="alert alert-light border mb-0" role="note">
                    <i class="bi bi-shield-check me-2 text-muted"></i>
                    <strong>¿Eres este profesional?</strong>
                    Este perfil está disponible para ser reclamado por el médico titular.
                    <a href="mailto:contacto@eiibd.com" class="ms-1">Contáctanos</a> para iniciar el proceso.
                </div>
            }
        </div>

        <aside class="right-panel">
            <div class="sidebar-section sidebar-static">
                <h4><i class="bi bi-plus-circle me-1"></i> ¿Conoces otro médico?</h4>
                <p style="font-size:var(--font-size-sm);">Ayuda a la comunidad EII agregando médicos con experiencia.</p>
                <a asp-action="Proponer" class="btn btn-outline-primary btn-sm w-100">
                    Agregar médico
                </a>
            </div>
            <div class="sidebar-section sidebar-static mt-3">
                <h4><i class="bi bi-info-circle me-1"></i> Aviso</h4>
                <p style="font-size:var(--font-size-xs); color:var(--color-text-secondary);">
                    Este directorio es comunitario y basado en experiencias reportadas por pacientes.
                    No constituye una recomendación médica oficial ni avala diagnósticos.
                </p>
            </div>
        </aside>
    </div>
</div>
```

- [ ] **Step 10.5: Crear Views/DirectorioMedicos/Proponer.cshtml**

```cshtml
@* Views/DirectorioMedicos/Proponer.cshtml *@
@model eiibd26.Models.Directorio.ProponerMedicoVm
@{
    ViewData["Title"] = "Agregar médico al directorio EII";
}

<div class="conte-detail">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index">Directorio</a></li>
            <li class="breadcrumb-item active">Agregar médico</li>
        </ol>
    </nav>

    <div class="page-title mb-4">
        <h1>Agregar médico al directorio</h1>
        <div class="se-subtitle">Ayuda a la comunidad EII compartiendo información de médicos con experiencia</div>
    </div>

    <div class="row justify-content-center">
        <div class="col-lg-8">
            <div class="alert alert-info mb-4">
                <i class="bi bi-info-circle me-2"></i>
                Solo comparte información que ya sea pública (nombre, especialidad, hospital).
                Tu identidad como aportante no será visible.
            </div>

            <form asp-action="Proponer" method="post">
                @Html.AntiForgeryToken()
                <div asp-validation-summary="ModelOnly" class="alert alert-danger d-none"></div>

                <div class="card shadow-sm mb-3">
                    <div class="card-header"><h6 class="mb-0">Datos profesionales</h6></div>
                    <div class="card-body">
                        <div class="mb-3">
                            <label asp-for="NombreCompleto" class="form-label"></label>
                            <input asp-for="NombreCompleto" class="form-control" placeholder="Ej: Dr. Juan García López" />
                            <span asp-validation-for="NombreCompleto" class="text-danger small"></span>
                        </div>
                        <div class="row g-3">
                            <div class="col-md-6 mb-0">
                                <label asp-for="CedulaProfesional" class="form-label"></label>
                                <input asp-for="CedulaProfesional" class="form-control" placeholder="Ej: 1234567" />
                                <span asp-validation-for="CedulaProfesional" class="text-danger small"></span>
                            </div>
                            <div class="col-md-6 mb-0">
                                <label asp-for="Especialidad" class="form-label"></label>
                                <input asp-for="Especialidad" class="form-control" placeholder="Ej: Gastroenterología" />
                            </div>
                        </div>
                        <div class="mt-3">
                            <label asp-for="Subespecialidad" class="form-label"></label>
                            <input asp-for="Subespecialidad" class="form-control" placeholder="Ej: Enfermedad Inflamatoria Intestinal" />
                        </div>
                    </div>
                </div>

                <div class="card shadow-sm mb-3">
                    <div class="card-header"><h6 class="mb-0">Ubicación</h6></div>
                    <div class="card-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label asp-for="Estado" class="form-label"></label>
                                <input asp-for="Estado" class="form-control" placeholder="Ej: Ciudad de México" />
                                <span asp-validation-for="Estado" class="text-danger small"></span>
                            </div>
                            <div class="col-md-6">
                                <label asp-for="Ciudad" class="form-label"></label>
                                <input asp-for="Ciudad" class="form-control" placeholder="Ej: Benito Juárez" />
                            </div>
                        </div>
                        <div class="row g-3 mt-0">
                            <div class="col-md-6">
                                <label asp-for="MunicipioAlcaldia" class="form-label"></label>
                                <input asp-for="MunicipioAlcaldia" class="form-control" placeholder="Opcional" />
                            </div>
                            <div class="col-md-6">
                                <label asp-for="HospitalClinica" class="form-label"></label>
                                <input asp-for="HospitalClinica" class="form-control" placeholder="Ej: Hospital Ángeles" />
                            </div>
                        </div>
                    </div>
                </div>

                @if (Model.AreasDisponibles.Any())
                {
                    <div class="card shadow-sm mb-4">
                        <div class="card-header">
                            <h6 class="mb-0">Áreas de experiencia EII que reportas</h6>
                        </div>
                        <div class="card-body">
                            <p class="text-muted mb-3" style="font-size:var(--font-size-sm);">
                                Selecciona las áreas en las que este médico tiene experiencia según tu vivencia.
                            </p>
                            <div class="row g-2">
                                @foreach (var area in Model.AreasDisponibles)
                                {
                                    <div class="col-6 col-md-4">
                                        <div class="form-check">
                                            <input class="form-check-input" type="checkbox"
                                                   name="AreasSeleccionadas"
                                                   value="@area.Id"
                                                   id="area-@area.Id"
                                                   checked="@(Model.AreasSeleccionadas.Contains(area.Id) ? "checked" : null)" />
                                            <label class="form-check-label" for="area-@area.Id">
                                                @area.Nombre
                                            </label>
                                        </div>
                                    </div>
                                }
                            </div>
                        </div>
                    </div>
                }

                <div class="d-flex gap-2 justify-content-end">
                    <a asp-action="Index" class="btn btn-outline-secondary">Cancelar</a>
                    <button type="submit" class="btn btn-primary">
                        <i class="bi bi-plus-lg me-1"></i> Agregar al directorio
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    @{ await Html.RenderPartialAsync("_ValidationScriptsPartial"); }
}
```

- [ ] **Step 10.6: Verificar que compila**

```powershell
dotnet build
```

Esperado: Build succeeded.

- [ ] **Step 10.7: Commit**

```
git add Views/DirectorioMedicos/ Views/Shared/_MedicoCard.cshtml wwwroot/css/
git commit -m "feat(directorio): agregar vistas Index, Detalle, Proponer y partial _MedicoCard con CSS"
```

---

## Task 11: Cargar TiposConfirmacion en la vista Detalle (ViewBag)

**Files:**
- Modify: `Controllers/DirectorioMedicosController.cs`

La vista Detalle necesita la lista de tipos de confirmación para el select. Esta tarea completa ese hueco.

- [ ] **Step 11.1: Modificar la acción Detalle para inyectar tipos**

Localizar la acción `Detalle` en `DirectorioMedicosController.cs` y reemplazarla:

```csharp
[HttpGet]
public async Task<IActionResult> Detalle(int id)
{
    var usuarioId = ObtenerUsuarioId();
    var vm = await _service.GetDetalleAsync(id, usuarioId);
    if (vm is null) return NotFound();

    ViewBag.TiposConfirmacion = await _db.TiposConfirmacion
        .AsNoTracking()
        .Where(t => t.Activo)
        .OrderBy(t => t.Orden)
        .ToListAsync();

    return View(vm);
}
```

Para esto, también inyectar `ApplicationDbContext` en el constructor:

```csharp
public class DirectorioMedicosController : Controller
{
    private readonly IMedicoDirectorioService _service;
    private readonly ApplicationDbContext _db;

    public DirectorioMedicosController(
        IMedicoDirectorioService service,
        ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }
    // ...
}
```

Y actualizar el select en `Detalle.cshtml` para usar `ViewBag.TiposConfirmacion`:

```cshtml
<select name="TipoConfirmacionId" class="form-select form-select-sm" required>
    <option value="">Selecciona…</option>
    @foreach (var tipo in (List<eiibd26.Models.Directorio.TipoConfirmacion>)ViewBag.TiposConfirmacion)
    {
        <option value="@tipo.Id"
                disabled="@(Model.TiposConfirmadosPorUsuario.Contains(tipo.Id) ? "disabled" : null)">
            @tipo.Nombre @(Model.TiposConfirmadosPorUsuario.Contains(tipo.Id) ? "✓" : "")
        </option>
    }
</select>
```

- [ ] **Step 11.2: Verificar que compila**

```powershell
dotnet build
```

Esperado: Build succeeded.

- [ ] **Step 11.3: Commit**

```
git add Controllers/DirectorioMedicosController.cs Views/DirectorioMedicos/Detalle.cshtml
git commit -m "feat(directorio): inyectar TiposConfirmacion en ViewBag para select en Detalle"
```

---

## Task 12: Verificación final end-to-end

- [ ] **Step 12.1: Arrancar la aplicación**

```powershell
dotnet run
```

Esperado: aplicación arranca sin errores.

- [ ] **Step 12.2: Verificar listado público**

Navegar a `/DirectorioMedicos` — debe mostrar la página de listado vacía con filtros y botón "Agregar médico".

- [ ] **Step 12.3: Proponer un médico de prueba**

Autenticarse con cualquier cuenta. Ir a `/DirectorioMedicos/Proponer` y crear un médico con:
- Nombre: "Dr. Prueba EII"
- Estado: "Ciudad de México"
- Áreas: CUCI + Biológicos

Verificar que redirige al detalle del médico creado con mensaje de éxito.

- [ ] **Step 12.4: Confirmar atención sobre ese médico**

En la ficha del médico, seleccionar un tipo de confirmación y enviar. Verificar que:
- Aparece mensaje de éxito
- El contador de confirmaciones incrementa
- El tipo ya confirmado aparece deshabilitado en el select

- [ ] **Step 12.5: Confirmar que el nivel de confianza se recalcula**

Crear 2 cuentas de prueba adicionales y confirmar atención. Verificar que el nivel sube de "Identificado" a "Confirmado" al alcanzar 3 pacientes únicos.

- [ ] **Step 12.6: Commit final**

```
git add .
git commit -m "feat(directorio): primera fase directorio comunitario médicos EII — completo"
```

---

## Resumen de criterios de aceptación cubiertos

| Criterio | Cubierto por |
|----------|-------------|
| Estructura médico separada de AspNetUsers | `MedicoDirectorio` — no hereda Identity |
| No obliga AspNetUsers en fase 1 | `AspNetUserId` es nullable |
| Vinculación futura preparada | `AspNetUserId`, `EstatusReclamacion`, `FechaReclamacion` |
| Ubicación para mapa | `Estado`, `Ciudad`, `MunicipioAlcaldia`, `HospitalClinica`, `Latitud`, `Longitud` |
| Experiencia EII estructurada | `AreaExperienciaEii` (catálogo seeded) + `MedicoExperienciaEii` |
| Confirmaciones comunitarias anónimas | `ConfirmacionComunitaria` — visible solo agregada |
| Validación de cédula | `EstatusValidacion` + `CedulaProfesional` |
| Nivel de confianza | `NivelConfianzaEnum` + `RecalcularNivelConfianzaAsync` |
| Sin OCR ni recetas | No existe en este plan |
| Sin estrellas ni ranking | Niveles de confianza comunitaria, no rating numérico |
| UI consistente con el proyecto | Mismo CSS variables, cards, paginación, breadcrumbs |
| Soft-delete y auditoría | `Eliminado` + `FechaCreacion` + `FechaModificacion` en todas las entidades |
| Lenguaje prudente | "Identificado por pacientes", "experiencia reportada", "confirmaciones comunitarias" |
