-- =====================================================
-- SOFT DELETE DE REGISTROS DUPLICADOS
-- =====================================================
-- ⚠️ Este script marca como eliminados los duplicados
-- ⚠️ Mantiene solo el registro con ID más bajo por cada slug
-- =====================================================

USE eiibd26;
GO

PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║    SOFT DELETE DE DUPLICADOS - MODO SEGURO        ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '⚠️ Este script usa transacciones. Si algo sale mal, puedes hacer ROLLBACK.';
PRINT '';

-- =====================================================
-- FUNCIÓN SLUG (temporal si no existe)
-- =====================================================
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NULL
BEGIN
    EXEC('
    CREATE FUNCTION dbo.fn_GenerateSlug(@nombre NVARCHAR(200))
    RETURNS NVARCHAR(200)
    AS
    BEGIN
        DECLARE @slug NVARCHAR(200);
        SET @slug = LOWER(@nombre);
        SET @slug = REPLACE(@slug, ''á'', ''a'');
        SET @slug = REPLACE(@slug, ''é'', ''e'');
        SET @slug = REPLACE(@slug, ''í'', ''i'');
        SET @slug = REPLACE(@slug, ''ó'', ''o'');
        SET @slug = REPLACE(@slug, ''ú'', ''u'');
        SET @slug = REPLACE(@slug, ''ñ'', ''n'');
        SET @slug = REPLACE(@slug, ''('', '''');
        SET @slug = REPLACE(@slug, '')'', '''');
        SET @slug = REPLACE(@slug, '' '', ''-'');
        SET @slug = REPLACE(@slug, ''/'', ''-'');
        SET @slug = REPLACE(@slug, '','', '''');
        SET @slug = REPLACE(@slug, ''.'', '''');
        SET @slug = REPLACE(@slug, '':'', '''');
        SET @slug = REPLACE(@slug, '';'', '''');
        WHILE CHARINDEX(''--'', @slug) > 0
            SET @slug = REPLACE(@slug, ''--'', ''-'');
        SET @slug = TRIM(''-'' FROM @slug);
        RETURN @slug;
    END
    ');
END
GO

-- =====================================================
-- PASO 1: VISTA PREVIA - QUÉ SE VA A ELIMINAR
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 1: VISTA PREVIA - REGISTROS A ELIMINAR';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Síntomas que se eliminarán
PRINT '--- SÍNTOMAS QUE SE MARCARÁN COMO ELIMINADOS ---';
WITH SintomasOrdenados AS (
    SELECT 
        id,
        nombre,
        dbo.fn_GenerateSlug(nombre) AS Slug,
        Eliminado,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0  -- Solo afecta activos
)
SELECT 
    id AS ID,
    nombre AS Nombre,
    Slug,
    '❌ Se eliminará (duplicado)' AS Accion
FROM SintomasOrdenados
WHERE RowNum > 1  -- Solo los duplicados (no el primero)
ORDER BY Slug, id;

DECLARE @sintomas_a_eliminar INT;
SELECT @sintomas_a_eliminar = COUNT(*)
FROM (
    SELECT 
        id,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0
) AS Calc
WHERE RowNum > 1;

PRINT '';
PRINT CONCAT('Total de síntomas a eliminar: ', ISNULL(@sintomas_a_eliminar, 0));
PRINT '';

-- Tratamientos que se eliminarán
PRINT '--- TRATAMIENTOS QUE SE MARCARÁN COMO ELIMINADOS ---';
WITH TratamientosOrdenados AS (
    SELECT 
        id,
        nombre,
        dbo.fn_GenerateSlug(nombre) AS Slug,
        Eliminado,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0  -- Solo afecta activos
)
SELECT 
    id AS ID,
    nombre AS Nombre,
    Slug,
    '❌ Se eliminará (duplicado)' AS Accion
FROM TratamientosOrdenados
WHERE RowNum > 1  -- Solo los duplicados (no el primero)
ORDER BY Slug, id;

DECLARE @tratamientos_a_eliminar INT;
SELECT @tratamientos_a_eliminar = COUNT(*)
FROM (
    SELECT 
        id,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0
) AS Calc
WHERE RowNum > 1;

PRINT '';
PRINT CONCAT('Total de tratamientos a eliminar: ', ISNULL(@tratamientos_a_eliminar, 0));
PRINT '';

-- =====================================================
-- PASO 2: CONFIRMACIÓN MANUAL
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 2: CONFIRMACIÓN REQUERIDA';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';
PRINT '⚠️ REVISA LOS REGISTROS DE ARRIBA ⚠️';
PRINT '';
PRINT 'Si estás de acuerdo, ejecuta la siguiente sección:';
PRINT '  → Descomenta las líneas entre BEGIN TRANSACTION y COMMIT';
PRINT '  → O ejecuta solo esa sección';
PRINT '';
PRINT 'Si algo sale mal:';
PRINT '  → Ejecuta: ROLLBACK TRANSACTION;';
PRINT '';

/*
-- =====================================================
-- PASO 3: EJECUCIÓN - DESCOMENTA ESTA SECCIÓN PARA EJECUTAR
-- =====================================================

BEGIN TRANSACTION;

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  EJECUTANDO SOFT DELETE...';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Soft delete en SÍNTOMAS (mantener solo el ID más bajo por slug)
UPDATE s
SET 
    s.Eliminado = 1,
    s.FechaModificacion = GETDATE()
FROM [dbo].[sintomas] s
INNER JOIN (
    SELECT 
        id,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0
) AS Ranked ON s.id = Ranked.id
WHERE Ranked.RowNum > 1;  -- Solo duplicados (no el primero)

DECLARE @sintomas_eliminados INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_eliminados, ' síntomas marcados como eliminados');

-- Soft delete en TRATAMIENTOS (mantener solo el ID más bajo por slug)
UPDATE t
SET 
    t.Eliminado = 1,
    t.FechaModificacion = GETDATE()
FROM [dbo].[tratamientos] t
INNER JOIN (
    SELECT 
        id,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(nombre) 
            ORDER BY id ASC
        ) AS RowNum
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL 
    AND LEN(TRIM(nombre)) > 0
    AND Eliminado = 0
) AS Ranked ON t.id = Ranked.id
WHERE Ranked.RowNum > 1;  -- Solo duplicados (no el primero)

DECLARE @tratamientos_eliminados INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_eliminados, ' tratamientos marcados como eliminados');

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  VERIFICACIÓN POST-ELIMINACIÓN';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Verificar que ya no hay duplicados activos
SELECT 
    'Síntomas' AS Tabla,
    COUNT(*) AS TotalActivos,
    COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS SlugUnicos,
    COUNT(*) - COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS DuplicadosRestantes
FROM [dbo].[sintomas]
WHERE nombre IS NOT NULL 
AND LEN(TRIM(nombre)) > 0
AND Eliminado = 0

UNION ALL

SELECT 
    'Tratamientos' AS Tabla,
    COUNT(*) AS TotalActivos,
    COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS SlugUnicos,
    COUNT(*) - COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS DuplicadosRestantes
FROM [dbo].[tratamientos]
WHERE nombre IS NOT NULL 
AND LEN(TRIM(nombre)) > 0
AND Eliminado = 0;

PRINT '';
PRINT '⚠️ IMPORTANTE: Revisa los resultados de arriba ⚠️';
PRINT '';
PRINT 'Si todo está correcto, ejecuta:';
PRINT '  → COMMIT TRANSACTION;';
PRINT '';
PRINT 'Si algo salió mal, ejecuta:';
PRINT '  → ROLLBACK TRANSACTION;';
PRINT '';

-- NO AUTO-COMMIT - Debe hacerse manualmente
-- Descomenta UNA de las siguientes líneas:

-- COMMIT TRANSACTION;     -- ✅ Confirmar cambios
-- ROLLBACK TRANSACTION;   -- ❌ Revertir cambios

*/

PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║              SCRIPT PREPARADO                      ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '📌 PRÓXIMOS PASOS:';
PRINT '   1. Revisa los registros que se van a eliminar arriba';
PRINT '   2. Descomenta la sección de EJECUCIÓN (líneas con /*)';
PRINT '   3. Ejecuta el script completo';
PRINT '   4. Verifica los resultados';
PRINT '   5. Ejecuta COMMIT o ROLLBACK según corresponda';
PRINT '';
GO
