-- =====================================================
-- MÓDULO GLOSARIO MÉDICO - CREACIÓN DE TABLAS
-- =====================================================
-- Fecha: 2025-01-XX
-- Descripción: Tablas para el módulo de glosario médico desacoplado
-- =====================================================

USE eiibd26;
GO

-- =====================================================
-- 1. TABLA: GlossaryTerm (Índice navegable)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTerm')
BEGIN
    CREATE TABLE [dbo].[GlossaryTerm] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Nombre] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(200) NOT NULL UNIQUE,
        [TipoTermino] INT NOT NULL, -- 1=Sintoma, 2=Tratamiento
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

-- =====================================================
-- 2. TABLA: GlossaryTermMedicalLink (Adapter)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GlossaryTermMedicalLink')
BEGIN
    CREATE TABLE [dbo].[GlossaryTermMedicalLink] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [GlossaryTermId] INT NOT NULL,
        [SintomaId] INT NULL,
        [TratamientoId] INT NULL,
        
        -- FK solo a GlossaryTerm (no a sintomas/tratamientos por desacoplamiento)
        CONSTRAINT FK_GlossaryTermMedicalLink_GlossaryTerm 
            FOREIGN KEY (GlossaryTermId) 
            REFERENCES [dbo].[GlossaryTerm](Id) 
            ON DELETE CASCADE,
        
        -- Constraint: solo uno puede tener valor
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

-- =====================================================
-- 3. ÍNDICES PARA OPTIMIZACIÓN
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTerm_Slug')
BEGIN
    CREATE UNIQUE INDEX IX_GlossaryTerm_Slug 
        ON [dbo].[GlossaryTerm]([Slug]);
    
    PRINT '✓ Índice IX_GlossaryTerm_Slug creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTerm_TipoTermino')
BEGIN
    CREATE INDEX IX_GlossaryTerm_TipoTermino 
        ON [dbo].[GlossaryTerm]([TipoTermino]) 
        INCLUDE ([Activo]);
    
    PRINT '✓ Índice IX_GlossaryTerm_TipoTermino creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTermMedicalLink_GlossaryTermId')
BEGIN
    CREATE INDEX IX_GlossaryTermMedicalLink_GlossaryTermId 
        ON [dbo].[GlossaryTermMedicalLink]([GlossaryTermId]);
    
    PRINT '✓ Índice IX_GlossaryTermMedicalLink_GlossaryTermId creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTermMedicalLink_SintomaId')
BEGIN
    CREATE INDEX IX_GlossaryTermMedicalLink_SintomaId 
        ON [dbo].[GlossaryTermMedicalLink]([SintomaId]) 
        WHERE SintomaId IS NOT NULL;
    
    PRINT '✓ Índice IX_GlossaryTermMedicalLink_SintomaId creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GlossaryTermMedicalLink_TratamientoId')
BEGIN
    CREATE INDEX IX_GlossaryTermMedicalLink_TratamientoId 
        ON [dbo].[GlossaryTermMedicalLink]([TratamientoId]) 
        WHERE TratamientoId IS NOT NULL;
    
    PRINT '✓ Índice IX_GlossaryTermMedicalLink_TratamientoId creado';
END
GO

-- =====================================================
-- 4. VERIFICACIÓN FINAL
-- =====================================================
SELECT 
    'GlossaryTerm' AS Tabla,
    COUNT(*) AS Registros
FROM [dbo].[GlossaryTerm]

UNION ALL

SELECT 
    'GlossaryTermMedicalLink' AS Tabla,
    COUNT(*) AS Registros
FROM [dbo].[GlossaryTermMedicalLink];

PRINT '';
PRINT '✓✓✓ MÓDULO GLOSARIO INSTALADO CORRECTAMENTE ✓✓✓';
PRINT 'Siguiente paso: Ejecutar 02_Bootstrap_GlossaryTerms.sql';
GO
