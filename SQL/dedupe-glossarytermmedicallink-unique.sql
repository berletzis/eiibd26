/* ============================================================================
   Depuración de MedicalLink duplicados + índice UNIQUE
   Tabla: dbo.GlossaryTermMedicalLink
   Corrido en producción (64.202.187.218\SQLSERVER · eiibd26) el 2026-08-06.

   PROBLEMA
   --------
   24 términos del glosario tenían DOS filas en GlossaryTermMedicalLink. El
   modelo EF trata MedicalLink como navegación de referencia (1:1), así que
   GlossaryService.GetTermBySlugAsync tomaba UNA de las dos sin orden definido.
   En los 24 casos el par era: un registro con la descripción real + un stub
   vacío (sin DescripcionIA, RelacionEII=0, ValidadoHumano=1, nombre en
   variante de caso/espacios, 0 usuarios). Cuando EF elegía el stub, el término
   rendía "Definición pendiente de generar" con la definición real disponible.

   QUÉ HACE
   --------
   Conserva por término el link de mayor calidad (rn=1: no eliminado, CON
   descripción, mayor RelacionEII, mayor ValidadoHumano, menor Id) y borra el
   resto. Después crea un índice UNIQUE sobre GlossaryTermId para que el 1:1
   que el modelo asume lo garantice la base.

   NO toca los registros vacíos de tratamientos/sintomas — solo los links.
   Su limpieza es un tema aparte (pueden estar referenciados en otros lados).

   IDEMPOTENTE: re-correrlo borra 0 filas y no recrea el índice.
   ============================================================================ */

SET NOCOUNT ON;

/* ---------- Paso 1 · Pre-check: qué links se van a borrar ---------- */
;WITH ranked AS (
    SELECT l.Id AS LinkId, l.GlossaryTermId,
           ROW_NUMBER() OVER (
               PARTITION BY l.GlossaryTermId
               ORDER BY
                   COALESCE(t.Eliminado, s.Eliminado, 0) ASC,
                   CASE WHEN NULLIF(LTRIM(RTRIM(COALESCE(t.DescripcionIA, s.DescripcionIA))),'') IS NULL THEN 0 ELSE 1 END DESC,
                   COALESCE(t.RelacionEII, s.RelacionEII, 0) DESC,
                   COALESCE(t.ValidadoHumano, s.ValidadoHumano, 0) DESC,
                   l.Id ASC
           ) AS rn
    FROM GlossaryTermMedicalLink l
    LEFT JOIN tratamientos t ON t.id = l.TratamientoId
    LEFT JOIN sintomas     s ON s.id = l.SintomaId
    WHERE l.GlossaryTermId IN (
        SELECT GlossaryTermId FROM GlossaryTermMedicalLink GROUP BY GlossaryTermId HAVING COUNT(*) > 1)
)
SELECT LinkId FROM ranked WHERE rn > 1 ORDER BY LinkId;

/* Set borrado el 2026-08-06 (24 filas):
   188, 257, 303, 464, 998, 1016, 1261, 1849, 3215, 3435, 3507, 4793,
   5119, 5792, 6078, 6292, 6517, 7197, 7861, 9075, 9329, 9879, 9940, 10056  */

/* ---------- Paso 2 · Borrado ---------- */
;WITH ranked AS (
    SELECT l.Id AS LinkId, l.GlossaryTermId,
           ROW_NUMBER() OVER (
               PARTITION BY l.GlossaryTermId
               ORDER BY
                   COALESCE(t.Eliminado, s.Eliminado, 0) ASC,
                   CASE WHEN NULLIF(LTRIM(RTRIM(COALESCE(t.DescripcionIA, s.DescripcionIA))),'') IS NULL THEN 0 ELSE 1 END DESC,
                   COALESCE(t.RelacionEII, s.RelacionEII, 0) DESC,
                   COALESCE(t.ValidadoHumano, s.ValidadoHumano, 0) DESC,
                   l.Id ASC
           ) AS rn
    FROM GlossaryTermMedicalLink l
    LEFT JOIN tratamientos t ON t.id = l.TratamientoId
    LEFT JOIN sintomas     s ON s.id = l.SintomaId
    WHERE l.GlossaryTermId IN (
        SELECT GlossaryTermId FROM GlossaryTermMedicalLink GROUP BY GlossaryTermId HAVING COUNT(*) > 1)
)
DELETE FROM GlossaryTermMedicalLink WHERE Id IN (SELECT LinkId FROM ranked WHERE rn > 1);

/* ---------- Paso 3 · Post-check ---------- */
-- Debe dar 0
SELECT COUNT(*) AS TerminosConDuplicado
FROM (SELECT GlossaryTermId FROM GlossaryTermMedicalLink
      GROUP BY GlossaryTermId HAVING COUNT(*) > 1) x;

-- Spot-check: Temblor conserva el síntoma 57 (el que trae la descripción)
SELECT l.GlossaryTermId, l.Id AS LinkId, l.SintomaId, l.TratamientoId
FROM GlossaryTermMedicalLink l
WHERE l.GlossaryTermId IN (56, 3496, 7843)
ORDER BY l.GlossaryTermId;

/* ---------- Paso 4 · Índice UNIQUE (previene la recurrencia) ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_GlossaryTermMedicalLink_Term'
                 AND object_id = OBJECT_ID('dbo.GlossaryTermMedicalLink'))
    CREATE UNIQUE INDEX UQ_GlossaryTermMedicalLink_Term
        ON dbo.GlossaryTermMedicalLink (GlossaryTermId);

/* ---------- Paso 5 · Quitar el índice no único, ya redundante ----------
   IX_GlossaryTermMedicalLink_GlossaryTermId indexaba la MISMA columna sin ser
   único: el UNIQUE de arriba sirve los mismos seeks, así que mantenerlo solo
   agrega mantenimiento en cada escritura. Ninguna consulta de la app lo
   referencia por nombre (no hay hints WITH (INDEX...)).
   Corrido en producción el 2026-08-06. Los scripts de SQL/Glossary/ ya crean
   el UNIQUE en su lugar, así que un install limpio no lo vuelve a traer.      */
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_GlossaryTermMedicalLink_GlossaryTermId'
             AND object_id = OBJECT_ID('dbo.GlossaryTermMedicalLink'))
    DROP INDEX IX_GlossaryTermMedicalLink_GlossaryTermId ON dbo.GlossaryTermMedicalLink;
