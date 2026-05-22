USE eiibd26;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MedicoPerfilExtendido') AND name = 'Estado')
BEGIN
    ALTER TABLE MedicoPerfilExtendido ADD
        Estado     NVARCHAR(150) NULL,
        Ciudad     NVARCHAR(150) NULL,
        PaisCodigo NVARCHAR(10)  NULL,
        Latitud    NVARCHAR(50)  NULL,
        Longitud   NVARCHAR(50)  NULL;
    PRINT 'Columnas de ubicación agregadas a MedicoPerfilExtendido.';
END
ELSE PRINT 'Columnas de ubicación ya existen.';
GO
