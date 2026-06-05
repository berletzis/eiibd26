-- GRIS: agregar campos de evaluación editorial a ContenidoCalidad
-- Ejecutar directo en SQL Server — NO es migración EF Core (proyecto no usa migraciones)

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisEvaluado')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisEvaluado BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisPuntajeGlobal')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisPuntajeGlobal TINYINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisResultado')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisResultado NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisSugerencias')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisSugerencias NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisFechaEvaluacion')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisFechaEvaluacion DATETIME2 NULL;
