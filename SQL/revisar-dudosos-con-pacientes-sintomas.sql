/*
    revisar-dudosos-con-pacientes-sintomas.sql   (solo lectura)
    ---------------------------------------------------------------------------
    Apoyo para el paso #8 (revisar dudosos). Lista los síntomas en estado Dudoso
    que TIENEN pacientes activos, ordenados por los más usados primero — que son
    los que conviene mirar y re-promover antes, porque un Dudoso deja la ficha con
    noindex + banner "en revisión" (no la despublica, pero baja su exposición).

    Para re-promover uno a Válido tras revisarlo a mano:
        UPDATE dbo.sintomas
           SET RevisionLimpiezaEstado = 1, RevisionLimpiezaConfianza = 1,
               RevisionLimpiezaMotivo = 'Revisado y confirmado (Berletzis).',
               RevisionLimpiezaFecha = GETUTCDATE(),
               -- El gate lo frenó, así que quedó sellado como procesado pero SIN descripción
               -- nueva. Al re-promoverlo hay que devolverlo a la cola del re-proceso o nunca
               -- la va a recibir. (Requiere SQL/add-regeneracion-procesada.sql aplicado.)
               RegeneracionProcesadaUtc = NULL
         WHERE id = <id>;
*/

SET NOCOUNT ON;

SELECT
    s.id,
    s.nombre,
    pacientes = (SELECT COUNT(*) FROM dbo.sintomasUsuario su
                 WHERE su.idSintoma = s.id AND su.Eliminado = 0),
    s.RevisionLimpiezaConfianza                 AS Confianza,
    s.RevisionLimpiezaMotivo                    AS Motivo
FROM dbo.sintomas s
WHERE s.Eliminado = 0
  AND s.RevisionLimpiezaEstado = 3              -- Dudoso
  AND EXISTS (SELECT 1 FROM dbo.sintomasUsuario su
              WHERE su.idSintoma = s.id AND su.Eliminado = 0)
ORDER BY pacientes DESC, s.RevisionLimpiezaConfianza ASC;

-- Referencia: cuántos dudosos hay en total y cuántos con pacientes.
SELECT
    dudosos_total     = (SELECT COUNT(*) FROM dbo.sintomas
                         WHERE Eliminado = 0 AND RevisionLimpiezaEstado = 3),
    dudosos_con_pac   = (SELECT COUNT(*) FROM dbo.sintomas s
                         WHERE s.Eliminado = 0 AND s.RevisionLimpiezaEstado = 3
                           AND EXISTS (SELECT 1 FROM dbo.sintomasUsuario su
                                       WHERE su.idSintoma = s.id AND su.Eliminado = 0));
