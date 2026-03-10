-- =====================================================
-- DIAGNÓSTICO: DETECTAR DUPLICADOS EN SINTOMAS Y TRATAMIENTOS
-- =====================================================
-- Este script analiza qué registros generarían slugs duplicados
-- =====================================================

USE eiibd26;
GO

-- =====================================================
-- FUNCIÓN TEMPORAL PARA GENERAR SLUG (si no existe)
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

PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║         DIAGNÓSTICO DE DUPLICADOS EN GLOSARIO     ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';

-- =====================================================
-- 1. RESUMEN GENERAL
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  RESUMEN GENERAL';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

SELECT 
    'Síntomas' AS Tabla,
    COUNT(*) AS TotalRegistros,
    COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS SlugUnicos,
    COUNT(*) - COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS Duplicados
FROM [dbo].[sintomas]
WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0

UNION ALL

SELECT 
    'Tratamientos' AS Tabla,
    COUNT(*) AS TotalRegistros,
    COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS SlugUnicos,
    COUNT(*) - COUNT(DISTINCT dbo.fn_GenerateSlug(nombre)) AS Duplicados
FROM [dbo].[tratamientos]
WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0;

PRINT '';

-- =====================================================
-- 2. SÍNTOMAS DUPLICADOS (DETALLE)
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  SÍNTOMAS CON SLUG DUPLICADO (DETALLE)';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

WITH SintomasSlugs AS (
    SELECT 
        id,
        nombre,
        dbo.fn_GenerateSlug(nombre) AS Slug,
        Eliminado,
        CASE WHEN Eliminado = 0 THEN 'Activo' ELSE 'Eliminado' END AS Estado,
        COUNT(*) OVER (PARTITION BY dbo.fn_GenerateSlug(nombre)) AS CantidadDuplicados
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
)
SELECT 
    Slug,
    CantidadDuplicados AS '# Duplicados',
    id AS ID,
    nombre AS Nombre,
    Estado,
    Eliminado
FROM SintomasSlugs
WHERE CantidadDuplicados > 1
ORDER BY Slug, Eliminado, id;

DECLARE @sintomasDuplicados INT;
SELECT @sintomasDuplicados = COUNT(DISTINCT dbo.fn_GenerateSlug(nombre))
FROM [dbo].[sintomas]
WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
GROUP BY dbo.fn_GenerateSlug(nombre)
HAVING COUNT(*) > 1;

PRINT '';
PRINT CONCAT('Total de slugs duplicados en síntomas: ', ISNULL(@sintomasDuplicados, 0));
PRINT '';

-- =====================================================
-- 3. TRATAMIENTOS DUPLICADOS (DETALLE)
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  TRATAMIENTOS CON SLUG DUPLICADO (DETALLE)';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

WITH TratamientosSlugs AS (
    SELECT 
        id,
        nombre,
        dbo.fn_GenerateSlug(nombre) AS Slug,
        Eliminado,
        CASE WHEN Eliminado = 0 THEN 'Activo' ELSE 'Eliminado' END AS Estado,
        COUNT(*) OVER (PARTITION BY dbo.fn_GenerateSlug(nombre)) AS CantidadDuplicados
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
)
SELECT 
    Slug,
    CantidadDuplicados AS '# Duplicados',
    id AS ID,
    nombre AS Nombre,
    Estado,
    Eliminado
FROM TratamientosSlugs
WHERE CantidadDuplicados > 1
ORDER BY Slug, Eliminado, id;

DECLARE @tratamientosDuplicados INT;
SELECT @tratamientosDuplicados = COUNT(DISTINCT dbo.fn_GenerateSlug(nombre))
FROM [dbo].[tratamientos]
WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
GROUP BY dbo.fn_GenerateSlug(nombre)
HAVING COUNT(*) > 1;

PRINT '';
PRINT CONCAT('Total de slugs duplicados en tratamientos: ', ISNULL(@tratamientosDuplicados, 0));
PRINT '';

-- =====================================================
-- 4. ANÁLISIS POR TIPO DE DUPLICADO
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  ANÁLISIS DE DUPLICADOS';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Síntomas: ¿Cuántos son todos activos vs uno activo y otros eliminados?
SELECT 
    'Síntomas' AS Tabla,
    CASE 
        WHEN TodosActivos = CantDuplicados THEN 'Todos Activos'
        WHEN TodosEliminados = CantDuplicados THEN 'Todos Eliminados'
        ELSE 'Mixto (Activos + Eliminados)'
    END AS TipoDuplicado,
    COUNT(*) AS CantidadCasos
FROM (
    SELECT 
        dbo.fn_GenerateSlug(nombre) AS Slug,
        COUNT(*) AS CantDuplicados,
        SUM(CASE WHEN Eliminado = 0 THEN 1 ELSE 0 END) AS TodosActivos,
        SUM(CASE WHEN Eliminado = 1 THEN 1 ELSE 0 END) AS TodosEliminados
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
    GROUP BY dbo.fn_GenerateSlug(nombre)
    HAVING COUNT(*) > 1
) AS Analisis
GROUP BY 
    CASE 
        WHEN TodosActivos = CantDuplicados THEN 'Todos Activos'
        WHEN TodosEliminados = CantDuplicados THEN 'Todos Eliminados'
        ELSE 'Mixto (Activos + Eliminados)'
    END

UNION ALL

-- Tratamientos: mismo análisis
SELECT 
    'Tratamientos' AS Tabla,
    CASE 
        WHEN TodosActivos = CantDuplicados THEN 'Todos Activos'
        WHEN TodosEliminados = CantDuplicados THEN 'Todos Eliminados'
        ELSE 'Mixto (Activos + Eliminados)'
    END AS TipoDuplicado,
    COUNT(*) AS CantidadCasos
FROM (
    SELECT 
        dbo.fn_GenerateSlug(nombre) AS Slug,
        COUNT(*) AS CantDuplicados,
        SUM(CASE WHEN Eliminado = 0 THEN 1 ELSE 0 END) AS TodosActivos,
        SUM(CASE WHEN Eliminado = 1 THEN 1 ELSE 0 END) AS TodosEliminados
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
    GROUP BY dbo.fn_GenerateSlug(nombre)
    HAVING COUNT(*) > 1
) AS Analisis
GROUP BY 
    CASE 
        WHEN TodosActivos = CantDuplicados THEN 'Todos Activos'
        WHEN TodosEliminados = CantDuplicados THEN 'Todos Eliminados'
        ELSE 'Mixto (Activos + Eliminados)'
    END;

PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║              FIN DEL DIAGNÓSTICO                   ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '📌 OPCIONES PARA RESOLVER:';
PRINT '   1. Tomar solo registros ACTIVOS (Eliminado=0)';
PRINT '   2. Tomar el primer ID de cada grupo duplicado';
PRINT '   3. Crear slugs únicos agregando sufijos (-1, -2, etc.)';
PRINT '   4. Revisar manualmente y limpiar duplicados en la base de datos';
PRINT '';
GO
