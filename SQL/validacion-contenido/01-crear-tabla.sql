-- ============================================================
-- Tabla: ValidacionesContenidoProfesional
-- Valida contenido médico (términos del glosario y artículos CMS)
-- por profesionales de la salud.
-- Reemplaza GlossaryValidation tipo=1; tipo=2 permanece en GlossaryValidation.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional')
)
BEGIN
    CREATE TABLE dbo.ValidacionesContenidoProfesional (
        Id              INT IDENTITY(1,1) NOT NULL,
        TipoContenido   TINYINT NOT NULL,        -- 1 = Termino, 2 = Articulo
        ContenidoId     INT NOT NULL,            -- GlossaryTerm.Id | Contenido.Id
        UsuarioMedicoId NVARCHAR(450) NOT NULL,  -- AspNetUsers.Id
        Comentario      NVARCHAR(800) NULL,
        Estado          TINYINT NOT NULL CONSTRAINT DF_VCP_Estado DEFAULT 1, -- 1=Validado, 2=EnRevision, 3=Oculto
        CreadoEn        DATETIME2 NOT NULL CONSTRAINT DF_VCP_CreadoEn DEFAULT GETUTCDATE(),
        ActualizadoEn   DATETIME2 NULL,
        ModeradoPorId   NVARCHAR(450) NULL,
        FechaModeracion DATETIME2 NULL,
        NotaModeracion  NVARCHAR(500) NULL,

        CONSTRAINT PK_ValidacionesContenidoProfesional PRIMARY KEY (Id),
        CONSTRAINT CK_VCP_TipoContenido CHECK (TipoContenido IN (1, 2)),
        CONSTRAINT CK_VCP_Estado CHECK (Estado IN (1, 2, 3))
    );
    PRINT 'Tabla ValidacionesContenidoProfesional creada.';
END
ELSE
    PRINT 'Tabla ValidacionesContenidoProfesional ya existe.';

-- Índice único principal: 1 validación por (tipo, contenido, médico)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional')
      AND name = 'IX_VCP_Unique_TipoContenidoId_Medico'
)
BEGIN
    CREATE UNIQUE INDEX IX_VCP_Unique_TipoContenidoId_Medico
        ON dbo.ValidacionesContenidoProfesional (TipoContenido, ContenidoId, UsuarioMedicoId);
    PRINT 'Índice único creado.';
END

-- Índice de búsqueda por médico (panel admin)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional')
      AND name = 'IX_VCP_UsuarioMedicoId'
)
BEGIN
    CREATE INDEX IX_VCP_UsuarioMedicoId
        ON dbo.ValidacionesContenidoProfesional (UsuarioMedicoId);
END

-- Índice de búsqueda por contenido (lectura pública)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional')
      AND name = 'IX_VCP_TipoContenido_ContenidoId'
)
BEGIN
    CREATE INDEX IX_VCP_TipoContenido_ContenidoId
        ON dbo.ValidacionesContenidoProfesional (TipoContenido, ContenidoId);
END

PRINT 'Script 01 completo.';
