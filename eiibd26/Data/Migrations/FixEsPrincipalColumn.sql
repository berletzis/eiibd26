-- =====================================================
-- Migration: Fix EsPrincipal column to be NOT NULL
-- =====================================================
-- This script fixes the EsPrincipal column that currently
-- allows NULL values, which causes SqlNullValueException
-- =====================================================

BEGIN TRANSACTION;

-- Step 1: Update all NULL values to 0 (false)
PRINT 'Step 1: Updating NULL values to 0...';
UPDATE dbo.contenidosCategoriasRelacion
SET EsPrincipal = 0
WHERE EsPrincipal IS NULL;
PRINT 'Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- Step 2: Alter column to NOT NULL
PRINT 'Step 2: Altering column to NOT NULL...';
ALTER TABLE dbo.contenidosCategoriasRelacion
ALTER COLUMN EsPrincipal BIT NOT NULL;
PRINT 'Column altered successfully';

-- Step 3: Add default constraint
PRINT 'Step 3: Adding default constraint...';
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints 
               WHERE parent_object_id = OBJECT_ID('dbo.contenidosCategoriasRelacion') 
               AND name = 'DF_ContenidosCategoriasRelacion_EsPrincipal')
BEGIN
    ALTER TABLE dbo.contenidosCategoriasRelacion
    ADD CONSTRAINT DF_ContenidosCategoriasRelacion_EsPrincipal DEFAULT (0) FOR EsPrincipal;
    PRINT 'Default constraint added';
END
ELSE
BEGIN
    PRINT 'Default constraint already exists';
END

-- Step 4: Mark the most recent category as primary for each content
PRINT 'Step 4: Marking primary categories...';
WITH RankedCategories AS (
    SELECT 
        Sequence,
        IdContenido,
        IdCategoria,
        ROW_NUMBER() OVER (PARTITION BY IdContenido ORDER BY FechaCreacion DESC) AS RowNum
    FROM dbo.contenidosCategoriasRelacion
    WHERE Borrado = 0 AND IdCategoria IS NOT NULL
)
UPDATE ccr
SET ccr.EsPrincipal = 1,
    ccr.FechaModificacion = GETUTCDATE()
FROM dbo.contenidosCategoriasRelacion ccr
INNER JOIN RankedCategories rc ON ccr.Sequence = rc.Sequence
WHERE rc.RowNum = 1 AND ccr.EsPrincipal = 0;
PRINT 'Primary categories marked: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- Step 5: Create index for better query performance
PRINT 'Step 5: Creating performance index...';
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE object_id = OBJECT_ID('dbo.contenidosCategoriasRelacion') 
               AND name = 'IX_ContenidosCategoriasRelacion_EsPrincipal')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContenidosCategoriasRelacion_EsPrincipal
    ON dbo.contenidosCategoriasRelacion (IdContenido, EsPrincipal)
    INCLUDE (IdCategoria, Borrado)
    WHERE EsPrincipal = 1 AND Borrado = 0;
    PRINT 'Index created successfully';
END
ELSE
BEGIN
    PRINT 'Index already exists';
END

-- Step 6: Verification
PRINT '';
PRINT '=== VERIFICATION ===';
PRINT 'Total rows in table: ' + CAST((SELECT COUNT(*) FROM dbo.contenidosCategoriasRelacion) AS VARCHAR);
PRINT 'Rows with EsPrincipal = 1: ' + CAST((SELECT COUNT(*) FROM dbo.contenidosCategoriasRelacion WHERE EsPrincipal = 1) AS VARCHAR);
PRINT 'Rows with EsPrincipal = 0: ' + CAST((SELECT COUNT(*) FROM dbo.contenidosCategoriasRelacion WHERE EsPrincipal = 0) AS VARCHAR);
PRINT 'Rows with NULL EsPrincipal: ' + CAST((SELECT COUNT(*) FROM dbo.contenidosCategoriasRelacion WHERE EsPrincipal IS NULL) AS VARCHAR);

-- Verify each content has exactly one primary category
SELECT 
    'Contents with multiple primary categories' AS Issue,
    COUNT(*) AS Count
FROM (
    SELECT IdContenido
    FROM dbo.contenidosCategoriasRelacion
    WHERE Borrado = 0 AND EsPrincipal = 1
    GROUP BY IdContenido
    HAVING COUNT(*) > 1
) AS MultiPrimary;

SELECT 
    'Contents with no primary category' AS Issue,
    COUNT(*) AS Count
FROM (
    SELECT c.Id
    FROM dbo.contenidos c
    WHERE c.Eliminado = 0
      AND NOT EXISTS (
          SELECT 1 
          FROM dbo.contenidosCategoriasRelacion ccr
          WHERE ccr.IdContenido = c.Id 
            AND ccr.Borrado = 0 
            AND ccr.EsPrincipal = 1
      )
) AS NoPrimary;

COMMIT TRANSACTION;
PRINT '';
PRINT '✅ Migration completed successfully!';
PRINT 'You can now restart your application.';
