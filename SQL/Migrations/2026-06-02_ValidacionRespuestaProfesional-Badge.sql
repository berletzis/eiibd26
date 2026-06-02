USE eiibd26;
GO
-- =============================================================================
-- FASE A: Tabla ValidacionRespuestaProfesional + badge validador_respuestas
-- Espejo de ValidacionesContenidoProfesional pero con RespuestaId UNIQUEIDENTIFIER
-- y sin TipoContenido (es específico de respuestas).
--
-- Tipos EF Core 8 confirmados contra tabla existente ValidacionesContenidoProfesional:
--   EstadoValidacion (enum int) → tinyint  (consistente con espejo)
--   DateTime                   → datetime2(7)
--   string [MaxLength(450)]     → nvarchar(450)
--   string [MaxLength(800)]     → nvarchar(800)
--   string [MaxLength(500)]     → nvarchar(500)
--   Guid                        → uniqueidentifier
--   int IDENTITY                → INT IDENTITY(1,1)
-- =============================================================================

-- ── Script 1 ── Crear tabla ────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'ValidacionRespuestaProfesional'
)
BEGIN
    CREATE TABLE [dbo].[ValidacionRespuestaProfesional] (
        [Id]              INT              IDENTITY(1,1) NOT NULL,
        [RespuestaId]     UNIQUEIDENTIFIER              NOT NULL,
        [UsuarioMedicoId] NVARCHAR(450)                 NOT NULL,
        [Comentario]      NVARCHAR(800)                     NULL,
        [Estado]          TINYINT                       NOT NULL
            CONSTRAINT [DF_ValidRespProf_Estado]    DEFAULT (1),
        [CreadoEn]        DATETIME2(7)                  NOT NULL
            CONSTRAINT [DF_ValidRespProf_CreadoEn]  DEFAULT (SYSUTCDATETIME()),
        [ActualizadoEn]   DATETIME2(7)                      NULL,
        [ModeradoPorId]   NVARCHAR(450)                     NULL,
        [FechaModeracion] DATETIME2(7)                      NULL,
        [NotaModeracion]  NVARCHAR(500)                     NULL,

        CONSTRAINT [PK_ValidacionRespuestaProfesional]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_ValidacionRespuestaProfesional_Respuestas_RespuestaId]
            FOREIGN KEY ([RespuestaId]) REFERENCES [dbo].[Respuestas] ([Id])
            ON DELETE CASCADE
    );

    -- Un médico valida una respuesta una sola vez (upsert)
    CREATE UNIQUE INDEX [UX_ValidRespProf_RespuestaId_UsuarioMedicoId]
        ON [dbo].[ValidacionRespuestaProfesional] ([RespuestaId], [UsuarioMedicoId]);

    -- Para contar validaciones por médico (badge) sin full scan
    CREATE INDEX [IX_ValidRespProf_UsuarioMedicoId]
        ON [dbo].[ValidacionRespuestaProfesional] ([UsuarioMedicoId]);

    PRINT 'Tabla ValidacionRespuestaProfesional creada correctamente.';
END
ELSE
    PRINT 'Tabla ValidacionRespuestaProfesional ya existía — sin cambios.';
GO

-- ── Script 2 ── Badge validador_respuestas (Nivel 5, Orden 8) ─────────────
-- Orden 8 confirmado libre (actuales llegan hasta Orden 7: creador_contenido)
IF NOT EXISTS (
    SELECT 1 FROM MedicoBadge WHERE Codigo = 'validador_respuestas'
)
BEGIN
    INSERT INTO MedicoBadge (Codigo, Nombre, Descripcion, ComoObtenerlo, Icono, Nivel, Orden, Activo)
    VALUES (
        'validador_respuestas',
        N'Validador de Respuestas',
        N'Ha validado 3 o más respuestas de la comunidad.',
        N'Validar 3 o más respuestas en preguntas',
        'bi-patch-check-fill',
        5,
        8,
        1
    );
    PRINT 'Badge validador_respuestas insertado (Nivel 5, Orden 8).';
END
ELSE
    PRINT 'Badge validador_respuestas ya existía — sin cambios.';
GO

-- Verificación post-script
SELECT 'ValidacionRespuestaProfesional' AS Tabla,
       COUNT(*) AS Filas
FROM [dbo].[ValidacionRespuestaProfesional]
UNION ALL
SELECT 'MedicoBadge validador_respuestas',
       COUNT(*)
FROM MedicoBadge WHERE Codigo = 'validador_respuestas';
GO

SELECT Codigo, Nombre, Nivel, Orden, Activo
FROM MedicoBadge
ORDER BY Orden;
