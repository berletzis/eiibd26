# REQ — Referencias: limpiar al regenerar + marcar las manuales fuera de lista blanca

**Fecha:** 21 JUL 2026
**Archivo:** editor de nota clínica (`Admin/Platillos/NotaClinicaDetalle` + su JS/CSS y, para el marcado, pasar la lista blanca al modelo). Diff antes de aplicar. Solo presentación/UX salvo el paso de la config al modelo (rebuild por el `.cs`).
**Origen:** una referencia fuera de la lista blanca ("Listeria / PMC…") apareció en el editor. NO fue fuga del candado (el filtro de la IA la habría descartado) — es una referencia **manual o heredada** de un borrador anterior. El candado protege a la IA, no al editor humano. Esto cierra el hueco de *percepción* y de arrastre.

## Contexto verificado (no rehacer)
- `FiltrarPorListaBlanca` en `PlatillosAiService` **funciona**: descarta toda fuente citada por la IA que no matchee la lista (`FuentesClinicasPermitidas` = ESPEN 2023, Crohn's & Colitis Foundation), marca `RevisionPrioritaria` y loguea. **No tocar.**
- Las referencias **manuales** del editor NO pasan por ese filtro, a propósito (el humano puede agregar una fuente real que no esté en la lista). Eso se mantiene.

## Cambio 1 — Limpiar referencias al regenerar con IA
Hoy "Generar borrador con IA" rellena las **secciones** pero puede dejar **referencias viejas** de un estado anterior (así apareció la de Listeria). Al regenerar (después del confirm de sobrescritura que ya existe):
- **Reemplazar TODAS las referencias** por las que devuelve la IA (que ya vienen filtradas por la lista blanca). Nada de mezclar con las anteriores.
- Consecuencia asumida y correcta: si el editor había agregado una referencia manual y regenera, se va — porque regenerar = borrador nuevo, y ya lo confirmó. El editor la vuelve a agregar si la quiere.

## Cambio 2 — Marcar en ámbar las referencias fuera de la lista blanca
En el editor, cada fila de referencia cuyo texto **no matchee** la lista blanca (misma lógica tolerante que `FiltrarPorListaBlanca`: contains / contained-by, sin acentos/mayúsculas) muestra una **marca ámbar** discreta con hint: *"Fuera de la lista blanca aprobada — verifica que sea una fuente real y que respalde lo que dice la nota."*
- **NO bloquea** — el editor puede dejarla si es legítima. Solo lo hace revisar con más ojo.
- Se re-evalúa al escribir/pegar en el campo (cliente) y al cargar.
- Requiere **pasar la lista blanca al modelo/vista** (hoy vive solo en config para el servicio). Exponerla read-only al PageModel del editor.

## Impacto sobre notas ya generadas (IMPORTANTE — leer)
Todas las notas de ingredientes/grupos **ya fueron generadas** con el servicio de IA. Ninguno de los dos cambios es una migración ni corre sobre datos existentes:
- **Cambio 1 es solo hacia adelante:** se dispara únicamente cuando alguien aprieta "regenerar" en una nota concreta (con el confirm ya existente). NO recorre ni reescribe las notas ya guardadas. Se quedan igual hasta que alguien las regenere a propósito.
- **Cambio 2 no modifica datos:** solo pinta la marca al **leer** la nota. No borra ni cambia nada; lee `FuentesClinicasPermitidas` en vivo (ya incluye Mayo Clinic, My Crohn's and Colitis Team, Crohn's & Colitis Foundation, ESPEN).
- **Beneficio extra:** como todo el corpus ya existe, el Cambio 2 funciona como **lente de auditoría** — permite ver de un vistazo, nota por nota, si quedó alguna referencia fuera de lista (una "Listeria/PMC" heredada) sin auditar a mano.
- **Único filo asumido:** si una nota tiene una referencia **manual** valiosa y se **regenera** en el futuro, el Cambio 1 la borra (regenerar = borrador nuevo, ya confirmado). Como se generaron con el servicio de IA (referencias ya filtradas), no debería haber manuales que perder.

## Fuera de alcance
- No cambiar el filtro de la IA (ya funciona).
- No bloquear referencias manuales (el humano manda; solo se marcan).
- No hacer migración ni batch sobre notas existentes — los cambios son a nivel de comportamiento (regenerar / leer), no de datos.

## Verificación
- Regenerar con IA sobre una nota que tenía una referencia manual → esa referencia desaparece; quedan solo las de la IA (filtradas).
- Escribir una referencia que no esté en la lista blanca → aparece la marca ámbar; una de la lista (ESPEN/CCF) → sin marca.
- La marca no impide guardar ni publicar.
- Diff antes de aplicar; rebuild solo por el paso de la config al modelo.
