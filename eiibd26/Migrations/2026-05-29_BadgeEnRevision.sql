USE eiibd26;
GO
IF COL_LENGTH('MedicoPerfilBadge', 'EnRevision') IS NULL
BEGIN
    ALTER TABLE MedicoPerfilBadge ADD
        EnRevision     BIT NOT NULL DEFAULT 0,
        RevisionMotivo NVARCHAR(300) NULL,
        RevisionPor    NVARCHAR(50)  NULL,
        FechaRevision  DATETIME      NULL;
    PRINT 'Columnas de revisión agregadas a MedicoPerfilBadge.';
END
ELSE PRINT 'Columnas de revisión ya existen.';
GO
