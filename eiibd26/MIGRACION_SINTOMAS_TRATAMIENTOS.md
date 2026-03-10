# Migración: Síntomas y Tratamientos con IA y Notas

## 1. SQL QUERIES - Crear Tablas y Campos

### 1.1 Agregar campos a tabla `sintomas`

```sql
-- Agregar campos a síntomas
ALTER TABLE dbo.sintomas ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII BIT DEFAULT 0,
    FechaActualizacionIA DATETIME NULL;

-- Crear índices
CREATE INDEX IX_sintomas_ValidadoIA ON dbo.sintomas(ValidadoIA);
CREATE INDEX IX_sintomas_RelacionEII ON dbo.sintomas(RelacionEII);
```

### 1.2 Agregar campos a tabla `tratamientos`

```sql
-- Agregar campos a tratamientos
ALTER TABLE dbo.tratamientos ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII BIT DEFAULT 0,
    FechaActualizacionIA DATETIME NULL;

-- Crear índices
CREATE INDEX IX_tratamientos_ValidadoIA ON dbo.tratamientos(ValidadoIA);
CREATE INDEX IX_tratamientos_RelacionEII ON dbo.tratamientos(RelacionEII);
```

### 1.3 Crear tabla `SintomasNotas`

```sql
CREATE TABLE dbo.SintomasNotas (
    id INT PRIMARY KEY IDENTITY(1,1),
    SintomaId INT NOT NULL,
    UsuarioId UNIQUEIDENTIFIER NULL,
    Nota NVARCHAR(MAX) NOT NULL,
    EsNotaIA BIT DEFAULT 0,
    FechaCreado DATETIME DEFAULT GETUTCDATE(),
    FechaModificado DATETIME DEFAULT GETUTCDATE(),
    Eliminado BIT DEFAULT 0,
    
    -- Foreign Keys
    CONSTRAINT FK_SintomasNotas_Sintomas FOREIGN KEY (SintomaId) 
        REFERENCES dbo.sintomas(id) ON DELETE CASCADE,
    CONSTRAINT FK_SintomasNotas_Usuario FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
);

-- Crear índices
CREATE INDEX IX_SintomasNotas_SintomaId ON dbo.SintomasNotas(SintomaId);
CREATE INDEX IX_SintomasNotas_UsuarioId ON dbo.SintomasNotas(UsuarioId);
CREATE INDEX IX_SintomasNotas_EsNotaIA ON dbo.SintomasNotas(EsNotaIA);
```

### 1.4 Crear tabla `TratamientosNotas`

```sql
CREATE TABLE dbo.TratamientosNotas (
    id INT PRIMARY KEY IDENTITY(1,1),
    TratamientoId INT NOT NULL,
    UsuarioId UNIQUEIDENTIFIER NULL,
    Nota NVARCHAR(MAX) NOT NULL,
    EsNotaIA BIT DEFAULT 0,
    FechaCreado DATETIME DEFAULT GETUTCDATE(),
    FechaModificado DATETIME DEFAULT GETUTCDATE(),
    Eliminado BIT DEFAULT 0,
    
    -- Foreign Keys
    CONSTRAINT FK_TratamientosNotas_Tratamientos FOREIGN KEY (TratamientoId) 
        REFERENCES dbo.tratamientos(id) ON DELETE CASCADE,
    CONSTRAINT FK_TratamientosNotas_Usuario FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
);

-- Crear índices
CREATE INDEX IX_TratamientosNotas_TratamientoId ON dbo.TratamientosNotas(TratamientoId);
CREATE INDEX IX_TratamientosNotas_UsuarioId ON dbo.TratamientosNotas(UsuarioId);
CREATE INDEX IX_TratamientosNotas_EsNotaIA ON dbo.TratamientosNotas(EsNotaIA);
```

---

## 2. MODELOS C# ACTUALIZADOS

### 2.1 Modelo `sintomas.cs` (ACTUALIZADO)

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class sintomas
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(250)]
        public string nombre { get; set; }

        public int? idPadre { get; set; }

        public int? idIdioma { get; set; }

        [StringLength(50)]
        public string icono { get; set; }

        // ===== NUEVOS CAMPOS PARA IA Y VALIDACIÓN =====
        [StringLength(int.MaxValue)] // NVARCHAR(MAX)
        public string DescripcionIA { get; set; }

        [Display(Name = "Validado por IA")]
        public bool ValidadoIA { get; set; } = false;

        [Display(Name = "Validado por Humano")]
        public bool ValidadoHumano { get; set; } = false;

        [Display(Name = "Relación con EII")]
        public string RelacionEII { get; set; } // Cambié a string para guardar explicación

        public DateTime? FechaActualizacionIA { get; set; }

        // ===== CAMPOS EXISTENTES =====
        public DateTime fechaEliminado { get; set; }
        public DateTime fechaModificado { get; set; }
        public DateTime fechaCreado { get; set; }
        public bool Eliminado { get; set; }

        // ===== NAVIGATION PROPERTIES =====
        public virtual ICollection<sintomasUsuario> SintomasUsuario { get; set; }
        public virtual ICollection<SintomasNotas> Notas { get; set; }
    }
}
```

### 2.2 Modelo `tratamientos.cs` (ACTUALIZADO)

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class tratamientos
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(250)]
        public string nombre { get; set; }

        public int? idPadre { get; set; }

        public int? idIdioma { get; set; }

        [StringLength(50)]
        public string icono { get; set; }

        // ===== NUEVOS CAMPOS PARA IA Y VALIDACIÓN =====
        [StringLength(int.MaxValue)] // NVARCHAR(MAX)
        public string DescripcionIA { get; set; }

        [Display(Name = "Validado por IA")]
        public bool ValidadoIA { get; set; } = false;

        [Display(Name = "Validado por Humano")]
        public bool ValidadoHumano { get; set; } = false;

        [Display(Name = "Relación con EII")]
        public string RelacionEII { get; set; } // Cambié a string para guardar explicación

        public DateTime? FechaActualizacionIA { get; set; }

        // ===== CAMPOS EXISTENTES =====
        public DateTime fechaEliminado { get; set; }
        public DateTime fechaModificado { get; set; }
        public DateTime fechaCreado { get; set; }
        public bool Eliminado { get; set; }

        // ===== NAVIGATION PROPERTIES =====
        public virtual ICollection<tratamientos> Hijos { get; set; }
        public virtual tratamientos Padre { get; set; }
        public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
        public virtual ICollection<TratamientosNotas> Notas { get; set; }
    }
}
```

### 2.3 Nuevo Modelo `SintomasNotas.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class SintomasNotas
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("Sintoma")]
        public int SintomaId { get; set; }

        [ForeignKey("Usuario")]
        public Guid? UsuarioId { get; set; }

        [Required]
        [StringLength(int.MaxValue)]
        public string Nota { get; set; }

        [Display(Name = "Nota de IA")]
        public bool EsNotaIA { get; set; } = false;

        public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificado { get; set; } = DateTime.UtcNow;
        public bool Eliminado { get; set; } = false;

        // ===== NAVIGATION PROPERTIES =====
        public virtual sintomas Sintoma { get; set; }
        public virtual ApplicationUser Usuario { get; set; }
    }
}
```

### 2.4 Nuevo Modelo `TratamientosNotas.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class TratamientosNotas
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("Tratamiento")]
        public int TratamientoId { get; set; }

        [ForeignKey("Usuario")]
        public Guid? UsuarioId { get; set; }

        [Required]
        [StringLength(int.MaxValue)]
        public string Nota { get; set; }

        [Display(Name = "Nota de IA")]
        public bool EsNotaIA { get; set; } = false;

        public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificado { get; set; } = DateTime.UtcNow;
        public bool Eliminado { get; set; } = false;

        // ===== NAVIGATION PROPERTIES =====
        public virtual tratamientos Tratamiento { get; set; }
        public virtual ApplicationUser Usuario { get; set; }
    }
}
```

---

## 3. ACTUALIZACIONES AL DBCONTEXT

Agregar a `ApplicationDbContext.cs`:

```csharp
public DbSet<SintomasNotas> SintomasNotas { get; set; }
public DbSet<TratamientosNotas> TratamientosNotas { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configuración de SintomasNotas
    modelBuilder.Entity<SintomasNotas>()
        .HasOne(n => n.Sintoma)
        .WithMany(s => s.Notas)
        .HasForeignKey(n => n.SintomaId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<SintomasNotas>()
        .HasOne(n => n.Usuario)
        .WithMany()
        .HasForeignKey(n => n.UsuarioId)
        .OnDelete(DeleteBehavior.SetNull);

    // Configuración de TratamientosNotas
    modelBuilder.Entity<TratamientosNotas>()
        .HasOne(n => n.Tratamiento)
        .WithMany(t => t.Notas)
        .HasForeignKey(n => n.TratamientoId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TratamientosNotas>()
        .HasOne(n => n.Usuario)
        .WithMany()
        .HasForeignKey(n => n.UsuarioId)
        .OnDelete(DeleteBehavior.SetNull);
}
```

---

## 4. PASOS SIGUIENTES

- [ ] Ejecutar SQL queries en la base de datos
- [ ] Crear migration: `Add-Migration AgregaSintomasYTratamientosIA`
- [ ] Crear modelos C# (SintomasNotas, TratamientosNotas)
- [ ] Actualizar DbContext
- [ ] Ejecutar update-database
- [ ] Modificar grid de Síntomas
- [ ] Modificar grid de Tratamientos
- [ ] Cambiar modal a panel lateral
- [ ] Crear endpoint para generar descripción IA
- [ ] Integrar Claude API
