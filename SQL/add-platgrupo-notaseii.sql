-- add-platgrupo-notaseii.sql
-- Contexto clinico del grupo para la vista de ingrediente ("Puedo comer queso?").
-- Contenido MEDICO: vive en BD (no en codigo) para poder corregirse sin recompilar
-- y para que lo revise un medico. Se carga vacio; se llena desde el CRUD de Grupos.
-- Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.PlatGrupo') AND name = 'NotasEII')
    ALTER TABLE dbo.PlatGrupo ADD NotasEII NVARCHAR(MAX) NULL;
