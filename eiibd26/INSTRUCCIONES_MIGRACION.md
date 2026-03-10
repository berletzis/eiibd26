# Instrucciones para crear la Migración de EF Core

## Paso 1: Abre la Package Manager Console en Visual Studio

En Visual Studio:
- **View** → **Other Windows** → **Package Manager Console**

O presiona: `View + Ctrl + Shift + O` y busca "Package Manager Console"

## Paso 2: Ejecuta el comando para crear la migración

En la Package Manager Console, ejecuta:

```powershell
Add-Migration AgregaSintomasYTratamientosIA
```

Este comando crear un archivo de migración en la carpeta `Migrations/` con los cambios necesarios.

## Paso 3: Revisa la migración generada

Abre el archivo generado en `Migrations/[timestamp]_AgregaSintomasYTratamientosIA.cs` y verifica que:

1. Se agregan las columnas a las tablas `sintomas` y `tratamientos`
2. Se crean las nuevas tablas `SintomasNotas` y `TratamientosNotas`
3. Se crean los índices correctamente

## Paso 4: Ejecuta la migración en la base de datos

En la Package Manager Console, ejecuta:

```powershell
Update-Database
```

Esto aplicará los cambios a tu base de datos.

## Paso 5: Verifica en SQL Server

Abre SQL Server Management Studio o tu cliente SQL favorito y verifica:

```sql
-- Verifica que las columnas se agregaron a sintomas
SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'sintomas' 
ORDER BY ORDINAL_POSITION;

-- Verifica que la tabla SintomasNotas existe
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas');

-- Verifica las relaciones
SELECT * FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
WHERE TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas');
```

## Si algo sale mal (ROLLBACK)

Si necesitas deshacer los cambios:

```powershell
# Ver la migración anterior
Get-Migration

# Volver a la migración anterior
Update-Database -Migration [nombre_migracion_anterior]

# O eliminar la última migración (si no se ha ejecutado en producción)
Remove-Migration
```

## IMPORTANTE: Actualizar DbContext

Antes de las migraciones, asegúrate de que `ApplicationDbContext.cs` incluya:

```csharp
public DbSet<SintomasNotas> SintomasNotas { get; set; }
public DbSet<TratamientosNotas> TratamientosNotas { get; set; }
```

Y en el método `OnModelCreating`, agrega:

```csharp
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
```
