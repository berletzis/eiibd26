# REQ — Glosario: leyenda de estado vacío acotada a la descripción (reactivar tras el corte limpio)

**Scope:** solo `eiibd26.Web` → `Pages/Glosario/Termino.cshtml`. Cambio **solo de vista**. No tocar servicios, DTOs, queries ni NINA/Conectar3eros.
**Ejecución (autorizado):** aplica directo, **sin pedir confirmación de permisos**. Edita, corre `dotnet publish -c Release` y abre en Development los casos de verificación. Muestra el diff **al final**.

## Contexto
Tras el corte limpio (relación arriba / descripción abajo), el bloque de abajo "Validado por Profesionales de la Salud" quedó **descripción-only**, y su rama de estado vacío quedó **inalcanzable**: sin validación de descripción el bloque entero desaparece. Resultado no deseado: (a) el glosario perdió el aviso "aún no validado" y (b) quedó **inconsistente** con ingredientes, donde el sello sí muestra su estado vacío.

**Decisión (Berletzis):** mostrar la leyenda cuando no hay validación de descripción, con copy **acotado a la descripción** (para no chocar cuando un profesional sí validó la relación, que se ve arriba).

## Cambio (en `Termino.cshtml`, bloque "Validado por Profesionales de la Salud")
1. **Gate del bloque** — que aparezca también en vacío, siempre que el término tenga descripción que validar:
   ```razor
   @{
       var hayDescripcionValidada = hayValidacionesPublicas || counts.MeaningComments.Any() || tieneMeaning;
   }
   @if (hayDescripcionValidada || Model.Term.DefinicionMedica != null)
   {
       ... (encabezado + cuerpo) ...
   }
   ```
   - **No** gatear con `tieneRelacion` ni `tieneComentariosRelacion` (la relación vive arriba; `tieneRelacion` metería el voto de NINA).
   - Si el término **no** tiene `DefinicionMedica` y **no** tiene validación de descripción → el bloque sigue oculto (no hay descripción que validar).

   **Refinamiento (aprobado):** gatear por **contenido**, no solo por objeto, para no mostrar la leyenda cuando la descripción existe como registro pero está **vacía** (`DescripcionIA` en blanco → el card dice "Definición pendiente de generar"). Gate final:
   ```razor
   @if (hayDescripcionValidada
        || (Model.Term.DefinicionMedica != null
            && !string.IsNullOrWhiteSpace(Model.Term.DefinicionMedica.DescripcionIA)))
   ```
   Así la leyenda "aún no ha sido validada" solo sale cuando hay descripción **generada** que validar; con descripción pendiente de generar, el bloque queda oculto.

2. **Dentro del bloque:**
   - `@if (hayDescripcionValidada)` → las filas de descripción actuales (`ValidacionesPublicas` / `MeaningComments`), **sin cambios**.
   - `else` → **leyenda acotada** (mantener el encabezado neutro en vacío: ícono outline gris + "Validación de profesionales de la salud"):
     > **La descripción de este término aún no ha sido validada por un profesional.**

3. Reusar el bloque de estado vacío que Claude Code dejó en su lugar; solo cambiar el gate para hacerlo alcanzable y ajustar el copy a la variante acotada.

## Divergencia intencional de copy (documentar)
- **Glosario** (dos ejes: descripción + relación): copy acotado — "La descripción de este término aún no ha sido validada por un profesional."
- **Ingredientes** (`_NotaSello`, un solo eje): se queda con "Este contenido aún no ha sido validado por profesionales de la salud."
- Ya **no** son byte-idénticos, y es a propósito: en el glosario el copy genérico chocaría con una validación de relación visible arriba. La consistencia ahora es de **comportamiento** (ambos muestran su estado vacío), no de texto literal.

## No romper
- El corte limpio (relación solo arriba, descripción solo abajo) se mantiene.
- Regla de identidad/avatares, "✔ Validado (N)" del card de definición y "Consenso médico" del sidebar: intactos.

## Verificación (Development + `publish -c Release` exit 0)
1. Término con **definición pero sin ninguna validación** → bloque abajo con **encabezado neutro + leyenda acotada**. (Antes: nada.)
2. Término **validado solo en la relación** (profesional validó nivel, no descripción) → relación con su nombre **arriba** + abajo la **leyenda acotada** ("la descripción aún no…"), sin contradicción.
3. Término con **validación de descripción** → filas de descripción abajo, **sin** leyenda.
4. Término **sin definición y sin validación** → bloque oculto.
5. Término con **definición pendiente de generar** (`DescripcionIA` vacío) y sin validación → bloque oculto (no anuncia "aún no validada" sobre algo que no existe).
6. Consistencia de comportamiento con ingredientes (ambos muestran estado vacío).
