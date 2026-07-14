/* ============================================================================
   rename-nota-publicado.sql  —  F2a: el candado deja de ser "revisado por médico"
                                 y pasa a ser "publicado por admin".
   ----------------------------------------------------------------------------
   Renombra las 3 columnas del candado en dbo.PlatNotaClinica:
       RevisadaPorMedico  -> Publicado           (EL CANDADO)
       RevisadaPorUserId  -> PublicadaPorUserId
       FechaRevision      -> FechaPublicacion
   y la default constraint del flag. Publicar es acto del ADMINISTRADOR; la
   validación médica es OTRA señal (F2b), por eso "Revisada*" era engañoso.

   Reset del queso (Id 18): se publicó a mano durante la prueba del candado en
   F1. Vuelve a borrador para cerrar el deploy-gate sin contenido a medio revisar.

   Idempotente: cada rename se guarda tras verificar que la columna vieja aún
   existe, así re-correr el script no falla. Correr DESPUÉS de desplegar el
   código de F2a (el modelo ya espera 'Publicado'); si se corre antes, la app
   vieja queda leyendo una columna que ya no existe.

   No usa migración EF (cambio de esquema por SQL directo, como el resto del repo).
   ============================================================================ */
SET NOCOUNT ON;
GO

/* ---------- 1) RevisadaPorMedico -> Publicado ---------- */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.PlatNotaClinica') AND name = 'RevisadaPorMedico')
    EXEC sp_rename 'dbo.PlatNotaClinica.RevisadaPorMedico', 'Publicado', 'COLUMN';
GO

/* ---------- 2) RevisadaPorUserId -> PublicadaPorUserId ---------- */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.PlatNotaClinica') AND name = 'RevisadaPorUserId')
    EXEC sp_rename 'dbo.PlatNotaClinica.RevisadaPorUserId', 'PublicadaPorUserId', 'COLUMN';
GO

/* ---------- 3) FechaRevision -> FechaPublicacion ---------- */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.PlatNotaClinica') AND name = 'FechaRevision')
    EXEC sp_rename 'dbo.PlatNotaClinica.FechaRevision', 'FechaPublicacion', 'COLUMN';
GO

/* ---------- 4) Default constraint del flag (cosmético, mantiene la convención) ---------- */
IF EXISTS (SELECT 1 FROM sys.objects
           WHERE name = 'DF_PlatNotaClinica_Revisada'
             AND parent_object_id = OBJECT_ID('dbo.PlatNotaClinica'))
    EXEC sp_rename 'DF_PlatNotaClinica_Revisada', 'DF_PlatNotaClinica_Publicado', 'OBJECT';
GO

/* ---------- 5) Reset del queso (Id 18) a borrador ---------- */
UPDATE dbo.PlatNotaClinica
SET Publicado = 0, PublicadaPorUserId = NULL, FechaPublicacion = NULL
WHERE Id = 18;
GO

/* ---------- Verificación ---------- */
-- Las 3 columnas nuevas deben existir (cuenta = 3); las viejas, 0.
SELECT
    SUM(CASE WHEN name IN ('Publicado','PublicadaPorUserId','FechaPublicacion') THEN 1 ELSE 0 END) AS ColsNuevas,   -- 3
    SUM(CASE WHEN name IN ('RevisadaPorMedico','RevisadaPorUserId','FechaRevision') THEN 1 ELSE 0 END) AS ColsViejas -- 0
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.PlatNotaClinica');

-- El queso debe quedar en borrador (Publicado = 0).
SELECT Id, TipoDestino, DestinoId, Publicado FROM dbo.PlatNotaClinica WHERE Id = 18;
GO
