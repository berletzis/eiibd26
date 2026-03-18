-- =====================================================
-- Script: Agregar columnas faltantes en producción
-- Fecha: 2025
-- Descripción: Las tablas sintomas, tratamientos y AspNetUsers
--   en producción fueron creadas manualmente sin las columnas
--   de IA/validación. Este script las agrega de forma idempotente.
-- =====================================================

-- =====================================================
-- 1. AspNetUsers: RequiresPasswordReset
-- =====================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'RequiresPasswordReset'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [RequiresPasswordReset] BIT NOT NULL DEFAULT 0;
    PRINT 'Added RequiresPasswordReset to AspNetUsers';
END
ELSE
    PRINT 'RequiresPasswordReset already exists in AspNetUsers';
GO

-- =====================================================
-- 2. sintomas: Columnas de IA y validación
-- =====================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'DescripcionIA'
)
BEGIN
    ALTER TABLE [sintomas] ADD [DescripcionIA] NVARCHAR(MAX) NULL;
    PRINT 'Added DescripcionIA to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'ValidadoIA'
)
BEGIN
    ALTER TABLE [sintomas] ADD [ValidadoIA] BIT NOT NULL DEFAULT 0;
    PRINT 'Added ValidadoIA to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'ValidadoHumano'
)
BEGIN
    ALTER TABLE [sintomas] ADD [ValidadoHumano] BIT NOT NULL DEFAULT 0;
    PRINT 'Added ValidadoHumano to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEIIDescripcion'
)
BEGIN
    ALTER TABLE [sintomas] ADD [RelacionEIIDescripcion] NVARCHAR(1000) NULL;
    PRINT 'Added RelacionEIIDescripcion to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII'
)
BEGIN
    ALTER TABLE [sintomas] ADD [RelacionEII] BIT NOT NULL DEFAULT 0;
    PRINT 'Added RelacionEII to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'Fuentes'
)
BEGIN
    ALTER TABLE [sintomas] ADD [Fuentes] NVARCHAR(500) NULL;
    PRINT 'Added Fuentes to sintomas';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'FechaActualizacionIA'
)
BEGIN
    ALTER TABLE [sintomas] ADD [FechaActualizacionIA] DATETIME2 NULL;
    PRINT 'Added FechaActualizacionIA to sintomas';
END
GO

-- =====================================================
-- 3. tratamientos: Columnas de IA y validación
-- =====================================================
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'DescripcionIA'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [DescripcionIA] NVARCHAR(MAX) NULL;
    PRINT 'Added DescripcionIA to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'ValidadoIA'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [ValidadoIA] BIT NOT NULL DEFAULT 0;
    PRINT 'Added ValidadoIA to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'ValidadoHumano'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [ValidadoHumano] BIT NOT NULL DEFAULT 0;
    PRINT 'Added ValidadoHumano to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEIIDescripcion'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [RelacionEIIDescripcion] NVARCHAR(1000) NULL;
    PRINT 'Added RelacionEIIDescripcion to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [RelacionEII] BIT NOT NULL DEFAULT 0;
    PRINT 'Added RelacionEII to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'Fuentes'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [Fuentes] NVARCHAR(500) NULL;
    PRINT 'Added Fuentes to tratamientos';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'FechaActualizacionIA'
)
BEGIN
    ALTER TABLE [tratamientos] ADD [FechaActualizacionIA] DATETIME2 NULL;
    PRINT 'Added FechaActualizacionIA to tratamientos';
END
GO

PRINT '✅ Script completado. Todas las columnas verificadas.';
GO
