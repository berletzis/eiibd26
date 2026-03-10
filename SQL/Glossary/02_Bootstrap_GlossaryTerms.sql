-- =====================================================
-- BOOTSTRAP INICIAL: POBLAR GLOSSARY DESDE SINTOMAS Y TRATAMIENTOS
-- =====================================================
-- Fecha: 2025-01-XX
-- Descripción: Crea registros iniciales en GlossaryTerm desde las tablas médicas
-- ⚠️ IMPORTANTE: Solo crea ÍNDICE, NO duplica descripciones médicas
-- =====================================================

USE eiibd26;
GO

-- =====================================================
-- FUNCIÓN AUXILIAR: Generar Slug
-- =====================================================
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_GenerateSlug;
GO

CREATE FUNCTION dbo.fn_GenerateSlug(@nombre NVARCHAR(200))
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @slug NVARCHAR(200);
    
    -- Convertir a minúsculas
    SET @slug = LOWER(@nombre);
    
    -- Remover acentos comunes
    SET @slug = REPLACE(@slug, 'á', 'a');
    SET @slug = REPLACE(@slug, 'é', 'e');
    SET @slug = REPLACE(@slug, 'í', 'i');
    SET @slug = REPLACE(@slug, 'ó', 'o');
    SET @slug = REPLACE(@slug, 'ú', 'u');
    SET @slug = REPLACE(@slug, 'ñ', 'n');
    
    -- Remover paréntesis y contenido
    SET @slug = REPLACE(@slug, '(', '');
    SET @slug = REPLACE(@slug, ')', '');
    
    -- Remover caracteres especiales
    SET @slug = REPLACE(@slug, ' ', '-');
    SET @slug = REPLACE(@slug, '/', '-');
    SET @slug = REPLACE(@slug, ',', '');
    SET @slug = REPLACE(@slug, '.', '');
    SET @slug = REPLACE(@slug, ':', '');
    SET @slug = REPLACE(@slug, ';', '');
    
    -- Remover guiones múltiples
    WHILE CHARINDEX('--', @slug) > 0
        SET @slug = REPLACE(@slug, '--', '-');
    
    -- Remover guiones al inicio/final
    SET @slug = TRIM('-' FROM @slug);
    
    RETURN @slug;
END
GO

PRINT '✓ Función fn_GenerateSlug creada';
GO

-- =====================================================
-- 1. BOOTSTRAP DESDE SÍNTOMAS
-- =====================================================
PRINT '';
PRINT '================================================';
PRINT 'INICIANDO BOOTSTRAP DESDE SÍNTOMAS...';
PRINT '================================================';

INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    s.nombre AS Nombre,
    dbo.fn_GenerateSlug(s.nombre) AS Slug,
    1 AS TipoTermino, -- 1 = Sintoma
    CASE WHEN s.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
    GETDATE() AS FechaCreacion
FROM [dbo].[sintomas] s
WHERE NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTerm] gt 
    WHERE gt.Slug = dbo.fn_GenerateSlug(s.nombre)
)
AND s.nombre IS NOT NULL
AND LEN(TRIM(s.nombre)) > 0;

DECLARE @sintomas_added INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_added, ' síntomas agregados al glosario');

-- Crear links médicos para síntomas
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT 
    gt.Id AS GlossaryTermId,
    s.id AS SintomaId,
    NULL AS TratamientoId
FROM [dbo].[sintomas] s
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(s.nombre)
WHERE gt.TipoTermino = 1 -- Sintoma
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id
);

DECLARE @sintomas_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_links, ' links médicos creados para síntomas');

-- =====================================================
-- 2. BOOTSTRAP DESDE TRATAMIENTOS
-- =====================================================
PRINT '';
PRINT '================================================';
PRINT 'INICIANDO BOOTSTRAP DESDE TRATAMIENTOS...';
PRINT '================================================';

INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    t.nombre AS Nombre,
    dbo.fn_GenerateSlug(t.nombre) AS Slug,
    2 AS TipoTermino, -- 2 = Tratamiento
    CASE WHEN t.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
    GETDATE() AS FechaCreacion
FROM [dbo].[tratamientos] t
WHERE NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTerm] gt 
    WHERE gt.Slug = dbo.fn_GenerateSlug(t.nombre)
)
AND t.nombre IS NOT NULL
AND LEN(TRIM(t.nombre)) > 0;

DECLARE @tratamientos_added INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_added, ' tratamientos agregados al glosario');

-- Crear links médicos para tratamientos
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT 
    gt.Id AS GlossaryTermId,
    NULL AS SintomaId,
    t.id AS TratamientoId
FROM [dbo].[tratamientos] t
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(t.nombre)
WHERE gt.TipoTermino = 2 -- Tratamiento
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id
);

DECLARE @tratamientos_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_links, ' links médicos creados para tratamientos');

-- =====================================================
-- 3. RESUMEN FINAL
-- =====================================================
PRINT '';
PRINT '================================================';
PRINT 'RESUMEN DEL BOOTSTRAP';
PRINT '================================================';

SELECT 
    CASE TipoTermino 
        WHEN 1 THEN 'Síntomas'
        WHEN 2 THEN 'Tratamientos'
    END AS Tipo,
    COUNT(*) AS Total,
    SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) AS Activos,
    SUM(CASE WHEN Activo = 0 THEN 1 ELSE 0 END) AS Inactivos
FROM [dbo].[GlossaryTerm]
GROUP BY TipoTermino
ORDER BY TipoTermino;

PRINT '';
PRINT '✓✓✓ BOOTSTRAP COMPLETADO EXITOSAMENTE ✓✓✓';
PRINT '';
PRINT '⚠️ RECORDATORIO:';
PRINT '   - GlossaryTerm solo contiene índice/metadata';
PRINT '   - Las descripciones médicas SIEMPRE se leen desde sintomas/tratamientos';
PRINT '   - Este es un proceso INICIAL, no recurrente';
GO
