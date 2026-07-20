-- ============================================================================
-- PlatTolerVoto — votos de la encuesta de tolerancia (/tolero/{slug}).
-- Un voto por paciente (UserId) o por cookie anónima (AnonId) por ingrediente.
-- Tabla PROPIA polimórfica sin FK física (mismo patrón que PlatCalificacion /
-- PlatNotaClinica). Alimenta el modelo bayesiano futuro (#16): se guarda TipoEII
-- desde ya aunque el MVP no segmente.
--
-- Ejecutar a mano en prod (deploy-gate, va con el código). Idempotente.
-- sqlcmd: usar  -I  (índices filtrados exigen QUOTED_IDENTIFIER ON)
--          y  -f 65001  si el archivo trae acentos.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatTolerVoto')
BEGIN
    CREATE TABLE dbo.PlatTolerVoto (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatTolerVoto PRIMARY KEY,
        IngredienteId INT NOT NULL,                       -- FK lógica a PlatIngrediente (sin FK física)
        UserId        UNIQUEIDENTIFIER NULL,              -- null si anónimo
        AnonId        UNIQUEIDENTIFIER NULL,              -- cookie de dedup para anónimos
        Tolera        TINYINT NOT NULL
            CONSTRAINT CK_PlatTolerVoto_Tolera CHECK (Tolera IN (1, 2, 3)),  -- 1=Sí 2=AVeces 3=No
        -- Condición principal CRUDA del paciente al momento del voto. FUENTE DE VERDAD para #16:
        -- permite recalcular el tipo de EII contra el catálogo aunque una condición se renombre.
        CondicionIdPrincipal INT NULL,                    -- FK lógica a condiciones (sin FK física)
        -- Denormalización de conveniencia derivada de CondicionIdPrincipal; recomputable.
        TipoEII       TINYINT NULL,                       -- 1=CUCI 2=Crohn; null si anónimo/desconocido
        FechaVoto     DATETIME NOT NULL
            CONSTRAINT DF_PlatTolerVoto_FechaVoto DEFAULT (GETUTCDATE())
    );

    -- Un voto por (ingrediente, paciente) y por (ingrediente, cookie). Filtrados para
    -- tolerar el NULL del otro origen; el upsert de la app permite CAMBIAR el voto.
    CREATE UNIQUE INDEX UQ_PlatTolerVoto_Ing_User
        ON dbo.PlatTolerVoto(IngredienteId, UserId) WHERE UserId IS NOT NULL;
    CREATE UNIQUE INDEX UQ_PlatTolerVoto_Ing_Anon
        ON dbo.PlatTolerVoto(IngredienteId, AnonId) WHERE AnonId IS NOT NULL;

    -- Lectura de resultados por ingrediente.
    CREATE INDEX IX_PlatTolerVoto_Ingrediente ON dbo.PlatTolerVoto(IngredienteId);
END
GO
