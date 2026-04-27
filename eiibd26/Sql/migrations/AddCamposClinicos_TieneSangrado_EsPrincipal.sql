-- ============================================================
-- Script: Agregar campos clínicos nuevos al esquema existente
-- Fecha:  2026-06
-- Tablas afectadas:
--   TrackingSintomaUsuario  → TieneSangrado BIT NULL
--   sintomasUsuario         → EsPrincipal   BIT NOT NULL DEFAULT 0
--   condicionUsuario        → EsPrincipal   BIT NOT NULL DEFAULT 0
--   tratamientoUsuario      → EsPrincipal   BIT NOT NULL DEFAULT 0
-- ============================================================

-- ── 1. TieneSangrado en TrackingSintomaUsuario ────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.TrackingSintomaUsuario') AND name = N'TieneSangrado'
)
BEGIN
	ALTER TABLE dbo.TrackingSintomaUsuario
		ADD TieneSangrado BIT NULL;
	PRINT 'TieneSangrado agregado a TrackingSintomaUsuario.';
END
ELSE
	PRINT 'TieneSangrado ya existe en TrackingSintomaUsuario.';
GO

-- ── 2. EsPrincipal en sintomasUsuario ─────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.sintomasUsuario') AND name = N'EsPrincipal'
)
BEGIN
	ALTER TABLE dbo.sintomasUsuario
		ADD EsPrincipal BIT NOT NULL DEFAULT 0;
	PRINT 'EsPrincipal agregado a sintomasUsuario.';
END
ELSE
	PRINT 'EsPrincipal ya existe en sintomasUsuario.';
GO

-- ── 3. EsPrincipal en condicionUsuario ────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.condicionUsuario') AND name = N'EsPrincipal'
)
BEGIN
	ALTER TABLE dbo.condicionUsuario
		ADD EsPrincipal BIT NOT NULL DEFAULT 0;
	PRINT 'EsPrincipal agregado a condicionUsuario.';
END
ELSE
	PRINT 'EsPrincipal ya existe en condicionUsuario.';
GO

-- ── 4. EsPrincipal en tratamientoUsuario ──────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.tratamientoUsuario') AND name = N'EsPrincipal'
)
BEGIN
	ALTER TABLE dbo.tratamientoUsuario
		ADD EsPrincipal BIT NOT NULL DEFAULT 0;
	PRINT 'EsPrincipal agregado a tratamientoUsuario.';
END
ELSE
	PRINT 'EsPrincipal ya existe en tratamientoUsuario.';
GO
