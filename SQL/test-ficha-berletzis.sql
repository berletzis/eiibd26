-- test-ficha-berletzis.sql
-- HELPER DE PRUEBA (no deploy-gate): da a la cuenta del fundador (c.berletzis) una ficha
-- de directorio vinculada, para poder aprobarla/verificarla y probar el ciclo completo
-- (tipo profesional, validaciones con nombre). Idempotente.
-- Enums: EstatusValidacionCedula.PendienteValidacion=0 · EstatusReclamacion.NoReclamado=0 · NivelConfianza.Identificado=0
-- Tras correrlo: en Admin/DirectorioMedicos verifica su cédula (otorga el badge "verificado" por la app).

DECLARE @uid UNIQUEIDENTIFIER = (SELECT Id FROM AspNetUsers WHERE Email = 'c.berletzis@dological.com');
IF @uid IS NULL THROW 50001, 'No existe c.berletzis@dological.com', 1;

-- 1. Ficha vinculada (solo si no tiene una activa)
IF NOT EXISTS (SELECT 1 FROM MedicosDirectorio WHERE AspNetUserId = @uid AND Eliminado = 0)
    INSERT INTO MedicosDirectorio
        (NombreCompleto, Especialidad, CedulaProfesional, NombrePais,
         AspNetUserId, EstatusValidacion, EstatusReclamacion, NivelConfianza,
         Activo, VisiblePublicamente, Eliminado, FechaCreacion)
    VALUES
        (N'Berletzis (prueba)', N'Gastroenterología', N'TEST-0001', N'mx',
         @uid, 0, 0, 0, 0, 0, 0, SYSDATETIMEOFFSET());

-- 2. Vincular el perfil-extendido a la ficha
DECLARE @fichaId INT = (SELECT TOP 1 Id FROM MedicosDirectorio
                        WHERE AspNetUserId = @uid AND Eliminado = 0 ORDER BY Id DESC);
UPDATE MedicoPerfilExtendido SET MedicoId = @fichaId WHERE UserId = @uid;

-- 3. Confirmar
SELECT d.Id AS FichaId, d.NombreCompleto, d.EstatusValidacion, pe.MedicoId, pe.TipoProfesional
FROM MedicosDirectorio d
JOIN MedicoPerfilExtendido pe ON pe.MedicoId = d.Id
WHERE d.AspNetUserId = @uid;
