-- Tabla de resultados de calidad de contenido
-- NivelSemaforo TINYINT sigue el enum C# NivelSemaforo: 0=Critico, 1=Mejorable, 2=Ok
-- UNIQUE(ContenidoId) garantiza 1 resultado vigente por contenido (upsert lo actualiza)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContenidoCalidad' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ContenidoCalidad (
        Id              INT IDENTITY(1,1) NOT NULL,
        ContenidoId     INT NOT NULL,
        NivelSemaforo   TINYINT NOT NULL,
        Senales         NVARCHAR(MAX) NULL,
        DuplicadoDeIds  NVARCHAR(MAX) NULL,
        FechaAnalisis   DATETIME2 NOT NULL,
        CONSTRAINT PK_ContenidoCalidad PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UX_ContenidoCalidad_Contenido UNIQUE (ContenidoId)
    );
    CREATE INDEX IX_ContenidoCalidad_Nivel ON dbo.ContenidoCalidad (NivelSemaforo);
    PRINT 'Tabla ContenidoCalidad creada.';
END
ELSE
BEGIN
    PRINT 'Tabla ContenidoCalidad ya existe — sin cambios.';
END
