-- =====================================================
-- MÓDULO GLOSARIO MÉDICO - INSTALACIÓN COMPLETA
-- =====================================================
-- Archivo: 00_Install_Glossary_Complete.sql
-- Descripción: Script único que instala el módulo completo
-- Ejecuta:
--   1. Creación de tablas
--   2. Bootstrap de datos iniciales
-- =====================================================

USE eiibd26;
GO

PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║   MÓDULO GLOSARIO MÉDICO - INSTALACIÓN COMPLETA   ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';

-- =====================================================
-- PARTE 1: CREACIÓN DE TABLAS
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 1/2: CREANDO ESTRUCTURA DE TABLAS';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- 1.1 Tabla: GlossaryTerm
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTerm')
BEGIN
    CREATE TABLE [dbo].[GlossaryTerm] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Nombre] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(200) NOT NULL UNIQUE,
        [TipoTermino] INT NOT NULL,
        [Activo] BIT NOT NULL DEFAULT 1,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaActualizacion] DATETIME NULL
    );
    PRINT '✓ Tabla GlossaryTerm creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠ Tabla GlossaryTerm ya existe';
END
GO

-- 1.2 Tabla: GlossaryTermMedicalLink
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTermMedicalLink')
BEGIN
    CREATE TABLE [dbo].[GlossaryTermMedicalLink] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [GlossaryTermId] INT NOT NULL,
        [SintomaId] INT NULL,
        [TratamientoId] INT NULL,
        
        CONSTRAINT FK_GlossaryTermMedicalLink_GlossaryTerm 
            FOREIGN KEY (GlossaryTermId) 
            REFERENCES [dbo].[GlossaryTerm](Id) 
            ON DELETE CASCADE,
        
        CONSTRAINT CHK_OnlyOneMedicalId 
            CHECK ((SintomaId IS NOT NULL AND TratamientoId IS NULL) 
                OR (SintomaId IS NULL AND TratamientoId IS NOT NULL))
    );
    PRINT '✓ Tabla GlossaryTermMedicalLink creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠ Tabla GlossaryTermMedicalLink ya existe';
END
GO

-- 1.3 Índices para optimización
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTerm_Slug')
BEGIN
    CREATE UNIQUE INDEX IX_GlossaryTerm_Slug ON [dbo].[GlossaryTerm]([Slug]);
    PRINT '✓ Índice IX_GlossaryTerm_Slug creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTerm_TipoTermino')
BEGIN
    CREATE INDEX IX_GlossaryTerm_TipoTermino 
        ON [dbo].[GlossaryTerm]([TipoTermino]) INCLUDE ([Activo]);
    PRINT '✓ Índice IX_GlossaryTerm_TipoTermino creado';
END

-- UNIQUE, no simple: el modelo EF trata MedicalLink como navegación 1:1, así que
-- un término con dos links deja la definición al azar. Lo garantiza la base.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_GlossaryTermMedicalLink_Term')
BEGIN
    CREATE UNIQUE INDEX UQ_GlossaryTermMedicalLink_Term
        ON [dbo].[GlossaryTermMedicalLink]([GlossaryTermId]);
    PRINT '✓ Índice UQ_GlossaryTermMedicalLink_Term creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTermMedicalLink_SintomaId')
BEGIN
    CREATE INDEX IX_GlossaryTermMedicalLink_SintomaId 
        ON [dbo].[GlossaryTermMedicalLink]([SintomaId]) WHERE SintomaId IS NOT NULL;
    PRINT '✓ Índice IX_GlossaryTermMedicalLink_SintomaId creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTermMedicalLink_TratamientoId')
BEGIN
    CREATE INDEX IX_GlossaryTermMedicalLink_TratamientoId 
        ON [dbo].[GlossaryTermMedicalLink]([TratamientoId]) WHERE TratamientoId IS NOT NULL;
    PRINT '✓ Índice IX_GlossaryTermMedicalLink_TratamientoId creado';
END
GO

PRINT '';
PRINT '✓✓✓ ESTRUCTURA DE TABLAS COMPLETADA ✓✓✓';
PRINT '';

-- =====================================================
-- PARTE 2: BOOTSTRAP DE DATOS INICIALES
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 2/2: POBLANDO DATOS INICIALES';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- 2.1 Función auxiliar: Generar Slug
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_GenerateSlug;
GO

CREATE FUNCTION dbo.fn_GenerateSlug(@nombre NVARCHAR(200))
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @slug NVARCHAR(200);
    
    SET @slug = LOWER(@nombre);
    
    -- Remover acentos comunes
    SET @slug = REPLACE(@slug, 'á', 'a');
    SET @slug = REPLACE(@slug, 'é', 'e');
    SET @slug = REPLACE(@slug, 'í', 'i');
    SET @slug = REPLACE(@slug, 'ó', 'o');
    SET @slug = REPLACE(@slug, 'ú', 'u');
    SET @slug = REPLACE(@slug, 'ñ', 'n');
    
    -- Remover paréntesis
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
PRINT '';

-- 2.2 Bootstrap desde Síntomas
PRINT '───────────────────────────────────────────────────';
PRINT '  PROCESANDO SÍNTOMAS...';
PRINT '───────────────────────────────────────────────────';

INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    s.nombre AS Nombre,
    dbo.fn_GenerateSlug(s.nombre) AS Slug,
    1 AS TipoTermino,
    CASE WHEN s.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
    GETDATE() AS FechaCreacion
FROM [dbo].[sintomas] s
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[GlossaryTerm] gt 
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
WHERE gt.TipoTermino = 1
AND NOT EXISTS (
    SELECT 1 FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id
);

DECLARE @sintomas_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_links, ' links médicos creados para síntomas');
PRINT '';

-- 2.3 Bootstrap desde Tratamientos
PRINT '───────────────────────────────────────────────────';
PRINT '  PROCESANDO TRATAMIENTOS...';
PRINT '───────────────────────────────────────────────────';

INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    t.nombre AS Nombre,
    dbo.fn_GenerateSlug(t.nombre) AS Slug,
    2 AS TipoTermino,
    CASE WHEN t.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
    GETDATE() AS FechaCreacion
FROM [dbo].[tratamientos] t
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[GlossaryTerm] gt 
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
WHERE gt.TipoTermino = 2
AND NOT EXISTS (
    SELECT 1 FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id
);

DECLARE @tratamientos_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_links, ' links médicos creados para tratamientos');
PRINT '';

-- =====================================================
-- RESUMEN FINAL
-- =====================================================
PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║              RESUMEN DE INSTALACIÓN               ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';

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
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║  ✓✓✓ INSTALACIÓN COMPLETADA EXITOSAMENTE ✓✓✓     ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '⚠️ RECORDATORIOS IMPORTANTES:';
PRINT '   ● GlossaryTerm solo contiene índice/metadata';
PRINT '   ● Las descripciones se leen desde sintomas/tratamientos';
PRINT '   ● Este es un proceso INICIAL, no recurrente';
PRINT '   ● PRÓXIMO PASO: Reiniciar la aplicación en Visual Studio';
PRINT '';
GO
