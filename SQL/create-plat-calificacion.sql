/* ============================================================================
   create-plat-calificacion.sql  —  Calificación de ingrediente (Platillos)
   ----------------------------------------------------------------------------
   Tabla PROPIA del módulo. Un voto por usuario por ingrediente, cambiable.
   Valor: 1 = "me fue útil", -1 = "no me fue útil".

   Aislamiento (§0) intacto: FK física SOLO a PlatIngrediente (dentro del módulo,
   ON DELETE CASCADE). `idUsuario` es referencia LÓGICA a Identity (AspNetUsers),
   sin FK física — igual que PlatPerfilExclusion. Si el módulo se apaga, no deja
   constraints colgando en tablas ajenas.

   NO reusa ArticleRating: es tabla aparte, con su propio endpoint.
   Idempotente (IF NOT EXISTS). Correr DESPUÉS de create-platillos.sql.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatIngredienteCalificacion' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatIngredienteCalificacion
    (
        Id            INT              IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatIngredienteCalificacion PRIMARY KEY,
        IngredienteId INT              NOT NULL,
        idUsuario     UNIQUEIDENTIFIER NOT NULL,
        Valor         SMALLINT         NOT NULL,   -- 1 = me fue útil, -1 = no me fue útil
        Fecha         DATETIME2        NOT NULL CONSTRAINT DF_PlatIngredienteCalificacion_Fecha DEFAULT (SYSUTCDATETIME()),
        -- Arista de pertenencia: los votos mueren con su ingrediente (CASCADE).
        CONSTRAINT FK_PlatIngredienteCalificacion_Ingrediente
            FOREIGN KEY (IngredienteId) REFERENCES dbo.PlatIngrediente (Id) ON DELETE CASCADE,
        -- Un voto por usuario por ingrediente (cambiable con UPDATE).
        CONSTRAINT UQ_PlatIngredienteCalificacion_IngUser UNIQUE (IngredienteId, idUsuario),
        CONSTRAINT CK_PlatIngredienteCalificacion_Valor CHECK (Valor IN (1, -1))
    );
END
GO

-- Índice en el FK (además del UNIQUE, que ya tiene IngredienteId como columna guía).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatIngredienteCalificacion_IngredienteId'
               AND object_id = OBJECT_ID('dbo.PlatIngredienteCalificacion'))
    CREATE INDEX IX_PlatIngredienteCalificacion_IngredienteId ON dbo.PlatIngredienteCalificacion (IngredienteId);
GO

/* ---------- Verificación ---------- */
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE name = 'PlatIngredienteCalificacion' AND schema_id = SCHEMA_ID('dbo')) AS Tabla,
    (SELECT delete_referential_action FROM sys.foreign_keys WHERE name = 'FK_PlatIngredienteCalificacion_Ingrediente') AS FkCascade; -- 1 = CASCADE
GO
