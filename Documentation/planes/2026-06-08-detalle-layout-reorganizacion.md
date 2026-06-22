# Detalle.cshtml — Reorganización de Layout en 6 Renglones

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el grid de 3 columnas de `Detalle.cshtml` por 6 filas independientes apiladas, sin romper el `<form>` ni perder ningún campo en el POST.

**Architecture:** Se elimina `.detalle-main-grid` (grid de 3 columnas con posicionamiento por `grid-column`) y se reemplaza por 6 contenedores `.detalle-fila--N` independientes, cada uno con su propio grid interno. El `<form>` y todos los campos permanecen en el mismo lugar; solo cambian los wrappers/contenedores.

**Tech Stack:** Razor Pages (.cshtml) · CSS Grid · Bootstrap 5

---

## Hallazgos previos (no requieren acción)

- `<form>` abre en **línea 348**, cierra en **línea 942**
- `.detalle-top-bar` (líneas 352–375): dentro del form, **NO SE TOCA**
- `.detalle-main-grid` (líneas 377–941): bloque a reemplazar
- **Todos los campos** están dentro del form — no hay bug de campos fuera del form. El problema es visual/layout únicamente.
- `@section Scripts` (líneas 945–1666): **NO SE TOCA**

## Mapa de contenido → fila nueva

| Fila | Contenido | Fuente actual |
|---|---|---|
| 1 (top bar) | URL SEO + botones | `.detalle-top-bar` — sin cambios |
| 2 (7fr/3fr) | Editar Contenido / GRIS | `.detalle-col-form` / `.detalle-col-gris` |
| 3 (1fr/1fr) | Categorías / GRIS·Categorías | Del `.detalle-rel-right` |
| 4 (full width) | Contenidos generales + Preguntas + Comparar + Sugerencias + comparación | Del `.detalle-rel-left` + `.detalle-rel-right` + `.detalle-row3` |
| 5 (repeat(3,1fr)) | Condiciones / Tratamientos / Síntomas | `.detalle-row2` |
| 6 (full width) | Cancelar / Guardar | `.detalle-row3` |

---

## Archivo en scope

- **Modificar:** `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Detalle.cshtml`
- **No tocar:** `Detalle.cshtml.cs`, ningún otro archivo

---

## Task 1: Reemplazar el bloque CSS (líneas 39–111)

**Files:**
- Modify: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Detalle.cshtml:39-111`

- [ ] **Paso 1.1:** En `@section Styles`, localizar el bloque que inicia en la línea 39 (`/* ── Grid principal ── */`) y termina en la línea 111 (`}`  del segundo `@@media`). Reemplazar ese bloque completo con el CSS siguiente. **No tocar ninguna regla fuera de esas líneas.**

El CSS a eliminar (líneas 39–111) contiene:
- `.detalle-main-grid`
- `.detalle-col-form` (sólo grid-column positioning; el card-style se re-agrega abajo)
- `.detalle-col-gris`
- `.detalle-col-rel`, `.detalle-rel-left`, `.detalle-rel-right`
- `.detalle-row2`, `.detalle-row3`
- Los dos bloques `@@media` que referencian esas clases

Reemplazar con:

```css
        /* ── Filas de layout ── */
        .detalle-fila {
            margin-bottom: 20px;
        }

        /* Fila 2: Editar Contenido (70%) / GRIS (30%) */
        .detalle-fila--2 {
            display: grid;
            grid-template-columns: 7fr 3fr;
            gap: 20px;
            align-items: start;
        }

        .detalle-col-form {
            background: #fff;
            border: 1px solid #e8eef6;
            border-radius: 10px;
            padding: 20px;
        }

        .detalle-col-gris {
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        /* Fila 3: Categorías (50%) / GRIS·Categorías (50%) */
        .detalle-fila--3 {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            align-items: start;
        }

        /* Fila 4: full-width con inner grid 50/50 */
        .detalle-fila--4-inner {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            align-items: start;
            margin-bottom: 16px;
        }

        .detalle-fila--4-col {
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        /* Fila 5: Condiciones / Tratamientos / Síntomas */
        .detalle-fila--5 {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            align-items: start;
        }

        /* Fila 6: botones */
        .detalle-fila--6 {
            background: #fff;
            border: 1px solid #e8eef6;
            border-radius: 10px;
            padding: 14px 20px;
        }

        /* ── Responsive ── */
        @@media (max-width: 1400px) {
            .detalle-fila--2 { grid-template-columns: 1fr; }
            .detalle-fila--3 { grid-template-columns: 1fr; }
            .detalle-fila--5 { grid-template-columns: 1fr 1fr; }
        }

        @@media (max-width: 900px) {
            .detalle-fila--2,
            .detalle-fila--3,
            .detalle-fila--4-inner,
            .detalle-fila--5 { grid-template-columns: 1fr; }
        }
```

---

## Task 2: Reemplazar la estructura HTML (líneas 377–941)

**Files:**
- Modify: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Detalle.cshtml:377-941`

- [ ] **Paso 2.1:** Localizar la línea 377 (`<div class="detalle-main-grid">`) y la línea 941 (`</div>` que cierra el grid). Reemplazar ese bloque completo — desde `<div class="detalle-main-grid">` hasta su `</div>` de cierre — con el HTML siguiente. El `<form>` (línea 348) y el `</form>` (línea 942) **no se mueven**.

```razor
        <!-- ── Fila 2: Editar Contenido (70%) / GRIS (30%) ── -->
        <div class="detalle-fila detalle-fila--2">

            <div class="detalle-col-form">
                <h3>@ViewData["Title"]</h3>

                @if (!string.IsNullOrWhiteSpace(Model.DebugInfoHtml))
                {
                    <div>
                        <button type="button" class="btn btn-sm btn-outline-secondary toggle-debug-btn" id="btnToggleDebug">Mostrar debug</button>
                        <div id="debugBlock" style="display:none" class="mb-3">
                            <label class="form-label">DEBUG</label>
                            <pre class="debug">@Html.Raw(Model.DebugInfoHtml)</pre>
                        </div>
                    </div>
                }

                @if (!string.IsNullOrEmpty(Model.ErrorMessage))
                {
                    <div class="alert alert-danger">@Model.ErrorMessage</div>
                }
                @if (!string.IsNullOrEmpty(Model.WarningMessage))
                {
                    <div class="alert alert-warning">@Model.WarningMessage</div>
                }
                @if (!string.IsNullOrEmpty(Model.SuccessMessage))
                {
                    <div id="successBanner" class="alert alert-success text-center">@Model.SuccessMessage</div>
                }

                <div class="mb-3">
                    <label class="form-label">Título</label>
                    <input class="form-control" asp-for="ContenidoTitulo" id="titulo" autocomplete="off" />
                </div>

                <div class="mb-3 d-flex align-items-center">
                    <div style="flex:1">
                        <label class="form-label">Slug</label>
                        <input class="form-control" asp-for="ContenidoTituloSlug" id="slug" autocomplete="off" />
                    </div>
                    <div style="margin-left:12px;">
                        <span id="slugStatus" class="slug-status" aria-live="polite"></span>
                    </div>
                </div>
                <small class="text-muted d-block mb-2">Se genera y valida en tiempo real. Puedes editarlo manualmente.</small>

                <div class="mb-3">
                    <label class="form-label">Resumen</label>
                    <textarea class="form-control" asp-for="ContenidoTextoC" rows="3"></textarea>
                </div>

                <div class="mb-3">
                    <label class="form-label">Contenido (HTML)</label>
                    <textarea class="form-control" asp-for="ContenidoTextoL" id="contenidoHtml" rows="15">@Html.Raw(Model.ContenidoTextoL)</textarea>
                </div>

                <div class="row g-2">
                    <div class="col-md-3">
                        <label class="form-label">Estado</label>
                        <select class="form-select" asp-for="EstadoPublicacion">
                            <option value="0">Borrador</option>
                            <option value="1">Publicado</option>
                            <option value="2">Publicado y Pagina de Inicio</option>
                            <option value="3">Publicado y Popular</option>
                        </select>
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">Inicio</label>
                        <input type="datetime-local" class="form-control" asp-for="ContenidoFechaInicio" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">Fin</label>
                        <input type="datetime-local" class="form-control" asp-for="ContenidoFechaFin" />
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">País</label>
                        <select class="form-select" name="PaisClave" id="paisSelect">
                            <option value="">(Seleccionar país)</option>
                            @foreach (var p in Model.PaisesLista)
                            {
                                if (p.code == Model.PaisClave)
                                {
                                    <option value="@p.code" selected>@p.name (@p.code)</option>
                                }
                                else
                                {
                                    <option value="@p.code">@p.name (@p.code)</option>
                                }
                            }
                        </select>
                    </div>
                </div>

                <div class="row g-2 mt-3">
                    <div class="col-md-6">
                        <label class="form-label">Autor</label>
                        <select name="SelectedAutorId" id="SelectedAutorId" class="form-select">
                            <option value="">(Seleccionar autor)</option>
                            @foreach (var a in Model.AdminAuthors)
                            {
                                if (Model.SelectedAutorId == a.id)
                                {
                                    <option value="@a.id" selected>@a.name</option>
                                }
                                else
                                {
                                    <option value="@a.id">@a.name</option>
                                }
                            }
                        </select>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label">Imagen principal</label>
                        <input type="file" class="form-control" name="UploadedImage" />
                        @if (!string.IsNullOrWhiteSpace(Model.URLImagenPrincipal))
                        {
                            <div class="mt-2">
                                <img src="~/uploads/contenidos/@Model.URLImagenPrincipal" style="max-width:220px;max-height:140px;" />
                            </div>
                        }
                    </div>
                </div>

            </div>

            <div class="detalle-col-gris" id="grisPanel">
                @if (Model.Id.HasValue)
                {
                    <div class="gris-card">
                        <div style="display:flex;align-items:center;gap:6px;margin-bottom:12px;">
                            <i class="bi bi-stars text-primary" style="font-size:1.05rem;"></i>
                            <span style="font-weight:600;font-size:0.9rem;">GRIS</span>
                            <span style="font-size:0.72rem;font-weight:400;color:#6b7280;">evaluación editorial</span>
                        </div>
                        <div id="grisResultadoDetalle">
                            @if (Model.GrisEvaluado && Model.GrisPuntajeGlobal.HasValue)
                            {
                                var gScore = Model.GrisPuntajeGlobal.Value;
                                var gBg = gScore >= 70 ? "success" : gScore >= 50 ? "warning" : "danger";
                                <div class="text-center mb-3">
                                    <span class="badge bg-@gBg" style="font-size:2rem;padding:10px 24px;">@gScore</span>
                                    <div class="text-muted mt-1" style="font-size:0.76rem;">Puntaje editorial (0-100)</div>
                                    @if (Model.GrisFechaEvaluacion.HasValue)
                                    {
                                        <div class="text-muted" style="font-size:0.72rem;">@Model.GrisFechaEvaluacion.Value.ToString("dd MMM yyyy") UTC</div>
                                    }
                                </div>
                                @if (Model.GrisAspectos != null && Model.GrisAspectos.Any())
                                {
                                    <div style="font-size:0.78rem;font-weight:600;color:#374151;border-bottom:1px solid #e5e7eb;padding-bottom:4px;margin-bottom:10px;">Aspectos</div>
                                    foreach (var a in Model.GrisAspectos)
                                    {
                                        var aBg = a.Puntaje >= 7 ? "success" : a.Puntaje >= 5 ? "warning" : "danger";
                                        <div class="mb-3">
                                            <div class="d-flex justify-content-between align-items-center mb-1">
                                                <span style="font-size:0.8rem;font-weight:600;">@a.Nombre</span>
                                                <span class="badge bg-@aBg" style="font-size:0.72rem;">@a.Puntaje/10</span>
                                            </div>
                                            <div class="gris-aspecto-bar">
                                                <div class="bg-@aBg" style="width:@(a.Puntaje * 10)%;height:100%;border-radius:3px;"></div>
                                            </div>
                                            <div style="font-size:0.76rem;color:#4b5563;margin-top:3px;line-height:1.4;">@a.Observacion</div>
                                        </div>
                                    }
                                }
                                @if (Model.GrisSugerencias != null && Model.GrisSugerencias.Any())
                                {
                                    <div style="font-size:0.78rem;font-weight:600;color:#374151;border-bottom:1px solid #e5e7eb;padding-bottom:4px;margin-bottom:8px;">Sugerencias</div>
                                    <ul style="font-size:0.78rem;color:#4b5563;padding-left:18px;line-height:1.5;margin-bottom:12px;">
                                        @foreach (var s in Model.GrisSugerencias)
                                        {
                                            <li>@s</li>
                                        }
                                    </ul>
                                }
                                <button class="btn btn-sm btn-outline-secondary w-100" onclick="evaluarConGrisDetalle(@Model.Id.Value)">
                                    <i class="bi bi-arrow-repeat me-1"></i>Re-evaluar con GRIS
                                </button>
                            }
                            else
                            {
                                <div class="text-muted mb-3" style="font-size:0.82rem;line-height:1.5;">
                                    Analiza la calidad editorial del artículo: claridad, utilidad, credibilidad, lenguaje, originalidad, engagement y SEO.
                                </div>
                                <button class="btn btn-sm btn-primary w-100" onclick="evaluarConGrisDetalle(@Model.Id.Value)">
                                    <i class="bi bi-stars me-1"></i>Evaluar con GRIS
                                </button>
                            }
                        </div>
                        <div id="grisSpinnerDetalle" style="display:none;" class="text-center py-3">
                            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                            <div class="text-muted small mt-1">GRIS evaluando...</div>
                        </div>
                    </div>
                }
            </div>

        </div>

        <!-- ── Fila 3: Categorías seleccionadas (50%) / GRIS·Categorías (50%) ── -->
        <div class="detalle-fila detalle-fila--3">

            <div class="rel-box">
                <h5>Categorías seleccionadas <span class="badge bg-light text-dark">@Model.SelectedCategoryIds.Count</span></h5>
                <div id="selectedCategoryBadges" class="rel-selected-badges mb-2">
                    @foreach (var seq in Model.SelectedCategoryIds)
                    {
                        var cat = Model.CategoryItems.FirstOrDefault(c => c.Sequence == seq);
                        if (cat != null)
                        {
                            var isPrincipal = Model.PrincipalCategoryId.HasValue && Model.PrincipalCategoryId.Value == cat.Sequence;
                            <span class="rel-badge-sel@(isPrincipal ? " principal" : "")" data-id="@cat.Sequence">@cat.Nombre
                                @if (isPrincipal)
                                {
                                    <span class="badge-principal">Principal</span>
                                }
                                <button type="button" class="badge-remove btn-close" data-target-name="SelectedCategoryIds" data-id="@cat.Sequence" aria-label="Eliminar"></button>
                            </span>
                        }
                    }
                </div>
                <div class="rel-search-row mb-2">
                    <input type="text" id="filterCategories" placeholder="Filtrar categorías..." autocomplete="off" />
                </div>
                <div class="rel-scroll" id="categoriesPanel">
                    @{
                        var parents = Model.CategoryItems.Where(ci => ci.CategoriaPadre == null).OrderBy(ci => ci.Nombre).ToList();
                        var children = Model.CategoryItems.Where(ci => ci.CategoriaPadre != null).OrderBy(ci => ci.Nombre).ToList();
                        var childrenByParent = children.GroupBy(c => c.CategoriaPadre).ToDictionary(g => g.Key, g => g.ToList());
                        var orphanChildren = children.Where(c => c.CategoriaPadre.HasValue && !parents.Any(p => p.Sequence == c.CategoriaPadre.Value)).ToList();
                    }
                    @foreach (var p in parents)
                    {
                        <div class="cond-group" data-filter="@p.Nombre.ToLowerInvariant()">
                            <div class="cond-parent">
                                <div style="display:flex;align-items:center;gap:8px;">
                                    <input type="checkbox" name="SelectedCategoryIds" class="cat-parent-checkbox" id="cat_parent_@p.Sequence" data-seq="@p.Sequence" value="@p.Sequence" @(Model.SelectedCategoryIds.Contains(p.Sequence) ? "checked" : "") />
                                    <input type="radio" name="PrincipalCategoryId" value="@p.Sequence" @(Model.PrincipalCategoryId.HasValue && Model.PrincipalCategoryId.Value == p.Sequence ? "checked" : "") title="Marcar como principal" />
                                    <span class="parent-title ms-2">@p.Nombre</span>
                                </div>
                            </div>
                            <div class="cond-children ms-3">
                                @if (childrenByParent.TryGetValue(p.Sequence, out var chs))
                                {
                                    foreach (var ch in chs)
                                    {
                                        <label class="rel-item" data-filter="@ch.Nombre.ToLowerInvariant()">
                                            <input type="checkbox" name="SelectedCategoryIds" class="cat-child-checkbox ms-1" data-parent="@p.Sequence" value="@ch.Sequence" @(Model.SelectedCategoryIds.Contains(ch.Sequence) ? "checked" : "") />
                                            <input type="radio" name="PrincipalCategoryId" value="@ch.Sequence" @(Model.PrincipalCategoryId.HasValue && Model.PrincipalCategoryId.Value == ch.Sequence ? "checked" : "") title="Marcar como principal" />
                                            <span class="ms-2">@ch.Nombre</span>
                                        </label>
                                    }
                                }
                            </div>
                        </div>
                    }

                    @if (orphanChildren.Any())
                    {
                        <div class="cond-group" data-filter="otras">
                            <div class="cond-parent"><strong>Otras categorías</strong></div>
                            <div class="cond-children ms-3">
                                @foreach (var oc in orphanChildren)
                                {
                                    <label class="rel-item" data-filter="@oc.Nombre.ToLowerInvariant()">
                                        <input type="checkbox" name="SelectedCategoryIds" class="cat-child-checkbox ms-1" value="@oc.Sequence" @(Model.SelectedCategoryIds.Contains(oc.Sequence) ? "checked" : "") />
                                        <input type="radio" name="PrincipalCategoryId" value="@oc.Sequence" @(Model.PrincipalCategoryId.HasValue && Model.PrincipalCategoryId.Value == oc.Sequence ? "checked" : "") title="Marcar como principal" />
                                        <span class="ms-2">@oc.Nombre</span>
                                    </label>
                                }
                            </div>
                        </div>
                    }
                </div>
            </div>

            <div>
                @if (Model.Id.HasValue)
                {
                    <div class="rel-box" id="panelGrisCategorias"@(!Model.GrisEvaluado ? " style=\"display:none\"" : "")>
                        <h5 style="font-size:0.88rem;">
                            <span><i class="bi bi-stars text-primary me-1"></i>GRIS · Categorías</span>
                        </h5>
                        <div id="panelGrisCategoriasContent">
                            @if (Model.GrisEvaluado)
                            {
                                @if (Model.GrisCategoriasAlerta != null && Model.GrisCategoriasAlerta.Any())
                                {
                                    <div style="font-size:0.71rem;color:#dc2626;font-weight:600;margin-bottom:4px;">⚠ Posible mala clasificación</div>
                                    foreach (var ca in Model.GrisCategoriasAlerta)
                                    {
                                        <div style="margin-bottom:5px;">
                                            <span class="badge bg-danger-subtle text-danger-emphasis" style="font-size:0.68rem;">@ca.Nombre</span>
                                            <div style="font-size:0.67rem;color:#6b7280;margin-top:1px;">@ca.Razon</div>
                                        </div>
                                    }
                                }
                                else
                                {
                                    <div style="font-size:0.71rem;color:#16a34a;margin-bottom:4px;">✓ Categorización coherente</div>
                                }
                                @if (Model.GrisCategoriasSugeridas != null && Model.GrisCategoriasSugeridas.Any())
                                {
                                    <div style="font-size:0.71rem;color:#6b7280;margin-bottom:3px;">Sugeridas (solo informativo):</div>
                                    <div style="display:flex;flex-wrap:wrap;gap:4px;">
                                        @foreach (var cs in Model.GrisCategoriasSugeridas)
                                        {
                                            <span class="badge bg-primary-subtle text-primary-emphasis"
                                                  style="font-size:0.68rem;cursor:default;"
                                                  title="@cs.Razon">@cs.Nombre</span>
                                        }
                                    </div>
                                }
                            }
                        </div>
                    </div>
                }
            </div>

        </div>

        <!-- ── Fila 4: Contenidos generales + Preguntas + Comparar + Sugerencias ── -->
        <div class="detalle-fila detalle-fila--4">

            <div class="detalle-fila--4-inner">

                <div class="detalle-fila--4-col">

                    <div class="rel-box">
                        <h5>Contenidos generales <span class="badge bg-light text-dark">@Model.AllGeneralContenidos.Count</span></h5>

                        <div class="rel-search-row">
                            <label class="form-label small mb-0">Filtrar por padre</label>
                            <select id="filterByParent" class="form-select">
                                <option value="">— Todos —</option>
                                @foreach (var p in Model.ParentCategories)
                                {
                                    <option value="@p.Sequence">@p.Nombre</option>
                                }
                            </select>

                            <input type="text" id="searchGeneralCont" placeholder="Filtrar por título..." autocomplete="off" />
                            <div id="generalContSelectedBadges" class="rel-selected-badges">
                                @foreach (var c in Model.AllGeneralContenidos.Where(x => Model.SelectedManualContenidoIds.Contains(x.Id)))
                                {
                                    <span class="rel-badge-sel" data-id="@c.Id">@c.Title <button type="button" class="badge-remove btn-close" data-target-name="SelectedManualContenidoIds" data-id="@c.Id" aria-label="Eliminar"></button></span>
                                }
                            </div>
                        </div>

                        <div class="rel-scroll" id="listGeneralCont" role="list">
                            @if (!Model.AllGeneralContenidos.Any())
                            {
                                <div class="rel-empty">Sin registros</div>
                            }
                            else
                            {
                                foreach (var c in Model.AllGeneralContenidos)
                                {
                                    var checkedAttr = Model.SelectedManualContenidoIds.Contains(c.Id) ? "checked" : "";
                                    var parentSeqAttr = c.ParentCategorySeq.HasValue ? c.ParentCategorySeq.Value.ToString() : "";
                                    <label class="rel-item" data-filter="@((c.Title ?? "").ToLowerInvariant())" data-parent="@parentSeqAttr" role="listitem">
                                        <input type="checkbox" name="SelectedManualContenidoIds" value="@c.Id" class="rel-check-generalcont" @checkedAttr />
                                        <span>@c.Title</span>
                                        @if (!string.IsNullOrEmpty(c.ParentCategoryName))
                                        {
                                            <small class="text-muted ms-2">· @c.ParentCategoryName</small>
                                        }
                                    </label>
                                }
                            }
                        </div>
                    </div>

                    <div class="rel-box">
                        <h5>Preguntas (con respuestas o votos) <span class="badge bg-light text-dark">@Model.AllPreguntasCandidate.Count</span></h5>
                        <div class="rel-search-row">
                            <input type="text" id="searchPreguntas" placeholder="Filtrar..." autocomplete="off" />
                            <div id="pregSelectedBadges" class="rel-selected-badges">
                                @foreach (var q in Model.AllPreguntasCandidate.Where(x => Model.SelectedManualPreguntasIds.Contains(x.id)))
                                {
                                    <span class="rel-badge-sel" data-id="@q.id">@q.title <button type="button" class="badge-remove" data-target-name="SelectedManualPreguntasIds" data-id="@q.id" aria-label="Quitar">✕</button></span>
                                }
                            </div>
                        </div>
                        <div class="rel-scroll" id="listPreguntas">
                            @if (!Model.AllPreguntasCandidate.Any())
                            {
                                <div class="rel-empty">Sin registros</div>
                            }
                            else
                            {
                                foreach (var q in Model.AllPreguntasCandidate)
                                {
                                    var checkedAttr = Model.SelectedManualPreguntasIds.Contains(q.id) ? "checked" : "";
                                    <label class="rel-item" data-filter="@((q.title ?? "").ToLowerInvariant())">
                                        <input type="checkbox" name="SelectedManualPreguntasIds" value="@q.id" class="rel-check-pregunta" @checkedAttr />
                                        <span>@q.title</span>
                                    </label>
                                }
                            }
                        </div>
                    </div>

                </div>

                <div class="detalle-fila--4-col">
                    @if (Model.Id.HasValue)
                    {
                        <div class="mb-2">
                            <button type="button" class="btn btn-sm btn-outline-info w-100" id="btnCompararRelacionados" onclick="compararRelacionados()">
                                <i class="bi bi-diagram-3 me-1"></i>Comparar artículos relacionados
                            </button>
                            <div id="spinnerComparar" style="display:none;" class="text-center py-1">
                                <div class="spinner-border spinner-border-sm text-info" role="status"></div>
                                <span class="text-muted small ms-1">Analizando...</span>
                            </div>
                        </div>
                    }
                    @if (Model.Id.HasValue)
                    {
                        <div class="rel-box" id="panelSugerirRelacionados">
                            <h5><i class="bi bi-link-45deg text-primary me-1"></i>Sugerencias para relacionar</h5>
                            <div id="sugerirRelContent">
                                <div class="text-muted small text-center py-2">
                                    <span class="spinner-border spinner-border-sm me-1" role="status"></span>Cargando...
                                </div>
                            </div>
                        </div>
                    }
                </div>

            </div>

            @if (Model.Id.HasValue)
            {
                <div id="resultadoComparar" class="comparador-panel mt-3" style="display:none;"></div>
            }

        </div>

        <!-- ── Fila 5: Condiciones / Tratamientos / Síntomas ── -->
        <div class="detalle-fila detalle-fila--5">

            <div class="rel-box" id="panelCondiciones">
                <h5>Condiciones <span class="badge bg-light text-dark">@Model.AllCondiciones.Count</span></h5>
                <div class="rel-search-row">
                    <input type="text" id="searchCondiciones" placeholder="Filtrar..." autocomplete="off" />
                    <div id="condSelectedBadges" class="rel-selected-badges">
                        @foreach (var c in Model.AllCondiciones.Where(x => Model.SelectedCondicionesIds.Contains(x.id)))
                        {
                            <span class="rel-badge-sel" data-id="@c.id">@c.nombre <button type="button" class="badge-remove" data-target-list="SelectedCondicionesIds" data-id="@c.id" aria-label="Quitar">✕</button></span>
                        }
                    </div>
                </div>

                <div class="rel-scroll" id="listCondiciones" role="list">
                    @if (!Model.AllCondiciones.Any())
                    {
                        <div class="rel-empty">Sin registros</div>
                    }
                    else
                    {
                        @foreach (var parent in parentsList)
                        {
                            <div class="cond-group" data-parent="@parent.id">
                                <div class="cond-parent">
                                <input type="checkbox" name="SelectedCondicionesIds" value="@parent.id" class="cond-parent-checkbox" data-parent-id="@parent.id" id="parentchk_@parent.id" @(Model.SelectedCondicionesIds.Contains(parent.id) ? "checked" : "") />
                                    <div class="parent-title">@parent.nombre</div>
                                </div>
                                <div class="cond-children">
                                    @{
                                        if (childrenByParentMap.TryGetValue(parent.id, out var chs) && chs.Any())
                                        {
                                            foreach (var ch in chs)
                                            {
                                                var checkedAttr = Model.SelectedCondicionesIds.Contains(ch.id) ? "checked" : "";
                                                <label class="rel-item" data-filter="@ch.nombre.ToLowerInvariant()">
                                                    <input type="checkbox" name="SelectedCondicionesIds" value="@ch.id" class="rel-check-condicion" @checkedAttr />
                                                    <span>@ch.nombre</span>
                                                </label>
                                            }
                                        }
                                        else
                                        {
                                            <div class="rel-item rel-empty">No hay subcondiciones</div>
                                        }
                                    }
                                </div>
                            </div>
                        }

                        @if (orphanChildrenList.Any())
                        {
                            <div class="cond-group" data-parent="0">
                                <div class="cond-parent">
                                    <input type="checkbox" class="cond-parent-checkbox" data-parent-id="0" id="parentchk_0" />
                                    <div class="parent-title">Otros</div>
                                </div>
                                <div class="cond-children">
                                    @foreach (var ch in orphanChildrenList)
                                    {
                                        var checkedAttr = Model.SelectedCondicionesIds.Contains(ch.id) ? "checked" : "";
                                        <label class="rel-item" data-filter="@ch.nombre.ToLowerInvariant()">
                                            <input type="checkbox" name="SelectedCondicionesIds" value="@ch.id" class="rel-check-condicion" @checkedAttr />
                                            <span>@ch.nombre</span>
                                        </label>
                                    }
                                </div>
                            </div>
                        }
                    }
                </div>
            </div>

            <div class="rel-box" id="panelTratamientos">
                <h5>Tratamientos <span class="badge bg-light text-dark">@Model.AllTratamientos.Count</span></h5>
                <div class="rel-search-row">
                    <input type="text" id="searchTratamientos" placeholder="Filtrar..." autocomplete="off" />
                    <div id="tratSelectedBadges" class="rel-selected-badges">
                        @foreach (var t in Model.AllTratamientos.Where(x => Model.SelectedTratamientosIds.Contains(x.id)))
                        {
                            <span class="rel-badge-sel" data-id="@t.id">@t.nombre <button type="button" class="badge-remove" data-target-list="SelectedTratamientosIds" data-id="@t.id" aria-label="Quitar">✕</button></span>
                        }
                    </div>
                </div>
                <div class="rel-scroll" id="listTratamientos">
                    @if (!Model.AllTratamientos.Any())
                    {
                        <div class="rel-empty">Sin registros</div>
                    }
                    else
                    {
                        foreach (var t in Model.AllTratamientos)
                        {
                            var checkedAttr = Model.SelectedTratamientosIds.Contains(t.id) ? "checked" : "";
                            <label class="rel-item" data-filter="@t.nombre?.ToLowerInvariant()">
                                <input type="checkbox" name="SelectedTratamientosIds" value="@t.id" class="rel-check-tratamiento" @checkedAttr />
                                <span>@t.nombre</span>
                            </label>
                        }
                    }
                </div>
            </div>

            <div class="rel-box" id="panelSintomas">
                <h5>Síntomas <span class="badge bg-light text-dark">@Model.AllSintomas.Count</span></h5>
                <div class="rel-search-row">
                    <input type="text" id="searchSintomas" placeholder="Filtrar..." autocomplete="off" />
                    <div id="sintSelectedBadges" class="rel-selected-badges">
                        @foreach (var s in Model.AllSintomas.Where(x => Model.SelectedSintomasIds.Contains(x.id)))
                        {
                            <span class="rel-badge-sel" data-id="@s.id">@s.nombre <button type="button" class="badge-remove" data-target-list="SelectedSintomasIds" data-id="@s.id" aria-label="Quitar">✕</button></span>
                        }
                    </div>
                </div>
                <div class="rel-scroll" id="listSintomas">
                    @if (!Model.AllSintomas.Any())
                    {
                        <div class="rel-empty">Sin registros</div>
                    }
                    else
                    {
                        foreach (var s in Model.AllSintomas)
                        {
                            var checkedAttr = Model.SelectedSintomasIds.Contains(s.id) ? "checked" : "";
                            <label class="rel-item" data-filter="@s.nombre?.ToLowerInvariant()">
                                <input type="checkbox" name="SelectedSintomasIds" value="@s.id" class="rel-check-sintoma" @checkedAttr />
                                <span>@s.nombre</span>
                            </label>
                        }
                    }
                </div>
            </div>

        </div>

        <!-- ── Fila 6: Cancelar / Guardar ── -->
        <div class="detalle-fila detalle-fila--6">
            <div class="text-end">
                <a class="btn btn-secondary me-2" href="@Url.Page("./Index")">Cancelar</a>
                <button type="submit" class="btn btn-primary" id="btnGuardar">Guardar</button>
            </div>
        </div>
```

---

## Task 3: Build limpio

**Files:**
- No changes

- [ ] **Paso 3.1:** Ejecutar build

```
dotnet build --no-restore
```

Resultado esperado: `Build succeeded. 0 Error(s)`

- [ ] **Paso 3.2:** Si hay errores de compilación de Razor, verificar que no haya variables `@{ }` duplicadas. En particular, las variables `parents`, `children`, `childrenByParent`, `orphanChildren` se declaran en la Fila 3. Si el compilador reporta redeclaración, renombrar las del bloque de Fila 3 a `catParents`, `catChildren`, `catChildrenByParent`, `catOrphanChildren` y ajustar sus usos en ese mismo bloque.

---

## Task 4: Checklist de verificación de campos POST

- [ ] **Paso 4.1:** Confirmar que cada campo sigue dentro del `<form id="detalleForm">`:

| Campo | Fila nueva | ¿Dentro del form? |
|---|---|---|
| `Id` (hidden) | antes de Fila 2 | ✓ |
| `ContenidoTitulo` | Fila 2 — `.detalle-col-form` | ✓ |
| `ContenidoTituloSlug` | Fila 2 — `.detalle-col-form` | ✓ |
| `ContenidoTextoC` | Fila 2 — `.detalle-col-form` | ✓ |
| `ContenidoTextoL` | Fila 2 — `.detalle-col-form` | ✓ |
| `EstadoPublicacion` | Fila 2 — `.detalle-col-form` | ✓ |
| `ContenidoFechaInicio` | Fila 2 — `.detalle-col-form` | ✓ |
| `ContenidoFechaFin` | Fila 2 — `.detalle-col-form` | ✓ |
| `PaisClave` | Fila 2 — `.detalle-col-form` | ✓ |
| `SelectedAutorId` | Fila 2 — `.detalle-col-form` | ✓ |
| `UploadedImage` | Fila 2 — `.detalle-col-form` | ✓ |
| `SelectedManualContenidoIds` | Fila 4 — col izquierda | ✓ |
| `SelectedManualPreguntasIds` | Fila 4 — col izquierda | ✓ |
| `SelectedCategoryIds` | Fila 3 — col izquierda | ✓ |
| `PrincipalCategoryId` | Fila 3 — col izquierda | ✓ |
| `SelectedCondicionesIds` | Fila 5 | ✓ |
| `SelectedTratamientosIds` | Fila 5 | ✓ |
| `SelectedSintomasIds` | Fila 5 | ✓ |

---

## Task 5: Verificación visual obligatoria

- [ ] **Paso 5.1:** Abrir la página de edición de un contenido existente. Confirmar visualmente:
  - Fila 2: formulario ocupa ~70% del ancho, GRIS ocupa ~30%
  - Fila 3: Categorías y GRIS·Categorías lado a lado (50/50)
  - Fila 4: Contenidos generales + Preguntas a la izquierda, Comparar + Sugerencias a la derecha
  - Fila 5: Condiciones, Tratamientos, Síntomas en 3 columnas iguales
  - Fila 6: botones Cancelar/Guardar alineados a la derecha

- [ ] **Paso 5.2:** Guardar un contenido existente cambiando Estado, Autor e Imagen. Confirmar en BD (o en el debug del PageModel) que los tres campos se actualizaron en el registro.

- [ ] **Paso 5.3:** No commitear hasta confirmar paso 5.2.
