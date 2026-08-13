/* =====================================================================================
   2026-08-10 — Alinear GlossaryTerm.Activo con tratamientos.Eliminado (retroactivo)
   -------------------------------------------------------------------------------------
   PROBLEMA
     El home del glosario cuenta desde dbo.tratamientos (WHERE Eliminado = 0) y la página
     /Glosario/Tratamientos cuenta desde dbo.GlossaryTerm (WHERE Activo = 1). Como el
     borrado lógico de tratamientos NUNCA propagó a GlossaryTerm.Activo, los números se
     separaron y lo desactivado (limpieza NINA + borrados manuales viejos) seguía saliendo
     en el glosario público.

   QUÉ HACE
     Aplica la invariante hacia atrás: tratamiento eliminado ⇒ término inactivo.
     De aquí en adelante el código la mantiene (IGlossaryService.SincronizarActivoPorTratamientosAsync).

   IDEMPOTENTE: correrlo dos veces afecta 0 filas la segunda vez.
   REVERSIBLE: ver bloque UNDO al final (comentado).
   ALCANCE: solo tratamientos. Los síntomas van en una pasada aparte.
   ===================================================================================== */

SET NOCOUNT ON;

-------------------------------------------------------------------------------
-- 1) Diagnóstico ANTES — los tres números que deben cuadrar
-------------------------------------------------------------------------------
SELECT
    N'ANTES' AS Momento,
    (SELECT COUNT(*) FROM dbo.tratamientos WHERE Eliminado = 0)                       AS HomeTratamientos,
    (SELECT COUNT(*) FROM dbo.GlossaryTerm WHERE TipoTermino = 2 AND Activo = 1)      AS GlosarioTodos,
    (SELECT COUNT(*)
       FROM dbo.GlossaryTerm gt
       JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
       JOIN dbo.tratamientos t           ON t.id = l.TratamientoId
      WHERE t.Eliminado = 1 AND gt.Activo = 1)                                        AS ADesactivar;

-------------------------------------------------------------------------------
-- 2) Muestra de lo que se va a ocultar (revisar antes de aplicar)
-------------------------------------------------------------------------------
SELECT TOP 25 gt.Id AS GlossaryTermId, gt.Nombre, gt.Slug, t.id AS TratamientoId,
       t.RevisionLimpiezaEstado, t.RevisionLimpiezaMotivo
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.tratamientos t            ON t.id = l.TratamientoId
WHERE t.Eliminado = 1 AND gt.Activo = 1
ORDER BY gt.Nombre;

-------------------------------------------------------------------------------
-- 3) APLICAR — término inactivo si su tratamiento está eliminado
--    Cubre de una vez la limpieza NINA (Estilo de Vida + Suplementos) y los
--    borrados manuales viejos que nunca propagaron.
-------------------------------------------------------------------------------
BEGIN TRANSACTION;

UPDATE gt
   SET gt.Activo             = 0,
       gt.FechaActualizacion = SYSUTCDATETIME()
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.tratamientos t            ON t.id = l.TratamientoId
WHERE t.Eliminado = 1
  AND gt.Activo   = 1;

PRINT N'Términos desactivados: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-------------------------------------------------------------------------------
-- 4) Diagnóstico DESPUÉS — Home ≈ Glosario "Todos", y 0 pendientes
-------------------------------------------------------------------------------
SELECT
    N'DESPUES' AS Momento,
    (SELECT COUNT(*) FROM dbo.tratamientos WHERE Eliminado = 0)                       AS HomeTratamientos,
    (SELECT COUNT(*) FROM dbo.GlossaryTerm WHERE TipoTermino = 2 AND Activo = 1)      AS GlosarioTodos,
    (SELECT COUNT(*)
       FROM dbo.GlossaryTerm gt
       JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
       JOIN dbo.tratamientos t           ON t.id = l.TratamientoId
      WHERE t.Eliminado = 1 AND gt.Activo = 1)                                        AS PendientesDebeSer0;

-- Desglose de los tabs de /Glosario/Tratamientos: Todos = D + I + S + Sin clasificar
SELECT
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 1 THEN 1 ELSE 0 END) AS Directa,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 2 THEN 1 ELSE 0 END) AS Indirecta,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 3 THEN 1 ELSE 0 END) AS Secundaria,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) IS NULL THEN 1 ELSE 0 END) AS SinClasificar,
    COUNT(*) AS Todos
FROM dbo.GlossaryTerm
WHERE TipoTermino = 2 AND Activo = 1;


/* =====================================================================================
   UNDO — deshacer la limpieza "Basura" (reactiva tratamiento Y término, en bloque)
   Descomentar y correr SOLO si se decide revertir la desactivación de la limpieza.
   Hay que tocar LAS DOS tablas: si solo se revierte `tratamientos`, el glosario queda
   desalineado del home otra vez.
   -------------------------------------------------------------------------------------
BEGIN TRANSACTION;

UPDATE t
   SET t.Eliminado       = 0,
       t.fechaEliminado  = '1900-01-01',
       t.fechaModificado = GETDATE()
FROM dbo.tratamientos t
WHERE t.RevisionLimpiezaEstado = 2
  AND t.Eliminado = 1;

UPDATE gt
   SET gt.Activo             = 1,
       gt.FechaActualizacion = SYSUTCDATETIME()
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.tratamientos t            ON t.id = l.TratamientoId
WHERE t.RevisionLimpiezaEstado = 2
  AND gt.Activo = 0;

COMMIT TRANSACTION;
   ===================================================================================== */
