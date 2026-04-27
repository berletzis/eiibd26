-- ============================================================
-- Script: CREATE TABLE TrackingSintomaUsuario
-- Fecha:  2026-06
-- Descripción:
--   Crea la tabla completa incluyendo la columna Dolor INT NULL
--   (dimensión clínica independiente de dolor, 0–10).
--   Si la tabla ya existe, solo agrega la columna Dolor si falta.
-- ============================================================

-- ── 1. Crear tabla si no existe ───────────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1
	FROM   sys.objects
	WHERE  object_id = OBJECT_ID(N'dbo.TrackingSintomaUsuario')
	  AND  type      = N'U'
)
BEGIN
	CREATE TABLE dbo.TrackingSintomaUsuario
	(
		Id                INT            NOT NULL IDENTITY(1,1),
		IdUsuario         UNIQUEIDENTIFIER NOT NULL,
		IdSintomaUsuario  INT            NOT NULL,
		Fecha             DATETIME2      NOT NULL DEFAULT GETDATE(),
		Estado            NVARCHAR(16)   NOT NULL,
		Dolor             INT            NULL,

		CONSTRAINT PK_TrackingSintomaUsuario
			PRIMARY KEY (Id),

		CONSTRAINT FK_TrackingSintomaUsuario_AspNetUsers
			FOREIGN KEY (IdUsuario)
			REFERENCES dbo.AspNetUsers (Id)
			ON DELETE NO ACTION
			ON UPDATE NO ACTION,

		CONSTRAINT FK_TrackingSintomaUsuario_sintomasUsuario
			FOREIGN KEY (IdSintomaUsuario)
			REFERENCES dbo.sintomasUsuario (id)
			ON DELETE NO ACTION
			ON UPDATE NO ACTION,

		CONSTRAINT CK_TrackingSintomaUsuario_Estado
			CHECK (Estado IN (N'Ninguno', N'Leve', N'Moderado', N'Severo')),

		-- Restricción opcional: valor entre 0 y 10
		CONSTRAINT CK_TrackingSintomaUsuario_Dolor
			CHECK (Dolor IS NULL OR (Dolor >= 0 AND Dolor <= 10))
	);

	PRINT 'Tabla TrackingSintomaUsuario creada correctamente.';
END
ELSE
BEGIN
	PRINT 'La tabla TrackingSintomaUsuario ya existe. Verificando columna Dolor...';

	-- ── 2. Agregar columna Dolor si la tabla ya existía y le falta ────────────
	IF NOT EXISTS (
		SELECT 1
		FROM   sys.columns
		WHERE  object_id = OBJECT_ID(N'dbo.TrackingSintomaUsuario')
		  AND  name      = N'Dolor'
	)
	BEGIN
		ALTER TABLE dbo.TrackingSintomaUsuario
			ADD Dolor INT NULL;

		-- Restricción opcional: valor entre 0 y 10
		ALTER TABLE dbo.TrackingSintomaUsuario
			ADD CONSTRAINT CK_TrackingSintomaUsuario_Dolor
				CHECK (Dolor IS NULL OR (Dolor >= 0 AND Dolor <= 10));

		PRINT 'Columna Dolor agregada correctamente.';
	END
	ELSE
	BEGIN
		PRINT 'La columna Dolor ya existe. No se realizaron cambios.';
	END
END
GO
