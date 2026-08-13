/*
    fixes-triage-sintomas-puntuales.sql
    ---------------------------------------------------------------------------
    Los 3 arreglos que salieron del dry-run de síntomas (2026-08-12).
    CORRER PRIMERO EL BLOQUE 0 (solo lectura) y confirmar ids/duplicados antes
    de tocar nada. Sin migración EF Core (política del proyecto).

    Contexto: el dry-run NO desactivó nada (Eliminado sigue 0). Solo selló
    RevisionLimpiezaEstado. Reclasificar a Válido (estado=1) basta para proteger
    un registro: el runner de batch-review reanuda solo sobre estado IS NULL.
*/

SET NOCOUNT ON;

-- ══ BLOQUE 0 — DESCUBRIR (solo lectura). Corre esto primero. ══════════════════
SELECT s.id, s.nombre, s.RevisionLimpiezaEstado, s.RevisionLimpiezaConfianza,
       s.Eliminado,
       pacientes = (SELECT COUNT(*) FROM dbo.sintomasUsuario su
                    WHERE su.idSintoma = s.id AND su.Eliminado = 0)
FROM dbo.sintomas s
WHERE s.nombre IN (
        'Soledad',
        'Niveles tóxicos del fármaco antiinflamatorio no esteroideo (AINE)',
        'Dolor en las articulaciones mal escritas o con error',
        'Dolor en las articulaciones'   -- el limpio: ¿ya existe? (posible duplicado)
      )
ORDER BY s.nombre;
GO


-- ══ FIX 1 — Rescatar "Soledad" (falso positivo: soledad = síntoma emocional) ══
UPDATE dbo.sintomas
   SET RevisionLimpiezaEstado    = 1,          -- Válido
       RevisionLimpiezaConfianza = 1,
       RevisionLimpiezaMotivo    = 'Rescate manual (Berletzis): soledad = síntoma emocional real (aislamiento), no nombre propio. Falso positivo del triage NINA.',
       RevisionLimpiezaFecha     = GETUTCDATE()
 WHERE nombre = 'Soledad' AND Eliminado = 0;
GO


-- ══ FIX 2 — "Niveles tóxicos del fármaco AINE" — ELEGIR UNA opción ════════════
-- Opción A (recomendada): confirmar Basura y desactivar (es hallazgo de toxicología,
--   no un síntoma sentido). Solo si el BLOQUE 0 muestra pacientes = 0.
/*
UPDATE dbo.sintomas
   SET RevisionLimpiezaEstado = 2,             -- Basura
       Eliminado = 1, fechaEliminado = CAST(GETDATE() AS DATE), fechaModificado = GETDATE(),
       RevisionLimpiezaMotivo = 'Confirmado Basura (Berletzis): hallazgo de toxicología, no síntoma.',
       RevisionLimpiezaFecha = GETUTCDATE()
 WHERE nombre = 'Niveles tóxicos del fármaco antiinflamatorio no esteroideo (AINE)';
-- OJO: si se desactiva, sincronizar GlossaryTerm.Activo (ver el patrón de
--      batch-apply-basura / 2026-08-12-glosario-sincronizar-activo-sintomas.sql).
*/
-- Opción B: dejarlo en Dudoso (conservar en cola humana, no desactivar).
/*
UPDATE dbo.sintomas
   SET RevisionLimpiezaEstado = 3,             -- Dudoso
       RevisionLimpiezaMotivo = 'Dudoso (Berletzis): hallazgo de toxicología; decidir si el catálogo lista hallazgos de lab como síntoma.',
       RevisionLimpiezaFecha = GETUTCDATE()
 WHERE nombre = 'Niveles tóxicos del fármaco antiinflamatorio no esteroideo (AINE)';
*/
GO


-- ══ FIX 3 — Registro corrupto "...mal escritas o con error" ═══════════════════
-- TRAMPA: el limpio "Dolor en las articulaciones" YA EXISTE (salió Válido en el
-- dry-run). Por eso NO se renombra (crearía duplicado). Dos caminos según BLOQUE 0:
--
--   (a) el corrupto tiene pacientes = 0  → desactivarlo como duplicado corrupto:
/*
UPDATE dbo.sintomas
   SET Eliminado = 1, fechaEliminado = CAST(GETDATE() AS DATE), fechaModificado = GETDATE(),
       RevisionLimpiezaEstado = 2,
       RevisionLimpiezaMotivo = 'Duplicado corrupto de "Dolor en las articulaciones" (nombre con texto de QA embebido). Desactivado.',
       RevisionLimpiezaFecha = GETUTCDATE()
 WHERE nombre = 'Dolor en las articulaciones mal escritas o con error';
*/
--   (b) el corrupto SÍ tiene pacientes → NO desactivar aún; hay que migrar esos
--       sintomasUsuario al id del limpio primero (trabajo aparte, con cuidado).
GO
