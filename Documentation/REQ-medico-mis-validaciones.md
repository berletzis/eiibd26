# REQ — Panel médico: "Mis Validaciones" (facilitar el ABC + historial + TOP 10)

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Problema:** el dashboard del médico solo muestra badges/niveles bloqueados; **no dice "empieza a validar aquí"**. Queremos facilitarles el arranque (sin pretextos), darles su **historial de validaciones con lo que escribieron** (para no ir a buscar al sitio), y un **TOP 10** de términos a validar con acceso directo y el estado de su validación.

## Lo que YA existe (reusar, NO rehacer)
- **Listar validaciones de CONTENIDO por médico:** `ValidacionContenidoService.ObtenerValidacionesMedicoAsync(string userId)` → `List<ValidacionAdminDto>` (TipoContenido, ContenidoId, ContenidoTitulo, ContenidoUrl `/Termino/{slug}`, Comentario, Estado, CreadoEn).
- **¿Validé el contenido de un término?** `ObtenerMiValidacionAsync(TipoContenidoValidado.Termino, terminoId, userId)` → null o `ValidacionExistenteDto`.
- **Ranking TOP para el card:** `GlossaryService.GetTopTermsByQualityAsync(GlossaryTermType tipo, int limit, ct)` → `List<GlossaryTermSummaryDto>` (Id, Nombre, **Slug**, UserRelationCount, RelationDirect/Indirect/SecondaryCount, RelationTotalCount). El score interno = `3*directo + 2*indirecto + 1*secundario + userCount`.
- Entidades: `ValidacionesContenidoProfesional`, `GlossaryValidation` (relación), `GlossaryTerm` (tiene `Slug` persistido y `TipoTermino`: Sintoma/Tratamiento).
- Sidebar médico en **ambas** versiones: `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` L426-466 y `Pages/Shared/_SidebarMenu.cshtml` L422-459 (ítems: Mi Dashboard, Mis P&R, Ver mi perfil público).

## Lo que HAY QUE CREAR
### 1. Métodos nuevos en `IGlossaryService`/`GlossaryService`
- **`ObtenerValidacionesRelacionMedicoAsync(string userId)`** → lista las `GlossaryValidations` del usuario con `ValidationType == RelationValidation`, incluyendo `GlossaryTermId`, nombre + slug del término, `MedicalRelationTypeId` (Directa/Indirecta/Secundaria), `Comment`, `CreatedAt`. (Consulta directa `_db.GlossaryValidations.Where(v => v.UserId == userId && v.ValidationType == RelationValidation)` con join a `GlossaryTerm`.)
- **`ObtenerTerminosConRelacionValidadaAsync(string userId)`** (o que el método anterior sirva para armar el set) → set de `GlossaryTermId` que el médico ya validó a nivel relación, para marcar el TOP 10 sin N+1.

### 2. Página `Areas/Identity/Pages/Medico/MisValidaciones.cshtml(.cs)`
- PageModel namespace `eiibd26.Areas.Identity.Pages.Medico`, **`[Authorize(Roles = "Medico,Administrador")]`** (NO MedicoPendiente — un pendiente aún no puede validar). Inyecta `IValidacionContenidoService` + `IGlossaryService`. userId como **string** (`User.FindFirstValue(ClaimTypes.NameIdentifier)`). Espejar estructura de `MedicoPreguntasRespuestas.cshtml.cs`.
- **Layout full-width** (usar el estándar de cards que llenan el ancho — NO caer en el encogido de flex; ver fix `pqr-header-card`).

Secciones de la página (en orden):

**A. Intro "Cómo validar" (el ABC, breve).** Un card corto que quite el pretexto:
> "Validar toma un minuto. 1) Elige un término de la lista de abajo (o búscalo). 2) Lee su descripción y su relación con EII. 3) Agrega tu validación de contenido y/o del nivel de relación, con tu comentario clínico."

**B. Card "TOP 10 — términos para validar".**
- Fuente: `GetTopTermsByQualityAsync(Sintoma, 10)` + `GetTopTermsByQualityAsync(Tratamiento, 10)`, **merge**, recomputar el score desde el DTO (`3*directo+2*indirecto+1*secundario+userCount`), ordenar desc, tomar 10. Mostrar tag por fila (Síntoma / Tratamiento).
- Cada **fila**: nombre del término · un **link** "Ir al término" → `/Termino/{Slug}` (ej. `/Termino/diarrea`) · y **dos indicadores de estado del propio médico**:
  - **Contenido:** ✓ validado / ○ pendiente.
  - **Relación:** ✓ validado / ○ pendiente.
- Batch (sin N+1): armar dos sets de `terminoId` del médico — contenido (de `ObtenerValidacionesMedicoAsync` filtrando `TipoContenido == Termino`, `ContenidoId` = terminoId) y relación (del método nuevo) — y marcar cada fila contra los sets.

**C. "Mis validaciones" (historial con lo que escribieron).** Para que no vayan a buscar al sitio:
- **Contenido:** `ObtenerValidacionesMedicoAsync(userId)` → por cada una: título + link (`ContenidoUrl`) + **su comentario** + tipo + fecha + estado.
- **Relación (glosario):** el método nuevo → por cada una: término + link `/Termino/{slug}` + **nivel** (Directa/Indirecta/Secundaria) + **su comentario** + fecha.
- Presentar como dos listas claras (o agrupado por término si es glosario), siempre mostrando el comentario que escribió y el link para volver.

### 3. Ítem de sidebar en LAS DOS versiones
- Insertar un `<li>` **"Mis Validaciones"** tras "Mis P&R", en **ambos** `_SidebarMenu.cshtml` (A ~L444-452, B ~L438-446), apuntando a `/Identity/Medico/MisValidaciones` (icono p.ej. `bi-patch-check`). Mantener el bloque dentro del `@if (User.IsInRole("Medico"))` existente.

### 4. Entrada desde el Dashboard (facilitar el arranque)
- En `Areas/Identity/Pages/Medico/Dashboard.cshtml`, agregar un CTA visible **"Empieza a validar"** que lleve a `/Identity/Medico/MisValidaciones`. Que sea lo primero que vean para arrancar, no solo los badges bloqueados.

## Fuera de alcance
- No cambiar la lógica de validación ni el candado de identidad/badges.
- No tocar el ranking `GetTopTermsByQualityAsync` (reusar tal cual).
- No mostrar esta sección a `MedicoPendiente` (no puede validar aún).

## Verificación
1. El médico ve **"Mis Validaciones"** en su sidebar (paneles reales, Versión A) y llega a la página.
2. **TOP 10:** muestra 10 términos (síntomas + tratamientos) rankeados, cada uno con link a `/Termino/{slug}` y los dos indicadores (contenido / relación) reflejando lo que el médico YA validó.
3. Al validar un término (contenido o relación) y volver, el indicador correspondiente pasa a ✓.
4. **Mis validaciones:** lista las de contenido y las de relación, cada una con **el comentario que escribió** y el link para volver.
5. Dashboard: hay un CTA "Empieza a validar" que lleva a la página.
6. Un `MedicoPendiente` NO ve la sección; un `Medico`/`Administrador` sí.
7. Layout full-width (cards llenan el ancho). `dotnet publish -c Release` limpio antes del push.
