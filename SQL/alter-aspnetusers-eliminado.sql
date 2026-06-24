-- Soft-delete reversible de usuarios (interruptor maestro, Opción A).
-- Agrega columna Eliminado a AspNetUsers. Al marcarse (true):
--   * el usuario queda excluido de TODOS los conteos vía UsuarioValidez.SoloValidos()
--   * se le corta el acceso (lockout aplicado desde el handler)
-- Los datos clínicos NO se tocan. Es reversible (restaurar => Eliminado = 0 + quitar lockout).
-- Ejecutar manualmente en producción. Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.AspNetUsers')
                 AND name = 'Eliminado')
    ALTER TABLE dbo.AspNetUsers ADD Eliminado bit NOT NULL DEFAULT 0;
