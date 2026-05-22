# Sistema Médicos: Registro, Reclamación, Perfil y Badges — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar el ciclo completo del médico: registro de cuenta, reclamación de perfil del directorio vía token, gestión de perfil profesional y sistema de badges progresivo.

**Architecture:** Raw SQL scripts para el schema + entidades C# + ApplicationDbContext para EF. Servicio IMedicoBadgeService centraliza toda la lógica de badges. Flujo de reclamación stateless via tokens de 72h en tabla propia. PerfilMedico.cshtml sigue el mismo layout que UsuarioPerfil.cshtml.

**Tech Stack:** ASP.NET Core 8 Razor Pages, EF Core 8 (code-first models, SQL-first schema), ASP.NET Identity, SendGrid (IEmailSender), Bootstrap 5, ImageSharp (foto de perfil)

---

## Mapa de archivos

| Acción | Ruta |
|--------|------|
| Crear | `Migrations/2026-05-21_MedicoPerfilBadgesTokens.sql` |
| Crear | `Models/Medico/MedicoPerfilExtendido.cs` |
| Crear | `Models/Medico/MedicoBadge.cs` |
| Crear | `Models/Medico/MedicoPerfilBadge.cs` |
| Crear | `Models/Medico/MedicoReclamacionToken.cs` |
| Crear | `Models/Medico/MedicoBadgeDto.cs` |
| Modificar | `Data/ApplicationDbContext.cs` (DbSets + Fluent config) |
| Crear | `Services/Medico/IMedicoBadgeService.cs` |
| Crear | `Services/Medico/MedicoBadgeService.cs` |
| Modificar | `Program.cs` (registrar IMedicoBadgeService) |
| Crear | `Areas/Identity/Pages/Account/RegisterM.cshtml` |
| Crear | `Areas/Identity/Pages/Account/RegisterM.cshtml.cs` |
| Modificar | `Areas/Identity/Pages/Account/Register.cshtml` (agregar link médico) |
| Crear | `Pages/Directorio/Reclamar.cshtml` |
| Crear | `Pages/Directorio/Reclamar.cshtml.cs` |
| Crear | `Pages/Directorio/Activar.cshtml` |
| Crear | `Pages/Directorio/Activar.cshtml.cs` |
| Modificar | `Pages/DirectorioMedicos/Detalle.cshtml` (nuevo botón reclamar) |
| Modificar | `Pages/DirectorioMedicos/Detalle.cshtml.cs` (cargar PerfilVinculado) |
| Modificar | `Areas/Identity/Pages/Account/Manage/ManageNavPages.cs` (agregar PerfilMedico) |
| Modificar | `Areas/Identity/Pages/Account/Manage/_ManageNav.cshtml` (item condicional) |
| Crear | `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml` |
| Crear | `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs` |

---

## Task 1: Schema SQL + Entidades C# + ApplicationDbContext

**Files:**
- Create: `Migrations/2026-05-21_MedicoPerfilBadgesTokens.sql`
- Create: `Models/Medico/MedicoPerfilExtendido.cs`
- Create: `Models/Medico/MedicoBadge.cs`
- Create: `Models/Medico/MedicoPerfilBadge.cs`
- Create: `Models/Medico/MedicoReclamacionToken.cs`
- Create: `Models/Medico/MedicoBadgeDto.cs`
- Modify: `Data/ApplicationDbContext.cs`

- [ ] **Step 1.1: Crear el script SQL**

Crear `Migrations/2026-05-21_MedicoPerfilBadgesTokens.sql` con contenido:

```sql
USE eiibd26;
GO

-- ── 1. MedicoPerfilExtendido ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoPerfilExtendido')
BEGIN
    CREATE TABLE MedicoPerfilExtendido (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId         INT NULL UNIQUE,
        UserId           UNIQUEIDENTIFIER NULL,
        Slug             NVARCHAR(100) NULL,
        Foto             NVARCHAR(500) NULL,
        Biografia        NVARCHAR(2000) NULL,
        Hospitales       NVARCHAR(1000) NULL,
        HorariosAtencion NVARCHAR(500) NULL,
        SitioWeb         NVARCHAR(300) NULL,
        Telefono         NVARCHAR(50) NULL,
        Instagram        NVARCHAR(150) NULL,
        LinkedIn         NVARCHAR(150) NULL,
        FechaCreado      DATETIME NOT NULL DEFAULT GETUTCDATE(),
        FechaModificado  DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MedicoPerfilExtendido_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE SET NULL,
        CONSTRAINT FK_MedicoPerfilExtendido_User
            FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX UX_MedicoPerfilExtendido_Slug
        ON MedicoPerfilExtendido(Slug) WHERE Slug IS NOT NULL;
    CREATE INDEX IX_MedicoPerfilExtendido_UserId
        ON MedicoPerfilExtendido(UserId);
    PRINT 'Tabla MedicoPerfilExtendido creada.';
END
ELSE PRINT 'Tabla MedicoPerfilExtendido ya existe.';
GO

-- ── 2. MedicoBadge ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoBadge')
BEGIN
    CREATE TABLE MedicoBadge (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        Codigo        NVARCHAR(50) NOT NULL,
        Nombre        NVARCHAR(150) NOT NULL,
        Descripcion   NVARCHAR(500) NOT NULL,
        ComoObtenerlo NVARCHAR(300) NOT NULL,
        Icono         NVARCHAR(100) NOT NULL,
        Nivel         INT NOT NULL,
        Orden         INT NOT NULL,
        Activo        BIT NOT NULL DEFAULT 1,
        FechaCreado   DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UX_MedicoBadge_Codigo UNIQUE (Codigo)
    );

    INSERT INTO MedicoBadge (Codigo, Nombre, Descripcion, ComoObtenerlo, Icono, Nivel, Orden) VALUES
    ('perfil_reclamado',   'Perfil Reclamado',       'El médico ha reclamado su perfil en el directorio EII.', 'Reclamar y completar el perfil',                'bi-person-check-fill',    1, 1),
    ('verificado',         'Verificado',             'El equipo EIIBD ha verificado las credenciales del médico.', 'El equipo EIIBD verifica tus credenciales', 'bi-patch-check-fill',     2, 2),
    ('activo_comunidad',   'Activo en Comunidad',    'Al menos 5 pacientes han recomendado a este médico.',    '5 o más pacientes te han recomendado',          'bi-people-fill',          3, 3),
    ('participante_qa',    'Participante Q&A',       'Ha respondido 3 o más preguntas en el foro.',            'Responder 3 o más preguntas en el foro',        'bi-chat-dots-fill',       4, 4),
    ('validador_contenido','Validador de Contenido', 'Ha validado 5 o más términos del glosario.',             'Validar 5 o más términos del glosario',         'bi-check-circle-fill',    5, 5),
    ('creador_contenido',  'Creador de Contenido',   'Contribuye activamente con contenido médico de calidad.','El equipo EIIBD lo otorga manualmente',         'bi-star-fill',            6, 6);

    PRINT 'Tabla MedicoBadge creada con seed.';
END
ELSE PRINT 'Tabla MedicoBadge ya existe.';
GO

-- ── 3. MedicoPerfilBadge ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoPerfilBadge')
BEGIN
    CREATE TABLE MedicoPerfilBadge (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId     INT NOT NULL,
        BadgeId      INT NOT NULL,
        FechaObtenido DATETIME NOT NULL DEFAULT GETUTCDATE(),
        OtorgadoPor  NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_MedicoPerfilBadge_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoPerfilBadge_Badge
            FOREIGN KEY (BadgeId) REFERENCES MedicoBadge(Id) ON DELETE CASCADE,
        CONSTRAINT UX_MedicoPerfilBadge_MedBadge UNIQUE (MedicoId, BadgeId)
    );
    PRINT 'Tabla MedicoPerfilBadge creada.';
END
ELSE PRINT 'Tabla MedicoPerfilBadge ya existe.';
GO

-- ── 4. MedicoReclamacionToken ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoReclamacionToken')
BEGIN
    CREATE TABLE MedicoReclamacionToken (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId     INT NOT NULL,
        Token        NVARCHAR(200) NOT NULL,
        EmailDestino NVARCHAR(200) NOT NULL,
        UserId       UNIQUEIDENTIFIER NULL,
        FechaCreado  DATETIME NOT NULL DEFAULT GETUTCDATE(),
        FechaExpira  DATETIME NOT NULL,
        FechaUsado   DATETIME NULL,
        Activo       BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_MedicoReclamacionToken_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoReclamacionToken_User
            FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT UX_MedicoReclamacionToken_Token UNIQUE (Token)
    );
    CREATE INDEX IX_MedicoReclamacionToken_MedicoId ON MedicoReclamacionToken(MedicoId);
    PRINT 'Tabla MedicoReclamacionToken creada.';
END
ELSE PRINT 'Tabla MedicoReclamacionToken ya existe.';
GO
```

- [ ] **Step 1.2: Aplicar el script SQL a la base de datos**

Obtener la cadena de conexión del archivo `appsettings.Development.json` o `appsettings.json`. Ejecutar el script SQL directamente contra la BD usando el servidor y credenciales de la configuración. Si hay herramientas de shell disponibles, usar:

```powershell
# Obtener connectionstring (ajustar ruta si es necesario)
$cs = (Get-Content "appsettings.Development.json" | ConvertFrom-Json).ConnectionStrings.DefaultConnection
# Ejecutar con sqlcmd (si disponible)
sqlcmd -S <servidor> -d eiibd26 -E -i "Migrations/2026-05-21_MedicoPerfilBadgesTokens.sql"
```

Si sqlcmd no está disponible, conectarse directamente desde código o usar Azure Data Studio / SSMS.

- [ ] **Step 1.3: Crear entidades C#**

`Models/Medico/MedicoPerfilExtendido.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

public class MedicoPerfilExtendido
{
    public int Id { get; set; }

    public int? MedicoId { get; set; }
    public Guid? UserId { get; set; }

    [MaxLength(100)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Foto { get; set; }

    [MaxLength(2000)]
    public string? Biografia { get; set; }

    [MaxLength(1000)]
    public string? Hospitales { get; set; }

    [MaxLength(500)]
    public string? HorariosAtencion { get; set; }

    [MaxLength(300)]
    public string? SitioWeb { get; set; }

    [MaxLength(50)]
    public string? Telefono { get; set; }

    [MaxLength(150)]
    public string? Instagram { get; set; }

    [MaxLength(150)]
    public string? LinkedIn { get; set; }

    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificado { get; set; } = DateTime.UtcNow;

    public MedicoDirectorio? Medico { get; set; }
    public ApplicationUser? Usuario { get; set; }
}
```

`Models/Medico/MedicoBadge.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Medico;

public class MedicoBadge
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string ComoObtenerlo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Icono { get; set; } = string.Empty;

    public int Nivel { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;

    public ICollection<MedicoPerfilBadge> PerfilesBadges { get; set; } = new List<MedicoPerfilBadge>();
}
```

`Models/Medico/MedicoPerfilBadge.cs`:
```csharp
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

public class MedicoPerfilBadge
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public int BadgeId { get; set; }
    public DateTime FechaObtenido { get; set; } = DateTime.UtcNow;
    public string OtorgadoPor { get; set; } = "sistema";

    public MedicoDirectorio? Medico { get; set; }
    public MedicoBadge? Badge { get; set; }
}
```

`Models/Medico/MedicoReclamacionToken.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

public class MedicoReclamacionToken
{
    public int Id { get; set; }
    public int MedicoId { get; set; }

    [Required, MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string EmailDestino { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpira { get; set; }
    public DateTime? FechaUsado { get; set; }
    public bool Activo { get; set; } = true;

    public MedicoDirectorio? Medico { get; set; }
    public ApplicationUser? Usuario { get; set; }
}
```

`Models/Medico/MedicoBadgeDto.cs`:
```csharp
namespace eiibd26.Models.Medico;

public class MedicoBadgeDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string ComoObtenerlo { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public bool Obtenido { get; set; }
    public DateTime? FechaObtenido { get; set; }
}
```

- [ ] **Step 1.4: Agregar DbSets y configuración Fluent a ApplicationDbContext**

En `Data/ApplicationDbContext.cs`, agregar después del último DbSet existente:

```csharp
// Medico Perfil Extended
public DbSet<eiibd26.Models.Medico.MedicoPerfilExtendido> MedicosPerfilExtendido { get; set; }
public DbSet<eiibd26.Models.Medico.MedicoBadge> MedicosBadge { get; set; }
public DbSet<eiibd26.Models.Medico.MedicoPerfilBadge> MedicosPerfilBadge { get; set; }
public DbSet<eiibd26.Models.Medico.MedicoReclamacionToken> MedicoReclamacionTokens { get; set; }
```

Y en el método `OnModelCreating`, agregar antes del cierre `}` de ese método:

```csharp
builder.Entity<eiibd26.Models.Medico.MedicoPerfilExtendido>(b =>
{
    b.ToTable("MedicoPerfilExtendido");
    b.HasKey(p => p.Id);
    b.HasIndex(p => p.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
    b.HasIndex(p => p.UserId);
    b.Property(p => p.MedicoId).IsRequired(false);
    b.HasOne(p => p.Medico)
     .WithMany()
     .HasForeignKey(p => p.MedicoId)
     .OnDelete(DeleteBehavior.SetNull);
    b.HasOne(p => p.Usuario)
     .WithMany()
     .HasForeignKey(p => p.UserId)
     .OnDelete(DeleteBehavior.SetNull);
});

builder.Entity<eiibd26.Models.Medico.MedicoBadge>(b =>
{
    b.ToTable("MedicoBadge");
    b.HasKey(x => x.Id);
    b.HasIndex(x => x.Codigo).IsUnique();
});

builder.Entity<eiibd26.Models.Medico.MedicoPerfilBadge>(b =>
{
    b.ToTable("MedicoPerfilBadge");
    b.HasKey(x => x.Id);
    b.HasIndex(x => new { x.MedicoId, x.BadgeId }).IsUnique();
    b.HasOne(x => x.Medico)
     .WithMany()
     .HasForeignKey(x => x.MedicoId)
     .OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.Badge)
     .WithMany(b => b.PerfilesBadges)
     .HasForeignKey(x => x.BadgeId)
     .OnDelete(DeleteBehavior.Cascade);
});

builder.Entity<eiibd26.Models.Medico.MedicoReclamacionToken>(b =>
{
    b.ToTable("MedicoReclamacionToken");
    b.HasKey(x => x.Id);
    b.HasIndex(x => x.Token).IsUnique();
    b.HasIndex(x => x.MedicoId);
    b.HasOne(x => x.Medico)
     .WithMany()
     .HasForeignKey(x => x.MedicoId)
     .OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.Usuario)
     .WithMany()
     .HasForeignKey(x => x.UserId)
     .OnDelete(DeleteBehavior.SetNull);
});
```

- [ ] **Step 1.5: Verificar compilación**

```powershell
cd "D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26"
dotnet build --no-restore 2>&1 | Select-String "error (RZ|CS)" 
```

Esperado: 0 errores de código. Solo posibles MSB30xx si la app corre.

- [ ] **Step 1.6: Commit**

```bash
git add Models/Medico/ Data/ApplicationDbContext.cs Migrations/2026-05-21_MedicoPerfilBadgesTokens.sql
git commit -m "feat(medico): entidades MedicoPerfilExtendido, MedicoBadge, Token + schema SQL"
```

---

## Task 2: IMedicoBadgeService

**Files:**
- Create: `Services/Medico/IMedicoBadgeService.cs`
- Create: `Services/Medico/MedicoBadgeService.cs`
- Modify: `Program.cs`

- [ ] **Step 2.1: Crear la interfaz**

`Services/Medico/IMedicoBadgeService.cs`:
```csharp
using eiibd26.Models.Medico;

namespace eiibd26.Services.Medico;

public interface IMedicoBadgeService
{
    Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId);
    Task<List<MedicoBadgeDto>> GetTodosLosBadgesAsync();
    Task<int> GetNivelActualAsync(int medicoId);
    Task<bool> OtorgarBadgeAsync(int medicoId, string codigo, string otorgadoPor);
    Task EvaluarBadgesAutomaticosAsync(int medicoId);
    Task<bool> TienePermisoAsync(int medicoId, string permiso);
}
```

- [ ] **Step 2.2: Implementar el servicio**

`Services/Medico/MedicoBadgeService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using eiibd26.Models.Medico;

namespace eiibd26.Services.Medico;

public class MedicoBadgeService : IMedicoBadgeService
{
    private readonly ApplicationDbContext _db;

    public MedicoBadgeService(ApplicationDbContext db) => _db = db;

    public async Task<List<MedicoBadgeDto>> GetTodosLosBadgesAsync()
    {
        var catalogo = await _db.MedicosBadge
            .AsNoTracking()
            .Where(b => b.Activo)
            .OrderBy(b => b.Orden)
            .ToListAsync();

        var ganados = await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Select(pb => new { pb.BadgeId, pb.FechaObtenido })
            .ToListAsync();

        return catalogo.Select(b =>
        {
            var ganado = ganados.FirstOrDefault(g => g.BadgeId == b.Id);
            return new MedicoBadgeDto
            {
                Id            = b.Id,
                Codigo        = b.Codigo,
                Nombre        = b.Nombre,
                Descripcion   = b.Descripcion,
                ComoObtenerlo = b.ComoObtenerlo,
                Icono         = b.Icono,
                Nivel         = b.Nivel,
                Obtenido      = ganado != null,
                FechaObtenido = ganado?.FechaObtenido
            };
        }).ToList();
    }

    public async Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId)
    {
        var ganados = await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Where(pb => pb.MedicoId == medicoId)
            .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id,
                (pb, b) => new MedicoBadgeDto
                {
                    Id            = b.Id,
                    Codigo        = b.Codigo,
                    Nombre        = b.Nombre,
                    Descripcion   = b.Descripcion,
                    ComoObtenerlo = b.ComoObtenerlo,
                    Icono         = b.Icono,
                    Nivel         = b.Nivel,
                    Obtenido      = true,
                    FechaObtenido = pb.FechaObtenido
                })
            .OrderBy(d => d.Nivel)
            .ToListAsync();

        return ganados;
    }

    public async Task<int> GetNivelActualAsync(int medicoId)
    {
        var badges = await GetBadgesGanadosAsync(medicoId);
        return badges.Count > 0 ? badges.Max(b => b.Nivel) : 0;
    }

    public async Task<bool> OtorgarBadgeAsync(int medicoId, string codigo, string otorgadoPor)
    {
        var badge = await _db.MedicosBadge.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Codigo == codigo && b.Activo);
        if (badge is null) return false;

        var yaExiste = await _db.MedicosPerfilBadge
            .AnyAsync(pb => pb.MedicoId == medicoId && pb.BadgeId == badge.Id);
        if (yaExiste) return false;

        _db.MedicosPerfilBadge.Add(new MedicoPerfilBadge
        {
            MedicoId     = medicoId,
            BadgeId      = badge.Id,
            FechaObtenido = DateTime.UtcNow,
            OtorgadoPor  = otorgadoPor
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task EvaluarBadgesAutomaticosAsync(int medicoId)
    {
        // perfil_reclamado: MedicoPerfilExtendido con UserId != null
        var tienePerfilVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == medicoId && p.UserId != null);
        if (tienePerfilVinculado)
            await OtorgarBadgeAsync(medicoId, "perfil_reclamado", "sistema");

        // activo_comunidad: >= 5 confirmaciones de pacientes
        var totalConfirmaciones = await _db.DirectorioMedicoConfirmaciones
            .CountAsync(c => c.MedicoId == medicoId && !c.Eliminado);
        if (totalConfirmaciones >= 5)
            await OtorgarBadgeAsync(medicoId, "activo_comunidad", "sistema");

        // participante_qa: >= 3 respuestas del usuario vinculado
        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicoId == medicoId && p.UserId != null);
        if (perfil?.UserId != null)
        {
            var respuestas = await _db.Respuestas
                .CountAsync(r => r.AutorId == perfil.UserId.Value);
            if (respuestas >= 3)
                await OtorgarBadgeAsync(medicoId, "participante_qa", "sistema");

            // validador_contenido: >= 5 validaciones en GlossaryValidations
            var validaciones = await _db.GlossaryValidations
                .CountAsync(v => v.UserId == perfil.UserId.Value.ToString());
            if (validaciones >= 5)
                await OtorgarBadgeAsync(medicoId, "validador_contenido", "sistema");
        }
    }

    public async Task<bool> TienePermisoAsync(int medicoId, string permiso)
    {
        var nivel = await GetNivelActualAsync(medicoId);
        return permiso switch
        {
            "editar_perfil"            => nivel >= 1,
            "ver_comentarios_anonimos" => nivel >= 2,
            "reportar_comentarios"     => nivel >= 2,
            "ver_nombre_paciente"      => nivel >= 3,
            "responder_comentarios"    => nivel >= 3,
            "participar_qa"            => nivel >= 4,
            "validar_contenido"        => nivel >= 5,
            "crear_contenido"          => nivel >= 6,
            _                          => false
        };
    }
}
```

> **Nota:** Si `Respuesta.AutorId` tiene un nombre diferente en el modelo, ajustar el nombre de la propiedad. Verificar con `grep -r "AutorId\|UsuarioId\|UserId" Models/Respuesta.cs`.

- [ ] **Step 2.3: Registrar en Program.cs**

En `Program.cs`, después de la línea `AddScoped<eiibd26.Services.Directorio.IMedicoDirectorioService...>`:

```csharp
builder.Services.AddScoped<eiibd26.Services.Medico.IMedicoBadgeService, eiibd26.Services.Medico.MedicoBadgeService>();
```

- [ ] **Step 2.4: Verificar compilación**

```powershell
dotnet build --no-restore 2>&1 | Select-String "error (RZ|CS)"
```

Esperado: 0 errores. Si `Respuesta.AutorId` no existe, corrija el nombre de propiedad antes de continuar.

- [ ] **Step 2.5: Commit**

```bash
git add Services/Medico/ Program.cs
git commit -m "feat(medico): IMedicoBadgeService con evaluación automática de badges"
```

---

## Task 3: RegisterM — Registro de médico

**Files:**
- Create: `Areas/Identity/Pages/Account/RegisterM.cshtml`
- Create: `Areas/Identity/Pages/Account/RegisterM.cshtml.cs`
- Modify: `Areas/Identity/Pages/Account/Register.cshtml`

- [ ] **Step 3.1: Crear la vista RegisterM.cshtml**

`Areas/Identity/Pages/Account/RegisterM.cshtml`:
```cshtml
@page
@model eiibd26.Areas.Identity.Pages.Account.RegisterMModel
@{
    ViewData["Title"] = "Registro Médico";
}

<style>
    .validation-summary-valid { display: none !important; }
</style>

<div class="account-page-root">
    <div class="account-card">
        <h2 class="perfil-title">Registro de médico</h2>
        <p class="text-muted mb-3" style="font-size:.9rem;">
            Crea tu cuenta para reclamar o gestionar tu perfil en el directorio EII.
        </p>

        @if (TempData["Success"] != null)
        { <div class="alert alert-success" role="alert">@TempData["Success"]</div> }
        @if (TempData["Error"] != null)
        { <div class="alert alert-danger" role="alert">@TempData["Error"]</div> }

        <form id="registerM-form" method="post">
            @Html.AntiForgeryToken()
            <input type="hidden" asp-for="ReturnUrl" />
            <div asp-validation-summary="All" class="text-danger mb-3" role="alert"></div>

            <div class="form-floating mb-3">
                <input asp-for="Input.Email" class="form-control" placeholder="Correo electrónico" />
                <label asp-for="Input.Email">Correo electrónico</label>
                <span asp-validation-for="Input.Email" class="text-danger"></span>
            </div>

            <div class="form-floating mb-3">
                <input asp-for="Input.Password" class="form-control" autocomplete="new-password" placeholder="Contraseña" />
                <label asp-for="Input.Password">Contraseña</label>
                <span asp-validation-for="Input.Password" class="text-danger"></span>
            </div>

            <div class="form-floating mb-3">
                <input asp-for="Input.ConfirmPassword" class="form-control" autocomplete="new-password" placeholder="Confirmar contraseña" />
                <label asp-for="Input.ConfirmPassword">Confirmar contraseña</label>
                <span asp-validation-for="Input.ConfirmPassword" class="text-danger"></span>
            </div>

            <div class="form-floating mb-3">
                <select asp-for="Input.PaisCodigo" asp-items="Model.PaisesSelectList" class="form-select">
                    <option value="">Selecciona país...</option>
                </select>
                <label asp-for="Input.PaisCodigo">País</label>
                <span asp-validation-for="Input.PaisCodigo" class="text-danger"></span>
            </div>

            <div class="form-floating mb-3">
                <input asp-for="Input.Especialidad" class="form-control" placeholder="Especialidad" />
                <label asp-for="Input.Especialidad">Especialidad</label>
                <span asp-validation-for="Input.Especialidad" class="text-danger"></span>
            </div>

            <div class="form-floating mb-3">
                <input asp-for="Input.CedulaProfesional" class="form-control" placeholder="Cédula profesional (opcional)" />
                <label asp-for="Input.CedulaProfesional">Cédula profesional <span class="text-muted">(para agilizar verificación)</span></label>
                <span asp-validation-for="Input.CedulaProfesional" class="text-danger"></span>
            </div>

            <div class="d-flex justify-content-center mt-lg-4">
                <button type="submit" class="w-100 btn btn-lg btn-primary">Crear cuenta médica</button>
            </div>
        </form>

        <div class="mt-3 text-center">
            <a class="link-muted" asp-area="Identity" asp-page="/Account/Register">
                ¿Eres paciente? Regístrate aquí
            </a>
        </div>
        <div class="mt-2 text-center">
            <a class="link-muted" asp-area="Identity" asp-page="/Account/Login">
                ¿Ya tienes cuenta? Inicia sesión
            </a>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 3.2: Crear el PageModel RegisterM.cshtml.cs**

`Areas/Identity/Pages/Account/RegisterM.cshtml.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Medico;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Account;

public class RegisterMModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterMModel> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;

    public RegisterMModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterMModel> logger,
        ApplicationDbContext db,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _db = db;
        _emailSender = emailSender;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public SelectList PaisesSelectList { get; set; } = new(Enumerable.Empty<object>());
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un país.")]
        [Display(Name = "País")]
        public string PaisCodigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La especialidad es requerida.")]
        [MaxLength(200)]
        [Display(Name = "Especialidad")]
        public string Especialidad { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Cédula profesional")]
        public string? CedulaProfesional { get; set; }
    }

    public async Task OnGetAsync()
    {
        ReturnUrl = Request.Query["returnUrl"].FirstOrDefault() ?? "/";
        await PopulatePaisesAsync();
    }

    private async Task PopulatePaisesAsync()
    {
        try
        {
            var paises = await _db.Paises
                .Where(p => !p.Borrado && p.VIsibleBuscador)
                .OrderBy(p => p.PaisNombre)
                .ToListAsync();
            PaisesSelectList = new SelectList(paises, "PaisCodigo", "PaisNombre");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando países para RegisterM.");
            PaisesSelectList = new SelectList(Enumerable.Empty<object>());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = Request.Form["ReturnUrl"].FirstOrDefault() ?? "/";
        await PopulatePaisesAsync();

        if (!ModelState.IsValid) return Page();

        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return Page();
        }

        try
        {
            string codigoPais = Input.PaisCodigo.Trim().ToLowerInvariant();
            var emailLocal = (user.Email ?? "medico").Split('@')[0];

            var perfil = new Perfil
            {
                idUser           = user.Id,
                Avatar           = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110",
                Titulo           = string.Empty,
                Nombre           = string.Empty,
                Apellidos        = string.Empty,
                FechaCreacion    = DateTime.UtcNow,
                UltimaActividad  = DateTime.UtcNow,
                NombrePais       = codigoPais,
                FechaCreado      = DateTime.UtcNow,
                PermitirTelefonoReal    = true,
                PermitirCorreoNoticias  = true,
                PermitirMostrarPais     = true
            };

            try { perfil.slug = await GenerateUniqueSlugAsync(emailLocal); }
            catch (Exception ex) { _logger.LogWarning(ex, "No se pudo generar slug para {Email}.", emailLocal); }

            _db.Perfil.Add(perfil);

            // MedicoPerfilExtendido vacío — se vincula a MedicosDirectorio vía flujo Activar
            _db.MedicosPerfilExtendido.Add(new MedicoPerfilExtendido
            {
                MedicoId   = null,
                UserId     = user.Id,
                FechaCreado = DateTime.UtcNow,
                FechaModificado = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _userManager.AddToRoleAsync(user, "Medico");
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email!));

            _logger.LogInformation("Médico registrado: {Email}", user.Email);

            // Email de confirmación (no bloquea si falla)
            try
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email,
                    "Confirma tu correo — EIIBD",
                    $"<p>Bienvenido al directorio médico de EIIBD.</p>" +
                    $"<p>Por favor <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>confirma tu cuenta</a>.</p>");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error enviando confirmación a {Email}.", user.Email); }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Account/Manage/PerfilMedico", new { area = "Identity" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completando registro médico para {Email}. Revirtiendo.", user.Email);
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al completar el registro. Intenta de nuevo.");
            return Page();
        }
    }

    private string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.ToLowerInvariant().Trim();
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        text = sb.ToString().Normalize(NormalizationForm.FormC);
        text = Regex.Replace(text, @"[^a-z0-9]+", "-");
        text = Regex.Replace(text, @"-+", "-").Trim('-');
        return text;
    }

    private async Task<string> GenerateUniqueSlugAsync(string baseText)
    {
        var baseSlug = string.IsNullOrWhiteSpace(Slugify(baseText)) ? "medico" : Slugify(baseText);
        string candidate = baseSlug;
        int suffix = 0;
        while (await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == candidate))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
            if (suffix > 10000) break;
        }
        return candidate;
    }
}
```

- [ ] **Step 3.3: Agregar link en Register.cshtml**

En `Areas/Identity/Pages/Account/Register.cshtml`, después del botón "Crear cuenta" y antes del cierre `</form>`, o después del enlace de login, agregar:

```cshtml
<div class="mt-2 text-center">
    <a class="link-muted" asp-area="Identity" asp-page="/Account/RegisterM">
        ¿Eres médico? Regístrate aquí
    </a>
</div>
```

- [ ] **Step 3.4: Verificar que el rol "Medico" existe**

En `Program.cs`, buscar el bloque donde se siembran roles (buscar "Paciente" con grep). Si no existe un seed para "Medico", agregar junto al de "Paciente":

```csharp
// Buscar bloque existente de seeding de roles y agregar:
if (!await roleManager.RoleExistsAsync("Medico"))
    await roleManager.CreateAsync(new ApplicationRole { Name = "Medico" });
```

Si no hay seeding en Program.cs, agregar inmediatamente después de `app.Run()` preparación (buscar el patrón de seeding en el proyecto).

- [ ] **Step 3.5: Verificar compilación**

```powershell
dotnet build --no-restore 2>&1 | Select-String "error (RZ|CS)"
```

- [ ] **Step 3.6: Commit**

```bash
git add Areas/Identity/Pages/Account/RegisterM.cshtml Areas/Identity/Pages/Account/RegisterM.cshtml.cs Areas/Identity/Pages/Account/Register.cshtml Program.cs
git commit -m "feat(medico): RegisterM — registro de cuenta médica con rol Medico"
```

---

## Task 4: Flujo de Reclamación — Reclamar + Activar

**Files:**
- Create: `Pages/Directorio/Reclamar.cshtml`
- Create: `Pages/Directorio/Reclamar.cshtml.cs`
- Create: `Pages/Directorio/Activar.cshtml`
- Create: `Pages/Directorio/Activar.cshtml.cs`
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml`
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml.cs`

- [ ] **Step 4.1: Crear Pages/Directorio/Reclamar.cshtml**

```cshtml
@page "/directorio/reclamar/{medicoId:int}"
@model eiibd26.Pages.Directorio.ReclamarModel
@{
    ViewData["Title"] = "Reclamar perfil de médico";
    Layout = "_Layout";
}

@section Styles {
    <link rel="stylesheet" href="~/css/directorio-medicos.css" asp-append-version="true" />
}

<div class="container py-4">
    <div class="row justify-content-center">
        <div class="col-lg-6">
            <div class="crm-card">
                <h2 class="crm-title mb-1">Reclamar perfil de médico</h2>
                <p class="text-muted mb-4">
                    Recibirás un enlace por correo para completar el proceso.
                </p>

                @if (TempData["Success"] is string success)
                { <div class="alert alert-success"><i class="bi bi-check-circle me-2"></i>@success</div> }
                @if (TempData["Error"] is string error)
                { <div class="alert alert-danger">@error</div> }

                @if (Model.Medico is not null)
                {
                    <div class="d-flex gap-3 align-items-center mb-4 p-3" style="background:#f9fafb;border-radius:8px;">
                        <div class="medico-card__avatar">
                            <i class="bi bi-person-badge-fill"></i>
                        </div>
                        <div>
                            <div class="fw-semibold">@Model.Medico.NombreCompleto</div>
                            @if (!string.IsNullOrWhiteSpace(Model.Medico.Especialidad))
                            { <div class="text-muted small">@Model.Medico.Especialidad</div> }
                        </div>
                    </div>

                    <form method="post">
                        @Html.AntiForgeryToken()
                        <input type="hidden" asp-for="MedicoId" />
                        <div class="form-floating mb-3">
                            <input asp-for="Email" type="email" class="form-control" placeholder="Tu correo electrónico" />
                            <label asp-for="Email">Tu correo electrónico</label>
                            <span asp-validation-for="Email" class="text-danger"></span>
                        </div>
                        <button type="submit" class="btn btn-primary w-100">
                            <i class="bi bi-envelope me-1"></i> Enviar enlace de verificación
                        </button>
                    </form>
                }
                else
                {
                    <div class="alert alert-warning">Médico no encontrado.</div>
                }

                <div class="text-center mt-3">
                    <a asp-page="/DirectorioMedicos/Detalle" asp-route-id="@Model.MedicoId"
                       class="text-muted small">← Volver al perfil</a>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

- [ ] **Step 4.2: Crear Pages/Directorio/Reclamar.cshtml.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using eiibd26.Models.Directorio;
using eiibd26.Models.Medico;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Directorio;

public class ReclamarModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReclamarModel> _logger;

    public ReclamarModel(ApplicationDbContext db, IEmailSender emailSender, ILogger<ReclamarModel> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int MedicoId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "El correo es requerido.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    public MedicoDirectorio? Medico { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MedicoId && m.Activo && !m.Eliminado);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MedicoId && m.Activo && !m.Eliminado);

        if (Medico is null)
        {
            TempData["Error"] = "Médico no encontrado.";
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        // Verificar si ya está vinculado
        var yaVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == MedicoId && p.UserId != null);
        if (yaVinculado)
        {
            TempData["Error"] = "Este perfil ya fue reclamado por un médico verificado.";
            return Page();
        }

        // Invalidar tokens activos previos para este médico
        var tokensActivos = await _db.MedicoReclamacionTokens
            .Where(t => t.MedicoId == MedicoId && t.Activo && t.FechaUsado == null)
            .ToListAsync();
        foreach (var t in tokensActivos) t.Activo = false;

        // Crear nuevo token
        var token = Guid.NewGuid().ToString("N");
        _db.MedicoReclamacionTokens.Add(new MedicoReclamacionToken
        {
            MedicoId     = MedicoId,
            Token        = token,
            EmailDestino = Email.Trim().ToLowerInvariant(),
            FechaCreado  = DateTime.UtcNow,
            FechaExpira  = DateTime.UtcNow.AddHours(72),
            Activo       = true
        });
        await _db.SaveChangesAsync();

        // Enviar email
        try
        {
            var link = $"{Request.Scheme}://{Request.Host}/directorio/activar?token={token}";
            await _emailSender.SendEmailAsync(Email,
                "Enlace para reclamar tu perfil — EIIBD",
                $"<p>Hola,</p>" +
                $"<p>Recibimos tu solicitud para reclamar el perfil de <strong>{HtmlEncoder.Default.Encode(Medico.NombreCompleto)}</strong> en el directorio EII.</p>" +
                $"<p><a href='{link}'>Haz clic aquí para completar la verificación</a></p>" +
                $"<p>Este enlace expira en <strong>72 horas</strong>.</p>" +
                $"<p>Si no solicitaste esto, ignora este mensaje.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de reclamación a {Email}", Email);
        }

        TempData["Success"] = $"Te enviamos un correo a {Email}. El link expira en 72 horas.";
        return RedirectToPage(new { medicoId = MedicoId });
    }
}
```

- [ ] **Step 4.3: Crear Pages/Directorio/Activar.cshtml**

```cshtml
@page "/directorio/activar"
@model eiibd26.Pages.Directorio.ActivarModel
@{
    ViewData["Title"] = "Activar perfil médico";
    Layout = "_Layout";
}

@section Styles {
    <link rel="stylesheet" href="~/css/directorio-medicos.css" asp-append-version="true" />
}

<div class="container py-4">
    <div class="row justify-content-center">
        <div class="col-lg-5">
            <div class="crm-card">
                <h2 class="crm-title mb-3">Activar perfil médico</h2>

                @if (Model.Estado == "invalid")
                {
                    <div class="alert alert-danger">
                        <i class="bi bi-x-circle me-2"></i>
                        Este enlace no es válido. Puede que haya expirado o ya fue utilizado.
                    </div>
                    <a asp-page="/DirectorioMedicos/Index" class="btn btn-outline-secondary">
                        Volver al directorio
                    </a>
                }
                else if (Model.Estado == "expired")
                {
                    <div class="alert alert-warning">
                        <i class="bi bi-clock me-2"></i>
                        Este enlace ha expirado.
                    </div>
                    <a href="/directorio/reclamar/@Model.TokenData!.MedicoId" class="btn btn-primary">
                        Solicitar nuevo enlace
                    </a>
                }
                else if (Model.Estado == "used")
                {
                    <div class="alert alert-info">
                        <i class="bi bi-info-circle me-2"></i>
                        Este enlace ya fue utilizado.
                    </div>
                    <a asp-page="/Account/Manage/PerfilMedico" asp-area="Identity" class="btn btn-primary">
                        Ir a mi perfil médico
                    </a>
                }
                else if (Model.Estado == "vinculado")
                {
                    <div class="alert alert-success">
                        <i class="bi bi-check-circle me-2"></i>
                        Tu perfil ha sido vinculado correctamente.
                    </div>
                    <a asp-page="/Account/Manage/PerfilMedico" asp-area="Identity" class="btn btn-primary">
                        Completar mi perfil médico
                    </a>
                }
                else if (Model.Estado == "login_requerido")
                {
                    <p class="text-muted mb-4">
                        Ingresa una contraseña para crear tu cuenta con el correo
                        <strong>@Model.TokenData!.EmailDestino</strong>.
                    </p>
                    <form method="post">
                        @Html.AntiForgeryToken()
                        <input type="hidden" asp-for="Token" />
                        <div asp-validation-summary="All" class="text-danger mb-3"></div>
                        <div class="form-floating mb-3">
                            <input asp-for="Password" type="password" class="form-control" placeholder="Contraseña" autocomplete="new-password" />
                            <label asp-for="Password">Contraseña</label>
                            <span asp-validation-for="Password" class="text-danger"></span>
                        </div>
                        <div class="form-floating mb-3">
                            <input asp-for="ConfirmPassword" type="password" class="form-control" placeholder="Confirmar contraseña" autocomplete="new-password" />
                            <label asp-for="ConfirmPassword">Confirmar contraseña</label>
                            <span asp-validation-for="ConfirmPassword" class="text-danger"></span>
                        </div>
                        <button type="submit" class="btn btn-primary w-100">
                            Crear cuenta y activar perfil
                        </button>
                    </form>
                }
            </div>
        </div>
    </div>
</div>

@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

- [ ] **Step 4.4: Crear Pages/Directorio/Activar.cshtml.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using eiibd26.Models;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Directorio;

public class ActivarModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMedicoBadgeService _badgeService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ActivarModel> _logger;

    public ActivarModel(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMedicoBadgeService badgeService,
        IEmailSender emailSender,
        ILogger<ActivarModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _badgeService = badgeService;
        _emailSender = emailSender;
        _logger = logger;
    }

    // "invalid" | "expired" | "used" | "vinculado" | "login_requerido"
    public string Estado { get; set; } = "invalid";

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public MedicoReclamacionToken? TokenData { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) { Estado = "invalid"; return Page(); }

        TokenData = await _db.MedicoReclamacionTokens
            .FirstOrDefaultAsync(t => t.Token == Token);

        if (TokenData is null || !TokenData.Activo) { Estado = "invalid"; return Page(); }
        if (TokenData.FechaUsado.HasValue)          { Estado = "used";    return Page(); }
        if (TokenData.FechaExpira < DateTime.UtcNow){ Estado = "expired"; return Page(); }

        // Si el usuario ya está autenticado con rol Medico, vincular directamente
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Medico"))
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await VincularAsync(TokenData, userId);
            Estado = "vinculado";
            return Page();
        }

        Estado = "login_requerido";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) { Estado = "invalid"; return Page(); }

        TokenData = await _db.MedicoReclamacionTokens
            .FirstOrDefaultAsync(t => t.Token == Token);

        if (TokenData is null || !TokenData.Activo) { Estado = "invalid"; return Page(); }
        if (TokenData.FechaUsado.HasValue)          { Estado = "used";    return Page(); }
        if (TokenData.FechaExpira < DateTime.UtcNow){ Estado = "expired"; return Page(); }

        if (!ModelState.IsValid) { Estado = "login_requerido"; return Page(); }

        // Crear nueva cuenta con el email del token
        var email = TokenData.EmailDestino;
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            Estado = "login_requerido";
            return Page();
        }

        try
        {
            var emailLocal = email.Split('@')[0];
            var perfil = new Perfil
            {
                idUser          = user.Id,
                Avatar          = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(emailLocal)}&size=110",
                Titulo          = string.Empty,
                Nombre          = string.Empty,
                Apellidos       = string.Empty,
                FechaCreacion   = DateTime.UtcNow,
                UltimaActividad = DateTime.UtcNow,
                FechaCreado     = DateTime.UtcNow,
                PermitirTelefonoReal   = true,
                PermitirCorreoNoticias = true,
                PermitirMostrarPais    = true
            };
            _db.Perfil.Add(perfil);
            await _db.SaveChangesAsync();

            await _userManager.AddToRoleAsync(user, "Medico");
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, email));

            await VincularAsync(TokenData, user.Id);
            await _signInManager.SignInAsync(user, isPersistent: false);

            Estado = "vinculado";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vinculando perfil médico para {Email}", email);
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Error al vincular el perfil. Intenta de nuevo.");
            Estado = "login_requerido";
            return Page();
        }
    }

    private async Task VincularAsync(MedicoReclamacionToken tokenData, Guid userId)
    {
        // Upsert MedicoPerfilExtendido
        var perfil = await _db.MedicosPerfilExtendido
            .FirstOrDefaultAsync(p => p.MedicoId == tokenData.MedicoId);

        if (perfil is null)
        {
            _db.MedicosPerfilExtendido.Add(new MedicoPerfilExtendido
            {
                MedicoId        = tokenData.MedicoId,
                UserId          = userId,
                FechaCreado     = DateTime.UtcNow,
                FechaModificado = DateTime.UtcNow
            });
        }
        else
        {
            perfil.UserId          = userId;
            perfil.FechaModificado = DateTime.UtcNow;
        }

        // Marcar token como usado
        tokenData.FechaUsado = DateTime.UtcNow;
        tokenData.Activo     = false;
        tokenData.UserId     = userId;

        await _db.SaveChangesAsync();

        // Otorgar badges
        await _badgeService.OtorgarBadgeAsync(tokenData.MedicoId, "perfil_reclamado", "sistema");
        await _badgeService.EvaluarBadgesAutomaticosAsync(tokenData.MedicoId);
    }
}
```

- [ ] **Step 4.5: Actualizar Detalle.cshtml.cs — agregar PerfilYaVinculado**

En `Pages/DirectorioMedicos/Detalle.cshtml.cs`, agregar la propiedad pública:

```csharp
public bool PerfilYaVinculado { get; set; }
```

Y en `OnGetAsync`, después de cargar `Medico`, agregar:

```csharp
PerfilYaVinculado = await _db.MedicosPerfilExtendido
    .AnyAsync(p => p.MedicoId == id && p.UserId != null);
```

- [ ] **Step 4.6: Actualizar Detalle.cshtml — nuevo botón reclamar**

En `Pages/DirectorioMedicos/Detalle.cshtml`, reemplazar el bloque completo de reclamación (las dos condiciones `@if ... @else if` que muestran "Reclama tu perfil") por:

```cshtml
@if (!Model.PerfilYaVinculado)
{
    <div class="mt-3">
        <a href="/directorio/reclamar/@Model.Medico.Id" class="btn btn-outline-primary">
            <i class="bi bi-person-check me-1"></i> ¿Eres este médico? Reclamar perfil
        </a>
    </div>
}
```

- [ ] **Step 4.7: Verificar compilación**

```powershell
dotnet build --no-restore 2>&1 | Select-String "error (RZ|CS)"
```

- [ ] **Step 4.8: Commit**

```bash
git add Pages/Directorio/ Pages/DirectorioMedicos/Detalle.cshtml Pages/DirectorioMedicos/Detalle.cshtml.cs
git commit -m "feat(medico): flujo reclamación perfil — Reclamar + Activar por token 72h"
```

---

## Task 5: PerfilMedico en Manage + ManageNav

**Files:**
- Modify: `Areas/Identity/Pages/Account/Manage/ManageNavPages.cs`
- Modify: `Areas/Identity/Pages/Account/Manage/_ManageNav.cshtml`
- Create: `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml`
- Create: `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs`

- [ ] **Step 5.1: Agregar PerfilMedico a ManageNavPages.cs**

En `Areas/Identity/Pages/Account/Manage/ManageNavPages.cs`, agregar después de la línea `public static string UsuarioPerfil => "UsuarioPerfil";`:

```csharp
public static string PerfilMedico => "PerfilMedico";
public static string PerfilMedicoNavClass(ViewContext viewContext) => PageNavClass(viewContext, PerfilMedico);
```

- [ ] **Step 5.2: Agregar ítem en _ManageNav.cshtml**

En `Areas/Identity/Pages/Account/Manage/_ManageNav.cshtml`, dentro de `<ul class="manage-nav-list">`, agregar como último `<li>`:

```cshtml
@if (User.IsInRole("Medico"))
{
    <li class="manage-nav-item" role="listitem">
        <a class="manage-nav-link @(IsPage("/Account/Manage/PerfilMedico") ? "active" : "")"
           asp-area="Identity" asp-page="/Account/Manage/PerfilMedico">
            <i class="bi bi-hospital me-1"></i> Perfil Médico
        </a>
    </li>
}
```

- [ ] **Step 5.3: Crear PerfilMedico.cshtml.cs**

`Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace eiibd26.Areas.Identity.Pages.Account.Manage;

[Authorize(Roles = "Medico")]
public class PerfilMedicoModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IMedicoBadgeService _badgeService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PerfilMedicoModel> _logger;

    public PerfilMedicoModel(
        ApplicationDbContext db,
        IMedicoBadgeService badgeService,
        IWebHostEnvironment env,
        ILogger<PerfilMedicoModel> logger)
    {
        _db = db;
        _badgeService = badgeService;
        _env = env;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public List<MedicoBadgeDto> TodosLosBadges { get; set; } = new();
    public int NivelActual { get; set; }
    public int? MedicoDirectorioId { get; set; }
    public bool PerfilVinculado { get; set; }
    public List<SelectListItem> AreasEiiDisponibles { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [MaxLength(500)] public string? Foto { get; set; }
        [MaxLength(200)] public string? NombreCompleto { get; set; }
        [MaxLength(200)] public string? Especialidad { get; set; }
        [MaxLength(2000)] public string? Biografia { get; set; }
        public List<string> Hospitales { get; set; } = new();
        [MaxLength(500)] public string? HorariosAtencion { get; set; }
        [MaxLength(300)] public string? SitioWeb { get; set; }
        [MaxLength(50)] public string? Telefono { get; set; }
        [MaxLength(150)] public string? Instagram { get; set; }
        [MaxLength(150)] public string? LinkedIn { get; set; }
        [MaxLength(100)] public string? Slug { get; set; }
        public List<int> AreasSeleccionadas { get; set; } = new();

        [Display(Name = "Foto de perfil")]
        public IFormFile? FotoFile { get; set; }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["ActivePage"] = ManageNavPages.PerfilMedico;
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is not null)
        {
            PerfilVinculado   = perfil.MedicoId.HasValue;
            MedicoDirectorioId = perfil.MedicoId;

            Input.Foto             = perfil.Foto;
            Input.Biografia        = perfil.Biografia;
            Input.HorariosAtencion = perfil.HorariosAtencion;
            Input.SitioWeb         = perfil.SitioWeb;
            Input.Telefono         = perfil.Telefono;
            Input.Instagram        = perfil.Instagram;
            Input.LinkedIn         = perfil.LinkedIn;
            Input.Slug             = perfil.Slug;
            Input.NombreCompleto   = perfil.Medico?.NombreCompleto;
            Input.Especialidad     = perfil.Medico?.Especialidad;

            if (!string.IsNullOrWhiteSpace(perfil.Hospitales))
                try { Input.Hospitales = JsonSerializer.Deserialize<List<string>>(perfil.Hospitales) ?? new(); }
                catch { Input.Hospitales = new(); }

            if (perfil.MedicoId.HasValue)
            {
                TodosLosBadges = await GetBadgesConContextoAsync(perfil.MedicoId.Value);
                NivelActual    = await _badgeService.GetNivelActualAsync(perfil.MedicoId.Value);

                var areasVinculadas = await _db.MedicoExperienciaEii
                    .AsNoTracking()
                    .Where(e => e.MedicoDirectorioId == perfil.MedicoId.Value && !e.Eliminado)
                    .Select(e => e.AreaExperienciaEiiId)
                    .ToListAsync();
                Input.AreasSeleccionadas = areasVinculadas;
            }
        }

        await PopulateAreasAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["ActivePage"] = ManageNavPages.PerfilMedico;
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is null)
        {
            perfil = new MedicoPerfilExtendido { UserId = userId, FechaCreado = DateTime.UtcNow };
            _db.MedicosPerfilExtendido.Add(perfil);
        }

        // Procesar foto si se subió
        if (Input.FotoFile is { Length: > 0 })
        {
            try
            {
                var fileName  = $"medico-{userId:N}.jpg";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "medicos");
                Directory.CreateDirectory(uploadsDir);
                var filePath = Path.Combine(uploadsDir, fileName);

                using var image = await SixLabors.ImageSharp.Image.LoadAsync(Input.FotoFile.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(400, 400), Mode = ResizeMode.Crop }));
                await image.SaveAsJpegAsync(filePath);

                perfil.Foto   = $"/uploads/medicos/{fileName}";
                Input.Foto    = perfil.Foto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando foto de perfil médico para {UserId}", userId);
            }
        }

        perfil.Biografia        = Input.Biografia;
        perfil.Hospitales       = Input.Hospitales.Any(h => !string.IsNullOrWhiteSpace(h))
            ? JsonSerializer.Serialize(Input.Hospitales.Where(h => !string.IsNullOrWhiteSpace(h)).ToList())
            : null;
        perfil.HorariosAtencion = Input.HorariosAtencion;
        perfil.SitioWeb         = Input.SitioWeb;
        perfil.Telefono         = Input.Telefono;
        perfil.Instagram        = Input.Instagram;
        perfil.LinkedIn         = Input.LinkedIn;
        perfil.FechaModificado  = DateTime.UtcNow;

        // Slug (solo si cambió)
        if (!string.IsNullOrWhiteSpace(Input.Slug) && Input.Slug != perfil.Slug)
        {
            var slugLimpio = Input.Slug.Trim().ToLowerInvariant();
            var ocupado    = await _db.MedicosPerfilExtendido
                .AnyAsync(p => p.Slug == slugLimpio && p.UserId != userId);
            if (ocupado)
                ModelState.AddModelError("Input.Slug", "Este slug ya está en uso.");
            else
                perfil.Slug = slugLimpio;
        }

        await _db.SaveChangesAsync();

        if (perfil.MedicoId.HasValue)
            await _badgeService.EvaluarBadgesAutomaticosAsync(perfil.MedicoId.Value);

        SuccessMessage = "Perfil actualizado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetCheckSlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return new JsonResult(new { disponible = false });
        var userId  = GetUserId();
        var ocupado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.Slug == slug.Trim().ToLowerInvariant() && p.UserId != userId);
        return new JsonResult(new { disponible = !ocupado });
    }

    public async Task<IActionResult> OnGetGenerateSlugAsync()
    {
        var userId = GetUserId();
        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var baseName = perfil?.Medico?.NombreCompleto ?? "medico";
        var suggestions = new List<string>();
        var parts  = baseName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) suggestions.Add($"dr-{parts[0]}-{parts[^1]}");
        suggestions.Add($"dr-{string.Join("-", parts.Take(2))}");
        suggestions.Add($"{string.Join("-", parts)}");

        return new JsonResult(suggestions.Distinct().Take(3).ToList());
    }

    private async Task PopulateAreasAsync()
    {
        var areas = await _db.condiciones
            .AsNoTracking()
            .Where(c => c.idPadre == null && !c.Eliminado)
            .OrderBy(c => c.nombre)
            .ToListAsync();
        AreasEiiDisponibles = areas.Select(a => new SelectListItem(a.nombre, a.id.ToString())).ToList();
    }

    private async Task<List<MedicoBadgeDto>> GetBadgesConContextoAsync(int medicoId)
    {
        var catalogo = await _db.MedicosBadge
            .AsNoTracking()
            .Where(b => b.Activo)
            .OrderBy(b => b.Orden)
            .ToListAsync();

        var ganados = await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Where(pb => pb.MedicoId == medicoId)
            .ToListAsync();

        return catalogo.Select(b =>
        {
            var ganado = ganados.FirstOrDefault(g => g.BadgeId == b.Id);
            return new MedicoBadgeDto
            {
                Id            = b.Id,
                Codigo        = b.Codigo,
                Nombre        = b.Nombre,
                Descripcion   = b.Descripcion,
                ComoObtenerlo = b.ComoObtenerlo,
                Icono         = b.Icono,
                Nivel         = b.Nivel,
                Obtenido      = ganado != null,
                FechaObtenido = ganado?.FechaObtenido
            };
        }).ToList();
    }
}
```

- [ ] **Step 5.4: Crear PerfilMedico.cshtml**

`Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml`:
```cshtml
@page
@model eiibd26.Areas.Identity.Pages.Account.Manage.PerfilMedicoModel
@{
    ViewData["Title"] = "Perfil Médico";
}

@section Styles {
    <link rel="stylesheet" href="~/css/avatar-card.css" />
    <link rel="stylesheet" href="~/css/perfil.css" />
}

<div class="manage-page-root container py-4">
    @await Html.PartialAsync("_ManageNav")

    <div class="manage-nav-content">
        <h2>Perfil Médico</h2>

        @if (TempData["SuccessMessage"] is string sm)
        { <div class="alert alert-success alert-dismissible fade show"><i class="bi bi-check-circle me-2"></i>@sm<button class="btn-close" data-bs-dismiss="alert"></button></div> }
        @if (TempData["ErrorMessage"] is string em)
        { <div class="alert alert-danger alert-dismissible fade show">@em<button class="btn-close" data-bs-dismiss="alert"></button></div> }

        @if (!Model.PerfilVinculado)
        {
            <div class="alert alert-info mb-4">
                <i class="bi bi-info-circle me-2"></i>
                Tu cuenta aún no está vinculada a un perfil del directorio.
                <a href="/DirectorioMedicos/Index" class="alert-link ms-1">Busca tu perfil</a> y haz clic en "Reclamar perfil".
            </div>
        }

        <form method="post" enctype="multipart/form-data">
            @Html.AntiForgeryToken()

            @* ── Bloque 1: Foto y datos básicos ── *@
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3"><i class="bi bi-person-circle"></i> Foto y datos básicos</h6>
                <div class="row g-3">
                    <div class="col-md-3 text-center">
                        <img src="@(string.IsNullOrWhiteSpace(Model.Input.Foto) ? "https://ui-avatars.com/api/?name=M&size=120" : Model.Input.Foto)"
                             class="rounded-circle" width="100" height="100" style="object-fit:cover;" />
                        <div class="mt-2">
                            <input asp-for="Input.FotoFile" type="file" class="form-control form-control-sm" accept="image/*" />
                        </div>
                    </div>
                    <div class="col-md-9">
                        <div class="form-floating mb-2">
                            <input asp-for="Input.NombreCompleto" class="form-control" placeholder="Nombre completo"
                                   readonly="@(Model.PerfilVinculado ? "readonly" : null)" />
                            <label asp-for="Input.NombreCompleto">Nombre completo</label>
                        </div>
                        <div class="form-floating">
                            <input asp-for="Input.Especialidad" class="form-control" placeholder="Especialidad"
                                   readonly="@(Model.PerfilVinculado ? "readonly" : null)" />
                            <label asp-for="Input.Especialidad">Especialidad</label>
                        </div>
                    </div>
                </div>
            </div>

            @* ── Bloque 2: Información profesional ── *@
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3"><i class="bi bi-clipboard2-pulse"></i> Información profesional</h6>

                <label class="perfil-label mb-2">Áreas EII en las que tienes experiencia</label>
                <div class="row g-1 mb-3">
                    @foreach (var area in Model.AreasEiiDisponibles)
                    {
                        <div class="col-6 col-md-4">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox"
                                       name="Input.AreasSeleccionadas"
                                       value="@area.Value"
                                       id="area-@area.Value"
                                       checked="@(Model.Input.AreasSeleccionadas.Contains(int.Parse(area.Value)) ? "checked" : null)" />
                                <label class="form-check-label" for="area-@area.Value">@area.Text</label>
                            </div>
                        </div>
                    }
                </div>

                <label class="perfil-label mb-1">Hospital(es) donde atiendes</label>
                <div id="hospitales-container" class="mb-2">
                    @for (int i = 0; i < Math.Max(1, Model.Input.Hospitales.Count); i++)
                    {
                        <div class="input-group mb-1">
                            <input type="text" name="Input.Hospitales"
                                   class="form-control form-control-sm"
                                   placeholder="Nombre del hospital o clínica"
                                   value="@(i < Model.Input.Hospitales.Count ? Model.Input.Hospitales[i] : "")" />
                            <button type="button" class="btn btn-outline-secondary btn-sm"
                                    onclick="this.closest('.input-group').remove()">✕</button>
                        </div>
                    }
                </div>
                <button type="button" class="btn btn-outline-secondary btn-sm"
                        onclick="addHospital()"><i class="bi bi-plus-sm"></i> Agregar hospital</button>

                <div class="form-floating mt-3">
                    <textarea asp-for="Input.HorariosAtencion" class="form-control" placeholder="Horarios"
                              style="height:80px;"></textarea>
                    <label asp-for="Input.HorariosAtencion">Horarios de atención</label>
                </div>
            </div>

            @* ── Bloque 3: Contacto y redes ── *@
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3"><i class="bi bi-link-45deg"></i> Contacto y redes</h6>
                <div class="row g-2">
                    <div class="col-md-6">
                        <div class="form-floating">
                            <input asp-for="Input.SitioWeb" class="form-control" placeholder="Sitio web" />
                            <label asp-for="Input.SitioWeb">Sitio web</label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-floating">
                            <input asp-for="Input.Telefono" class="form-control" placeholder="Teléfono" />
                            <label asp-for="Input.Telefono">Teléfono</label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-floating">
                            <input asp-for="Input.Instagram" class="form-control" placeholder="Instagram" />
                            <label asp-for="Input.Instagram"><i class="bi bi-instagram"></i> Instagram</label>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-floating">
                            <input asp-for="Input.LinkedIn" class="form-control" placeholder="LinkedIn" />
                            <label asp-for="Input.LinkedIn"><i class="bi bi-linkedin"></i> LinkedIn</label>
                        </div>
                    </div>
                </div>
            </div>

            @* ── Bloque 4: Biografía ── *@
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3"><i class="bi bi-file-person"></i> Acerca de mí</h6>
                <div class="form-floating">
                    <textarea asp-for="Input.Biografia" class="form-control" placeholder="Biografía"
                              style="height:120px;"></textarea>
                    <label asp-for="Input.Biografia">Biografía / Presentación profesional</label>
                </div>
            </div>

            @* ── Bloque 5: URL pública ── *@
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3"><i class="bi bi-link"></i> Tu URL pública</h6>
                <p class="text-muted small mb-2">Tu perfil público: <code>https://eiibd.com/medicos/<strong id="slug-preview">@(Model.Input.Slug ?? "tu-slug")</strong></code></p>
                <div class="input-group">
                    <span class="input-group-text text-muted" style="font-size:.85rem;">/medicos/</span>
                    <input asp-for="Input.Slug" class="form-control" placeholder="tu-slug"
                           id="slug-input"
                           oninput="previewSlug(this.value)" />
                    <button type="button" class="btn btn-outline-secondary btn-sm"
                            onclick="checkSlug()">Verificar</button>
                </div>
                <span asp-validation-for="Input.Slug" class="text-danger small"></span>
                <div id="slug-status" class="small mt-1"></div>
                <div class="mt-2">
                    <button type="button" class="btn btn-outline-secondary btn-sm"
                            onclick="generateSlug()">Sugerir slug</button>
                    <div id="slug-suggestions" class="mt-1 d-flex flex-wrap gap-1"></div>
                </div>
            </div>

            <div class="d-flex gap-2 justify-content-end mb-4">
                <button type="submit" class="btn btn-primary">
                    <i class="bi bi-check2 me-1"></i> Guardar cambios
                </button>
            </div>
        </form>

        @* ── Bloque 6: Badges (solo lectura) ── *@
        @if (Model.PerfilVinculado && Model.TodosLosBadges.Any())
        {
            <div class="manage-card perfil-section-block mb-3">
                <h6 class="card-section-title mb-3">
                    <i class="bi bi-award"></i> Mis Badges
                    <span class="badge ms-2" style="background:#7c3aed;font-size:.8rem;">Nivel @Model.NivelActual de 6</span>
                </h6>
                <div class="row g-3">
                    @foreach (var badge in Model.TodosLosBadges)
                    {
                        <div class="col-6 col-md-4">
                            <div class="d-flex gap-2 align-items-start p-2 rounded"
                                 style="background:@(badge.Obtenido ? "#f5f3ff" : "#f9fafb");">
                                <i class="@badge.Icono fs-4" style="color:@(badge.Obtenido ? "#7c3aed" : "#9ca3af");"></i>
                                <div>
                                    <div class="fw-semibold" style="font-size:.88rem;color:@(badge.Obtenido ? "#1f2937" : "#9ca3af");">
                                        @badge.Nombre
                                    </div>
                                    @if (badge.Obtenido && badge.FechaObtenido.HasValue)
                                    {
                                        <div class="text-muted" style="font-size:.75rem;">@badge.FechaObtenido.Value.ToString("dd MMM yyyy")</div>
                                    }
                                    else if (!badge.Obtenido)
                                    {
                                        <div class="text-muted" style="font-size:.75rem;">Cómo obtenerlo: @badge.ComoObtenerlo</div>
                                    }
                                </div>
                            </div>
                        </div>
                    }
                </div>
            </div>
        }

        @* ── Bloque 7: Estado de verificación ── *@
        <div class="manage-card perfil-section-block">
            <h6 class="card-section-title mb-3"><i class="bi bi-shield-check"></i> Estado de verificación</h6>
            @if (!Model.PerfilVinculado)
            {
                <div class="alert alert-light border">
                    <strong>No vinculado</strong> — Busca tu perfil en el directorio y reclámaló para activar la verificación.
                    <a asp-page="/DirectorioMedicos/Index" class="ms-1">Ir al directorio</a>
                </div>
            }
            else
            {
                var esVerificado = Model.TodosLosBadges.Any(b => b.Codigo == "verificado" && b.Obtenido);
                <div class="d-flex align-items-center gap-2">
                    <i class="bi @(esVerificado ? "bi-patch-check-fill text-primary" : "bi-clock text-warning") fs-5"></i>
                    <span>@(esVerificado ? "Verificado por el equipo EIIBD" : "Pendiente de verificación")</span>
                </div>
            }
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        function previewSlug(val) {
            document.getElementById('slug-preview').textContent = val || 'tu-slug';
        }
        function addHospital() {
            const c = document.getElementById('hospitales-container');
            const d = document.createElement('div');
            d.className = 'input-group mb-1';
            d.innerHTML = '<input type="text" name="Input.Hospitales" class="form-control form-control-sm" placeholder="Nombre del hospital o clínica" />'
                        + '<button type="button" class="btn btn-outline-secondary btn-sm" onclick="this.closest(\'.input-group\').remove()">✕</button>';
            c.appendChild(d);
        }
        async function checkSlug() {
            const slug = document.getElementById('slug-input').value;
            if (!slug) return;
            const r = await fetch(`?handler=CheckSlug&slug=${encodeURIComponent(slug)}`);
            const data = await r.json();
            const el = document.getElementById('slug-status');
            el.innerHTML = data.disponible
                ? '<span class="text-success"><i class="bi bi-check-circle me-1"></i>Disponible</span>'
                : '<span class="text-danger"><i class="bi bi-x-circle me-1"></i>No disponible</span>';
        }
        async function generateSlug() {
            const r = await fetch('?handler=GenerateSlug');
            const suggestions = await r.json();
            const container = document.getElementById('slug-suggestions');
            container.innerHTML = '';
            suggestions.forEach(s => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-outline-secondary btn-sm';
                btn.textContent = s;
                btn.onclick = () => {
                    document.getElementById('slug-input').value = s;
                    previewSlug(s);
                };
                container.appendChild(btn);
            });
        }
    </script>
}
```

- [ ] **Step 5.5: Verificar compilación**

```powershell
dotnet build --no-restore 2>&1 | Select-String "error (RZ|CS)"
```

- [ ] **Step 5.6: Commit**

```bash
git add Areas/Identity/Pages/Account/Manage/ManageNavPages.cs Areas/Identity/Pages/Account/Manage/_ManageNav.cshtml Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs
git commit -m "feat(medico): PerfilMedico manage page con badges, slug, foto y vinculación"
```

---

## Task 6: Verificación final y limpieza

- [ ] **Step 6.1: Build limpio final**

```powershell
dotnet build --no-restore -c Debug 2>&1 | Select-Object -Last 5
```

Esperado: `0 Error(s)` de código (solo posibles MSB30xx si la app corre).

- [ ] **Step 6.2: Verificar rutas nuevas**

Confirmar que estas páginas existen y compilan correctamente:
- `/Identity/Account/RegisterM` → `Areas/Identity/Pages/Account/RegisterM.cshtml`
- `/directorio/reclamar/{id}` → `Pages/Directorio/Reclamar.cshtml`
- `/directorio/activar?token=...` → `Pages/Directorio/Activar.cshtml`
- `/Identity/Account/Manage/PerfilMedico` → `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml`

- [ ] **Step 6.3: Verificar Respuesta.AutorId**

```powershell
# Verificar el nombre exacto de la propiedad FK de usuario en Respuesta
grep -r "AutorId\|UsuarioId\|UserId\|AutorUsuarioId" Models/ --include="*.cs" | grep -i respuesta
```

Si el nombre es diferente a `AutorId`, actualizar `MedicoBadgeService.cs` en la query de `participante_qa`:
```csharp
// Ajustar según el nombre real de la FK:
var respuestas = await _db.Respuestas.CountAsync(r => r.NombreRealDeLaFK == perfil.UserId.Value);
```

- [ ] **Step 6.4: Commit final**

```bash
git add -A
git commit -m "feat(medico): sistema completo médicos — registro, reclamación, perfil, badges"
```

---

## Notas de Implementación

### Rol "Medico"
Si el seed de roles no incluye "Medico", buscar en `Program.cs` el bloque donde se siembra "Paciente" (o similar) y agregar el rol "Medico" al mismo lugar.

### GlossaryValidation.UserId
El campo `UserId` en `GlossaryValidations` se usa como `string` en la query de `EvaluarBadgesAutomaticosAsync`. Verificar con `grep "UserId" Models/Glossary/GlossaryValidation.cs` y ajustar el cast si es necesario.

### Respuesta.AutorId
Confirmar el nombre de la FK antes de hacer build (Step 6.3). El servicio asume `AutorId` — si difiere, actualizar en Task 2.

### Foto de perfil
El patrón ImageSharp usado en `PerfilMedicoModel` es idéntico al de `UsuarioPerfilModel`. Si `SixLabors.ImageSharp` no está en las dependencias (poco probable dado que ya se usa), agregar `dotnet add package SixLabors.ImageSharp`.

### Conexión directa a BD (fallback)
Si el SQL script falla al aplicarse, usar la connection string de `appsettings.Development.json` o variables de entorno y ejecutar el script directamente contra el servidor SQL configurado.
