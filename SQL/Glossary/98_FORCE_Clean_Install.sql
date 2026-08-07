-- =====================================================
-- INSTALACIÓN FORZOSA: LIMPIA Y REINSTALA SIN ERRORES
-- =====================================================
-- ⚠️ ESTE SCRIPT ELIMINA TODO Y REINSTALA DESDE CERO
-- =====================================================

USE eiibd26;
GO

PRINT '';
PRINT '⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️';
PRINT '  INSTALACIÓN FORZOSA - ELIMINANDO TODO PRIMERO';
PRINT '⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️';
PRINT '';

-- =====================================================
-- PASO 1: ELIMINAR TODO (SIN PREGUNTAR)
-- =====================================================

-- Eliminar función
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_GenerateSlug;
    PRINT '✓ Función fn_GenerateSlug eliminada';
END

-- Eliminar tablas
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

-- =====================================================
-- PASO 2: CREAR TABLAS LIMPIAS
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  CREANDO ESTRUCTURA DE TABLAS...';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Tabla: GlossaryTerm
CREATE TABLE [dbo].[GlossaryTerm] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Nombre] NVARCHAR(200) NOT NULL,
    [Slug] NVARCHAR(200) NOT NULL UNIQUE,
    [TipoTermino] INT NOT NULL,
    [Activo] BIT NOT NULL DEFAULT 1,
    [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
    [FechaActualizacion] DATETIME NULL
);
PRINT '✓ Tabla GlossaryTerm creada';

-- Tabla: GlossaryTermMedicalLink
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
PRINT '✓ Tabla GlossaryTermMedicalLink creada';

-- Índices
CREATE UNIQUE INDEX IX_GlossaryTerm_Slug ON [dbo].[GlossaryTerm]([Slug]);
CREATE INDEX IX_GlossaryTerm_TipoTermino ON [dbo].[GlossaryTerm]([TipoTermino]) INCLUDE ([Activo]);
-- UNIQUE: el modelo EF trata MedicalLink como navegación 1:1 (ver dedupe-glossarytermmedicallink-unique.sql)
CREATE UNIQUE INDEX UQ_GlossaryTermMedicalLink_Term ON [dbo].[GlossaryTermMedicalLink]([GlossaryTermId]);
CREATE INDEX IX_GlossaryTermMedicalLink_SintomaId ON [dbo].[GlossaryTermMedicalLink]([SintomaId]) WHERE SintomaId IS NOT NULL;
CREATE INDEX IX_GlossaryTermMedicalLink_TratamientoId ON [dbo].[GlossaryTermMedicalLink]([TratamientoId]) WHERE TratamientoId IS NOT NULL;
PRINT '✓ Índices creados';

PRINT '';
PRINT '✓✓✓ ESTRUCTURA COMPLETADA ✓✓✓';
PRINT '';

-- =====================================================
-- PASO 3: FUNCIÓN SLUG
-- =====================================================
GO
CREATE FUNCTION dbo.fn_GenerateSlug(@nombre NVARCHAR(200))
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @slug NVARCHAR(200);
    SET @slug = LOWER(@nombre);
    SET @slug = REPLACE(@slug, 'á', 'a');
    SET @slug = REPLACE(@slug, 'é', 'e');
    SET @slug = REPLACE(@slug, 'í', 'i');
    SET @slug = REPLACE(@slug, 'ó', 'o');
    SET @slug = REPLACE(@slug, 'ú', 'u');
    SET @slug = REPLACE(@slug, 'ñ', 'n');
    SET @slug = REPLACE(@slug, '(', '');
    SET @slug = REPLACE(@slug, ')', '');
    SET @slug = REPLACE(@slug, ' ', '-');
    SET @slug = REPLACE(@slug, '/', '-');
    SET @slug = REPLACE(@slug, ',', '');
    SET @slug = REPLACE(@slug, '.', '');
    SET @slug = REPLACE(@slug, ':', '');
    SET @slug = REPLACE(@slug, ';', '');
    WHILE CHARINDEX('--', @slug) > 0
        SET @slug = REPLACE(@slug, '--', '-');
    SET @slug = TRIM('-' FROM @slug);
    RETURN @slug;
END
GO
PRINT '✓ Función fn_GenerateSlug creada';
PRINT '';

-- =====================================================
-- PASO 4: POBLAR DATOS
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  POBLANDO DATOS INICIALES...';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Síntomas (con manejo de duplicados)
-- ⚠️ Si hay múltiples síntomas con el mismo slug, toma el primero activo
INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    Nombre,
    Slug,
    1 AS TipoTermino,
    Activo,
    GETDATE() AS FechaCreacion
FROM (
    SELECT 
        s.nombre AS Nombre,
        dbo.fn_GenerateSlug(s.nombre) AS Slug,
        CASE WHEN s.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(s.nombre) 
            ORDER BY 
                CASE WHEN s.Eliminado = 0 THEN 0 ELSE 1 END, -- Activos primero
                s.id -- En caso de empate, el primer ID
        ) AS RowNum
    FROM [dbo].[sintomas] s
    WHERE s.nombre IS NOT NULL
    AND LEN(TRIM(s.nombre)) > 0
) AS Deduplicated
WHERE RowNum = 1;

DECLARE @sintomas_added INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_added, ' síntomas agregados (duplicados eliminados)');

-- Links síntomas
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT gt.Id, s.id, NULL
FROM [dbo].[sintomas] s
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(s.nombre)
WHERE gt.TipoTermino = 1;

DECLARE @sintomas_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_links, ' links de síntomas creados');
PRINT '';

-- Tratamientos (con manejo de duplicados)
-- ⚠️ Si hay múltiples tratamientos con el mismo slug, toma el primero activo
INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    Nombre,
    Slug,
    2 AS TipoTermino,
    Activo,
    GETDATE() AS FechaCreacion
FROM (
    SELECT 
        t.nombre AS Nombre,
        dbo.fn_GenerateSlug(t.nombre) AS Slug,
        CASE WHEN t.Eliminado = 0 THEN 1 ELSE 0 END AS Activo,
        ROW_NUMBER() OVER (
            PARTITION BY dbo.fn_GenerateSlug(t.nombre) 
            ORDER BY 
                CASE WHEN t.Eliminado = 0 THEN 0 ELSE 1 END, -- Activos primero
                t.id -- En caso de empate, el primer ID
        ) AS RowNum
    FROM [dbo].[tratamientos] t
    WHERE t.nombre IS NOT NULL
    AND LEN(TRIM(t.nombre)) > 0
) AS Deduplicated
WHERE RowNum = 1;

DECLARE @tratamientos_added INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_added, ' tratamientos agregados (duplicados eliminados)');

-- Links tratamientos
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT gt.Id, NULL, t.id
FROM [dbo].[tratamientos] t
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(t.nombre)
WHERE gt.TipoTermino = 2;

DECLARE @tratamientos_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_links, ' links de tratamientos creados');
PRINT '';

-- =====================================================
-- PASO 5: VERIFICACIÓN Y RESUMEN
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

-- Verificar links
SELECT 
    'GlossaryTermMedicalLink' AS Tabla,
    COUNT(*) AS TotalLinks
FROM [dbo].[GlossaryTermMedicalLink];

-- Ejemplos
PRINT '';
PRINT 'Ejemplos de términos creados:';
SELECT TOP 3 Nombre, Slug, TipoTermino FROM [dbo].[GlossaryTerm] WHERE TipoTermino = 1 ORDER BY Nombre;
SELECT TOP 3 Nombre, Slug, TipoTermino FROM [dbo].[GlossaryTerm] WHERE TipoTermino = 2 ORDER BY Nombre;

PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║     ✓✓✓ INSTALACIÓN FORZOSA EXITOSA ✓✓✓          ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '➡️ PRÓXIMO PASO: Reiniciar Visual Studio (Shift+F5 → F5)';
PRINT '';
GO
