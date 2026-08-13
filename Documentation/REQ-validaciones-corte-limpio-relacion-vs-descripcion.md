# REQ — Corte limpio de validaciones: relación arriba, descripción abajo (sin duplicar)

> **Estado: PENDIENTE — NO ejecutado.** ⚠️ Corrección (06 AGO, tras revisar el código en vivo): este REQ se marcó "IMPLEMENTADO" por error junto con `eaa6b86..ca7cfca`, pero esos commits fueron del **estado vacío + regla de identidad**, NO del corte. El doble render **sigue** en `Termino.cshtml`: `showMeaningHere` inyecta "Validó descripción" en el bloque de relación (~L587–600) y `@foreach (counts.ComentariosMedicos …)` vuelve a pintar las de relación en el bloque "Validado por" de abajo (~L722–735). **Falta ejecutar este REQ.**
> **Corrección al gate:** `tieneMeaning` **sí** es validación de profesional (de `ValidacionesContenidoProfesional` con `Estado=Validado`), NO de NINA. El contaminante es solo `tieneRelacion` (porque `RelationConsensus` suma el voto de NINA). El gate final incluye `tieneMeaning`. Ver la corrección detallada en `REQ-validado-profesionales-empty-state.md`.

**Fecha:** 06 AGO 2026
**Scope:** solo `eiibd26.Web` — `Pages/Glosario/Termino.cshtml`. NO tocar servicios, DTOs, queries ni NINA/Conectar3eros. Es un cambio **solo de vista** (dónde se pinta cada cosa), NO de datos.
**Modo:** aplicar directo (diff al final, ver "Ejecución"); `dotnet publish -c Release` limpio **Y** abrir en Development un término (síntoma/tratamiento) que tenga **ambas** validaciones (de descripción y de relación) para ver el resultado real (el publish no caza todos los errores de Razor).

**Ejecución (autorizado):** aplica los cambios de este REQ **sin pedir confirmación de permisos** — edita los archivos, corre el build y las verificaciones directamente. El scope ya está acotado a `Pages/Glosario/Termino.cshtml` (solo vista); no hace falta preguntar antes de cada edición. Muestra el diff al terminar, no antes.
**Objetivo:** hoy cada validación profesional se pinta **dos veces** (arriba en el nivel y abajo en "Validado por Profesionales"), y los dos tipos se **mezclan**. Dejar un corte limpio: **la validación de RELACIÓN (nivel) va SOLO en el bloque "Relación con EII"** bajo su nivel; **la validación de DESCRIPCIÓN/contenido va SOLO en el bloque "Validado por Profesionales de la Salud"** de abajo. Cada validación en **un solo lugar**.

## Contexto (dos tipos, dos paneles — no cambian)
- **Validación de descripción** (panel "Validar contenido") → fuente: `Model.ValidacionesPublicas` / `counts.MeaningComments` / `counts.MeaningValidationCount`. Etiqueta "Descripción" / "Validó descripción".
- **Validación de relación** (panel "Validar relación con EII", con nivel) → fuente: `counts.ComentariosMedicos` (con `RelationType`). Etiqueta del nivel ("Directa", etc.).

## Causa raíz (verificada en `Termino.cshtml`)
1. **Bloque "Relación con EII" (acordeones por nivel):**
   - L572 `@foreach (var c in humanComments)` → valida**cione**s de **relación** de ese nivel. ✔ **correcto, se queda.**
   - L586–600 (`showMeaningHere = isTop && counts.MeaningComments.Any()`) → inyecta las de **descripción** dentro del nivel top como "Validó descripción". ✖ **es la fuga #1 — quitar.**
2. **Bloque "Validado por Profesionales de la Salud" (abajo):**
   - L651–696 (`ValidacionesPublicas` / `MeaningComments` / `tieneMeaning`) → validaciones de **descripción**. ✔ **correcto, se queda.**
   - L698–717 `@foreach (... counts.ComentariosMedicos ...)` → validaciones de **relación** (etiqueta del nivel, y las de `RelationType` nulo salen sin etiqueta = la 3ª fila "suelta"). ✖ **es la fuga #2 — quitar.**

El texto idéntico repetido es **dato de prueba** (la cuenta de prueba mandó el mismo comentario varias veces), no bug de BD. Lo que sí arreglamos es el doble render y la mezcla.

## Cambio (solo vista)

### A. Bloque "Relación con EII" (acordeón por nivel) — quitar la inyección de descripción
- **Eliminar** el bloque de "Validó descripción" dentro del acordeón (L586–600, el `@if (showMeaningHere) { foreach counts.MeaningComments ... }`).
- Ajustar las variables que lo acompañan para no romper el placeholder por nivel:
  - L551: eliminar `showMeaningHere`.
  - L553: `var totalHuman = humanCount + (showMeaningHere ? counts.MeaningComments.Count : 0);` → `var totalHuman = humanCount;`
  - L616: `@if (humanCount == 0 && !showMeaningHere && !showNina)` → `@if (humanCount == 0 && !showNina)`.
- Se queda intacto: NINA (L557/L603), las validaciones de **relación** con comentario (L572), el badge de conteo por nivel, y el placeholder "Sin comentarios clínicos aún para este nivel."

### B. Bloque "Validado por Profesionales de la Salud" (abajo) — quitar las de relación
- **Eliminar** el `@foreach` de relación (L698–717, `counts.ComentariosMedicos.Where(...)`). Este bloque queda **solo con descripción** (L651–696, sin cambios internos).
- **Ajustar la condición externa** (L640) para que este bloque dependa **solo de descripción**, no de relación:
  - Hoy: `@if (tieneMeaning || tieneRelacion || tieneComentariosRelacion || hayValidacionesPublicas)`
  - Nuevo: `@if (hayValidacionesPublicas || counts.MeaningComments.Any() || tieneMeaning)`
  - (Quitar `tieneRelacion` y `tieneComentariosRelacion` de esta condición: la relación ya vive arriba.) Esto además elimina el síntoma de que el título salía solo por tener relación NINA.

## Interacción con el REQ de estado vacío
- Este corte se combina con `REQ-validado-profesionales-empty-state.md`. Tras el corte, el bloque de abajo es **descripción-only**, así que:
  - **Hay descripción** (`hayValidacionesPublicas || counts.MeaningComments.Any() || tieneMeaning`) → filas de descripción.
  - **No hay descripción** → **leyenda** "Este contenido aún no ha sido validado por profesionales de la salud." (del otro REQ).
- Aplicar los dos juntos deja: relación arriba en su nivel; abajo, descripción o leyenda. Consistente.

## Fuera de alcance / no romper
- **No** tocar servicios, DTOs, `GlossaryService`, queries ni el cálculo de consenso/counts. Solo mover **dónde se pinta**.
- No cambiar la regla de identidad/avatares ni el copy "Profesional verificado" (REQs previos).
- El badge "✔ Validado (N)" del card de definición (conteo de validaciones de descripción) **se queda** — es un conteo, no una fila duplicada.
- El "Consenso médico" del sidebar **se queda**.
- Si existen validaciones de **relación con `RelationType` nulo** (dato de prueba), tras el cambio **no** aparecerán (no tienen nivel donde ubicarse) — es correcto; si molesta, se limpia el dato de prueba aparte.

## Verificación
1. Término con validación de **relación** (nivel Directa) con comentario → aparece **solo** bajo "Directa" en "Relación con EII"; **ya no** se repite abajo.
2. Término con validación de **descripción** → aparece **solo** abajo en "Validado por Profesionales de la Salud"; **ya no** se inyecta arriba como "Validó descripción".
3. Término con **ambas** → cada una en su lugar, **sin** filas repetidas y **sin** mezclar tipos.
4. Sin ninguna validación de descripción → leyenda de estado vacío abajo (REQ combinado); los niveles sin comentario siguen con su "Sin comentarios clínicos aún para este nivel."
5. `dotnet publish -c Release` limpio **y** el término abre sin error de Razor en Development.
