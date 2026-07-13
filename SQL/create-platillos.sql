/* ============================================================================
   create-platillos.sql  —  Esquema del Módulo de Platillos (F1)
   ----------------------------------------------------------------------------
   Tablas PROPIAS del Web, creadas por SQL directo (sin migración EF).
   Prefijo Plat*. Módulo autocontenido: no toca ninguna tabla existente.
   Única relación externa: PlatPerfilExclusion.idUsuario (uniqueidentifier),
   referencia LÓGICA a Identity (AspNetUsers) — igual que condicionUsuario.
   NO se crea FK física a AspNetUsers para preservar el aislamiento del módulo.

   Idempotente: cada CREATE/INDEX está guardado con IF NOT EXISTS.
   Debe correrse ANTES de seed-platillos.sql (el seed depende de estos
   nombres exactos de tabla y columna).

   Convenciones del repo: esquema dbo explícito, GO como separador de lote,
   INT IDENTITY para PKs, DATETIME2 con DEFAULT SYSUTCDATETIME(),
   constraints nombradas (PK_/FK_/UQ_/DF_/UX_/IX_), índice en cada FK.
   ============================================================================ */
SET NOCOUNT ON;
-- Requerido para el índice único filtrado (WHERE Eliminado = 0)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- 1) CATÁLOGOS ---------- */

-- PlatGrupo: qué ES el alimento (lácteo, verdura, pescado…)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatGrupo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatGrupo
    (
        Id     INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatGrupo PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Orden  INT           NOT NULL CONSTRAINT DF_PlatGrupo_Orden  DEFAULT (0),
        Activo BIT           NOT NULL CONSTRAINT DF_PlatGrupo_Activo DEFAULT (1),
        CONSTRAINT UQ_PlatGrupo_Nombre UNIQUE (Nombre)
    );
END
GO

-- PlatAtributo: cómo se comporta / se usa. Ambito = 'Ingrediente' (intrínseco) | 'Uso'
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatAtributo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatAtributo
    (
        Id          INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatAtributo PRIMARY KEY,
        Nombre      NVARCHAR(100) NOT NULL,
        Ambito      NVARCHAR(20)  NOT NULL,   -- 'Ingrediente' | 'Uso'
        Descripcion NVARCHAR(500) NULL,
        Activo      BIT           NOT NULL CONSTRAINT DF_PlatAtributo_Activo DEFAULT (1),
        CONSTRAINT UQ_PlatAtributo_Nombre UNIQUE (Nombre)
    );
END
GO

-- PlatCategoria: Entrada, Plato fuerte, Ensalada…
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatCategoria' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatCategoria
    (
        Id     INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatCategoria PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Orden  INT           NOT NULL CONSTRAINT DF_PlatCategoria_Orden  DEFAULT (0),
        Activo BIT           NOT NULL CONSTRAINT DF_PlatCategoria_Activo DEFAULT (1),
        CONSTRAINT UQ_PlatCategoria_Nombre UNIQUE (Nombre)
    );
END
GO

-- PlatUnidad: pieza, taza, g, ml, cda, al gusto…
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatUnidad' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatUnidad
    (
        Id     INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatUnidad PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Activo BIT           NOT NULL CONSTRAINT DF_PlatUnidad_Activo DEFAULT (1),
        CONSTRAINT UQ_PlatUnidad_Nombre UNIQUE (Nombre)
    );
END
GO

/* ---------- 2) INGREDIENTES ---------- */

-- PlatIngrediente: el alimento base (único, minúscula, singular)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatIngrediente' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatIngrediente
    (
        Id            INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatIngrediente PRIMARY KEY,
        Nombre        NVARCHAR(200) NOT NULL,
        GrupoId       INT           NOT NULL,
        NotasEII      NVARCHAR(MAX) NULL,
        Activo        BIT           NOT NULL CONSTRAINT DF_PlatIngrediente_Activo DEFAULT (1),
        FechaCreacion DATETIME2     NOT NULL CONSTRAINT DF_PlatIngrediente_Fecha  DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_PlatIngrediente_Nombre UNIQUE (Nombre),
        CONSTRAINT FK_PlatIngrediente_Grupo FOREIGN KEY (GrupoId) REFERENCES dbo.PlatGrupo (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatIngrediente_GrupoId'
               AND object_id = OBJECT_ID('dbo.PlatIngrediente'))
    CREATE INDEX IX_PlatIngrediente_GrupoId ON dbo.PlatIngrediente (GrupoId);
GO

-- PlatIngredienteAtributo: atributos INTRÍNSECOS del ingrediente (PK compuesta)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatIngredienteAtributo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatIngredienteAtributo
    (
        IngredienteId INT NOT NULL,
        AtributoId    INT NOT NULL,
        CONSTRAINT PK_PlatIngredienteAtributo PRIMARY KEY (IngredienteId, AtributoId),
        CONSTRAINT FK_PlatIngredienteAtributo_Ingrediente
            FOREIGN KEY (IngredienteId) REFERENCES dbo.PlatIngrediente (Id),
        CONSTRAINT FK_PlatIngredienteAtributo_Atributo
            FOREIGN KEY (AtributoId)    REFERENCES dbo.PlatAtributo (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatIngredienteAtributo_AtributoId'
               AND object_id = OBJECT_ID('dbo.PlatIngredienteAtributo'))
    CREATE INDEX IX_PlatIngredienteAtributo_AtributoId ON dbo.PlatIngredienteAtributo (AtributoId);
GO

/* ---------- 3) PLATILLOS ---------- */

-- PlatPlatillo: la cabecera. Codigo (P001) único. CreadoPorUserId nullable.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatPlatillo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatPlatillo
    (
        Id             INT              IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatPlatillo PRIMARY KEY,
        Codigo         NVARCHAR(20)     NOT NULL,
        Nombre         NVARCHAR(250)    NOT NULL,
        CategoriaId    INT              NOT NULL,
        Porciones      INT              NULL,
        TiempoPrepMin  INT              NULL,
        Dificultad     NVARCHAR(20)     NULL,   -- 'Fácil' | 'Media' | 'Difícil'
        PasosResumidos NVARCHAR(MAX)    NULL,
        FuenteNombre   NVARCHAR(300)    NULL,
        FuenteUrl      NVARCHAR(1000)   NULL,
        Notas          NVARCHAR(MAX)    NULL,
        Activo         BIT              NOT NULL CONSTRAINT DF_PlatPlatillo_Activo DEFAULT (1),
        FechaCreacion  DATETIME2        NOT NULL CONSTRAINT DF_PlatPlatillo_Fecha  DEFAULT (SYSUTCDATETIME()),
        CreadoPorUserId UNIQUEIDENTIFIER NULL,
        CONSTRAINT UQ_PlatPlatillo_Codigo UNIQUE (Codigo),
        CONSTRAINT FK_PlatPlatillo_Categoria FOREIGN KEY (CategoriaId) REFERENCES dbo.PlatCategoria (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPlatillo_CategoriaId'
               AND object_id = OBJECT_ID('dbo.PlatPlatillo'))
    CREATE INDEX IX_PlatPlatillo_CategoriaId ON dbo.PlatPlatillo (CategoriaId);
GO

-- PlatPlatilloIngrediente: renglón de ingrediente dentro de un platillo (tiene Id propio)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatPlatilloIngrediente' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatPlatilloIngrediente
    (
        Id              INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatPlatilloIngrediente PRIMARY KEY,
        PlatilloId      INT           NOT NULL,
        IngredienteId   INT           NOT NULL,
        TextoOriginal   NVARCHAR(500) NULL,
        Cantidad        DECIMAL(9,3)  NULL,
        UnidadId        INT           NULL,
        EsAlGusto       BIT           NOT NULL CONSTRAINT DF_PlatPlatIng_EsAlGusto DEFAULT (0),
        NotaPreparacion NVARCHAR(500) NULL,
        -- Arista de pertenencia: los renglones mueren con su platillo (CASCADE).
        CONSTRAINT FK_PlatPlatIng_Platillo
            FOREIGN KEY (PlatilloId)    REFERENCES dbo.PlatPlatillo (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PlatPlatIng_Ingrediente
            FOREIGN KEY (IngredienteId) REFERENCES dbo.PlatIngrediente (Id),
        CONSTRAINT FK_PlatPlatIng_Unidad
            FOREIGN KEY (UnidadId)      REFERENCES dbo.PlatUnidad (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPlatIng_PlatilloId'
               AND object_id = OBJECT_ID('dbo.PlatPlatilloIngrediente'))
    CREATE INDEX IX_PlatPlatIng_PlatilloId ON dbo.PlatPlatilloIngrediente (PlatilloId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPlatIng_IngredienteId'
               AND object_id = OBJECT_ID('dbo.PlatPlatilloIngrediente'))
    CREATE INDEX IX_PlatPlatIng_IngredienteId ON dbo.PlatPlatilloIngrediente (IngredienteId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPlatIng_UnidadId'
               AND object_id = OBJECT_ID('dbo.PlatPlatilloIngrediente'))
    CREATE INDEX IX_PlatPlatIng_UnidadId ON dbo.PlatPlatilloIngrediente (UnidadId);
GO

-- PlatPlatilloIngredienteAtributo: atributos de USO en ESE platillo (PK compuesta)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatPlatilloIngredienteAtributo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatPlatilloIngredienteAtributo
    (
        PlatilloIngredienteId INT NOT NULL,
        AtributoId            INT NOT NULL,
        CONSTRAINT PK_PlatPlatilloIngredienteAtributo PRIMARY KEY (PlatilloIngredienteId, AtributoId),
        -- Arista de pertenencia: el atributo de uso muere con su renglón (CASCADE). Hijo puro.
        CONSTRAINT FK_PlatPlatIngAtr_PlatilloIngrediente
            FOREIGN KEY (PlatilloIngredienteId) REFERENCES dbo.PlatPlatilloIngrediente (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PlatPlatIngAtr_Atributo
            FOREIGN KEY (AtributoId)            REFERENCES dbo.PlatAtributo (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPlatIngAtr_AtributoId'
               AND object_id = OBJECT_ID('dbo.PlatPlatilloIngredienteAtributo'))
    CREATE INDEX IX_PlatPlatIngAtr_AtributoId ON dbo.PlatPlatilloIngredienteAtributo (AtributoId);
GO

/* ---------- 4) PERFIL ALIMENTARIO DEL PACIENTE ---------- */

-- PlatPerfilExclusion: lo que el paciente NO tolera. Soft delete (Eliminado), igual que condicionUsuario.
-- Tipo distingue a qué apunta RefId: 'Grupo' | 'Ingrediente' | 'Atributo'.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatPerfilExclusion' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PlatPerfilExclusion
    (
        Id             INT              IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatPerfilExclusion PRIMARY KEY,
        idUsuario      UNIQUEIDENTIFIER NOT NULL,
        Tipo           NVARCHAR(20)     NOT NULL,   -- 'Grupo' | 'Ingrediente' | 'Atributo'
        RefId          INT              NOT NULL,
        FechaCreacion  DATETIME2        NOT NULL CONSTRAINT DF_PlatPerfilExclusion_Fecha DEFAULT (SYSUTCDATETIME()),
        Eliminado      BIT              NOT NULL CONSTRAINT DF_PlatPerfilExclusion_Elim  DEFAULT (0),
        FechaEliminado DATETIME2        NULL
    );
END
GO

-- Índice en el FK lógico idUsuario
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlatPerfilExclusion_idUsuario'
               AND object_id = OBJECT_ID('dbo.PlatPerfilExclusion'))
    CREATE INDEX IX_PlatPerfilExclusion_idUsuario ON dbo.PlatPerfilExclusion (idUsuario);
GO

-- Único filtrado: una exclusión activa por (usuario, tipo, refId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlatPerfilExclusion_User_Tipo_Ref'
               AND object_id = OBJECT_ID('dbo.PlatPerfilExclusion'))
    CREATE UNIQUE INDEX UX_PlatPerfilExclusion_User_Tipo_Ref
        ON dbo.PlatPerfilExclusion (idUsuario, Tipo, RefId)
        WHERE Eliminado = 0;
GO

/* ---------- Ajuste: ON DELETE CASCADE en las aristas de pertenencia ----------
   Idempotente. Corrige instalaciones donde estas FKs se crearon como NO ACTION.
   delete_referential_action: 0=NO_ACTION, 1=CASCADE. Solo tocamos si no es CASCADE.
   Los atributos de uso / renglones son hijos puros: mueren con su padre por
   construcción (así la edición de platillo es un delete de un paso, no de dos). */

IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_PlatPlatIng_Platillo' AND delete_referential_action <> 1)
BEGIN
    ALTER TABLE dbo.PlatPlatilloIngrediente DROP CONSTRAINT FK_PlatPlatIng_Platillo;
    ALTER TABLE dbo.PlatPlatilloIngrediente
        ADD CONSTRAINT FK_PlatPlatIng_Platillo
        FOREIGN KEY (PlatilloId) REFERENCES dbo.PlatPlatillo (Id) ON DELETE CASCADE;
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_PlatPlatIngAtr_PlatilloIngrediente' AND delete_referential_action <> 1)
BEGIN
    ALTER TABLE dbo.PlatPlatilloIngredienteAtributo DROP CONSTRAINT FK_PlatPlatIngAtr_PlatilloIngrediente;
    ALTER TABLE dbo.PlatPlatilloIngredienteAtributo
        ADD CONSTRAINT FK_PlatPlatIngAtr_PlatilloIngrediente
        FOREIGN KEY (PlatilloIngredienteId) REFERENCES dbo.PlatPlatilloIngrediente (Id) ON DELETE CASCADE;
END
GO

/* ---------- Verificación ---------- */
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE name LIKE 'Plat%' AND schema_id = SCHEMA_ID('dbo')) AS TablasPlat;
-- Aristas de pertenencia: ambas deben quedar en CASCADE (delete_referential_action = 1)
SELECT name AS FK, delete_referential_action AS DelAction
FROM sys.foreign_keys
WHERE name IN ('FK_PlatPlatIng_Platillo', 'FK_PlatPlatIngAtr_PlatilloIngrediente')
ORDER BY name;
GO
