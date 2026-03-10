-- ============================================
-- ACTUALIZACIÓN DE SINTOMAS Y TRATAMIENTOS CON CAMPOS IA
-- EJECUTAR EN SQL SERVER
-- ============================================

-- ============================================
-- PASO 1: Actualizar tabla SINTOMAS
-- ============================================

-- Agregar columna RelacionEIIDescripcion si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEIIDescripcion')
BEGIN
    ALTER TABLE dbo.sintomas ADD RelacionEIIDescripcion NVARCHAR(255) NULL;
    PRINT '✅ Columna RelacionEIIDescripcion agregada a sintomas';
END
ELSE
    PRINT '⚠️ Columna RelacionEIIDescripcion ya existe en sintomas';

-- Cambiar RelacionEII de NVARCHAR a BIT si es necesario
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII' AND DATA_TYPE = 'nvarchar')
BEGIN
    -- Crear columna temporal
    ALTER TABLE dbo.sintomas ADD RelacionEII_Temp BIT DEFAULT 0;
    
    -- Copiar datos convertidos (si tiene texto, considerar TRUE)
    UPDATE dbo.sintomas 
    SET RelacionEII_Temp = CASE 
        WHEN LEN(ISNULL(RelacionEII, '')) > 0 THEN 1 
        ELSE 0 
    END;
    
    -- Eliminar columna antigua
    ALTER TABLE dbo.sintomas DROP COLUMN RelacionEII;
    
    -- Renombrar columna temporal
    EXEC sp_rename 'dbo.sintomas.RelacionEII_Temp', 'RelacionEII', 'COLUMN';
    
    PRINT '✅ Columna RelacionEII convertida a BIT en sintomas';
END
ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII')
BEGIN
    ALTER TABLE dbo.sintomas ADD RelacionEII BIT DEFAULT 0;
    PRINT '✅ Columna RelacionEII agregada como BIT a sintomas';
END
ELSE
    PRINT '⚠️ Columna RelacionEII ya existe como BIT en sintomas';

-- ============================================
-- PASO 2: Actualizar tabla TRATAMIENTOS
-- ============================================

-- Agregar columna RelacionEIIDescripcion si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEIIDescripcion')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RelacionEIIDescripcion NVARCHAR(255) NULL;
    PRINT '✅ Columna RelacionEIIDescripcion agregada a tratamientos';
END
ELSE
    PRINT '⚠️ Columna RelacionEIIDescripcion ya existe en tratamientos';

-- Cambiar RelacionEII de NVARCHAR a BIT si es necesario
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII' AND DATA_TYPE = 'nvarchar')
BEGIN
    -- Crear columna temporal
    ALTER TABLE dbo.tratamientos ADD RelacionEII_Temp BIT DEFAULT 0;
    
    -- Copiar datos convertidos
    UPDATE dbo.tratamientos 
    SET RelacionEII_Temp = CASE 
        WHEN LEN(ISNULL(RelacionEII, '')) > 0 THEN 1 
        ELSE 0 
    END;
    
    -- Eliminar columna antigua
    ALTER TABLE dbo.tratamientos DROP COLUMN RelacionEII;
    
    -- Renombrar columna temporal
    EXEC sp_rename 'dbo.tratamientos.RelacionEII_Temp', 'RelacionEII', 'COLUMN';
    
    PRINT '✅ Columna RelacionEII convertida a BIT en tratamientos';
END
ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RelacionEII BIT DEFAULT 0;
    PRINT '✅ Columna RelacionEII agregada como BIT a tratamientos';
END
ELSE
    PRINT '⚠️ Columna RelacionEII ya existe como BIT en tratamientos';

-- ============================================
-- PASO 3: Rellenar datos NULL con valores por defecto
-- ============================================

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

PRINT '✅ Datos NULL rellenados en sintomas';

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

PRINT '✅ Datos NULL rellenados en tratamientos';

-- ============================================
-- PASO 4: Verificar estructura final
-- ============================================

PRINT '';
PRINT '=== ESTRUCTURA SINTOMAS ===';
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'sintomas'
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'RelacionEIIDescripcion', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== ESTRUCTURA TRATAMIENTOS ===';
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tratamientos'
AND COLUMN_NAME IN ('DescripcionIA', 'ValidadoIA', 'ValidadoHumano', 'RelacionEII', 'RelacionEIIDescripcion', 'FechaActualizacionIA')
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '✅ ========================================';
PRINT '✅ ACTUALIZACIÓN COMPLETADA EXITOSAMENTE';
PRINT '✅ ========================================';
