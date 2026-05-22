USE eiibd26;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoAreaEii')
BEGIN
    CREATE TABLE MedicoAreaEii (
        MedicoPerfilId INT NOT NULL,
        CondicionId    INT NOT NULL,
        CONSTRAINT PK_MedicoAreaEii PRIMARY KEY (MedicoPerfilId, CondicionId),
        CONSTRAINT FK_MedicoAreaEii_Perfil
            FOREIGN KEY (MedicoPerfilId) REFERENCES MedicoPerfilExtendido(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoAreaEii_Condicion
            FOREIGN KEY (CondicionId) REFERENCES condiciones(id) ON DELETE CASCADE
    );
    PRINT 'Tabla MedicoAreaEii creada.';
END
ELSE PRINT 'Tabla MedicoAreaEii ya existe.';
GO
