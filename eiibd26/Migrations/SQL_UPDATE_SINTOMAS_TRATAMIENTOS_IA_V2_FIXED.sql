-- LIMPIEZA MANUAL DE EMERGENCIA
USE eiibd26;
GO

-- Limpiar sintomas
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII_Temp')
    ALTER TABLE dbo.sintomas DROP COLUMN RelacionEII_Temp;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'sintomas' AND COLUMN_NAME = 'RelacionEII_New')
    ALTER TABLE dbo.sintomas DROP COLUMN RelacionEII_New;

-- Limpiar tratamientos
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII_Temp')
    ALTER TABLE dbo.tratamientos DROP COLUMN RelacionEII_Temp;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tratamientos' AND COLUMN_NAME = 'RelacionEII_New')
    ALTER TABLE dbo.tratamientos DROP COLUMN RelacionEII_New;

PRINT '✅ Limpieza completada - Ahora ejecuta el script V2';