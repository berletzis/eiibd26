-- ============================================================
-- GlossaryTermRatings
-- Calificaciones (Like/Dislike) de usuarios sobre términos del
-- glosario médico.
-- Equivalente funcional de ArticleRatings para artículos.
-- Ejecutar manualmente en SQL Server (NO usar EF migrations).
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'GlossaryTermRatings'
)
BEGIN
    CREATE TABLE [dbo].[GlossaryTermRatings] (
        [Id]          INT              IDENTITY(1,1)   NOT NULL,
        [TermId]      INT              NOT NULL,
        [RatingType]  INT              NOT NULL,
        [UserId]      UNIQUEIDENTIFIER NULL,
        [IpAddress]   NVARCHAR(45)     NULL,
        [CreatedAt]   DATETIME2(7)     NOT NULL CONSTRAINT [DF_GlossaryTermRatings_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]   DATETIME2(7)     NULL,

        CONSTRAINT [PK_GlossaryTermRatings]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_GlossaryTermRatings_GlossaryTerm_TermId]
            FOREIGN KEY ([TermId])
            REFERENCES [dbo].[GlossaryTerm] ([Id])
            ON DELETE CASCADE,

        CONSTRAINT [FK_GlossaryTermRatings_AspNetUsers_UserId]
            FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id])
            ON DELETE SET NULL
    );

    -- Lookup por término
    CREATE NONCLUSTERED INDEX [IX_GlossaryTermRatings_TermId]
        ON [dbo].[GlossaryTermRatings] ([TermId] ASC);

    -- Lookup por usuario
    CREATE NONCLUSTERED INDEX [IX_GlossaryTermRatings_UserId]
        ON [dbo].[GlossaryTermRatings] ([UserId] ASC);

    -- Un solo voto por usuario autenticado por término
    CREATE UNIQUE NONCLUSTERED INDEX [UX_GlossaryTermRatings_UserId_TermId]
        ON [dbo].[GlossaryTermRatings] ([UserId] ASC, [TermId] ASC)
        WHERE [UserId] IS NOT NULL;

    PRINT 'Tabla GlossaryTermRatings creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla GlossaryTermRatings ya existe. No se realizaron cambios.';
END
