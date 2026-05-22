USE eiibd26;
GO

-- ── 1. MedicoPerfilExtendido ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoPerfilExtendido')
BEGIN
    CREATE TABLE MedicoPerfilExtendido (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId         INT NULL,
        UserId           UNIQUEIDENTIFIER NULL,
        Slug             NVARCHAR(100) NULL,
        Foto             NVARCHAR(500) NULL,
        Biografia        NVARCHAR(2000) NULL,
        Hospitales       NVARCHAR(1000) NULL,
        HorariosAtencion NVARCHAR(500) NULL,
        SitioWeb         NVARCHAR(300) NULL,
        Telefono         NVARCHAR(50) NULL,
        Instagram        NVARCHAR(150) NULL,
        LinkedIn         NVARCHAR(150) NULL,
        FechaCreado      DATETIME NOT NULL DEFAULT GETUTCDATE(),
        FechaModificado  DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MedicoPerfilExtendido_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE SET NULL,
        CONSTRAINT FK_MedicoPerfilExtendido_User
            FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX UX_MedicoPerfilExtendido_MedicoId
        ON MedicoPerfilExtendido(MedicoId) WHERE MedicoId IS NOT NULL;
    CREATE UNIQUE INDEX UX_MedicoPerfilExtendido_Slug
        ON MedicoPerfilExtendido(Slug) WHERE Slug IS NOT NULL;
    CREATE INDEX IX_MedicoPerfilExtendido_UserId
        ON MedicoPerfilExtendido(UserId);
    PRINT 'Tabla MedicoPerfilExtendido creada.';
END
ELSE PRINT 'Tabla MedicoPerfilExtendido ya existe.';
GO

-- ── 2. MedicoBadge ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoBadge')
BEGIN
    CREATE TABLE MedicoBadge (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        Codigo        NVARCHAR(50) NOT NULL,
        Nombre        NVARCHAR(150) NOT NULL,
        Descripcion   NVARCHAR(500) NOT NULL,
        ComoObtenerlo NVARCHAR(300) NOT NULL,
        Icono         NVARCHAR(100) NOT NULL,
        Nivel         INT NOT NULL,
        Orden         INT NOT NULL,
        Activo        BIT NOT NULL DEFAULT 1,
        FechaCreado   DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UX_MedicoBadge_Codigo UNIQUE (Codigo)
    );

    INSERT INTO MedicoBadge (Codigo, Nombre, Descripcion, ComoObtenerlo, Icono, Nivel, Orden) VALUES
    ('perfil_reclamado',   'Perfil Reclamado',       'El médico ha reclamado su perfil en el directorio EII.', 'Reclamar y completar el perfil',                'bi-person-check-fill',    1, 1),
    ('verificado',         'Verificado',             'El equipo EIIBD ha verificado las credenciales del médico.', 'El equipo EIIBD verifica tus credenciales', 'bi-patch-check-fill',     2, 2),
    ('activo_comunidad',   'Activo en Comunidad',    'Al menos 5 pacientes han recomendado a este médico.',    '5 o más pacientes te han recomendado',          'bi-people-fill',          3, 3),
    ('participante_qa',    'Participante Q&A',       'Ha respondido 3 o más preguntas en el foro.',            'Responder 3 o más preguntas en el foro',        'bi-chat-dots-fill',       4, 4),
    ('validador_contenido','Validador de Contenido', 'Ha validado 5 o más términos del glosario.',             'Validar 5 o más términos del glosario',         'bi-check-circle-fill',    5, 5),
    ('creador_contenido',  'Creador de Contenido',   'Contribuye activamente con contenido médico de calidad.','El equipo EIIBD lo otorga manualmente',         'bi-star-fill',            6, 6);

    PRINT 'Tabla MedicoBadge creada con seed.';
END
ELSE PRINT 'Tabla MedicoBadge ya existe.';
GO

-- ── 3. MedicoPerfilBadge ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoPerfilBadge')
BEGIN
    CREATE TABLE MedicoPerfilBadge (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId      INT NOT NULL,
        BadgeId       INT NOT NULL,
        FechaObtenido DATETIME NOT NULL DEFAULT GETUTCDATE(),
        OtorgadoPor   NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_MedicoPerfilBadge_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoPerfilBadge_Badge
            FOREIGN KEY (BadgeId) REFERENCES MedicoBadge(Id) ON DELETE CASCADE,
        CONSTRAINT UX_MedicoPerfilBadge_MedBadge UNIQUE (MedicoId, BadgeId)
    );
    PRINT 'Tabla MedicoPerfilBadge creada.';
END
ELSE PRINT 'Tabla MedicoPerfilBadge ya existe.';
GO

-- ── 4. MedicoReclamacionToken ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MedicoReclamacionToken')
BEGIN
    CREATE TABLE MedicoReclamacionToken (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        MedicoId     INT NOT NULL,
        Token        NVARCHAR(200) NOT NULL,
        EmailDestino NVARCHAR(200) NOT NULL,
        UserId       UNIQUEIDENTIFIER NULL,
        FechaCreado  DATETIME NOT NULL DEFAULT GETUTCDATE(),
        FechaExpira  DATETIME NOT NULL,
        FechaUsado   DATETIME NULL,
        Activo       BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_MedicoReclamacionToken_Medico
            FOREIGN KEY (MedicoId) REFERENCES MedicosDirectorio(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MedicoReclamacionToken_User
            FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT UX_MedicoReclamacionToken_Token UNIQUE (Token)
    );
    CREATE INDEX IX_MedicoReclamacionToken_MedicoId ON MedicoReclamacionToken(MedicoId);
    PRINT 'Tabla MedicoReclamacionToken creada.';
END
ELSE PRINT 'Tabla MedicoReclamacionToken ya existe.';
GO
