USE eiibd26;
GO
-- =============================================================================
-- FIX NINA — Parte 1: Crear tabla AIRequestLogs
-- Causa raíz: tabla ausente causaba que SaveChangesAsync revertiera la respuesta IA
-- Tipos mapeados de eiibd26.Models.AIRequestLog (EF Core 8 defaults):
--   Guid            → uniqueidentifier
--   string sin Max  → nvarchar(max)
--   QuestionLevel   → int  (enum)
--   bool            → bit
--   double          → float
--   DateTimeOffset  → datetimeoffset(7)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'AIRequestLogs'
)
BEGIN
    CREATE TABLE [dbo].[AIRequestLogs] (
        [Id]                uniqueidentifier    NOT NULL,
        [PreguntaId]        uniqueidentifier    NOT NULL,
        [QuestionText]      nvarchar(max)       NOT NULL,
        [Level]             int                 NOT NULL,
        [HighRisk]          bit                 NOT NULL,
        [ModelUsed]         nvarchar(max)       NOT NULL,
        [ProcessingTimeMs]  float               NOT NULL,
        [Timestamp]         datetimeoffset(7)   NOT NULL,
        [Success]           bit                 NOT NULL,
        [ErrorMessage]      nvarchar(max)       NULL,

        CONSTRAINT [PK_AIRequestLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AIRequestLogs_Preguntas_PreguntaId]
            FOREIGN KEY ([PreguntaId]) REFERENCES [dbo].[Preguntas] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_AIRequestLogs_PreguntaId]
        ON [dbo].[AIRequestLogs] ([PreguntaId]);

    PRINT 'Tabla AIRequestLogs creada correctamente.';
END
ELSE
BEGIN
    PRINT 'Tabla AIRequestLogs ya existía — sin cambios.';
END
GO

-- =============================================================================
-- FIX NINA — Parte 3: Crear Perfil para el usuario NINA
-- Columnas NOT NULL reales en BD: idUser, FechaCreacion,
--   PermitirTelefonoReal, PermitirCorreoNoticias, PermitirMostrarPais
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Perfil]
    WHERE idUser = '50649075-660F-4431-9049-98C9E3AC6D73'
)
BEGIN
    -- idZone incluido explícitamente como NULL para anular DEFAULT ((0))
    -- que dispara FK_Perfil_TimeZones contra ZonaHoraria (mínimo id=1)
    INSERT INTO [dbo].[Perfil] (
        idUser,
        idZone,
        Avatar,
        Nombre,
        slug,
        FechaCreacion,
        FechaCreado,
        PermitirTelefonoReal,
        PermitirCorreoNoticias,
        PermitirMostrarPais
    )
    VALUES (
        '50649075-660F-4431-9049-98C9E3AC6D73',
        NULL,
        'https://ui-avatars.com/api/?name=NINA&size=110&background=6a4e7a&color=fff',
        'NINA',
        'nina-asistente-ia',
        GETUTCDATE(),
        GETUTCDATE(),
        0,  -- PermitirTelefonoReal
        0,  -- PermitirCorreoNoticias
        0   -- PermitirMostrarPais
    );

    PRINT 'Perfil de NINA creado correctamente.';
END
ELSE
BEGIN
    PRINT 'Perfil de NINA ya existía — sin cambios.';
END
GO

-- Verificación post-fix
SELECT 'AIRequestLogs' AS Tabla, COUNT(*) AS Filas FROM [dbo].[AIRequestLogs]
UNION ALL
SELECT 'Perfil NINA', COUNT(*) FROM [dbo].[Perfil] WHERE idUser = '50649075-660F-4431-9049-98C9E3AC6D73';
