-- fix-platillos-vacios-desactivar.sql
-- Contexto: un PlatPlatillo Activo=1 sin renglones en PlatPlatilloIngrediente no viola
-- ninguna exclusion del paciente (no tiene ingredientes que evaluar), asi que PASABA todos
-- los filtros de /Platillos y se le mostraba como "si puedes comer" un platillo cuyo
-- contenido desconocemos. Ausencia de datos != seguridad.
--
-- Arreglo de datos: desactivar (dejar como borrador) todo platillo publicado sin ingredientes,
-- hasta que se le capturen. El codigo ya impide publicarlos de nuevo:
--   - Admin/Platillos/Detalle: guard al guardar (Activo requiere >=1 ingrediente).
--   - Admin/Platillos/Index: guard en el toggle (no se puede activar un platillo vacio).
--   - Pages/Platillos (F3b): un platillo sin ingredientes se excluye del listado publico.
--
-- Idempotente: solo toca los que esten Activo=1 y sin renglones. Correr las veces que sea.

SET NOCOUNT ON;

UPDATE p
SET p.Activo = 0
FROM dbo.PlatPlatillo p
WHERE p.Activo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.PlatPlatilloIngrediente pi WHERE pi.PlatilloId = p.Id);

-- Verificacion: debe dar 0.
SELECT COUNT(*) AS ActivosSinIngredientes
FROM dbo.PlatPlatillo p
WHERE p.Activo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.PlatPlatilloIngrediente pi WHERE pi.PlatilloId = p.Id);
