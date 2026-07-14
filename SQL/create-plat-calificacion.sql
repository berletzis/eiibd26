/* ============================================================================
   create-plat-calificacion.sql  —  Calificación GENÉRICA (Platillos)
   ----------------------------------------------------------------------------
   Tabla PROPIA polimórfica (mismo patrón que PlatNotaClinica): sirve para
   calificar tanto ingredientes como platillos, sin duplicar tablas.
   Valor: 1 = "me fue útil", -1 = "no me fue útil". Un voto por usuario por
   destino, cambiable.

   Aislamiento (§0) intacto: NO hay FK física a nada (destino polimórfico
   TipoDestino+DestinoId, como PlatNotaClinica). `idUsuario` es referencia
   lógica a Identity, sin FK. NO reusa ArticleRating (esa se llavea por
   ContenidoId y mezclaría votos de ingrediente #5 con artículo #5).

   MIGRACIÓN idempotente: si existe la tabla vieja PlatIngredienteCalificacion
   (solo-ingredientes), copia sus votos como TipoDestino='Ingrediente' y la
   elimina. Preserva Ids. Si no existe, crea la genérica limpia. Re-run: no-op.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID('dbo.PlatCalificacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatCalificacion
    (
        Id          INT              IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatCalificacion PRIMARY KEY,
        TipoDestino VARCHAR(12)      NOT NULL,   -- 'Platillo' | 'Ingrediente'
        DestinoId   INT              NOT NULL,
        idUsuario   UNIQUEIDENTIFIER NOT NULL,
        Valor       SMALLINT         NOT NULL,   -- 1 = me fue útil, -1 = no me fue útil
        Fecha       DATETIME2        NOT NULL CONSTRAINT DF_PlatCalificacion_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_PlatCalificacion_Destino_User UNIQUE (TipoDestino, DestinoId, idUsuario),
        CONSTRAINT CK_PlatCalificacion_TipoDestino CHECK (TipoDestino IN ('Platillo', 'Ingrediente')),
        CONSTRAINT CK_PlatCalificacion_Valor CHECK (Valor IN (1, -1))
    );

    CREATE INDEX IX_PlatCalificacion_Destino ON dbo.PlatCalificacion (TipoDestino, DestinoId);

    -- Migración de la tabla vieja (solo-ingredientes), si existe: copiar votos como 'Ingrediente'.
    IF OBJECT_ID('dbo.PlatIngredienteCalificacion', 'U') IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT dbo.PlatCalificacion ON;
        INSERT INTO dbo.PlatCalificacion (Id, TipoDestino, DestinoId, idUsuario, Valor, Fecha)
            SELECT Id, 'Ingrediente', IngredienteId, idUsuario, Valor, Fecha
            FROM dbo.PlatIngredienteCalificacion;
        SET IDENTITY_INSERT dbo.PlatCalificacion OFF;

        DROP TABLE dbo.PlatIngredienteCalificacion;   -- su FK a PlatIngrediente se va con ella
    END
END
GO

/* ---------- Verificación ---------- */
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE name = 'PlatCalificacion' AND schema_id = SCHEMA_ID('dbo'))            AS TablaNueva,       -- 1
    (SELECT COUNT(*) FROM sys.tables WHERE name = 'PlatIngredienteCalificacion' AND schema_id = SCHEMA_ID('dbo')) AS TablaViejaRestante,-- 0
    (SELECT COUNT(*) FROM dbo.PlatCalificacion)                                                                   AS Votos;
GO
