-- =============================================================================
-- Vista "Oportunidades de contenido" (F1) — tabla de estado del backlog editorial.
-- Tabla PROPIA del Web, creada por SQL directo (sin migración EF, regla del proyecto).
-- Idempotente: se puede ejecutar varias veces sin efecto secundario.
--
-- Tipo   : 'Externo' (oportunidad de cobertura, RefId = ScrapedPage.ScrapedPageId)
--          'Propio'  (lente Mejorar/GRIS en F2,   RefId = contenidos.Id)
-- Estado : 0 Nuevo · 1 Planificado · 2 EnProgreso · 3 Publicado · 4 Descartado
-- =============================================================================

-- Diagnóstico previo (regla §10: SELECT COUNT antes de nada).
IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.OportunidadEstado'))
    SELECT COUNT(*) AS FilasExistentes FROM dbo.OportunidadEstado;
ELSE
    SELECT -1 AS FilasExistentes; -- la tabla aún no existe

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.OportunidadEstado'))
BEGIN
    CREATE TABLE dbo.OportunidadEstado
    (
        Id               INT            IDENTITY(1,1) NOT NULL,
        Tipo             VARCHAR(10)    NOT NULL,   -- 'Externo' | 'Propio'
        RefId            INT            NOT NULL,   -- ScrapedPageId (Externo) | ContenidoId (Propio)
        Estado           TINYINT        NOT NULL CONSTRAINT DF_OportunidadEstado_Estado DEFAULT (0),
        EditorUserId     NVARCHAR(450)  NULL,
        FechaActualizada DATETIME2      NOT NULL CONSTRAINT DF_OportunidadEstado_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_OportunidadEstado PRIMARY KEY (Id),
        CONSTRAINT UQ_OportunidadEstado_Tipo_RefId UNIQUE (Tipo, RefId)
    );
END
GO
