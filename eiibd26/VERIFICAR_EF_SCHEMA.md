# 🔍 VERIFICACIÓN DE SCHEMA DE ENTITY FRAMEWORK

Si después de limpiar cache y reiniciar **TODAVÍA** hay error, sigue estos pasos:

## 1. Verificar que EF Core reconoce los cambios

Agrega temporalmente este código en `Program.cs` (después de `var app = builder.Build();`):

```csharp
// CÓDIGO TEMPORAL DE DIAGNÓSTICO
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();
    
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII'";
    var result = await cmd.ExecuteScalarAsync();
    
    Console.WriteLine($"🔍 VERIFICACIÓN: RelacionEII tipo = {result}");
    
    if (result?.ToString() != "bit")
    {
        Console.WriteLine("❌ ERROR: EF Core ve el tipo incorrecto!");
        Console.WriteLine("   Solución: Reinicia el servidor SQL o cierra todas las conexiones");
    }
    else
    {
        Console.WriteLine("✅ EF Core ve el tipo correcto (bit)");
    }
}
// FIN CÓDIGO TEMPORAL
```

Ejecuta la app y revisa la consola. Deberías ver:
```
✅ EF Core ve el tipo correcto (bit)
```

Si dice `❌ ERROR`, entonces hay un problema con las conexiones al servidor SQL.

## 2. Reiniciar Conexiones SQL Server

Ejecuta en SSMS:

```sql
USE master;
GO

-- Ver conexiones activas a eiibd26
SELECT 
    session_id,
    login_name,
    host_name,
    program_name,
    status
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('eiibd26');

-- Cerrar todas las conexiones (CUIDADO: solo si estás seguro)
-- ALTER DATABASE eiibd26 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
-- ALTER DATABASE eiibd26 SET MULTI_USER;
```

## 3. Solución Nuclear (último recurso)

Si nada funciona:

1. **Exporta la estructura de la tabla** (para no perder datos):
```sql
SELECT * INTO sintomas_backup FROM sintomas;
SELECT * INTO tratamientos_backup FROM tratamientos;
```

2. **Elimina y recrea las columnas**:
```sql
ALTER TABLE sintomas DROP COLUMN RelacionEII;
ALTER TABLE sintomas ADD RelacionEII BIT NOT NULL DEFAULT 0;

ALTER TABLE tratamientos DROP COLUMN RelacionEII;
ALTER TABLE tratamientos ADD RelacionEII BIT NOT NULL DEFAULT 0;
```

3. **Reinicia SQL Server** (en Services)

4. **Reinicia Visual Studio**

---

**En el 99% de los casos, limpiar el cache (bin/obj) y reiniciar Visual Studio soluciona el problema** ✅
