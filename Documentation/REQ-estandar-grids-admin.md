# Requerimiento — Estándar de grids admin (componente compartido + migrar Platillos)

**Fecha:** 14 JUL 2026
**Objetivo:** que los catálogos de Platillos usen el mismo estándar de grid + edición que Síntomas/Directorio Médicos, **extrayendo un componente reusable** en el camino (no copiar-pegar). Las 11 páginas establecidas NO se tocan; se migran oportunistamente después.

> ## ⚠️ OVERRIDE (14 JUL, decisión del owner) — edición en página aparte, NO panel lateral
>
> La edición y el alta de **todos** los catálogos (Unidades, Categorías, Grupos, Atributos, Ingredientes) y de Platillos van en una **página completa aparte** (patrón `Platillos/Detalle`), **no** en panel lateral / slide-over.
>
> **Qué cambia respecto a lo de abajo:**
> - `admin-grid.js` se queda con **DataTables client-side + `stateSave`** y **se le quita el shell del panel lateral**. `stateSave` ahora importa MÁS: al volver de la página de edición, la grilla restaura búsqueda/orden/página.
> - Cada catálogo = **`Index` (grid con "Nuevo"/"Editar" que NAVEGAN)** + una **página de edición full-page** server-rendered, PRG, `?handler=`.
> - **Unidades (F0):** ajustar de panel → página de edición aparte.
> - **Ingredientes:** su página de edición full-page **lleva la nota clínica + toggle Publicado (F2a)** — ahora encaja natural. El §7.2 (referencias inactivas) se calcula server-side al cargar la página.
> - Se resuelve la tensión del §2 (dos arquitecturas de panel): ya no hay panel. Todo es grid → página.
> - Siguen firmes: sin Controllers (`?handler=`), tokens `eii-*`, sin migraciones EF, no tocar las 11 páginas.
>
> **Ampliación (mismo día):**
> - **"Nuevo" también es página aparte** — ningún formulario arriba del grid, ni para alta ni para edición. Nuevo y Editar son ambas vistas full-page (puede ser la misma página: con `id` = editar, sin `id` = alta).
> - **Los enlaces "Nuevo" y "Editar" abren en pestaña nueva** (`target="_blank" rel="noopener"`). El grid queda como "banco de trabajo" fijo.
> - **Tras guardar (PRG)** en la pestaña de edición: se queda en esa página con mensaje de éxito + enlace "Volver al listado". **No** intentar auto-cerrar la pestaña (los navegadores lo bloquean si no la abrió un script).
> - **Consecuencia a aceptar:** la pestaña del grid **no se auto-refresca** tras guardar en la otra pestaña. Mitigación **opcional** (decide el owner): recargar el grid cuando su pestaña recupera el foco (`visibilitychange`/`focus`). Si no se hace, el admin recarga a mano.
> - `stateSave` se mantiene (sigue útil aunque el grid ya no navegue).
>
> El resto del documento aplica salvo donde diga "panel lateral" — eso queda superado por este override.
>
> ### Nota clínica en página completa — bloque de contexto OBLIGATORIO
> Cuando la nota clínica pase a página completa (fase Ingredientes), la página **debe encabezarse con el contexto de a qué pertenece la nota**, para que el editor nunca se pierda:
> - **Título inequívoco:** "Nota clínica de: **Queso**" (con etiqueta ingrediente) o "Nota clínica de: **Lácteos**" (con etiqueta grupo).
> - **Ficha del destino:**
>   - Ingrediente → su **grupo** + **atributos intrínsecos** + enlace a su página pública (`/Platillos/Ingrediente/{slug}`) para verla como el paciente.
>   - Grupo → los **ingredientes que pertenecen al grupo** (para que el editor vea el alcance: una nota de "lácteos" aplica a queso, leche, yogur…).
> - **Estado:** badge Publicado/Borrador + conteo de validaciones médicas.
> - Todo esto **arriba**, antes del editor de secciones + bibliografía.
> - Objetivo: abrir la nota nunca es un formulario huérfano — el editor ve de inmediato **qué** anota y **para quién** aplica.
>
> **Resolución de dos trampas (decididas):**
> - **Enlace "verla como el paciente":**
>   - Ingrediente **activo** → mostrar el enlace SIEMPRE, con **leyenda honesta según estado**: si la nota está en borrador, "Así ve el paciente esta página hoy — tu nota aparecerá cuando la publiques." (El editor necesita ver la página igual; una leyenda explica mejor que un enlace ausente.)
>   - Ingrediente **inactivo** → **sin enlace** (la página pública da 404), con nota: "El ingrediente está inactivo; no tiene página pública hasta reactivarlo desde el listado." Nunca mandar a un 404.
> - **Nota de grupo:** un grupo NO tiene página pública propia — su nota sale en la página de cada ingrediente del grupo. Por eso **no hay un botón único** "verla como el paciente"; el camino es la **lista de ingredientes del grupo, cada uno enlazando a su página** (que es justo el bloque de contexto ya pedido). Documentar esto para que no se busque un botón que no existe.
>
> ### Platillos/Index = OPCIÓN B (15 JUL, decisión del owner) — solo traje visual
> `Platillos/Index` **conserva su motor actual server-side** (búsqueda Q, filtro por categoría con §7.2 incluyendo la categoría inactiva, "Mostrar inactivos", paginación server, guard de `ToggleActivo` que impide publicar un platillo sin ingredientes). **NO** se migra a DataTables client-side ni se toca la query.
> Solo se aplica el traje visual de la familia: card `eii-*`, "Nuevo"/"Editar" → `Platillos/Detalle` en `target="_blank" rel="noopener"`, búsqueda/combo/paginador estilizados con componentes `eii-*`, y la píldora "Actualizar" vía `data-eii-grid-notify="platillo"` emitida desde `Detalle` dentro de `@if(SuccessMessage)`.
> Razón: el motor ya jala y no está mal hecho — no se reinventa (MVP, deuda técnica consciente OK). El componente `admin-grid.js` ya soporta el modo "solo píldora + notify" sin forzar su DataTables.

---

## 0. Convenciones del repo (mandan sobre el skill)

- **Razor Pages con PageModels**, no Controllers.
- **Sin migraciones EF.** Aquí no hay cambio de esquema (es puro presentación) — pero que quede claro.
- **Tokens `eii-*`.** El CSS inline que hoy vive en `Sintomas/Index.cshtml` se extrae a un archivo compartido tokenizado.
- Reusar los servicios/handlers CRUD que **ya existen** en cada catálogo de Platillos. Solo cambia la capa de grid + edición, no la lógica.

## 1. Refinamiento de senior: client-side, NO server-side

En mi plan mencioné un helper C# de DataTables *server-side*. Al mirar los datos, **lo retiro**: los catálogos de Platillos son minúsculos — Grupos 18, Unidades 12, Atributos 11, Categorías 8, Ingredientes 57, Platillos 17. Para ≤57 filas, el procesamiento server-side (protocolo `draw/start/length/search/order`) es **sobre-ingeniería**.

**Decisión:** DataTables **client-side** (`serverSide: false`) sobre la tabla ya renderizada por Razor. DataTables hace búsqueda/orden/paginación en el navegador sobre las pocas filas. Cero endpoint de grid-data nuevo, cero protocolo. Misma apariencia y comportamiento que Síntomas para el usuario (la diferencia client/server es invisible).

El componente compartido **soporta ambos modos** (flag de config): client-side por defecto (Platillos), server-side disponible para cuando una tabla grande adopte el componente. El helper C# server-side **no se construye ahora** (YAGNI); queda documentado como punto de extensión.

## 2. Analizar PRIMERO (leer, reportar antes de construir)

Leer completos y extraer lo reusable vs lo por-página:
- `Areas/Identity/Pages/Admin/Sintomas/Index.cshtml` (+ `.cshtml.cs`) — el `<style>` inline del `.side-panel` (~L33-250), el `@section Scripts` con el init de DataTables y el open/close/edit/delete del panel, y los handlers `OnGetGridData`/`OnGetGetSintoma`/`OnPostEditar`/`OnPostEliminar`.
- `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml` — combina DataTables + `.side-panel` + `eii-*`; es el ejemplo más cercano al estándar objetivo.
- El panel lateral que ya existe en Platillos (F2a): `_NotaClinicaPanel.cshtml`, `admin-nota-panel.css`, `admin-nota-panel.js` — **consolidar con el componente nuevo** en vez de dejar dos implementaciones de panel.

**Reportar el API del componente antes de construir:** qué config recibe una página (id del grid, selector de columnas orderables, url del handler de edición, campos del panel), y qué queda como markup por-página.

## 3. El componente compartido (construir una vez)

1. **`wwwroot/js/admin-grid.js`** — módulo parametrizable:
   - Init de DataTables (client-side por defecto; opción server-side) con español (i18n), sin re-declarar en cada página.
   - Lógica del **panel lateral**: abrir al "Editar" (carga la fila — por `data-*` embebido o un `OnGetGet` chico), cerrar, y refrescar el grid tras guardar/eliminar.
   - Config por página vía `data-*` en el contenedor o un objeto init: `{ gridId, panelId, editUrl, ... }`.
2. **`wwwroot/css/admin-grid.css`** — el `.side-panel`, el grid y el slide-over, **tokenizado `eii-*`** (hoy inline en Síntomas). Absorbe lo de `admin-nota-panel.css`.
3. **Un partial de andamiaje** opcional (`_AdminGridPanel.cshtml`) para el esqueleto del panel lateral, si reduce repetición.

## 4. Migración por catálogo (fase por fase)

**Alcance:** los 6 grids de Platillos. `Detalle.cshtml` (captura de platillo con renglones de ingredientes) **se queda como formulario de página completa** — es una entidad compleja, no un catálogo de panel lateral; solo se alinea visualmente si hace falta.

Para cada catálogo:
- Renderizar la tabla completa con Razor (ya casi está) e **inicializar DataTables client-side** sobre ella con `admin-grid.js` → búsqueda/orden/paginación.
- Edición en **panel lateral** (estándar Síntomas), reusando el handler CRUD que ya existe. En **Ingredientes**, el panel debe **conservar** la edición de la nota clínica + toggle Publicado de F2a — no perder eso al migrar.
- `Platillos/Index`: grid DataTables, pero su "Editar/Nuevo" **navega a `Detalle`** (no panel), porque el platillo es complejo.
- Baja lógica (`Activo`/`Eliminado`) y el manejo §7.2 de referencias inactivas: **preservarlos**.

## 5. Fases (build limpio + prueba en vivo entre cada una)

- **F0 — Componente compartido + prueba con Unidades** (el más simple). Construir `admin-grid.js/.css`, migrar Unidades, validar en vivo que el grid (búsqueda/orden/paginación) y el panel de edición funcionan idénticos a Síntomas.
- **F1 — Resto de catálogos**, uno por uno con build+verificación entre cada: Categorías → Grupos → Atributos → **Ingredientes** (el complejo, con nota clínica) → Platillos/Index.
- Cada fase: diff para revisar antes de aplicar; prueba en vivo (es UI — el compilador no la ve).

## 6. Criterios de aceptación

1. Existe **un** `admin-grid.js` + `admin-grid.css` compartidos; los catálogos de Platillos los **consumen** (no copian el init).
2. Cada catálogo de Platillos: grid con búsqueda/orden/paginación (DataTables) + edición en panel lateral, visual y funcionalmente como Síntomas/Directorio.
3. `Ingredientes` conserva la edición de nota clínica + toggle Publicado (F2a intacto).
4. Cero CSS/JS inline nuevo duplicado; lo de Síntomas no se copió — se extrajo.
5. Las 11 páginas establecidas **no se tocaron**.
6. Sin cambio de esquema, sin migraciones EF. Servicios CRUD reusados.
7. Baja lógica y §7.2 (referencias inactivas) preservados.

## 7. Fuera de alcance (explícito)

- Helper C# server-side (los catálogos son pequeños; client-side basta).
- Refactor de las 11 páginas establecidas (oportunista, después).
- `Platillos/Detalle` como panel lateral (se queda como formulario de página).
