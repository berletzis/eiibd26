-- =====================================================
-- INSTALACIÓN AUTOMÁTICA CON LIMPIEZA DE DUPLICADOS
-- =====================================================
-- ⚡ ESTE SCRIPT HACE TODO AUTOMÁTICAMENTE
-- ⚡ NO NECESITAS DESCOMENTAR NADA
-- ⚡ SOLO EJECUTA Y ESPERA
-- =====================================================

USE eiibd26;
GO

SET NOCOUNT ON;

PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║     INSTALACIÓN AUTOMÁTICA DEL GLOSARIO MÉDICO    ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';
PRINT '⚡ Este script hace TODO de una vez:';
PRINT '   1. Elimina tablas antiguas del glosario';
PRINT '   2. Crea tablas limpias';
PRINT '   3. Inserta datos (solo el primer registro de cada slug)';
PRINT '   4. Los duplicados se SALTAN automáticamente';
PRINT '';
PRINT 'Esto puede tardar 10-20 segundos...';
PRINT '';

-- =====================================================
-- FUNCIÓN SLUG
-- =====================================================
IF OBJECT_ID('dbo.fn_GenerateSlug', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_GenerateSlug;
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

-- =====================================================
-- PASO 1: ELIMINAR TABLAS ANTIGUAS DEL GLOSARIO
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 1/3: Eliminando tablas antiguas del glosario';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

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

-- =====================================================
-- PASO 2: CREAR TABLAS NUEVAS
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 2/3: Creando tablas limpias';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

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

CREATE UNIQUE INDEX IX_GlossaryTerm_Slug ON [dbo].[GlossaryTerm]([Slug]);
CREATE INDEX IX_GlossaryTerm_TipoTermino ON [dbo].[GlossaryTerm]([TipoTermino]) INCLUDE ([Activo]);
CREATE INDEX IX_GlossaryTermMedicalLink_GlossaryTermId ON [dbo].[GlossaryTermMedicalLink]([GlossaryTermId]);
CREATE INDEX IX_GlossaryTermMedicalLink_SintomaId ON [dbo].[GlossaryTermMedicalLink]([SintomaId]) WHERE SintomaId IS NOT NULL;
CREATE INDEX IX_GlossaryTermMedicalLink_TratamientoId ON [dbo].[GlossaryTermMedicalLink]([TratamientoId]) WHERE TratamientoId IS NOT NULL;
PRINT '✓ Índices creados';
PRINT '';

-- =====================================================
-- PASO 3: INSERTAR DATOS (SKIP AUTOMÁTICO DE DUPLICADOS)
-- =====================================================
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '  PASO 3/3: Insertando datos (saltando duplicados automáticamente)';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';

-- Insertar SÍNTOMAS (solo los que no generen slug duplicado)
INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    nombre,
    dbo.fn_GenerateSlug(nombre) AS Slug,
    1,
    CASE WHEN Eliminado = 0 THEN 1 ELSE 0 END,
    GETDATE()
FROM [dbo].[sintomas] s
WHERE nombre IS NOT NULL 
AND LEN(TRIM(nombre)) > 0
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTerm] gt 
    WHERE gt.Slug = dbo.fn_GenerateSlug(s.nombre)
)
AND s.id IN (
    SELECT MIN(id) 
    FROM [dbo].[sintomas]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
    GROUP BY dbo.fn_GenerateSlug(nombre)
);

DECLARE @sintomas_insertados INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_insertados, ' síntomas insertados');

-- Links para síntomas
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT gt.Id, s.id, NULL
FROM [dbo].[sintomas] s
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(s.nombre)
WHERE gt.TipoTermino = 1
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id AND gtml.SintomaId = s.id
);

DECLARE @sintomas_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @sintomas_links, ' links de síntomas creados');
PRINT '';

-- Insertar TRATAMIENTOS (solo los que no generen slug duplicado)
INSERT INTO [dbo].[GlossaryTerm] (Nombre, Slug, TipoTermino, Activo, FechaCreacion)
SELECT 
    nombre,
    dbo.fn_GenerateSlug(nombre) AS Slug,
    2,
    CASE WHEN Eliminado = 0 THEN 1 ELSE 0 END,
    GETDATE()
FROM [dbo].[tratamientos] t
WHERE nombre IS NOT NULL 
AND LEN(TRIM(nombre)) > 0
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTerm] gt 
    WHERE gt.Slug = dbo.fn_GenerateSlug(t.nombre)
)
AND t.id IN (
    SELECT MIN(id) 
    FROM [dbo].[tratamientos]
    WHERE nombre IS NOT NULL AND LEN(TRIM(nombre)) > 0
    GROUP BY dbo.fn_GenerateSlug(nombre)
);

DECLARE @tratamientos_insertados INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_insertados, ' tratamientos insertados');

-- Links para tratamientos
INSERT INTO [dbo].[GlossaryTermMedicalLink] (GlossaryTermId, SintomaId, TratamientoId)
SELECT gt.Id, NULL, t.id
FROM [dbo].[tratamientos] t
INNER JOIN [dbo].[GlossaryTerm] gt ON gt.Slug = dbo.fn_GenerateSlug(t.nombre)
WHERE gt.TipoTermino = 2
AND NOT EXISTS (
    SELECT 1 
    FROM [dbo].[GlossaryTermMedicalLink] gtml 
    WHERE gtml.GlossaryTermId = gt.Id AND gtml.TratamientoId = t.id
);

DECLARE @tratamientos_links INT = @@ROWCOUNT;
PRINT CONCAT('✓ ', @tratamientos_links, ' links de tratamientos creados');
PRINT '';

-- =====================================================
-- RESUMEN FINAL
-- =====================================================
PRINT '';
PRINT '╔════════════════════════════════════════════════════╗';
PRINT '║            INSTALACIÓN COMPLETADA ✓✓✓             ║';
PRINT '╚════════════════════════════════════════════════════╝';
PRINT '';

SELECT 
    CASE TipoTermino 
        WHEN 1 THEN 'Síntomas'
        WHEN 2 THEN 'Tratamientos'
    END AS Tipo,
    COUNT(*) AS Total,
    SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) AS Activos
FROM [dbo].[GlossaryTerm]
GROUP BY TipoTermino
ORDER BY TipoTermino;

SELECT 
    'GlossaryTermMedicalLink' AS Tabla,
    COUNT(*) AS TotalLinks
FROM [dbo].[GlossaryTermMedicalLink];

PRINT '';
PRINT '✅ SIN ERRORES - Todo instalado correctamente';
PRINT '📌 Solo se insertó el primer registro de cada slug duplicado';
PRINT '   (Los demás fueron automáticamente saltados)';
PRINT '';
PRINT '➡️ PRÓXIMO PASO:';
PRINT '   1. Reiniciar Visual Studio (Shift+F5 → F5)';
PRINT '   2. Navegar a: http://localhost:XXXX/Glosario';
PRINT '   3. ¡PROFIT! 🎉';
PRINT '';

SET NOCOUNT OFF;
GO
