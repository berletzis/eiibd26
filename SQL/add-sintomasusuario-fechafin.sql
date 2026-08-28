-- add-sintomasusuario-fechafin.sql
-- Fecha de fin OPCIONAL del sintoma del usuario (cuando dejo de tenerlo).
-- NULL = sintoma aun activo/vigente. Espejo de tratamientoUsuario.FechaFin (datetime2 NULL).
-- Las filas existentes quedan en NULL, que es el estado correcto.
-- DEPLOY-GATE: correr ANTES de desplegar el codigo que lee/escribe sintomasUsuario.FechaFin.
-- Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomasUsuario') AND name = 'FechaFin')
    ALTER TABLE dbo.sintomasUsuario ADD FechaFin DATETIME2 NULL;
