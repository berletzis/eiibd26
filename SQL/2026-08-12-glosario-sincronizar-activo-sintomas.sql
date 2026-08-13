/* =====================================================================================
   2026-08-12 — Alinear GlossaryTerm.Activo con sintomas.Eliminado (retroactivo)
   -------------------------------------------------------------------------------------
   PROBLEMA
     El home del glosario cuenta desde dbo.sintomas (WHERE Eliminado = 0) y la página
     /Glosario/Sintomas cuenta desde dbo.GlossaryTerm (WHERE Activo = 1). Como el borrado
     lógico de síntomas NUNCA propagó a GlossaryTerm.Activo, los números se separaron y lo
     desactivado (borrados manuales viejos + la limpieza NINA) seguía saliendo en el
     glosario público.

   QUÉ HACE
     Aplica la invariante hacia atrás: síntoma eliminado ⇒ término inactivo.
     De aquí en adelante el código la mantiene (IGlossaryService.SincronizarActivoPorSintomasAsync).

   IDEMPOTENTE: correrlo dos veces afecta 0 filas la segunda vez.
   REVERSIBLE: ver bloque UNDO al final (comentado).
   ALCANCE: solo síntomas. Los tratamientos ya se hicieron en
            2026-08-10-glosario-sincronizar-activo-tratamientos.sql.

   NOTA: TipoTermino 1 = Síntoma, 2 = Tratamiento.
   ===================================================================================== */

SET NOCOUNT ON;

-------------------------------------------------------------------------------
-- 1) Diagnóstico ANTES — los tres números que deben cuadrar
-------------------------------------------------------------------------------
SELECT
    N'ANTES' AS Momento,
    (SELECT COUNT(*) FROM dbo.sintomas WHERE Eliminado = 0)                          AS HomeSintomas,
    (SELECT COUNT(*) FROM dbo.GlossaryTerm WHERE TipoTermino = 1 AND Activo = 1)     AS GlosarioTodos,
    (SELECT COUNT(*)
       FROM dbo.GlossaryTerm gt
       JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
       JOIN dbo.sintomas s               ON s.id = l.SintomaId
      WHERE s.Eliminado = 1 AND gt.Activo = 1)                                       AS ADesactivar;

-------------------------------------------------------------------------------
-- 2) Muestra de lo que se va a ocultar (revisar antes de aplicar)
-------------------------------------------------------------------------------
SELECT TOP 25 gt.Id AS GlossaryTermId, gt.Nombre, gt.Slug, s.id AS SintomaId,
       s.RevisionLimpiezaEstado, s.RevisionLimpiezaMotivo
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.sintomas s                ON s.id = l.SintomaId
WHERE s.Eliminado = 1 AND gt.Activo = 1
ORDER BY gt.Nombre;

-------------------------------------------------------------------------------
-- 3) APLICAR — término inactivo si su síntoma está eliminado
--    Cubre de una vez la limpieza NINA y los borrados manuales viejos que nunca
--    propagaron.
-------------------------------------------------------------------------------
BEGIN TRANSACTION;

UPDATE gt
   SET gt.Activo             = 0,
       gt.FechaActualizacion = SYSUTCDATETIME()
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.sintomas s                ON s.id = l.SintomaId
WHERE s.Eliminado = 1
  AND gt.Activo   = 1;

PRINT N'Términos desactivados: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-------------------------------------------------------------------------------
-- 4) Diagnóstico DESPUÉS — Home ≈ Glosario "Todos", y 0 pendientes
-------------------------------------------------------------------------------
SELECT
    N'DESPUES' AS Momento,
    (SELECT COUNT(*) FROM dbo.sintomas WHERE Eliminado = 0)                          AS HomeSintomas,
    (SELECT COUNT(*) FROM dbo.GlossaryTerm WHERE TipoTermino = 1 AND Activo = 1)     AS GlosarioTodos,
    (SELECT COUNT(*)
       FROM dbo.GlossaryTerm gt
       JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
       JOIN dbo.sintomas s               ON s.id = l.SintomaId
      WHERE s.Eliminado = 1 AND gt.Activo = 1)                                       AS PendientesDebeSer0;

-- Desglose de los tabs de /Glosario/Sintomas: Todos = D + I + S + Sin clasificar
SELECT
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 1 THEN 1 ELSE 0 END) AS Directa,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 2 THEN 1 ELSE 0 END) AS Indirecta,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) = 3 THEN 1 ELSE 0 END) AS Secundaria,
    SUM(CASE WHEN COALESCE(MedicalRelationTypeId, MedicalRelationSuggestedId) IS NULL THEN 1 ELSE 0 END) AS SinClasificar,
    COUNT(*) AS Todos
FROM dbo.GlossaryTerm
WHERE TipoTermino = 1 AND Activo = 1;


/* =====================================================================================
   UNDO — deshacer la limpieza "Basura" (reactiva síntoma Y término, en bloque)
   Descomentar y correr SOLO si se decide revertir la desactivación de la limpieza.
   Hay que tocar LAS DOS tablas: si solo se revierte `sintomas`, el glosario queda
   desalineado del home otra vez.
   -------------------------------------------------------------------------------------
BEGIN TRANSACTION;

UPDATE s
   SET s.Eliminado       = 0,
       s.fechaEliminado  = '1900-01-01',
       s.fechaModificado = GETDATE()
FROM dbo.sintomas s
WHERE s.RevisionLimpiezaEstado = 2
  AND s.Eliminado = 1;

UPDATE gt
   SET gt.Activo             = 1,
       gt.FechaActualizacion = SYSUTCDATETIME()
FROM dbo.GlossaryTerm gt
JOIN dbo.GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN dbo.sintomas s                ON s.id = l.SintomaId
WHERE s.RevisionLimpiezaEstado = 2
  AND gt.Activo = 0;

COMMIT TRANSACTION;
   ===================================================================================== */
