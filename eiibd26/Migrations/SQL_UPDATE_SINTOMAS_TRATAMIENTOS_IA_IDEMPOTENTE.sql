-- ============================================
-- SCRIPT IDEMPOTENTE - Actualización Sintomas y Tratamientos con IA
-- PUEDE EJECUTARSE MÚLTIPLES VECES SIN ERRORES
-- ============================================

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'INICIO - Actualización de Base de Datos';
PRINT '====================================';
PRINT '';

-- ============================================
-- PASO 1: ACTUALIZAR TABLA SINTOMAS
-- ============================================

PRINT '--- TABLA SINTOMAS ---';

-- 1.1 Agregar DescripcionIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'DescripcionIA')
BEGIN
    ALTER TABLE dbo.sintomas ADD DescripcionIA NVARCHAR(MAX) NULL;
    PRINT '✅ Columna DescripcionIA agregada';
END
ELSE
    PRINT '⚠️  Columna DescripcionIA ya existe (OK)';

-- 1.2 Agregar ValidadoIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'ValidadoIA')
BEGIN
    ALTER TABLE dbo.sintomas ADD ValidadoIA BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna ValidadoIA agregada';
END
ELSE
    PRINT '⚠️  Columna ValidadoIA ya existe (OK)';

-- 1.3 Agregar ValidadoHumano si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'ValidadoHumano')
BEGIN
    ALTER TABLE dbo.sintomas ADD ValidadoHumano BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna ValidadoHumano agregada';
END
ELSE
    PRINT '⚠️  Columna ValidadoHumano ya existe (OK)';

-- 1.4 Agregar RelacionEIIDescripcion si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEIIDescripcion')
BEGIN
    ALTER TABLE dbo.sintomas ADD RelacionEIIDescripcion NVARCHAR(255) NULL;
    PRINT '✅ Columna RelacionEIIDescripcion agregada';
END
ELSE
    PRINT '⚠️  Columna RelacionEIIDescripcion ya existe (OK)';

-- 1.5 Agregar FechaActualizacionIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'FechaActualizacionIA')
BEGIN
    ALTER TABLE dbo.sintomas ADD FechaActualizacionIA DATETIME NULL;
    PRINT '✅ Columna FechaActualizacionIA agregada';
END
ELSE
    PRINT '⚠️  Columna FechaActualizacionIA ya existe (OK)';

-- 1.6 Convertir RelacionEII de NVARCHAR a BIT si es necesario
DECLARE @RelacionEIIType NVARCHAR(50);
SELECT @RelacionEIIType = DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII';

IF @RelacionEIIType = 'nvarchar'
BEGIN
    PRINT '🔄 Convirtiendo RelacionEII de NVARCHAR a BIT...';
    
    -- Crear columna temporal
    ALTER TABLE dbo.sintomas ADD RelacionEII_Temp BIT DEFAULT 0 NOT NULL;
    
    -- Copiar datos convertidos
    UPDATE dbo.sintomas 
    SET RelacionEII_Temp = CASE 
        WHEN LEN(ISNULL(RelacionEII, '')) > 0 THEN 1 
        ELSE 0 
    END;
    
    -- Eliminar columna antigua
    ALTER TABLE dbo.sintomas DROP COLUMN RelacionEII;
    
    -- Renombrar columna temporal
    EXEC sp_rename 'dbo.sintomas.RelacionEII_Temp', 'RelacionEII', 'COLUMN';
    
    PRINT '✅ RelacionEII convertida a BIT';
END
ELSE IF @RelacionEIIType IS NULL
BEGIN
    -- No existe, crear como BIT
    ALTER TABLE dbo.sintomas ADD RelacionEII BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna RelacionEII agregada como BIT';
END
ELSE
BEGIN
    PRINT '⚠️  Columna RelacionEII ya es BIT (OK)';
END

-- 1.7 Crear índice en ValidadoIA si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_sintomas_ValidadoIA' AND object_id = OBJECT_ID('dbo.sintomas'))
BEGIN
    CREATE INDEX IX_sintomas_ValidadoIA ON dbo.sintomas(ValidadoIA);
    PRINT '✅ Índice IX_sintomas_ValidadoIA creado';
END
ELSE
    PRINT '⚠️  Índice IX_sintomas_ValidadoIA ya existe (OK)';

-- 1.8 Crear índice en FechaActualizacionIA si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_sintomas_FechaActualizacionIA' AND object_id = OBJECT_ID('dbo.sintomas'))
BEGIN
    CREATE INDEX IX_sintomas_FechaActualizacionIA ON dbo.sintomas(FechaActualizacionIA);
    PRINT '✅ Índice IX_sintomas_FechaActualizacionIA creado';
END
ELSE
    PRINT '⚠️  Índice IX_sintomas_FechaActualizacionIA ya existe (OK)';

PRINT '';

-- ============================================
-- PASO 2: ACTUALIZAR TABLA TRATAMIENTOS
-- ============================================

PRINT '--- TABLA TRATAMIENTOS ---';

-- 2.1 Agregar DescripcionIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'DescripcionIA')
BEGIN
    ALTER TABLE dbo.tratamientos ADD DescripcionIA NVARCHAR(MAX) NULL;
    PRINT '✅ Columna DescripcionIA agregada';
END
ELSE
    PRINT '⚠️  Columna DescripcionIA ya existe (OK)';

-- 2.2 Agregar ValidadoIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'ValidadoIA')
BEGIN
    ALTER TABLE dbo.tratamientos ADD ValidadoIA BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna ValidadoIA agregada';
END
ELSE
    PRINT '⚠️  Columna ValidadoIA ya existe (OK)';

-- 2.3 Agregar ValidadoHumano si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'ValidadoHumano')
BEGIN
    ALTER TABLE dbo.tratamientos ADD ValidadoHumano BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna ValidadoHumano agregada';
END
ELSE
    PRINT '⚠️  Columna ValidadoHumano ya existe (OK)';

-- 2.4 Agregar RelacionEIIDescripcion si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEIIDescripcion')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RelacionEIIDescripcion NVARCHAR(255) NULL;
    PRINT '✅ Columna RelacionEIIDescripcion agregada';
END
ELSE
    PRINT '⚠️  Columna RelacionEIIDescripcion ya existe (OK)';

-- 2.5 Agregar FechaActualizacionIA si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'FechaActualizacionIA')
BEGIN
    ALTER TABLE dbo.tratamientos ADD FechaActualizacionIA DATETIME NULL;
    PRINT '✅ Columna FechaActualizacionIA agregada';
END
ELSE
    PRINT '⚠️  Columna FechaActualizacionIA ya existe (OK)';

-- 2.6 Convertir RelacionEII de NVARCHAR a BIT si es necesario
DECLARE @TratRelacionEIIType NVARCHAR(50);
SELECT @TratRelacionEIIType = DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII';

IF @TratRelacionEIIType = 'nvarchar'
BEGIN
    PRINT '🔄 Convirtiendo RelacionEII de NVARCHAR a BIT...';
    
    ALTER TABLE dbo.tratamientos ADD RelacionEII_Temp BIT DEFAULT 0 NOT NULL;
    
    UPDATE dbo.tratamientos 
    SET RelacionEII_Temp = CASE 
        WHEN LEN(ISNULL(RelacionEII, '')) > 0 THEN 1 
        ELSE 0 
    END;
    
    ALTER TABLE dbo.tratamientos DROP COLUMN RelacionEII;
    EXEC sp_rename 'dbo.tratamientos.RelacionEII_Temp', 'RelacionEII', 'COLUMN';
    
    PRINT '✅ RelacionEII convertida a BIT';
END
ELSE IF @TratRelacionEIIType IS NULL
BEGIN
    ALTER TABLE dbo.tratamientos ADD RelacionEII BIT DEFAULT 0 NOT NULL;
    PRINT '✅ Columna RelacionEII agregada como BIT';
END
ELSE
BEGIN
    PRINT '⚠️  Columna RelacionEII ya es BIT (OK)';
END

-- 2.7 Crear índice en ValidadoIA si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tratamientos_ValidadoIA' AND object_id = OBJECT_ID('dbo.tratamientos'))
BEGIN
    CREATE INDEX IX_tratamientos_ValidadoIA ON dbo.tratamientos(ValidadoIA);
    PRINT '✅ Índice IX_tratamientos_ValidadoIA creado';
END
ELSE
    PRINT '⚠️  Índice IX_tratamientos_ValidadoIA ya existe (OK)';

-- 2.8 Crear índice en FechaActualizacionIA si no existe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tratamientos_FechaActualizacionIA' AND object_id = OBJECT_ID('dbo.tratamientos'))
BEGIN
    CREATE INDEX IX_tratamientos_FechaActualizacionIA ON dbo.tratamientos(FechaActualizacionIA);
    PRINT '✅ Índice IX_tratamientos_FechaActualizacionIA creado';
END
ELSE
    PRINT '⚠️  Índice IX_tratamientos_FechaActualizacionIA ya existe (OK)';

PRINT '';

-- ============================================
-- PASO 3: CREAR TABLA SINTOMASNOTAS SI NO EXISTE
-- ============================================

PRINT '--- TABLA SINTOMASNOTAS ---';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SintomasNotas')
BEGIN
    CREATE TABLE dbo.SintomasNotas (
        id INT PRIMARY KEY IDENTITY(1,1),
        SintomaId INT NOT NULL,
        UsuarioId UNIQUEIDENTIFIER NULL,
        Nota NVARCHAR(MAX) NOT NULL,
        EsNotaIA BIT DEFAULT 0 NOT NULL,
        FechaCreado DATETIME DEFAULT GETUTCDATE() NOT NULL,
        FechaModificado DATETIME DEFAULT GETUTCDATE() NOT NULL,
        Eliminado BIT DEFAULT 0 NOT NULL,
        
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
    
    PRINT '✅ Tabla SintomasNotas creada con índices';
END
ELSE
BEGIN
    PRINT '⚠️  Tabla SintomasNotas ya existe (OK)';
    
    -- Verificar y agregar índices faltantes
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SintomasNotas_SintomaId' AND object_id = OBJECT_ID('dbo.SintomasNotas'))
    BEGIN
        CREATE INDEX IX_SintomasNotas_SintomaId ON dbo.SintomasNotas(SintomaId);
        PRINT '✅ Índice IX_SintomasNotas_SintomaId creado';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SintomasNotas_EsNotaIA' AND object_id = OBJECT_ID('dbo.SintomasNotas'))
    BEGIN
        CREATE INDEX IX_SintomasNotas_EsNotaIA ON dbo.SintomasNotas(EsNotaIA);
        PRINT '✅ Índice IX_SintomasNotas_EsNotaIA creado';
    END
END

PRINT '';

-- ============================================
-- PASO 4: CREAR TABLA TRATAMIENTOSNOTAS SI NO EXISTE
-- ============================================

PRINT '--- TABLA TRATAMIENTOSNOTAS ---';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TratamientosNotas')
BEGIN
    CREATE TABLE dbo.TratamientosNotas (
        id INT PRIMARY KEY IDENTITY(1,1),
        TratamientoId INT NOT NULL,
        UsuarioId UNIQUEIDENTIFIER NULL,
        Nota NVARCHAR(MAX) NOT NULL,
        EsNotaIA BIT DEFAULT 0 NOT NULL,
        FechaCreado DATETIME DEFAULT GETUTCDATE() NOT NULL,
        FechaModificado DATETIME DEFAULT GETUTCDATE() NOT NULL,
        Eliminado BIT DEFAULT 0 NOT NULL,
        
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
    
    PRINT '✅ Tabla TratamientosNotas creada con índices';
END
ELSE
BEGIN
    PRINT '⚠️  Tabla TratamientosNotas ya existe (OK)';
    
    -- Verificar y agregar índices faltantes
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TratamientosNotas_TratamientoId' AND object_id = OBJECT_ID('dbo.TratamientosNotas'))
    BEGIN
        CREATE INDEX IX_TratamientosNotas_TratamientoId ON dbo.TratamientosNotas(TratamientoId);
        PRINT '✅ Índice IX_TratamientosNotas_TratamientoId creado';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TratamientosNotas_EsNotaIA' AND object_id = OBJECT_ID('dbo.TratamientosNotas'))
    BEGIN
        CREATE INDEX IX_TratamientosNotas_EsNotaIA ON dbo.TratamientosNotas(EsNotaIA);
        PRINT '✅ Índice IX_TratamientosNotas_EsNotaIA creado';
    END
END

PRINT '';

-- ============================================
-- PASO 5: ACTUALIZAR VALORES NULL
-- ============================================

PRINT '--- ACTUALIZACIÓN DE VALORES NULL ---';

-- Sintomas
UPDATE dbo.sintomas
SET 
    DescripcionIA = COALESCE(DescripcionIA, ''),
    ValidadoIA = COALESCE(ValidadoIA, 0),
    ValidadoHumano = COALESCE(ValidadoHumano, 0),
    RelacionEIIDescripcion = COALESCE(RelacionEIIDescripcion, ''),
    RelacionEII = COALESCE(RelacionEII, 0),
    FechaActualizacionIA = COALESCE(FechaActualizacionIA, GETUTCDATE())
WHERE DescripcionIA IS NULL 
   OR ValidadoIA IS NULL 
   OR ValidadoHumano IS NULL 
   OR RelacionEIIDescripcion IS NULL
   OR RelacionEII IS NULL
   OR FechaActualizacionIA IS NULL;

PRINT '✅ Valores NULL actualizados en sintomas';

-- Tratamientos
UPDATE dbo.tratamientos
SET 
    DescripcionIA = COALESCE(DescripcionIA, ''),
    ValidadoIA = COALESCE(ValidadoIA, 0),
    ValidadoHumano = COALESCE(ValidadoHumano, 0),
    RelacionEIIDescripcion = COALESCE(RelacionEIIDescripcion, ''),
    RelacionEII = COALESCE(RelacionEII, 0),
    FechaActualizacionIA = COALESCE(FechaActualizacionIA, GETUTCDATE())
WHERE DescripcionIA IS NULL 
   OR ValidadoIA IS NULL 
   OR ValidadoHumano IS NULL 
   OR RelacionEIIDescripcion IS NULL
   OR RelacionEII IS NULL
   OR FechaActualizacionIA IS NULL;

PRINT '✅ Valores NULL actualizados en tratamientos';

PRINT '';

-- ============================================
-- PASO 6: VERIFICACIÓN FINAL
-- ============================================

PRINT '--- VERIFICACIÓN FINAL ---';
PRINT '';

PRINT '=== SINTOMAS ===';
SELECT 
    COLUMN_NAME AS Columna, 
    DATA_TYPE AS Tipo, 
    CHARACTER_MAXIMUM_LENGTH AS Longitud,
    IS_NULLABLE AS Nullable
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sintomas'
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'RelacionEIIDescripcion', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== TRATAMIENTOS ===';
SELECT 
    COLUMN_NAME AS Columna, 
    DATA_TYPE AS Tipo, 
    CHARACTER_MAXIMUM_LENGTH AS Longitud,
    IS_NULLABLE AS Nullable
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tratamientos'
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'RelacionEIIDescripcion', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== TABLAS NUEVAS ===';
SELECT 
    TABLE_NAME AS Tabla,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS NumColumnas
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas')
ORDER BY TABLE_NAME;

PRINT '';
PRINT '====================================';
PRINT '✅ ACTUALIZACIÓN COMPLETADA EXITOSAMENTE';
PRINT '====================================';
PRINT '';
PRINT 'RESULTADO:';
PRINT '- Todas las columnas de IA están disponibles';
PRINT '- Índices creados para mejor rendimiento';
PRINT '- Tablas de notas configuradas';
PRINT '- Valores NULL actualizados';
PRINT '';
PRINT '🚀 Puedes descomentar los campos en C# y recompilar';
