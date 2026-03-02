# ⚠️ ERROR: SqlNullValueException en EsPrincipal

## Problema
```
System.Data.SqlTypes.SqlNullValueException: Data is Null. 
This method or property cannot be called on Null values.
```

### Causa Raíz
La columna `EsPrincipal` en la tabla `contenidosCategoriasRelacion` está definida como:
```sql
EsPrincipal BIT NULL  -- ❌ Permite NULL
```

Cuando Entity Framework intenta mapear valores NULL a `bool` en C#, lanza `SqlNullValueException`.

---

## ✅ Solución Aplicada

### 1. **Modelo C# Actualizado** (Cambio Temporal)
```csharp
// eiibd26/Models/ContenidoCategoriaRelacion.cs
public bool? EsPrincipal { get; set; }  // ✅ Ahora acepta NULL
```

### 2. **Código Actualizado para Manejar NULL**
Todos los archivos que usan `EsPrincipal` ahora usan comparación explícita:

```csharp
// ❌ ANTES (fallaba con NULL)
var primary = g.FirstOrDefault(x => x.EsPrincipal);

// ✅ DESPUÉS (maneja NULL correctamente)
var primary = g.FirstOrDefault(x => x.EsPrincipal == true);
// O en algunos casos:
var primary = g.FirstOrDefault(x => x.EsPrincipal.HasValue && x.EsPrincipal.Value);
```

**Archivos modificados:**
- ✅ `eiibd26/Models/ContenidoCategoriaRelacion.cs`
- ✅ `eiibd26/Pages/Contenidos/porCategoria.cshtml.cs`
- ✅ `eiibd26/Pages/Home/BlogMore.cshtml.cs`
- ✅ `eiibd26/Pages/Contenidos/Index.cshtml.cs`
- ✅ `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Detalle.cshtml.cs`

---

## 🔧 Migración de Base de Datos (RECOMENDADO)

Para solucionar permanentemente el problema, ejecuta el script SQL:

```sql
-- Archivo: eiibd26/Data/Migrations/FixEsPrincipalColumn.sql
```

### Pasos para ejecutar:

#### **Opción 1: SQL Server Management Studio (SSMS)**
1. Conecta a: `132.148.74.136\ybridio`
2. Selecciona la base de datos: `eiibd26`
3. Abre el archivo `FixEsPrincipalColumn.sql`
4. Ejecuta (F5)

#### **Opción 2: Visual Studio**
1. View → SQL Server Object Explorer
2. Conecta a `132.148.74.136\ybridio`
3. Click derecho en `eiibd26` → New Query
4. Pega el contenido de `FixEsPrincipalColumn.sql`
5. Ejecuta

#### **Opción 3: PowerShell (desde VS Terminal)**
```powershell
sqlcmd -S "132.148.74.136\ybridio" -d eiibd26 -i "Data\Migrations\FixEsPrincipalColumn.sql"
```

### Qué hace el script:
1. ✅ Actualiza todos los `NULL` → `0` (false)
2. ✅ Cambia la columna a `BIT NOT NULL`
3. ✅ Agrega constraint DEFAULT (0)
4. ✅ Marca la categoría más reciente como primaria para cada contenido
5. ✅ Crea índice para mejor performance
6. ✅ Verifica que cada contenido tenga exactamente una categoría primaria

---

## 📊 Verificación Después de la Migración

El script imprimirá un resumen:
```
✅ Migration completed successfully!

Total rows in table: 150
Rows with EsPrincipal = 1: 50
Rows with EsPrincipal = 0: 100
Rows with NULL EsPrincipal: 0  ← Debe ser 0

Contents with multiple primary categories: 0  ← Debe ser 0
Contents with no primary category: 0  ← Idealmente 0
```

---

## 🔄 Después de la Migración SQL

### 1. **Revertir el Modelo C# a NOT NULL** (Opcional)
Una vez que la columna sea NOT NULL en la DB, puedes cambiar:

```csharp
// eiibd26/Models/ContenidoCategoriaRelacion.cs
public bool EsPrincipal { get; set; }  // ✅ De vuelta a no-nullable
```

### 2. **Simplificar Comparaciones** (Opcional)
Puedes volver a usar:
```csharp
var primary = g.FirstOrDefault(x => x.EsPrincipal);  // Más simple
```

### 3. **Reiniciar Aplicación**
```powershell
# Detener debugging en Visual Studio
# Presionar F5 para reiniciar completamente
```

---

## 📝 Resumen

| Estado | Solución | Resultado |
|--------|----------|-----------|
| 🟢 **APLICADO** | Modelo C# acepta NULL (`bool?`) | ✅ App compila y funciona ahora |
| 🟢 **APLICADO** | Código usa `== true` para comparar | ✅ No más SqlNullValueException |
| 🟡 **PENDIENTE** | Ejecutar script SQL | ⚠️ Arregla permanentemente la DB |
| 🔵 **OPCIONAL** | Revertir a `bool` no-nullable | ℹ️ Después de migración SQL |

---

## 🚀 Estado Actual

**La aplicación ahora funciona correctamente** con los cambios en el código C#.

**Siguiente paso recomendado:** Ejecutar `FixEsPrincipalColumn.sql` para arreglar la base de datos permanentemente y evitar futuros problemas con datos NULL.

---

## 🐛 Si Sigue Fallando

1. **Verifica que la app se haya recompilado:**
   ```powershell
   dotnet build --no-incremental
   ```

2. **Reinicia completamente (no Hot Reload):**
   - Detén el debugging (Shift+F5)
   - Limpia solución: Build → Clean Solution
   - Reconstruye: Build → Rebuild Solution
   - Inicia de nuevo (F5)

3. **Verifica cambios en el modelo:**
   ```csharp
   // En ContenidoCategoriaRelacion.cs línea 45
   public bool? EsPrincipal { get; set; }  // ← Debe tener "?"
   ```
