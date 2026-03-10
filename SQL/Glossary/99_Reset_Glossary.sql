-- =====================================================
-- LIMPIAR MÓDULO GLOSARIO (RESET COMPLETO)
-- =====================================================
-- Archivo: 99_Reset_Glossary.sql
-- ⚠️ CUIDADO: Este script ELIMINA todas las tablas del glosario
-- Solo ejecutar si quieres empezar desde cero
-- =====================================================

USE eiibd26;
GO

PRINT '';
PRINT '⚠️⚠️⚠️ ADVERTENCIA: LIMPIANDO MÓDULO GLOSARIO ⚠️⚠️⚠️';
PRINT '';

-- Eliminar función
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_GenerateSlug;
    PRINT '✓ Función fn_GenerateSlug eliminada';
END

-- Eliminar tablas (en orden por FKs)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTermMedicalLink')
BEGIN
    DROP TABLE [dbo].[GlossaryTermMedicalLink];
    PRINT '✓ Tabla GlossaryTermMedicalLink eliminada';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTerm')
BEGIN
    DROP TABLE [dbo].[GlossaryTerm];
    PRINT '✓ Tabla GlossaryTerm eliminada';
END

PRINT '';
PRINT '✓✓✓ LIMPIEZA COMPLETADA ✓✓✓';
PRINT '';
PRINT 'Ahora puedes ejecutar de nuevo: 00_Install_Glossary_Complete.sql';
GO
