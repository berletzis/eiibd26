-- ============================================================================
-- SCRIPT: indices_tablas_clinicas.sql
-- Propósito: Agregar índices en FK de tablas clínicas para mejorar performance
--            de consultas de dashboard y perfil de usuario.
-- Issues:    DB-001 (condicionUsuario), DB-002 (sintomasUsuario / tratamientoUsuario),
--            DB-003 (EstadoAnimoUsuario / TrackingSintomaUsuario)
-- POLÍTICA:  Sin migraciones EF. Aplicar manualmente en servidor SQL.
-- Validar:   Ejecutar SELECT al final del script para confirmar creación.
-- ============================================================================

-- ─── DB-001: condicionUsuario ────────────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_condicionUsuario_IdUsuario'
	AND object_id = OBJECT_ID('dbo.condicionUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_condicionUsuario_IdUsuario
		ON dbo.condicionUsuario (idUsuario)
		INCLUDE (idCondicion, fechaInicio, Eliminado);
	PRINT 'Creado: IX_condicionUsuario_IdUsuario';
END
ELSE
	PRINT 'Ya existe: IX_condicionUsuario_IdUsuario';
GO

-- ─── DB-002a: sintomasUsuario ─────────────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_sintomasUsuario_IdUsuario'
	AND object_id = OBJECT_ID('dbo.sintomasUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_sintomasUsuario_IdUsuario
		ON dbo.sintomasUsuario (idUsuario)
		INCLUDE (idSintoma, Eliminado);
	PRINT 'Creado: IX_sintomasUsuario_IdUsuario';
END
ELSE
	PRINT 'Ya existe: IX_sintomasUsuario_IdUsuario';
GO

-- ─── DB-002b: tratamientoUsuario ──────────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_tratamientoUsuario_IdUsuario'
	AND object_id = OBJECT_ID('dbo.tratamientoUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_tratamientoUsuario_IdUsuario
		ON dbo.tratamientoUsuario (idUsuario)
		INCLUDE (idTratamiento, Eliminado);
	PRINT 'Creado: IX_tratamientoUsuario_IdUsuario';
END
ELSE
	PRINT 'Ya existe: IX_tratamientoUsuario_IdUsuario';
GO

-- ─── DB-003a: EstadoAnimoUsuario ──────────────────────────────────────────────
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_EstadoAnimoUsuario_IdUsuario'
	AND object_id = OBJECT_ID('dbo.EstadoAnimoUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_EstadoAnimoUsuario_IdUsuario
		ON dbo.EstadoAnimoUsuario (IdUsuario)
		INCLUDE (EstadoMood, FechaRegistro, Eliminado);
	PRINT 'Creado: IX_EstadoAnimoUsuario_IdUsuario';
END
ELSE
	PRINT 'Ya existe: IX_EstadoAnimoUsuario_IdUsuario';
GO

-- ─── DB-003b: EstadoAnimoUsuario compuesto (para rango de fechas en dashboard) ─
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro'
	AND object_id = OBJECT_ID('dbo.EstadoAnimoUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro
		ON dbo.EstadoAnimoUsuario (IdUsuario, FechaRegistro DESC)
		INCLUDE (EstadoMood, Eliminado);
	PRINT 'Creado: IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro';
END
ELSE
	PRINT 'Ya existe: IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro';
GO

-- ─── DB-003c: TrackingSintomaUsuario (consultas de historial de síntomas) ─────
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = 'IX_TrackingSintomaUsuario_IdUsuario_Fecha'
	AND object_id = OBJECT_ID('dbo.TrackingSintomaUsuario'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_TrackingSintomaUsuario_IdUsuario_Fecha
		ON dbo.TrackingSintomaUsuario (IdUsuario, Fecha DESC)
		INCLUDE (IdSintomaUsuario, Estado);
	PRINT 'Creado: IX_TrackingSintomaUsuario_IdUsuario_Fecha';
END
ELSE
	PRINT 'Ya existe: IX_TrackingSintomaUsuario_IdUsuario_Fecha';
GO

-- ─── VALIDACIÓN ──────────────────────────────────────────────────────────────
SELECT
	t.name AS Tabla,
	i.name AS Indice,
	i.type_desc AS Tipo,
	i.is_disabled
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name IN (
	'IX_condicionUsuario_IdUsuario',
	'IX_sintomasUsuario_IdUsuario',
	'IX_tratamientoUsuario_IdUsuario',
	'IX_EstadoAnimoUsuario_IdUsuario',
	'IX_EstadoAnimoUsuario_IdUsuario_FechaRegistro',
	'IX_TrackingSintomaUsuario_IdUsuario_Fecha'
)
ORDER BY t.name, i.name;
GO
