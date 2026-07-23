-- ============================================================================
-- PlatToleroEnvio — control de envío de la encuesta de tolerancia (/tolero/{slug}).
-- UNA fila por ingrediente: solo la ÚLTIMA vez que el admin la marcó como enviada.
-- Sin historial (varios envíos / canal / nota = versión futura).
--
-- Tabla PROPIA sin FK física (mismo patrón que PlatTolerVoto / PlatCalificacion):
-- es estado de CAMPAÑA, separado a propósito del catálogo PlatIngrediente.
-- No participa en ningún cálculo — el bayesiano (#16) no lee esta tabla.
--
-- Ejecutar a mano en prod (deploy-gate, va con el código). Idempotente.
-- sqlcmd: usar  -I  (QUOTED_IDENTIFIER ON)  y  -f 65001  si el archivo trae acentos.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatToleroEnvio')
BEGIN
    CREATE TABLE dbo.PlatToleroEnvio (
        Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatToleroEnvio PRIMARY KEY,
        IngredienteId    INT NOT NULL,               -- FK lógica a PlatIngrediente (sin FK física)
        -- NULL = pendiente. Dato MANUAL: lo pone el admin, no lo infiere el sistema
        -- (esta pantalla no manda correos, solo registra que ya se envió).
        EnviadaEn        DATETIME2 NULL,
        MarcadaPorUserId UNIQUEIDENTIFIER NULL       -- admin que la marcó; NULL al deshacer
    );

    -- Una fila por ingrediente: el "marcar enviada" es un upsert, no un log.
    CREATE UNIQUE INDEX UQ_PlatToleroEnvio_Ingrediente
        ON dbo.PlatToleroEnvio(IngredienteId);
END
GO

-- Verificación rápida tras correrlo:
-- SELECT TOP 20 * FROM dbo.PlatToleroEnvio ORDER BY EnviadaEn DESC;
