/* =====================================================================================
   2026-08-12 — Reset del triage para RE-GENERAR el catálogo (paso 2 del REQ del gate)
   -------------------------------------------------------------------------------------
   CONTEXTO
     El triage viejo se corrió pasándole al modelo la DescripcionIA autogenerada como
     "contexto". Cuando esa descripción estaba confabulada (caso Aangamik: un suplemento
     de venta libre descrito como inyectable para colitis), el contexto envenenado
     confirmaba al registro y el triage lo dejaba pasar. Por eso hay que re-clasificar
     POR NOMBRE, ya con el gate y el guardrail en producción.

   ORDEN OBLIGATORIO — no correr esto antes de tiempo:
     1. Desplegar el pipeline nuevo (gate + guardrail + modelo de descripción).
     2. Correr la sonda de aceptación: GET /api/admin/reconocimiento/aceptacion
        Veredicto APROBADA. Si la regla anti-supresión falla, PARAR y recalibrar.
     3. (Opcional pero recomendado) add-revision-limpieza-rescate-manual.sql, para que
        los rescates manuales tengan un flag de verdad y no un texto.
     4. ESTE script.
     5. Re-correr el triage desde el botón "Revisar con NINA" (dry-run primero).
     6. Re-generar descripciones con batch-generate-ia { "regenerar": true }.

   QUÉ PRESERVA (NO se resetea, por diseño):
     · ValidadoHumano = 1 en la propia tabla.
     · Términos con validación médica APROBADA en GlossaryValidation — ahí es donde
       valida el médico desde /Termino/{slug}, NO en tratamientos.ValidadoHumano.
       Ignorarlo sobrescribiría fichas ya revisadas por un profesional.
     · Rescates manuales (RevisionLimpiezaRescateManual = 1), si la columna existe.
     · Lo ya eliminado (Eliminado = 1): no se resucita nada aquí.

   IDEMPOTENTE: la segunda corrida afecta 0 filas (ya están en NULL).
   NO ES REVERSIBLE tal cual: el sello anterior se pierde. Por eso el paso 0 guarda una
   copia de respaldo antes de tocar nada.
   ===================================================================================== */

SET NOCOUNT ON;

-------------------------------------------------------------------------------
-- 0) RESPALDO — sin esto no hay vuelta atrás. Correr SIEMPRE.
-------------------------------------------------------------------------------
IF OBJECT_ID('dbo.BackupTriage_20260812', 'U') IS NULL
BEGIN
    SELECT 'tratamientos' AS Tabla, id, RevisionLimpiezaEstado, RevisionLimpiezaConfianza,
           RevisionLimpiezaMotivo, RevisionLimpiezaFecha
      INTO dbo.BackupTriage_20260812
      FROM dbo.tratamientos
     WHERE RevisionLimpiezaEstado IS NOT NULL;

    INSERT INTO dbo.BackupTriage_20260812 (Tabla, id, RevisionLimpiezaEstado, RevisionLimpiezaConfianza,
                                           RevisionLimpiezaMotivo, RevisionLimpiezaFecha)
    SELECT 'sintomas', id, RevisionLimpiezaEstado, RevisionLimpiezaConfianza,
           RevisionLimpiezaMotivo, RevisionLimpiezaFecha
      FROM dbo.sintomas
     WHERE RevisionLimpiezaEstado IS NOT NULL;

    PRINT N'Respaldo creado en dbo.BackupTriage_20260812.';
END
ELSE PRINT N'SKIP respaldo (dbo.BackupTriage_20260812 ya existe).';
GO

-------------------------------------------------------------------------------
-- 1) Diagnóstico ANTES — cuánto se preserva y cuánto se resetea
-------------------------------------------------------------------------------
;WITH protegidos AS (
    SELECT t.id
      FROM dbo.tratamientos t
     WHERE t.ValidadoHumano = 1
        OR EXISTS (SELECT 1
                     FROM dbo.GlossaryTermMedicalLink l
                     JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                    WHERE l.TratamientoId = t.id AND v.Approved = 1)
)
SELECT N'tratamientos' AS Tabla,
       (SELECT COUNT(*) FROM dbo.tratamientos WHERE RevisionLimpiezaEstado IS NOT NULL) AS ConSello,
       (SELECT COUNT(*) FROM protegidos)                                                AS ProtegidosPorHumano,
       (SELECT COUNT(*) FROM dbo.tratamientos WHERE Eliminado = 1)                      AS Eliminados;

;WITH protegidosS AS (
    SELECT s.id
      FROM dbo.sintomas s
     WHERE s.ValidadoHumano = 1
        OR EXISTS (SELECT 1
                     FROM dbo.GlossaryTermMedicalLink l
                     JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                    WHERE l.SintomaId = s.id AND v.Approved = 1)
)
SELECT N'sintomas' AS Tabla,
       (SELECT COUNT(*) FROM dbo.sintomas WHERE RevisionLimpiezaEstado IS NOT NULL) AS ConSello,
       (SELECT COUNT(*) FROM protegidosS)                                           AS ProtegidosPorHumano,
       (SELECT COUNT(*) FROM dbo.sintomas WHERE Eliminado = 1)                      AS Eliminados;
GO

-------------------------------------------------------------------------------
-- 2) APLICAR el reset (descomentar tras revisar el diagnóstico de arriba)
--    El predicado del flag de rescate se resuelve por COL_LENGTH para que el script
--    funcione tanto si la columna existe como si no.
-------------------------------------------------------------------------------
/*
DECLARE @hayFlagTrat bit = CASE WHEN COL_LENGTH('dbo.tratamientos','RevisionLimpiezaRescateManual') IS NULL THEN 0 ELSE 1 END;
DECLARE @hayFlagSint bit = CASE WHEN COL_LENGTH('dbo.sintomas','RevisionLimpiezaRescateManual')     IS NULL THEN 0 ELSE 1 END;

BEGIN TRANSACTION;

IF @hayFlagTrat = 1
    EXEC sp_executesql N'
        UPDATE t
           SET t.RevisionLimpiezaEstado = NULL, t.RevisionLimpiezaConfianza = NULL,
               t.RevisionLimpiezaMotivo = NULL, t.RevisionLimpiezaFecha = NULL
          FROM dbo.tratamientos t
         WHERE t.RevisionLimpiezaEstado IS NOT NULL
           AND t.Eliminado = 0
           AND t.ValidadoHumano = 0
           AND t.RevisionLimpiezaRescateManual = 0
           AND NOT EXISTS (SELECT 1 FROM dbo.GlossaryTermMedicalLink l
                             JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                            WHERE l.TratamientoId = t.id AND v.Approved = 1);';
ELSE
    UPDATE t
       SET t.RevisionLimpiezaEstado = NULL, t.RevisionLimpiezaConfianza = NULL,
           t.RevisionLimpiezaMotivo = NULL, t.RevisionLimpiezaFecha = NULL
      FROM dbo.tratamientos t
     WHERE t.RevisionLimpiezaEstado IS NOT NULL
       AND t.Eliminado = 0
       AND t.ValidadoHumano = 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GlossaryTermMedicalLink l
                         JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                        WHERE l.TratamientoId = t.id AND v.Approved = 1);

PRINT N'Tratamientos reseteados: ' + CAST(@@ROWCOUNT AS nvarchar(10));

IF @hayFlagSint = 1
    EXEC sp_executesql N'
        UPDATE s
           SET s.RevisionLimpiezaEstado = NULL, s.RevisionLimpiezaConfianza = NULL,
               s.RevisionLimpiezaMotivo = NULL, s.RevisionLimpiezaFecha = NULL
          FROM dbo.sintomas s
         WHERE s.RevisionLimpiezaEstado IS NOT NULL
           AND s.Eliminado = 0
           AND s.ValidadoHumano = 0
           AND s.RevisionLimpiezaRescateManual = 0
           AND NOT EXISTS (SELECT 1 FROM dbo.GlossaryTermMedicalLink l
                             JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                            WHERE l.SintomaId = s.id AND v.Approved = 1);';
ELSE
    UPDATE s
       SET s.RevisionLimpiezaEstado = NULL, s.RevisionLimpiezaConfianza = NULL,
           s.RevisionLimpiezaMotivo = NULL, s.RevisionLimpiezaFecha = NULL
      FROM dbo.sintomas s
     WHERE s.RevisionLimpiezaEstado IS NOT NULL
       AND s.Eliminado = 0
       AND s.ValidadoHumano = 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GlossaryTermMedicalLink l
                         JOIN dbo.GlossaryValidation v ON v.GlossaryTermId = l.GlossaryTermId
                        WHERE l.SintomaId = s.id AND v.Approved = 1);

PRINT N'Síntomas reseteados: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;
*/

-------------------------------------------------------------------------------
-- 3) UNDO — restaurar el sello desde el respaldo (si algo salió mal)
-------------------------------------------------------------------------------
/*
BEGIN TRANSACTION;

UPDATE t
   SET t.RevisionLimpiezaEstado    = b.RevisionLimpiezaEstado,
       t.RevisionLimpiezaConfianza = b.RevisionLimpiezaConfianza,
       t.RevisionLimpiezaMotivo    = b.RevisionLimpiezaMotivo,
       t.RevisionLimpiezaFecha     = b.RevisionLimpiezaFecha
  FROM dbo.tratamientos t
  JOIN dbo.BackupTriage_20260812 b ON b.id = t.id AND b.Tabla = 'tratamientos';

UPDATE s
   SET s.RevisionLimpiezaEstado    = b.RevisionLimpiezaEstado,
       s.RevisionLimpiezaConfianza = b.RevisionLimpiezaConfianza,
       s.RevisionLimpiezaMotivo    = b.RevisionLimpiezaMotivo,
       s.RevisionLimpiezaFecha     = b.RevisionLimpiezaFecha
  FROM dbo.sintomas s
  JOIN dbo.BackupTriage_20260812 b ON b.id = s.id AND b.Tabla = 'sintomas';

COMMIT TRANSACTION;
*/
