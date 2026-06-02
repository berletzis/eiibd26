USE eiibd26;
GO
-- Backfill: alinear AspNetUserId en directorios ya reclamados donde quedó NULL,
-- tomando el UserId del MedicoPerfilExtendido vinculado.
-- Bug: VincularAsync no seteaba AspNetUserId → corregido en Activar.cshtml.cs.
-- EstatusReclamacion.Reclamado = 2
SET QUOTED_IDENTIFIER ON;
UPDATE d
SET d.AspNetUserId = pe.UserId
FROM MedicosDirectorio d
INNER JOIN MedicoPerfilExtendido pe ON pe.MedicoId = d.Id
WHERE d.AspNetUserId IS NULL
  AND pe.UserId IS NOT NULL
  AND d.EstatusReclamacion = 2;
GO
-- Verificación post-fix: debe devolver 0 filas
SELECT d.Id, d.EstatusReclamacion, d.AspNetUserId, pe.UserId
FROM MedicosDirectorio d
INNER JOIN MedicoPerfilExtendido pe ON pe.MedicoId = d.Id
WHERE d.AspNetUserId IS NULL AND pe.UserId IS NOT NULL;
