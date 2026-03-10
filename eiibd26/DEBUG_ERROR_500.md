# 🔧 DEBUGGING - Error 500 en Grid

## Si aún recibes error 500:

### Paso 1: Habilitar Logging Detallado

En `Program.cs`, agrega:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddDebug();
    logging.AddConsole();
});
```

### Paso 2: Ver los Logs

Abre la ventana **Debug Output** en Visual Studio:
```
Debug → Windows → Output
```

Observa los logs detallados de la excepción.

### Paso 3: Verificar la Base de Datos

Asegúrate de que las columnas nuevas existen:

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sintomas'
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII')
ORDER BY ORDINAL_POSITION;
```

Si no existen, ejecuta el SQL de migración nuevamente.

### Paso 4: Limpiar y Reconstruir

```powershell
# En Visual Studio Terminal o PowerShell
dotnet clean
dotnet build
```

### Paso 5: Si aún hay error

Revisa el archivo de código completo para asegurar que:
- ✅ El método `OnGetGridDataAsync` está completo
- ✅ No hay puntos y comas faltantes
- ✅ Los paréntesis están balanceados
- ✅ Las estructuras JSON son válidas

---

## Cambios Realizados

✅ Sintomas/Index.cshtml.cs - Separado el `.Cast<dynamic>()` en dos líneas
✅ Tratamientos/Index.cshtml.cs - Mismo cambio
✅ Build completado sin errores

---

## Próximo Step

Intenta nuevamente la URL del grid:
```
https://localhost:7002/Identity/Admin/Sintomas?handler=GridData
```

Debería funcionar sin error 500.

Si persiste, proporciona el mensaje de error exacto de la consola Debug Output.

