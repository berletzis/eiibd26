/* ============================================================================
   create-notas-clinicas.sql  —  Notas clínicas estructuradas (Platillos, F1)
   ----------------------------------------------------------------------------
   3 tablas PROPIAS del Web, creadas por SQL directo (sin migración EF).
   Prefijo Plat*. Mismo módulo, mismo aislamiento: no toca ninguna tabla ajena.
   NO hay FK física a PlatGrupo/PlatIngrediente: la relación es polimórfica
   (TipoDestino + DestinoId), igual que PlatPerfilExclusion.

   EL CANDADO nace CERRADO: PlatNotaClinica.RevisadaPorMedico DEFAULT 0.
   Ninguna nota se muestra al paciente hasta que un médico la apruebe (F2).

   Idempotente (IF NOT EXISTS). Debe correrse DESPUÉS de create-platillos.sql.
   Correr create-notas-clinicas.sql ANTES de seed-notas-clinicas.sql.

   Convenciones del repo: esquema dbo explícito, GO como separador de lote,
   INT IDENTITY para PKs, DATETIME2 DEFAULT SYSUTCDATETIME(), constraints
   nombradas, índice en cada FK, ON DELETE CASCADE en aristas de pertenencia.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- 1) PlatNotaClinica: una nota por grupo o por ingrediente ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatNotaClinica' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatNotaClinica
    (
        Id                INT              IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatNotaClinica PRIMARY KEY,
        TipoDestino       VARCHAR(12)      NOT NULL,   -- 'Grupo' | 'Ingrediente'
        DestinoId         INT              NOT NULL,   -- Id de PlatGrupo o PlatIngrediente (sin FK física)
        Titulo            NVARCHAR(200)    NOT NULL,
        RevisadaPorMedico BIT              NOT NULL CONSTRAINT DF_PlatNotaClinica_Revisada DEFAULT (0),  -- ← EL CANDADO
        RevisadaPorUserId UNIQUEIDENTIFIER NULL,
        FechaRevision     DATETIME2        NULL,
        Activo            BIT              NOT NULL CONSTRAINT DF_PlatNotaClinica_Activo   DEFAULT (1),
        FechaCreacion     DATETIME2        NOT NULL CONSTRAINT DF_PlatNotaClinica_Fecha   DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_PlatNotaClinica_Destino UNIQUE (TipoDestino, DestinoId),
        CONSTRAINT CK_PlatNotaClinica_TipoDestino CHECK (TipoDestino IN ('Grupo', 'Ingrediente'))
    );
END
GO

/* ---------- 2) PlatNotaSeccion: secciones en orden (CASCADE) ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatNotaSeccion' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatNotaSeccion
    (
        Id            INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatNotaSeccion PRIMARY KEY,
        NotaClinicaId INT           NOT NULL,
        Orden         INT           NOT NULL CONSTRAINT DF_PlatNotaSeccion_Orden DEFAULT (0),
        Titulo        NVARCHAR(200) NULL,
        Contenido     NVARCHAR(MAX) NULL,   -- una viñeta por línea si empieza con "- "
        -- Arista de pertenencia: la sección muere con su nota (CASCADE).
        CONSTRAINT FK_PlatNotaSeccion_Nota
            FOREIGN KEY (NotaClinicaId) REFERENCES dbo.PlatNotaClinica (Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatNotaSeccion_NotaClinicaId'
               AND object_id = OBJECT_ID('dbo.PlatNotaSeccion'))
    CREATE INDEX IX_PlatNotaSeccion_NotaClinicaId ON dbo.PlatNotaSeccion (NotaClinicaId);
GO

/* ---------- 3) PlatNotaReferencia: bibliografía en orden (CASCADE) ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatNotaReferencia' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatNotaReferencia
    (
        Id            INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatNotaReferencia PRIMARY KEY,
        NotaClinicaId INT            NOT NULL,
        Orden         INT            NOT NULL CONSTRAINT DF_PlatNotaReferencia_Orden DEFAULT (0),
        Titulo        NVARCHAR(500)  NOT NULL,
        Url           NVARCHAR(1000) NULL,
        -- Arista de pertenencia: la referencia muere con su nota (CASCADE).
        CONSTRAINT FK_PlatNotaReferencia_Nota
            FOREIGN KEY (NotaClinicaId) REFERENCES dbo.PlatNotaClinica (Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatNotaReferencia_NotaClinicaId'
               AND object_id = OBJECT_ID('dbo.PlatNotaReferencia'))
    CREATE INDEX IX_PlatNotaReferencia_NotaClinicaId ON dbo.PlatNotaReferencia (NotaClinicaId);
GO

/* ---------- Verificación ---------- */
SELECT
    (SELECT COUNT(*) FROM sys.tables
     WHERE name IN ('PlatNotaClinica', 'PlatNotaSeccion', 'PlatNotaReferencia')
       AND schema_id = SCHEMA_ID('dbo')) AS TablasNota;   -- debe ser 3

-- Aristas de pertenencia: ambas deben quedar en CASCADE (delete_referential_action = 1)
SELECT name AS FK, delete_referential_action AS DelAction
FROM sys.foreign_keys
WHERE name IN ('FK_PlatNotaSeccion_Nota', 'FK_PlatNotaReferencia_Nota')
ORDER BY name;
GO
