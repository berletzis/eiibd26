# Requerimiento — Notas clínicas F2: gestión admin (publicar) + validación médica (señal)

**Fecha:** 14 JUL 2026
**Depende de:** F1 (tablas `PlatNotaClinica/Seccion/Referencia` + candado en lectura) — ya hecho.

---

## 0. Decisiones cerradas (no re-abrir)

1. **Publicar lo decide el ADMINISTRADOR**, con un toggle tipo "Registro activo" (como en Síntomas). **Ese es el candado.** No hay consenso para publicar.
2. **La validación médica es una SEÑAL de confianza, aparte** — NO el interruptor de publicación. Se recoge en la **vista pública** del ingrediente, en un card visible solo para médicos/admin (espejo del glosario).
3. **No se necesita rol nuevo.** El admin publica (rol Administrador ya existe); el médico valida como señal (rol Medico ya existe). Nutriólogo queda para después sin rehacer nada.
4. **Reset-al-editar:** si se edita el contenido de una nota **publicada**, vuelve a **borrador** hasta que el admin la re-publique. Va en el **backend**, no en la vista.

## 1. Analizar PRIMERO (leer del repo, reportar antes de construir)

Claude Code debe leer y replicar estos patrones — **no inventar**:

- **Grid + panel de edición:** `Areas/Identity/Pages/Admin/Sintomas/Index.cshtml` y `.cshtml.cs`. Fíjate en: DataTables (`#sintomasGrid`, `OnGetGridDataAsync`), el **side-panel** (`.side-panel #panelEditarSintoma`, se abre al "Editar" vía `OnGetGetSintomaAsync`), el toggle **"Registro activo"** (~línea 470), y `OnPostEditarSintomaAsync`.
- **Validación médica:** `Pages/Glosario/Termino.cshtml` (card `Validar contenido`, ~línea 1038-1085; consenso "Validado por Profesionales de la Salud"), el partial `_ValidacionContenidoMedico`, y `Services/Validacion/IValidacionContenidoService`.

**Reportar antes de tocar:** ¿el `IValidacionContenidoService` se puede **reusar/generalizar** para las notas de platillos, o conviene una tabla paralela `PlatNotaValidacion`? El servicio del glosario se llavea a `ContenidoId`/`GlossaryTerm`; las notas son otra entidad. **No lo asumas — analízalo y propón.**

## 2. Parte A — Catálogo admin (editar + publicar la nota)

**Dónde:** las notas viven en **ingredientes y grupos**, así que el editor de nota + el toggle de publicar van en el panel de edición de:
- `Areas/Identity/Pages/Admin/Platillos/Ingredientes`
- `Areas/Identity/Pages/Admin/Platillos/Grupos`

**Cómo:** reusar el patrón de `Admin/Sintomas/Index` — grid DataTables + **side-panel** de edición. Las pantallas de Ingredientes/Grupos que ya existen se llevan a ese patrón.

**En el side-panel, además de los campos que ya tienen** (nombre, grupo, atributos…), agregar la edición de su **nota clínica**:
- Las **secciones** (título + contenido, ordenables) — de `PlatNotaSeccion`.
- La **bibliografía** (título + URL) — de `PlatNotaReferencia`.
- El toggle **"Publicado (visible para pacientes)"** — es la columna del candado (§4). Solo el admin lo mueve.
- **Guard:** no se puede publicar una nota **sin al menos una sección con contenido** (mismo criterio que ya está en el servicio de lectura).

**En el grid:** columna de **estado de la nota** (Sin nota / Borrador / Publicada) para ver de un vistazo qué falta.

*(Opcional, consistencia: llevar también `Admin/Platillos/Index` —el catálogo de platillos— al mismo patrón visual de grid. No lleva notas; es solo homogeneidad. Puede ir aparte.)*

## 3. Parte B — Validación médica en la vista pública (señal de confianza)

**Dónde:** en el sidebar de `/Platillos/Ingrediente/{slug}` (y de la vista de grupo si existe), un card **solo visible para médicos/admin** (`@if (CanValidate)`, como el glosario).

**Qué hace:** el médico lee la nota **en su contexto real** (la ve igual que el paciente) y la valida, con comentario clínico opcional — espejo del card "Validar contenido" del glosario (segunda imagen de referencia). Se acumula el consenso: "N profesionales de la salud validaron esta nota".

**Regla dura:** la validación médica **NO publica ni despublica**. Es una señal que se muestra junto a la nota. **Solo el toggle del admin (§2) controla la visibilidad al paciente.** Son dos ejes independientes:
- **Publicado** (admin) → decide si el paciente la ve.
- **Validada por N médicos** (consenso) → señal de confianza, informativa.

**Mostrar el consenso al paciente** (cuando la nota esté publicada): un sello discreto tipo "Validado por profesionales de la salud" si tiene validaciones, igual que el glosario. Si no tiene, no se muestra nada.

## 4. El candado, actualizado

- La columna de F1 (`RevisadaPorMedico`) se **renombra conceptualmente a `Publicado`** (o se agrega `Publicado` y se retira la vieja — Claude Code propone lo más limpio).
- El servicio de lectura (`PlatNotaClinicaService`) ya filtra por ese flag + activa + con contenido. Solo cambia el nombre del flag; el candado en un solo lugar **se mantiene**.
- Las 23 notas siguen en **no publicado** hasta que el admin las publique una por una.

## 5. Criterios de aceptación

1. Desde el catálogo admin (Ingredientes/Grupos, estilo Síntomas) se edita la nota —secciones + bibliografía— y se publica con el toggle.
2. Publicar una nota **sin secciones con contenido** se rechaza.
3. Editar el contenido de una nota publicada la **regresa a borrador** y desaparece de la vista del paciente (backend, no vista).
4. En la vista pública, el card de validación médica **solo lo ven médicos/admin**; validar **no** cambia la visibilidad al paciente.
5. Una nota publicada **con** validaciones muestra el sello de consenso al paciente; **sin** validaciones no muestra nada.
6. El candado sigue en un solo lugar (grep: nadie lee las notas fuera del servicio).
7. Aislamiento §0 intacto: solo `Plat*` + `idUsuario`.

## 6. Fases sugeridas (build limpio entre cada una)

- **F2a:** catálogo admin (editar nota + toggle Publicado) estilo Síntomas. Renombrar el flag. Es lo que desbloquea que las notas salgan.
- **F2b:** card de validación médica en la vista pública + sello de consenso.
