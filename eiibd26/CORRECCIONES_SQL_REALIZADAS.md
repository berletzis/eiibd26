# 🔧 CORRECCIONES REALIZADAS - SQL Queries

## ✅ Problemas Identificados y Solucionados

### Problema 1: Query de Integridad Referencial
**Error Original:**
```
Msg 209: Ambiguous column name 'CONSTRAINT_NAME'
Msg 207: Invalid column name 'REFERENCED_OBJECT_ID'
```

**Causa:** El SQL Server Management Studio no tiene una columna `REFERENCED_OBJECT_ID` en las vistas de información. La syntax era incorrecta.

**Solución:** Reemplazado con query correcta usando:
- `REFERENTIAL_CONSTRAINTS`
- `KEY_COLUMN_USAGE`
- `CONSTRAINT_COLUMN_USAGE`

---

### Problema 2: Queries de Validación Final
**Error Original:**
```
Msg 207: Invalid column name 'DescripcionIA'
Msg 207: Invalid column name 'ValidadoIA'
```

**Causa:** El query intentaba seleccionar columnas que no habían sido creadas aún si los pasos anteriores fallaban.

**Solución:** Agregadas verificaciones condicionales usando:
- `COL_LENGTH()` para verificar si una columna existe
- `OBJECT_ID()` para verificar si una tabla existe
- `IF` statements para mensajes claros si algo falla

---

## 📝 Cambios en SQL_QUERIES_DIRECTAS.sql

### PASO 5: Verificar Integridad Referencial (CORREGIDO)

**ANTES:**
```sql
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME = OBJECT_NAME(REFERENCED_OBJECT_ID)
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS 
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ON ...
```

**DESPUÉS:**
```sql
SELECT 
    CONSTRAINT_NAME = rc.CONSTRAINT_NAME,
    TABLE_NAME = kcu.TABLE_NAME,
    COLUMN_NAME = kcu.COLUMN_NAME,
    REFERENCED_TABLE_NAME = ccu.TABLE_NAME,
    REFERENCED_COLUMN = ccu.COLUMN_NAME
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu
    ON rc.UNIQUE_CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
WHERE kcu.TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas')
ORDER BY kcu.TABLE_NAME;
```

---

### PASO 8: Validación Final (CORREGIDO)

**ANTES:**
```sql
SELECT 
    'sintomas' AS Tabla, 
    COUNT(*) AS Total,
    SUM(CASE WHEN DescripcionIA IS NOT NULL THEN 1 ELSE 0 END) ...
FROM dbo.sintomas
```

**DESPUÉS:**
```sql
IF COL_LENGTH('dbo.sintomas', 'DescripcionIA') IS NOT NULL
BEGIN
    SELECT 
        Tabla = 'sintomas', 
        Total = COUNT(*),
        ConDescripcionIA = SUM(CASE WHEN DescripcionIA IS NOT NULL THEN 1 ELSE 0 END),
        ValidadoIA_Count = SUM(CASE WHEN ValidadoIA = 1 THEN 1 ELSE 0 END),
        ValidadoHumano_Count = SUM(CASE WHEN ValidadoHumano = 1 THEN 1 ELSE 0 END)
    FROM dbo.sintomas
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA: Las columnas aún no se han agregado a sintomas...'
END
```

---

## 📚 NUEVO DOCUMENTO CREADO

**Archivo:** `GUIA_EJECUCION_SQL.md`

Contiene:
- ✅ Instrucciones paso a paso para ejecutar el SQL
- ✅ Cada paso por separado (RECOMENDADO)
- ✅ Qué verificar después de cada paso
- ✅ Soluciones para errores comunes
- ✅ Cómo deshacer cambios si es necesario
- ✅ Tips y mejores prácticas

---

## 🎯 PRÓXIMOS PASOS

1. **Lee:** `GUIA_EJECUCION_SQL.md`
2. **Ejecuta:** Cada paso del SQL en SQL Server Management Studio
3. **Verifica:** Que no hay errores después de cada paso
4. **Continúa:** Con PLAN_ACCION_FINAL.md paso 2

---

## ✅ VALIDACIÓN

Después de ejecutar todo el SQL, deberías ver:

### Tabla sintomas
```
DescripcionIA      NVARCHAR(MAX)      YES
ValidadoIA         BIT                NO
ValidadoHumano     BIT                NO
RelacionEII        NVARCHAR(MAX)      YES
FechaActualizacionIA DATETIME         YES
```

### Tabla tratamientos
```
DescripcionIA      NVARCHAR(MAX)      YES
ValidadoIA         BIT                NO
ValidadoHumano     BIT                NO
RelacionEII        NVARCHAR(MAX)      YES
FechaActualizacionIA DATETIME         YES
```

### Tablas nuevas
```
SintomasNotas
TratamientosNotas
```

---

## 💡 NOTA IMPORTANTE

Si ejecutas el SQL_QUERIES_DIRECTAS.sql completo sin separar los pasos, probablemente verás errores. Por eso:

✅ **Usa GUIA_EJECUCION_SQL.md** (RECOMENDADO)
- Ejecuta cada paso en una ventana de query separada
- Verifica que no hay errores
- Continúa al siguiente paso

O usar EF Core Migrations:
```powershell
Add-Migration AgregaSintomasYTratamientosIA
Update-Database
```

---

¡Los SQL scripts están listos! 🎉
