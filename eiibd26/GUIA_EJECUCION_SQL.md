# 🔧 GUÍA DE EJECUCIÓN SQL - Paso a Paso

## ⚠️ IMPORTANTE

Debes ejecutar los queries **por PASOS**, no todos juntos. Aquí está cómo:

---

## 📋 INSTRUCCIONES

### OPCIÓN A: Ejecutar cada paso por separado (RECOMENDADO)

#### PASO 1: Agregar campos a SINTOMAS
```sql
ALTER TABLE dbo.sintomas ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII NVARCHAR(MAX) NULL,
    FechaActualizacionIA DATETIME NULL;

CREATE INDEX IX_sintomas_ValidadoIA ON dbo.sintomas(ValidadoIA);
CREATE INDEX IX_sintomas_RelacionEII ON dbo.sintomas(RelacionEII);
CREATE INDEX IX_sintomas_FechaActualizacionIA ON dbo.sintomas(FechaActualizacionIA);
```

**✅ Verifica que no haya errores**

---

#### PASO 2: Agregar campos a TRATAMIENTOS
```sql
ALTER TABLE dbo.tratamientos ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII NVARCHAR(MAX) NULL,
    FechaActualizacionIA DATETIME NULL;

CREATE INDEX IX_tratamientos_ValidadoIA ON dbo.tratamientos(ValidadoIA);
CREATE INDEX IX_tratamientos_RelacionEII ON dbo.tratamientos(RelacionEII);
CREATE INDEX IX_tratamientos_FechaActualizacionIA ON dbo.tratamientos(FechaActualizacionIA);
```

**✅ Verifica que no haya errores**

---

#### PASO 3: Crear tabla SINTOMASNOTAS
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
    
    CONSTRAINT FK_SintomasNotas_Sintomas 
        FOREIGN KEY (SintomaId) 
        REFERENCES dbo.sintomas(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT FK_SintomasNotas_Usuario 
        FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) 
        ON DELETE SET NULL
);

CREATE INDEX IX_SintomasNotas_SintomaId ON dbo.SintomasNotas(SintomaId);
CREATE INDEX IX_SintomasNotas_UsuarioId ON dbo.SintomasNotas(UsuarioId);
CREATE INDEX IX_SintomasNotas_EsNotaIA ON dbo.SintomasNotas(EsNotaIA);
CREATE INDEX IX_SintomasNotas_Eliminado ON dbo.SintomasNotas(Eliminado);
CREATE INDEX IX_SintomasNotas_FechaCreado ON dbo.SintomasNotas(FechaCreado);
```

**✅ Verifica que no haya errores**

---

#### PASO 4: Crear tabla TRATAMIENTOSNOTAS
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
    
    CONSTRAINT FK_TratamientosNotas_Tratamientos 
        FOREIGN KEY (TratamientoId) 
        REFERENCES dbo.tratamientos(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT FK_TratamientosNotas_Usuario 
        FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) 
        ON DELETE SET NULL
);

CREATE INDEX IX_TratamientosNotas_TratamientoId ON dbo.TratamientosNotas(TratamientoId);
CREATE INDEX IX_TratamientosNotas_UsuarioId ON dbo.TratamientosNotas(UsuarioId);
CREATE INDEX IX_TratamientosNotas_EsNotaIA ON dbo.TratamientosNotas(EsNotaIA);
CREATE INDEX IX_TratamientosNotas_Eliminado ON dbo.TratamientosNotas(Eliminado);
CREATE INDEX IX_TratamientosNotas_FechaCreado ON dbo.TratamientosNotas(FechaCreado);
```

**✅ Verifica que no haya errores**

---

#### PASO 5: Verificar que todo se creó correctamente
```sql
-- Verificar columnas en sintomas
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'sintomas' 
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

-- Verificar columnas en tratamientos
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'tratamientos' 
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

-- Verificar que existen las tablas nuevas
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas');
```

**✅ Deberías ver 5 filas, 5 filas y 2 filas respectivamente**

---

## 🚨 SI ALGO FALLA

### Error: "Column already exists"
→ Las columnas ya existen. Continúa al siguiente paso.

### Error: "Table already exists"
→ Las tablas ya existen. Continúa al siguiente paso.

### Error: "Invalid constraint name"
→ La constraint ya existe. Usa un nombre diferente en el CREATE TABLE.

### Solución rápida: Eliminar todo y empezar de nuevo
```sql
-- ⚠️ SOLO si necesitas empezar de cero
DROP TABLE IF EXISTS dbo.TratamientosNotas;
DROP TABLE IF EXISTS dbo.SintomasNotas;

ALTER TABLE dbo.sintomas DROP COLUMN IF EXISTS DescripcionIA;
ALTER TABLE dbo.sintomas DROP COLUMN IF EXISTS ValidadoIA;
ALTER TABLE dbo.sintomas DROP COLUMN IF EXISTS ValidadoHumano;
ALTER TABLE dbo.sintomas DROP COLUMN IF EXISTS RelacionEII;
ALTER TABLE dbo.sintomas DROP COLUMN IF EXISTS FechaActualizacionIA;

ALTER TABLE dbo.tratamientos DROP COLUMN IF EXISTS DescripcionIA;
ALTER TABLE dbo.tratamientos DROP COLUMN IF EXISTS ValidadoIA;
ALTER TABLE dbo.tratamientos DROP COLUMN IF EXISTS ValidadoHumano;
ALTER TABLE dbo.tratamientos DROP COLUMN IF EXISTS RelacionEII;
ALTER TABLE dbo.tratamientos DROP COLUMN IF EXISTS FechaActualizacionIA;

-- Luego ejecuta todos los pasos nuevamente
```

---

## ✅ CUANDO HAYAS TERMINADO

1. **Ejecuta las migraciones de EF Core:**
```powershell
Add-Migration AgregaSintomasYTratamientosIA
Update-Database
```

2. **Compila el proyecto:**
```
Ctrl+Shift+B (o Build → Build Solution)
```

3. **Continúa con el siguiente paso del PLAN_ACCION_FINAL.md**

---

## 💡 TIPS

- Usa **SQL Server Management Studio** para mejor visualización
- Si usas **Azure Data Studio**, también funciona perfectamente
- Puedes copiar y pegar cada paso en una nueva ventana de query
- No cierres el SQL Server entre pasos
- Verifica cada paso con `SELECT` después de ejecutarlo

---

## 🎯 RESUMEN RÁPIDO

```
PASO 1: ALTER TABLE sintomas ADD... ✅
        ↓
PASO 2: ALTER TABLE tratamientos ADD... ✅
        ↓
PASO 3: CREATE TABLE SintomasNotas... ✅
        ↓
PASO 4: CREATE TABLE TratamientosNotas... ✅
        ↓
PASO 5: Verificar con SELECT... ✅
        ↓
LISTO PARA EF CORE MIGRATIONS!
```

