# REQ — NINA limpieza: enfocar por rama + acción "Aplicar Basura"

Extiende `REQ-nina-limpieza-tratamientos-triage.md` (ya CONSTRUIDO, commit 7819016). **Scope:** solo `eiibd26.Web` — `TratamientosAdminController` + `Admin/Tratamientos/Index`. Las columnas de estado ya existen. NO tocar NINA-WorkerService.

**Ejecución (AUTORIZADO por Berletzis, sin pedir permisos):** construye las dos adiciones directo; build + `publish -c Release`; diff al final.
**GATE de seguridad (respetar):** la acción **"Aplicar Basura" DESACTIVA datos de prod** → **constrúyela pero NO la invoques.** La dispara Berletzis por rama, tras revisar el bucket. El `batch-review` sigue siendo dry-run mientras no se cruce el gate.

## Contexto
El dry-run de 100 confirmó que el clasificador y los guards funcionan, pero midió el **catálogo curado de fármacos** (primeros ~2000 ids) → 97 válidos, 0 basura. La basura heredada vive bajo categorías tipo **"Cambios en el Estilo de Vida"**, no bajo **"Medicamentos con Receta"**. Barrer por id de arriba hacia abajo gasta horas de llamadas a la IA en puros "Válido" antes de llegar a la basura. Y el dry-run estampa estado → una corrida posterior con `DryRun=false` saltaría los ya marcados: falta el paso de **aplicar**.

## Adición 1 — Enfocar el review por rama
- **Endpoint:** `batch-review { Take, DryRun, RaizId? }`. Con `RaizId`, procesa solo esa rama:
  `WHERE RevisionLimpiezaEstado IS NULL AND Eliminado = 0 AND (id = @RaizId OR idPadre = @RaizId)` (ajustar si el árbol tiene más de 2 niveles), `OrderBy(id)`, `Take`. Sin `RaizId` → comportamiento actual.
- **UI:** dropdown de **categorías raíz** (idPadre IS NULL) con su conteo de **no revisados**, para elegir qué rama revisar. Arrancar por las que la gente llenó (mayor "SinRevisar").
- **Diagnóstico** (para ver el panorama y elegir rama):
```sql
SELECT p.id AS RaizId, p.nombre AS Categoria,
       COUNT(h.id) AS Hijos,
       SUM(CASE WHEN h.RevisionLimpiezaEstado IS NULL AND h.Eliminado = 0 THEN 1 ELSE 0 END) AS SinRevisar
FROM tratamientos p
LEFT JOIN tratamientos h ON h.idPadre = p.id
WHERE p.idPadre IS NULL
GROUP BY p.id, p.nombre
ORDER BY SinRevisar DESC;
```

## Adición 2 — Acción "Aplicar desactivación a Basura"
El dry-run estampa `RevisionLimpiezaEstado = 2` (Basura) con motivo/confianza pero **no** desactiva. Esta acción aplica en bloque, **sin llamadas a la IA**:
- **Endpoint:** `POST batch-apply-basura { RaizId? }`:
  `UPDATE tratamientos SET Eliminado = 1, fechaModificado = SYSDATETIME() WHERE RevisionLimpiezaEstado = 2 AND Eliminado = 0 [AND (id = @RaizId OR idPadre = @RaizId)]`
  **respetando los MISMOS guards** que el batch-review (defensa en profundidad, aunque no deberían estar marcados Basura): **nunca** desactivar un nodo con **hijos activos**, ni con `ValidadoHumano = true`, ni con **usuarios activos** (`tratamientoUsuario` no eliminado). Si alguno cae en un guard, saltarlo y reportarlo.
- **UI:** botón **"Aplicar desactivación a Basura"** (por rama), con confirmación que muestra **cuántos** desactivará antes de hacerlo.
- **Reversible:** el undo en bloque ya documentado (`UPDATE ... SET Eliminado = 0 WHERE RevisionLimpiezaEstado = 2 AND Eliminado = 1 [AND rama]`).
- **Este es el paso gateado** (equivale a `DryRun=false`): lo dispara Berletzis después de revisar el bucket Basura de esa rama.

## Flujo resultante (para Berletzis, por rama)
1. Correr el query de diagnóstico → ver categorías raíz y su "SinRevisar".
2. Elegir la rama más sucia → `batch-review` con `RaizId`, dry-run ON (clasifica, no desactiva).
3. Revisar en la UI el bucket **Basura** de esa rama (y de paso Dudoso).
4. Si cuadra → **"Aplicar desactivación a Basura"** de esa rama (con confirmación).
5. Repetir con la siguiente rama. El catálogo curado ("Medicamentos con Receta") se puede dejar sin revisar o marcar Válido aparte.

## Verificación
- `batch-review` con `RaizId` toca solo esa rama; sin `RaizId`, igual que hoy.
- El dropdown muestra conteos de no-revisados por raíz (cuadra con el query).
- `batch-apply-basura` desactiva solo `RevisionLimpiezaEstado = 2`, respeta los guards (probar con un nodo padre con hijos → NO se desactiva), y el conteo previo = filas afectadas. Reversible restaura exactamente esas.
- Build + `publish -c Release` limpios.
