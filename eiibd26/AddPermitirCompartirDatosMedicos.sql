BEGIN TRANSACTION;
GO

ALTER TABLE [Perfil] ADD [PermitirCompartirDatosMedicos] bit NULL;
GO

CREATE TABLE [GlossaryTermRatings] (
    [Id] int NOT NULL IDENTITY,
    [TermId] int NOT NULL,
    [RatingType] int NOT NULL,
    [UserId] uniqueidentifier NULL,
    [IpAddress] nvarchar(45) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_GlossaryTermRatings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GlossaryTermRatings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_GlossaryTermRatings_GlossaryTerm_TermId] FOREIGN KEY ([TermId]) REFERENCES [GlossaryTerm] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_GlossaryTermRatings_TermId] ON [GlossaryTermRatings] ([TermId]);
GO

CREATE INDEX [IX_GlossaryTermRatings_UserId] ON [GlossaryTermRatings] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_GlossaryTermRatings_UserId_TermId] ON [GlossaryTermRatings] ([UserId], [TermId]) WHERE [UserId] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260401192819_AddPermitirCompartirDatosMedicos', N'8.0.21');
GO

COMMIT;
GO

