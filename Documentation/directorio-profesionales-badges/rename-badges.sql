-- =============================================================================
-- Script SQL: Renombramiento de Badges para Simplificación
-- =============================================================================
-- Proyecto: eiibd26 - Directorio de Profesionales de Salud
-- Fecha: 2025-06-04
-- Objetivo: Renombrar badges ambiguos por nombres específicos y claros
-- =============================================================================

USE [eiibd]; -- Ajustar nombre de base de datos según ambiente
GO

-- -----------------------------------------------------------------------------
-- 1. Renombrar "Verificado" → "Cédula Verificada"
-- -----------------------------------------------------------------------------
-- Razón: "Verificado" es ambiguo. "Cédula Verificada" es específico y claro.
-- Tipo: Validación administrativa por parte de administradores.
-- -----------------------------------------------------------------------------

UPDATE MedicoBadge
SET 
	Nombre = 'Cédula Verificada',
	Descripcion = 'La cédula profesional de este médico ha sido verificada manualmente por administradores de EIIBD.'
WHERE Codigo = 'verificado';

SELECT * FROM MedicoBadge WHERE Codigo = 'verificado';
GO

-- -----------------------------------------------------------------------------
-- 2. Renombrar "Activo en Comunidad" → "Validado por Pacientes"
-- -----------------------------------------------------------------------------
-- Razón: "Activo en Comunidad" es ambiguo (¿activo en redes? ¿en Q&A?).
--        "Validado por Pacientes" es directo y honesto.
-- Tipo: Reputación comunitaria basada en confirmaciones de pacientes.
-- -----------------------------------------------------------------------------

UPDATE MedicoBadge
SET 
	Nombre = 'Validado por Pacientes',
	Descripcion = 'Al menos 5 pacientes de la comunidad EII han confirmado atención médica con este profesional.'
WHERE Codigo = 'activo_comunidad';

SELECT * FROM MedicoBadge WHERE Codigo = 'activo_comunidad';
GO

-- =============================================================================
-- Verificación Final
-- =============================================================================

SELECT 
	Id,
	Codigo,
	Nombre,
	Descripcion,
	Nivel,
	Orden,
	Activo
FROM MedicoBadge
WHERE Codigo IN ('verificado', 'activo_comunidad', 'perfil_reclamado')
ORDER BY Orden;
GO

-- =============================================================================
-- Rollback (en caso de error)
-- =============================================================================
-- Descomentar y ejecutar solo si se requiere revertir:
/*
UPDATE MedicoBadge SET Nombre = 'Verificado' WHERE Codigo = 'verificado';
UPDATE MedicoBadge SET Nombre = 'Activo en Comunidad' WHERE Codigo = 'activo_comunidad';
*/
