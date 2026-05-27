-- repair-badge-verificado-sync.sql
-- Sincroniza el badge "verificado" con EstatusValidacion en datos históricos.
-- Ejecutar UNA VEZ después de deployar el fix BUG-3 (Mayo 2026).
-- Idempotente: se puede volver a ejecutar sin duplicar datos.

DECLARE @BadgeId INT = (SELECT Id FROM MedicoBadge WHERE Codigo = 'verificado');

IF @BadgeId IS NULL
BEGIN
    PRINT 'ERROR: Badge "verificado" no encontrado en tabla MedicoBadge.';
    RETURN;
END

-- 1. Otorgar badge a profesionales con cédula verificada que no lo tienen
INSERT INTO MedicoPerfilBadge (MedicoId, BadgeId, FechaObtenido, OtorgadoPor)
SELECT md.Id, @BadgeId, GETUTCDATE(), 'sistema-repair'
FROM MedicosDirectorio md
WHERE md.EstatusValidacion = 1   -- Validado
  AND md.Eliminado = 0
  AND NOT EXISTS (
      SELECT 1 FROM MedicoPerfilBadge mpb
      WHERE mpb.MedicoId = md.Id AND mpb.BadgeId = @BadgeId
  );

PRINT CONCAT('Badges otorgados: ', @@ROWCOUNT);

-- 2. Revocar badge de profesionales sin cédula verificada que sí lo tienen
DELETE FROM MedicoPerfilBadge
WHERE BadgeId = @BadgeId
  AND MedicoId IN (
      SELECT md.Id FROM MedicosDirectorio md
      WHERE md.EstatusValidacion != 1  -- No validado
  );

PRINT CONCAT('Badges revocados: ', @@ROWCOUNT);
PRINT 'Sincronización badge "verificado" completada.';

-- 3. Auditoría: mostrar estado final
SELECT md.Id, md.NombreCompleto, md.EstatusValidacion,
    CASE WHEN mpb.MedicoId IS NOT NULL THEN 'SI' ELSE 'NO' END AS TieneBadgeVerificado
FROM MedicosDirectorio md
LEFT JOIN MedicoPerfilBadge mpb ON mpb.MedicoId = md.Id AND mpb.BadgeId = @BadgeId
WHERE md.Eliminado = 0
  AND (md.EstatusValidacion = 1 OR mpb.MedicoId IS NOT NULL)
ORDER BY md.NombreCompleto;
