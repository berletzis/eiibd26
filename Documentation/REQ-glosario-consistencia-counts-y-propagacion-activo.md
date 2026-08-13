# REQ — Consistencia de counts del glosario (home vs glosario vs tabs) + propagar el desactivado a GlossaryTerm

**Scope:** solo `eiibd26.Web` — `Services/Glossary/GlossaryService.cs`, `Controllers/TratamientosAdminController.cs`, `Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml.cs` (delete/restore) + un SQL retroactivo. NO tocar NINA-WorkerService.
**Ejecución (AUTORIZADO por Berletzis, sin pedir permisos):** aplica los cambios de código, corre el SQL retroactivo, invalida la caché y verifica los counts. Muestra diffs al final. Es reversible (Activo vuelve a 1).

## Contexto — dos desajustes distintos en las secciones del usuario
Los counts de tratamientos NO cuadran entre secciones del usuario:
- **Home "Tratamientos" (9704)** = `GetGlossaryHomeAsync` → `_db.tratamientos.CountAsync(!Eliminado)`. **Bajó** con el desactivado.
- **Glosario "Todos" (10033)** = `GetTermsByTypeAsync` → `GlossaryTerms WHERE TipoTermino=Tratamiento AND Activo`. **NO bajó** — nunca tocamos `GlossaryTerm.Activo`.
- **Glosario tabs**: Directa 106 + Indirecta 3524 + Secundaria 21 + Sin clasificar 0 = 3651 ≠ Todos 10033.

## Causa raíz
1. **Home vs Glosario:** el desactivado de la limpieza pone `tratamientos.Eliminado=1` pero **no** propaga a `GlossaryTerm.Activo`. El home cuenta `tratamientos` (refleja la limpieza); el glosario cuenta `GlossaryTerm` (no la refleja). Por eso además Oscillococcinum y los ~312 desactivados siguen apareciendo.
2. **Todos vs tabs:** en `GetTermsByTypeAsync`, `NivelRelacion = gt.MedicalRelationTypeId ?? gt.MedicalRelationSuggestedId ?? (MedicalRelationType)0`. A los términos **sin relación** les asigna el valor `0`, que no es Directa/Indirecta/Secundaria **ni** null → se caen de todos los tabs y "Sin clasificar" (que busca `== null`) muestra 0. Los ~6382 sin nivel quedan invisibles en tabs pero contados en Todos. (El DTO `NivelRelacion` YA es `MedicalRelationType?` nullable; el `?? (MedicalRelationType)0` es el bug.)

## Cambios

### A. Propagar `tratamiento.Eliminado ↔ GlossaryTerm.Activo` (invariante)
Regla: **un tratamiento eliminado ⇒ su término del glosario inactivo; restaurado ⇒ activo.**
Aplicar en TODOS los lugares que voltean `tratamientos.Eliminado`:
- `TratamientosAdminController.batch-apply-basura`: al poner `Eliminado=1`, poner `GlossaryTerm.Activo=false` del término vinculado (vía `GlossaryTermMedicalLink.TratamientoId → GlossaryTermId`).
- `Admin/Tratamientos/Index.cshtml.cs` → `OnPostEliminarTratamientoAsync` (borrado manual): igual, desactivar el término.
- `OnPostRestaurarTratamientoAsync` (restaurar): reactivar el término (`Activo=true`).
- Reutilizar/extender `PropagateToGlossaryTermAsync` o un helper análogo (ya resuelve el link tratamiento→GlossaryTerm).

### B. Quitar el default `(MedicalRelationType)0` en el listado
En `GetTermsByTypeAsync`:
```csharp
// antes:
NivelRelacion = gt.MedicalRelationTypeId ?? gt.MedicalRelationSuggestedId ?? (MedicalRelationType)0
// después (dejar null cuando no hay relación):
NivelRelacion = gt.MedicalRelationTypeId ?? gt.MedicalRelationSuggestedId
```
Verificar que las vistas que consumen `NivelRelacion` manejan null (los tabs de `Tratamientos.cshtml` ya cuentan `== null` como "Sin clasificar"; el color/label tiene fallback `_ => "Sin clasificar"`). Revisar también `Sintomas.cshtml` por si comparte el patrón.

### C. SQL retroactivo — alinear lo ya existente
```sql
-- Invariante para todo lo ya eliminado: término inactivo si su tratamiento está eliminado
UPDATE gt SET gt.Activo = 0
FROM GlossaryTerm gt
JOIN GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
JOIN tratamientos t ON t.id = l.TratamientoId
WHERE t.Eliminado = 1 AND gt.Activo = 1;
```
Esto oculta de una vez lo ya desactivado (Estilo de Vida + Suplementos) **y** los borrados manuales viejos, cerrando la brecha home↔glosario.
Undo de la limpieza basura (reactiva tratamiento Y término, en bloque):
```sql
UPDATE t SET t.Eliminado = 0 FROM tratamientos t WHERE t.RevisionLimpiezaEstado = 2 AND t.Eliminado = 1;
UPDATE gt SET gt.Activo = 1 FROM GlossaryTerm gt
  JOIN GlossaryTermMedicalLink l ON l.GlossaryTermId = gt.Id
  JOIN tratamientos t ON t.id = l.TratamientoId
  WHERE t.RevisionLimpiezaEstado = 2;
```

### D. Caché
Invalidar la caché del listado del glosario (la de ~10 min en `GlossaryService`) cuando se desactiva/activa un término, o al menos documentar que los counts se refrescan al expirar. Idealmente invalidar la clave del listado de Tratamientos tras A/C para que los números se actualicen al toque.

## Verificación (las tres vistas cuadran)
1. **Home "Tratamientos"** ≈ **Glosario "Todos"** (ambos reflejan lo eliminado; near-1:1 salvo diferencia estructural mínima term↔tratamiento).
2. **Glosario "Todos"** = Directa + Indirecta + Secundaria + **Sin clasificar** (ahora "Sin clasificar" muestra los ~6382, no 0).
3. **Oscillococcinum / Oscap / Herbalife / Agua de Plata** (y demás basura desactivada) **ya no aparecen** en `/Glosario/Tratamientos`.
4. **Restaurar** un tratamiento desde el admin → su término **vuelve a aparecer** en el glosario (reversibilidad).
5. `dotnet publish -c Release` limpio y las páginas del glosario abren sin error de Razor.

## Nota / fuera de alcance inmediato
- Los **síntomas** tienen la misma estructura (home cuenta `sintomas`, glosario cuenta `GlossaryTerm`). Si se ve el mismo desajuste, aplicar el mismo patrón (propagar `sintomas.Eliminado → GlossaryTerm.Activo`) en una pasada aparte.
