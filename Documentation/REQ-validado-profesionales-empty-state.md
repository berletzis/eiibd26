# REQ — "Validado por Profesionales de la Salud": estado vacío con leyenda (síntomas, tratamientos e ingredientes)

**Fecha:** 06 AGO 2026
**Scope:** solo `eiibd26.Web` — `Pages/Glosario/Termino.cshtml` y `Pages/Platillos/_NotaSello.cshtml`. NO tocar NINA ni Conectar3eros.
**Modo:** diff antes de aplicar; `dotnet publish -c Release` limpio **Y** abrir en Development (el publish no caza todos los errores de Razor):
 1. un término de glosario (síntoma/tratamiento) **sin** validaciones de profesionales, y
 2. una nota de ingrediente/grupo **sin** validaciones.
**Objetivo:** cuando **no hay ninguna validación de profesional de la salud**, el bloque "Validado por Profesionales de la Salud" debe mostrar una **leyenda de estado vacío** en vez de una caja en blanco (glosario) o de desaparecer sin decir nada (ingredientes). Debe verse **igual y consistente** en síntomas, tratamientos e ingredientes.

## Causa raíz (verificada en código) — CORREGIDA
> **Corrección (Berletzis, post-implementación):** la versión inicial de este REQ decía que `tieneMeaning` es "de NINA". **Es incorrecto.** `tieneMeaning` = `counts.MeaningValidationCount > 0`, que sale de **`ValidacionesContenidoProfesional` con `Estado=Validado`** → **sí es validación de profesional** (de descripción). El único que **contamina** es **`tieneRelacion`**, porque `RelationConsensus` **suma el voto de NINA**. Con la definición literal errónea, un término con validaciones de descripción contadas habría dicho "aún no validado". El gate final **incluye `tieneMeaning`** como señal válida.

- **`Termino.cshtml` (~L640):** el bloque se pinta con
  `@if (tieneMeaning || tieneRelacion || tieneComentariosRelacion || hayValidacionesPublicas)`.
  El problema es **`tieneRelacion`**: proviene de `RelationConsensus`, que incluye el voto de **NINA**, así que se activa aunque no haya validación de profesional. Entonces con relación NINA (ej. "Directa · NINA") pero **cero** validaciones de profesional, el título **sí** sale pero **el contenido queda vacío** → la caja en blanco de la captura. `tieneMeaning` **NO** es el culpable: es validación de profesional legítima y debe **contar** como "sí hay".
- **`_NotaSello.cshtml` (L9):** `@if (Model != null && Model.Any())` — si no hay validaciones, el partial **no pinta nada** (ni título ni leyenda). No hay estado vacío.

## Concepto de "hay validación de profesional" (usar esta definición en ambos archivos)
- **Glosario (`Termino.cshtml`):** `hayValidacionProfesional = hayValidacionesPublicas || counts.MeaningComments.Any() || tieneMeaning;`
  (es decir: hay validación de **descripción** — pública o contada. **NO** gatear con `tieneRelacion`/`tieneComentariosRelacion`: la relación vive arriba, en su nivel, tras el corte limpio.)
- **Ingredientes (`_NotaSello.cshtml`):** `hayValidacionProfesional = Model != null && Model.Any();`

## Cambio

### 1. `Pages/Glosario/Termino.cshtml`
- El bloque "Validado por Profesionales de la Salud" **debe seguir apareciendo** cuando aplique hoy (no cambiar la condición externa que ya lo muestra con la relación NINA).
- **Dentro** del bloque, envolver el render de validaciones así:
  - `@if (hayValidacionProfesional)` → lo que ya hace hoy (las filas de `ValidacionesPublicas` y de `ComentariosMedicos`), **sin cambios**.
  - `else` → pintar **la leyenda de estado vacío** (una sola fila discreta, no una caja en blanco):
    `Este contenido aún no ha sido validado por profesionales de la salud.`
- No cambiar la regla de identidad ni los avatares (eso ya quedó en REQs previos).

### 2. `Pages/Platillos/_NotaSello.cshtml`
- Que el bloque (título "Validado por Profesionales de la Salud") **se pinte también cuando `Model` está vacío**, mostrando la **misma leyenda**:
  `Este contenido aún no ha sido validado por profesionales de la salud.`
- Cuando **sí** hay validaciones → como hoy (bloques `conNombre` / `anonimosConComentario` / resumen de `anonimosSinComentario`), sin cambios.
- **Nota:** confirmar que el partial se sigue invocando aunque no haya notas (si el `Page` padre solo lo incluye cuando existe una nota, ajustar esa inclusión para que el estado vacío pueda mostrarse). Si el padre nunca llama al partial sin nota, dejar constancia en el diff y decidirlo juntos antes de aplicar.

## Copy (ajustable)
- Texto propuesto: **"Este contenido aún no ha sido validado por profesionales de la salud."**
- Alternativas si se quiere más corto: "Aún sin validación de profesionales de la salud." / "Todavía no hay validaciones de profesionales de la salud."
- Estilo: texto atenuado (mismo `text-muted` / clase de "Sin comentarios clínicos aún" que ya se usa en los niveles de relación), **sin** ícono de check verde (el check es para lo validado, no para el vacío).

## Regla (no romper)
- El estado vacío es **informativo**, no un sello: nada de check verde ni de dar a entender que ya está validado.
- Mismo texto y mismo estilo en **glosario (síntomas + tratamientos)** e **ingredientes** → consistencia total.
- No tocar la lógica de identidad/avatares/comentarios ya definida en REQs anteriores.

## Verificación
1. Término (síntoma/tratamiento) **con relación NINA pero sin validaciones de profesional** → sale el título + **leyenda** (no caja en blanco).
2. Término **con** validaciones de profesional → filas normales, **sin** leyenda.
3. Nota de ingrediente/grupo **sin** validaciones → sale título + **leyenda** (antes no salía nada).
4. Nota **con** validaciones → como hoy, sin leyenda.
5. La leyenda se ve **idéntica** en los tres contextos (síntomas, tratamientos, ingredientes).
6. `dotnet publish -c Release` limpio **y** ambas páginas abren sin error de Razor en Development.
