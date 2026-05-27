# Editorial System Unification — Blog + Glosario

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unify the visual language of `Pages/Contenidos/Detalle.cshtml` and `Pages/Glosario/Termino.cshtml` so both render as a single editorial product: minimal, lightweight, token-based, no aggressive bolds, no hardcoded values.

**Architecture:** Shared CSS lives in `wwwroot/css/detalle.css`. Each page has a minimal inline `<style>` block for page-specific overrides. All values migrate to `--eii-*` tokens; no new CSS files, no new colors, no new components. The 6 documentation HTML files in `Documentation/ux-editorial/` serve as audit trail and style guide extension.

**Tech Stack:** ASP.NET Core 8 Razor Pages · `detalle.css` (shared editorial CSS) · `eiibd-tokens.css` · `eiibd-components.css` (eii-btn, eii-card) · `eiibd-layout.css` (eii-page-header) · Bootstrap 5 · Playwright MCP

---

## Hallazgos de auditoría (síntesis pre-plan)

### Conflictos críticos
| Selector | Archivo | Valor actual | Problema |
|---|---|---|---|
| `.page-title h1` | detalle.css:400 | `font-size:3rem; font-weight:800` | Anula `eii-page-title` (2.1rem/300) |
| `.conte-detail .page-title h1` | detalle.css:53 | `color:#000 !important` | Hardcode + !important |
| `.sidebar-section-title` | detalle.css:959 | `font-weight:800` | Negrita agresiva |

### Hardcodes a migrar (detalle.css)
| Selector | Propiedad | Actual | Reemplazar por |
|---|---|---|---|
| `.sidebar-section` | `border-radius` | `1rem` | `var(--eii-radius-xl)` |
| `.sidebar-section` | `border` | `1px solid #eff3f4` | `1px solid var(--eii-border-soft)` |
| `.related-card` | `border-radius` | `10px` | `var(--eii-radius-md)` |
| `.related-card:hover` | `box-shadow` | `0 4px 12px rgba(0,0,0,0.08)` | `var(--eii-shadow-md)` |
| `.tabs` | `border-radius` | `10px` | `var(--eii-radius-md)` |
| `.tabs` | `border` | `1px solid #eef2f7` | `1px solid var(--eii-border)` |
| `.pregunta-card` | `border-radius` | `8px` | `var(--eii-radius-md)` |
| `.pregunta-card` | `border` | `1px solid #eef2f7` | `1px solid var(--eii-border)` |
| `.manual-block` | `border-radius` | `10px` | `var(--eii-radius-md)` |
| `.manual-block` | `border` | `1px solid #eef2f7` | `1px solid var(--eii-border)` |
| `.detail-sub` | `background` | `#f4edfc` | `var(--eii-primary-soft)` |
| `.detail-sub` | `color` | `#24527a` | `var(--eii-primary)` |
| `.detail-sub` | `border-radius` | `10px` | `var(--eii-radius-md)` |
| `.badge-cat` (categories/social) | `background` | `#f4edfc / #eef5ff` | `var(--eii-primary-soft)` |
| `.badge-cat` (categories/social) | `color` | `#24527a` | `var(--eii-primary)` |
| `.aviso-termino-box` | `background` | `#f5f3ff` | `var(--eii-primary-soft)` |
| `.aviso-termino-box` | `border` | `1px solid #ede9fe` | `1px solid var(--eii-primary-border)` |
| `.aviso-termino-box .aviso-text` | `color` | `#4c1d95` | `var(--eii-primary-hover)` |
| `.exp-card` | `border-radius` | `12px` | `var(--eii-radius-lg)` |
| `.exp-card:hover` | `box-shadow` | `0 4px 12px rgba(0,0,0,.08)` | `var(--eii-shadow-md)` |
| `.user-card` | `border-radius` | `8px` | `var(--eii-radius-md)` |
| `.user-card:hover` | `box-shadow` | `0 4px 12px rgba(0,0,0,0.08)` | `var(--eii-shadow-md)` |
| `.article-index-sidebar:hover` | `box-shadow` | `0 0 20px rgba(106,78,122,0.2)` | `var(--eii-shadow-md)` |

### Inline styles en Termino.cshtml a eliminar
| Elemento | Problema | Solución |
|---|---|---|
| `.termino-tipo-pill` | `style="background:@tipoColorBg;color:@tipoColor;border:..."` | Clases BEM: `termino-tipo-pill--sintoma` / `--tratamiento` |
| H2 definición | `style="font-size:2rem;font-weight:500;margin:0 0 12px 0;"` | Clase `.editorial-section-h2` |
| Comunidad header | `style="display:flex;align-items:center;gap:10px;..."` | Clase existente `d-flex align-items-center gap-2` |
| Sidebar imágenes | `style="width:64px;height:48px;object-fit:cover;border-radius:6px;"` | Clase `.sidebar-thumb` |
| `.btn-purple` | Duplica `eii-btn--solid` | Migrar a `eii-btn eii-btn--solid` |
| `.btn-outline-purple` | Duplica `eii-btn--primary` | Migrar a `eii-btn eii-btn--primary` |

---

## Mapa de archivos

| Acción | Archivo |
|---|---|
| Modificar | `eiibd26/wwwroot/css/detalle.css` |
| Modificar | `eiibd26/Pages/Glosario/Termino.cshtml` |
| Modificar | `Documentation/style-guide.html` |
| Crear | `Documentation/ux-editorial/01-auditoria.html` |
| Crear | `Documentation/ux-editorial/02-inventario-css.html` |
| Crear | `Documentation/ux-editorial/03-estandar-editorial.html` |
| Crear | `Documentation/ux-editorial/04-implementacion.html` |
| Crear | `Documentation/ux-editorial/05-validacion-playwright.html` |
| Crear | `Documentation/ux-editorial/06-resumen-final.html` |

---

## Task 1 — Crear directorio + 01-auditoria.html

**Files:**
- Create: `Documentation/ux-editorial/01-auditoria.html`

- [ ] **Step 1: Crear el directorio `Documentation/ux-editorial/`** y el archivo `01-auditoria.html` con la auditoría visual completa documentada como HTML estático auto-contenido.

El archivo debe ser un HTML legible en el navegador sin dependencias externas. Estructura:

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <title>01 — Auditoría Editorial</title>
  <style>
    /* tokens inline para ser auto-contenido */
    :root {
      --eii-primary: #6a4e7a; --eii-text: #111827;
      --eii-border: #e5e7eb; --eii-surface-soft: #f9fafb;
      --eii-danger-soft: #fef2f2; --eii-danger: #dc2626;
      --eii-success-soft: #f0fdf4; --eii-success: #16a34a;
      --eii-warning-soft: #fffbeb; --eii-warning: #d97706;
    }
    body { font-family: system-ui, sans-serif; max-width: 960px; margin: 0 auto; padding: 2rem; color: var(--eii-text); }
    h1 { font-size: 1.5rem; font-weight: 300; letter-spacing: -0.04em; }
    h2 { font-size: 1rem; font-weight: 600; margin-top: 2rem; border-bottom: 1px solid var(--eii-border); padding-bottom: .5rem; }
    table { width: 100%; border-collapse: collapse; font-size: .875rem; margin: 1rem 0; }
    th, td { text-align: left; padding: .5rem .75rem; border-bottom: 1px solid var(--eii-border); }
    th { background: var(--eii-surface-soft); font-weight: 600; }
    .issue { background: var(--eii-danger-soft); color: var(--eii-danger); padding: 2px 8px; border-radius: 4px; font-size: .78rem; }
    .ok { background: var(--eii-success-soft); color: var(--eii-success); padding: 2px 8px; border-radius: 4px; font-size: .78rem; }
    .warn { background: var(--eii-warning-soft); color: var(--eii-warning); padding: 2px 8px; border-radius: 4px; font-size: .78rem; }
    code { background: #f1f5f9; padding: 1px 6px; border-radius: 3px; font-size: .85em; }
  </style>
</head>
<body>
<h1>Auditoría Visual Editorial — Blog + Glosario</h1>
<p>Sesión 2026-05-26 · Archivos analizados: <code>Detalle.cshtml</code>, <code>Termino.cshtml</code>, <code>detalle.css</code></p>

<h2>H1 — Título de página</h2>
<table>
  <tr><th>Componente</th><th>Selector actual</th><th>Valor actual</th><th>Estado</th><th>Propuesta</th></tr>
  <tr><td>Blog H1</td><td><code>.eii-page-title</code> (en eii-page-header)</td><td>2.1rem / weight:300 / color:--eii-text-heading</td><td><span class="ok">OK</span></td><td>Mantener. Page-header ya aplicado.</td></tr>
  <tr><td>Termino H1</td><td><code>.page-title h1.eii-page-title</code></td><td>2.1rem via eii-page-title PERO anulado por .page-title h1 (3rem/800)</td><td><span class="issue">CONFLICTO</span></td><td>Eliminar .page-title h1 en detalle.css. Aplicar eii-page-header a Termino.</td></tr>
  <tr><td>Override de color</td><td><code>.conte-detail .page-title h1</code></td><td><code>color:#000 !important</code></td><td><span class="issue">!important hardcode</span></td><td>Eliminar. eii-page-title ya define color via token.</td></tr>
</table>

<h2>Sidebar — Títulos de sección</h2>
<table>
  <tr><th>Selector</th><th>Propiedad</th><th>Valor actual</th><th>Estado</th><th>Propuesta</th></tr>
  <tr><td><code>.sidebar-section-title</code></td><td>font-weight</td><td>800</td><td><span class="issue">Negrita agresiva</span></td><td>var(--eii-fw-semibold) = 600</td></tr>
  <tr><td><code>.sidebar-section-title</code></td><td>font-size</td><td>var(--eii-text-lg) = 1.25rem</td><td><span class="ok">OK</span></td><td>Mantener</td></tr>
  <tr><td><code>.sidebar-section-title</code></td><td>color</td><td>#0f1419</td><td><span class="warn">Twitter-style, funcional</span></td><td>Mantener (identidad sidebar)</td></tr>
</table>

<h2>Cards y Contenedores</h2>
<table>
  <tr><th>Selector</th><th>Propiedad</th><th>Actual</th><th>Estado</th><th>Token correcto</th></tr>
  <tr><td><code>.related-card</code></td><td>border-radius</td><td>10px</td><td><span class="warn">Hardcode</span></td><td>var(--eii-radius-md) = 8px</td></tr>
  <tr><td><code>.related-card:hover</code></td><td>box-shadow</td><td>0 4px 12px rgba(0,0,0,0.08)</td><td><span class="warn">Hardcode</span></td><td>var(--eii-shadow-md)</td></tr>
  <tr><td><code>.tabs</code></td><td>border-radius</td><td>10px</td><td><span class="warn">Hardcode</span></td><td>var(--eii-radius-md)</td></tr>
  <tr><td><code>.pregunta-card</code></td><td>border-radius</td><td>8px</td><td><span class="ok">≈correcto</span></td><td>var(--eii-radius-md) explícito</td></tr>
  <tr><td><code>.sidebar-section</code></td><td>border-radius</td><td>1rem = 16px</td><td><span class="ok">Equivale</span></td><td>var(--eii-radius-xl) = 16px</td></tr>
  <tr><td><code>.sidebar-section</code></td><td>border-color</td><td>#eff3f4</td><td><span class="warn">Hardcode</span></td><td>var(--eii-border-soft)</td></tr>
  <tr><td><code>.exp-card</code> (Termino)</td><td>border-radius</td><td>12px</td><td><span class="ok">Equivale</span></td><td>var(--eii-radius-lg) = 12px</td></tr>
  <tr><td><code>.user-card</code> (Termino)</td><td>border-radius</td><td>8px</td><td><span class="ok">Equivale</span></td><td>var(--eii-radius-md)</td></tr>
</table>

<h2>Badges y Pills</h2>
<table>
  <tr><th>Selector</th><th>Propiedad</th><th>Actual</th><th>Estado</th><th>Token correcto</th></tr>
  <tr><td><code>.detail-sub</code></td><td>background</td><td>#f4edfc</td><td><span class="warn">Hardcode ≈ eii-primary-soft</span></td><td>var(--eii-primary-soft)</td></tr>
  <tr><td><code>.detail-sub</code></td><td>color</td><td>#24527a</td><td><span class="issue">No existe token</span></td><td>var(--eii-primary)</td></tr>
  <tr><td><code>.badge-cat</code> (categories)</td><td>background/color</td><td>#f4edfc / #24527a</td><td><span class="issue">Hardcode sin token</span></td><td>primary-soft / primary</td></tr>
  <tr><td><code>.termino-tipo-pill</code></td><td>background/color/border</td><td>Inline C# string interpolation</td><td><span class="issue">Inline style</span></td><td>Clases BEM modifier</td></tr>
  <tr><td><code>.aviso-termino-box</code></td><td>background</td><td>#f5f3ff ≈ primary-soft</td><td><span class="warn">Hardcode</span></td><td>var(--eii-primary-soft)</td></tr>
</table>

<h2>Botones</h2>
<table>
  <tr><th>Selector</th><th>Archivo</th><th>Estado</th><th>Propuesta</th></tr>
  <tr><td><code>.btn-purple</code></td><td>Termino.cshtml inline</td><td><span class="issue">Duplica eii-btn--solid</span></td><td>Migrar a <code>eii-btn eii-btn--solid</code></td></tr>
  <tr><td><code>.btn-outline-purple</code></td><td>Termino.cshtml inline</td><td><span class="issue">Duplica eii-btn--primary</span></td><td>Migrar a <code>eii-btn eii-btn--primary</code></td></tr>
  <tr><td>Rating buttons</td><td>detalle.css</td><td><span class="ok">OK — tokens usados</span></td><td>Mantener</td></tr>
  <tr><td><code>.btn-continuar-leyendo</code></td><td>detalle.css</td><td><span class="ok">Usa var(--eii-primary)</span></td><td>Mantener</td></tr>
</table>

<h2>Metadata (autor, fecha, tiempo lectura)</h2>
<table>
  <tr><th>Componente</th><th>Selector</th><th>Estado</th></tr>
  <tr><td>Blog meta-card</td><td><code>.meta-card</code></td><td><span class="ok">Usa tokens: eii-text-sm, eii-text-soft, eii-space-*</span></td></tr>
  <tr><td>Blog inline style autor</td><td><code>style="font-weight:100;"</code></td><td><span class="warn">Inline weight — cosmético, no crítico</span></td></tr>
  <tr><td>Termino no tiene metadata</td><td>—</td><td><span class="ok">No aplica (términos no tienen autor)</span></td></tr>
</table>

<h2>Contenido editorial HTML (contenido-html)</h2>
<table>
  <tr><th>Selector</th><th>Estado</th></tr>
  <tr><td><code>.contenido-html h2</code></td><td><span class="ok">var(--eii-text-3xl), border-bottom token</span></td></tr>
  <tr><td><code>.contenido-html h3</code></td><td><span class="ok">var(--eii-text-2xl)</span></td></tr>
  <tr><td><code>.contenido-html p</code></td><td><span class="ok">margin-bottom var(--eii-space-7), line-height 1.7</span></td></tr>
  <tr><td><code>.contenido-html ul/ol/li</code></td><td><span class="ok">Tokens correctos</span></td></tr>
  <tr><td><code>.contenido-html blockquote</code></td><td><span class="ok">eii-primary, eii-primary-soft</span></td></tr>
</table>

<h2>Inconsistencias Blog vs Termino</h2>
<table>
  <tr><th>Elemento</th><th>Blog (Detalle)</th><th>Termino</th><th>Acción</th></tr>
  <tr><td>H1 wrapper</td><td>eii-page-header > eii-page-header__content</td><td>&lt;div class="page-title"&gt;</td><td>Alinear Termino</td></tr>
  <tr><td>H2 secciones</td><td>No hay H2 propios (dentro de contenido-html)</td><td>Inline styles + .contenido wrapper</td><td>Clase .editorial-section-h2</td></tr>
  <tr><td>Breadcrumb</td><td>.breadcrumbs — tokens ✓</td><td>.breadcrumbs — tokens ✓</td><td>Igual — OK</td></tr>
  <tr><td>Sidebar structure</td><td>sidebar-section / sidebar-section-header</td><td>sidebar-section / sidebar-section-header</td><td>Igual — OK</td></tr>
  <tr><td>Rating</td><td>article-rating / rating-btn</td><td>glossaryRating / rating-btn</td><td>Misma CSS — OK</td></tr>
</table>
</body>
</html>
```

- [ ] **Step 2: Commit**

```bash
git add Documentation/ux-editorial/01-auditoria.html
git commit -m "docs: auditoría editorial — blog + termino análisis visual"
```

---

## Task 2 — 02-inventario-css.html

**Files:**
- Create: `Documentation/ux-editorial/02-inventario-css.html`

- [ ] **Step 1: Crear 02-inventario-css.html** con inventario completo de clases CSS editoriales, lo que existe, lo que está duplicado y lo que puede eliminarse.

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <title>02 — Inventario CSS Editorial</title>
  <style>
    :root {
      --eii-primary:#6a4e7a; --eii-text:#111827;
      --eii-border:#e5e7eb; --eii-surface-soft:#f9fafb;
      --eii-danger-soft:#fef2f2; --eii-danger:#dc2626;
      --eii-success-soft:#f0fdf4; --eii-success:#16a34a;
      --eii-warning-soft:#fffbeb; --eii-warning:#d97706;
    }
    body { font-family: system-ui, sans-serif; max-width: 960px; margin: 0 auto; padding: 2rem; color: var(--eii-text); }
    h1 { font-size: 1.5rem; font-weight: 300; letter-spacing: -0.04em; }
    h2 { font-size: 1rem; font-weight: 600; margin-top: 2rem; border-bottom: 1px solid var(--eii-border); padding-bottom: .5rem; }
    table { width: 100%; border-collapse: collapse; font-size: .875rem; margin: 1rem 0; }
    th, td { text-align: left; padding: .5rem .75rem; border-bottom: 1px solid var(--eii-border); }
    th { background: var(--eii-surface-soft); font-weight: 600; }
    .keep { color: var(--eii-success); font-weight: 600; }
    .remove { color: var(--eii-danger); font-weight: 600; }
    .migrate { color: var(--eii-warning); font-weight: 600; }
    code { background: #f1f5f9; padding: 1px 6px; border-radius: 3px; font-size: .85em; }
  </style>
</head>
<body>
<h1>Inventario CSS Editorial</h1>
<p>Sesión 2026-05-26 · Fuente: <code>detalle.css</code> (1393 líneas)</p>

<h2>Clases existentes — acción recomendada</h2>
<table>
  <tr><th>Clase</th><th>Descripción</th><th>Tokens usados</th><th>Acción</th></tr>
  <!-- Layout -->
  <tr><td><code>.conte-detail</code></td><td>Wrapper máx 1340px, padding 28/16px</td><td>Parcial</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.detail-grid</code></td><td>Grid 2fr/1fr content+sidebar</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.content-panel</code></td><td>Columna principal</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.right-panel</code></td><td>Sidebar derecho</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <!-- Título -->
  <tr><td><code>.page-title</code></td><td>Wrapper con margin 8/18px</td><td>No</td><td><span class="migrate">VACIAR — quitar h1 rules, conservar solo wrapper margin</span></td></tr>
  <tr><td><code>.page-title h1</code></td><td>3rem / 800 / conflicto con eii-page-title</td><td>No</td><td><span class="remove">ELIMINAR — conflicto</span></td></tr>
  <!-- Metadata -->
  <tr><td><code>.meta-card</code></td><td>Autor + fecha + tiempo lectura</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.breadcrumbs</code></td><td>Nav breadcrumb</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <!-- Contenido -->
  <tr><td><code>.contenido-html</code></td><td>Body del artículo — tipografía completa</td><td>Sí (completo)</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.detail-image</code></td><td>Hero image del artículo</td><td>Parcial</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.detail-sub</code></td><td>Excerpt/subtítulo pill</td><td>No — hardcode</td><td><span class="migrate">MIGRAR a tokens</span></td></tr>
  <!-- Sidebar -->
  <tr><td><code>.sidebar-section</code></td><td>Card sidebar con toggle</td><td>Parcial</td><td><span class="migrate">MIGRAR border/radius a tokens</span></td></tr>
  <tr><td><code>.sidebar-section-title</code></td><td>Header de card sidebar</td><td>Parcial</td><td><span class="migrate">MIGRAR font-weight 800→600</span></td></tr>
  <tr><td><code>.sidebar-section-content</code></td><td>Body colapsable</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.sidebar-static</code></td><td>Variante siempre visible</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <!-- Compartir -->
  <tr><td><code>.share-btn</code></td><td>Botón de red social</td><td>Parcial (#f7f9f9)</td><td><span class="keep">MANTENER — Twitter palette intencional</span></td></tr>
  <!-- Rating -->
  <tr><td><code>.rating-btn</code></td><td>Me fue útil / No me fue útil</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.rating-like.active</code></td><td>Estado votado positivo</td><td>Parcial (success colors)</td><td><span class="keep">MANTENER</span></td></tr>
  <!-- Related -->
  <tr><td><code>.related-card</code></td><td>Card artículo relacionado</td><td>No — hardcode</td><td><span class="migrate">MIGRAR border/radius/shadow</span></td></tr>
  <tr><td><code>.related-grid</code></td><td>Grid 3 columnas relacionados</td><td>No (16px hardcode)</td><td><span class="keep">MANTENER — gap 16px = space-4 ≈OK</span></td></tr>
  <tr><td><code>.tabs</code></td><td>Tabs relacionados</td><td>No — hardcode</td><td><span class="migrate">MIGRAR border/radius</span></td></tr>
  <tr><td><code>.pregunta-card</code></td><td>Card pregunta relacionada</td><td>No — hardcode</td><td><span class="migrate">MIGRAR border/radius</span></td></tr>
  <!-- Badges -->
  <tr><td><code>.badge-cat-compact</code></td><td>Badge sidebar compacto</td><td>Sí (xs, #f7f9f9)</td><td><span class="keep">MANTENER</span></td></tr>
  <tr><td><code>.categories-list .badge-cat</code></td><td>Badge categoría artículo</td><td>No — #f4edfc / #24527a</td><td><span class="migrate">MIGRAR a primary-soft / primary</span></td></tr>
  <!-- Índice -->
  <tr><td><code>.article-index-sidebar</code></td><td>TOC sidebar</td><td>Parcial</td><td><span class="migrate">MIGRAR hover shadow a token</span></td></tr>
  <tr><td><code>.article-index-list</code></td><td>Lista del TOC</td><td>Sí</td><td><span class="keep">MANTENER</span></td></tr>
  <!-- Aviso -->
  <tr><td><code>.aviso-termino-box</code></td><td>Disclaimer médico</td><td>No — hardcode</td><td><span class="migrate">MIGRAR colores a tokens</span></td></tr>
  <!-- Termino específicos (inline) -->
  <tr><td><code>.termino-tipo-pill</code></td><td>Pill "Síntoma"/"Tratamiento"</td><td>No — C# inline style</td><td><span class="migrate">Crear modificadores BEM CSS</span></td></tr>
  <tr><td><code>.btn-purple</code></td><td>Botón acción comunidad</td><td>No — duplica eii-btn--solid</td><td><span class="remove">ELIMINAR — migrar a eii-btn eii-btn--solid</span></td></tr>
  <tr><td><code>.btn-outline-purple</code></td><td>Botón secundario comunidad</td><td>No — duplica eii-btn--primary</td><td><span class="remove">ELIMINAR — migrar a eii-btn eii-btn--primary</span></td></tr>
</table>

<h2>Tokens disponibles para migración</h2>
<table>
  <tr><th>Token</th><th>Valor</th><th>Usos editoriales</th></tr>
  <tr><td><code>--eii-radius-md</code></td><td>8px</td><td>related-card, tabs, pregunta-card, manual-block, user-card</td></tr>
  <tr><td><code>--eii-radius-lg</code></td><td>12px</td><td>exp-card (Termino)</td></tr>
  <tr><td><code>--eii-radius-xl</code></td><td>16px</td><td>sidebar-section (reemplaza 1rem)</td></tr>
  <tr><td><code>--eii-shadow-md</code></td><td>0 4px 12px rgba(0,0,0,.08)...</td><td>Todos los :hover con sombra</td></tr>
  <tr><td><code>--eii-shadow-sm</code></td><td>0 1px 3px rgba(0,0,0,.08)...</td><td>article-index-sidebar:hover</td></tr>
  <tr><td><code>--eii-primary-soft</code></td><td>#f5eefb</td><td>detail-sub bg, badge-cat bg, aviso bg</td></tr>
  <tr><td><code>--eii-primary</code></td><td>#6a4e7a</td><td>detail-sub color, badge-cat color</td></tr>
  <tr><td><code>--eii-primary-border</code></td><td>#d4b8e6</td><td>aviso-termino-box border</td></tr>
  <tr><td><code>--eii-border-soft</code></td><td>#f3f4f6</td><td>sidebar-section border (reemplaza #eff3f4)</td></tr>
  <tr><td><code>--eii-border</code></td><td>#e5e7eb</td><td>tabs border, card borders</td></tr>
  <tr><td><code>--eii-fw-semibold</code></td><td>600</td><td>sidebar-section-title (reemplaza 800)</td></tr>
</table>

<h2>Lo que NO hay que tocar</h2>
<ul>
  <li><code>.contenido-html</code> — ya está correctamente migrado</li>
  <li><code>.meta-card</code> — ya usa tokens</li>
  <li><code>.rating-btn</code> — bien estructurado</li>
  <li>Paleta Twitter del sidebar (#f7f9f9, #eff3f4, #0f1419, #536471) — identidad visual intencional</li>
  <li>Gradientes IA NINA — excepciones documentadas en style-guide</li>
</ul>
</body>
</html>
```

- [ ] **Step 2: Commit**

```bash
git add Documentation/ux-editorial/02-inventario-css.html
git commit -m "docs: inventario CSS editorial — mapa de migración"
```

---

## Task 3 — Fix detalle.css: conflictos y tokens

**Files:**
- Modify: `eiibd26/wwwroot/css/detalle.css`

Cambios exactos (en orden de aparición en el archivo):

- [ ] **Step 1: Eliminar override con !important (líneas 53–56)**

Localizar y eliminar este bloque completo:
```css
/* breadcrumb and title overrides for glossary pages */
.conte-detail .page-title h1,
.display-5 {
    color: #000 !important;
}
```
Este bloque anula el color definido por `eii-page-title` con un hardcode + `!important`. La clase `.display-5` no existe en este módulo; el bloque es obsoleto.

- [ ] **Step 2: Eliminar .page-title h1 conflictivo (líneas ~400–407)**

Localizar y eliminar solo la regla para `h1` dentro de `.page-title`, conservando `.page-title` como wrapper de spacing:

Antes:
```css
.page-title {
    margin: 8px 0 18px 0;
}

.page-title h1 {
    font-size: 3rem;
    margin: 0 0 var(--eii-space-4) 0;
    line-height: 1.2;
    font-weight: 800;
    color: var(--eii-text);
    letter-spacing: -0.03em;
}

.page-title h1.sintomas-title {
    color: var(--eii-primary);
}
```

Después:
```css
.page-title {
    margin: var(--eii-space-2) 0 var(--eii-space-5) 0;
}

.page-title h1.sintomas-title {
    color: var(--eii-primary);
}
```

- [ ] **Step 3: Token-ify .detail-sub**

Antes:
```css
.detail-sub {
    color: #6b7280;
    margin-top: 6px;
    display: inline-block;
    background: #f4edfc;
    color: #24527a;
    padding: 6px 10px;
    border-radius: 10px;
    font-size: .85rem;
}
```

Después:
```css
.detail-sub {
    margin-top: var(--eii-space-2);
    display: inline-block;
    background: var(--eii-primary-soft);
    color: var(--eii-primary);
    padding: var(--eii-space-2) var(--eii-space-3);
    border-radius: var(--eii-radius-md);
    font-size: var(--eii-text-sm);
}
```

- [ ] **Step 4: Token-ify .sidebar-section**

Antes:
```css
.sidebar-section {
    background: var(--eii-surface);
    border: 1px solid #eff3f4;
    border-radius: 1rem;
    margin-bottom: var(--eii-space-6);
    overflow: hidden;
    transition: box-shadow 0.2s ease;
}
```

Después:
```css
.sidebar-section {
    background: var(--eii-surface);
    border: 1px solid var(--eii-border-soft);
    border-radius: var(--eii-radius-xl);
    margin-bottom: var(--eii-space-6);
    overflow: hidden;
    transition: box-shadow var(--eii-transition-base);
}
```

- [ ] **Step 5: Reducir font-weight de .sidebar-section-title**

Antes:
```css
.sidebar-section-title {
    font-size: var(--eii-text-lg);
    font-weight: 800;
    color: #0f1419;
    ...
}
```

Después:
```css
.sidebar-section-title {
    font-size: var(--eii-text-lg);
    font-weight: var(--eii-fw-semibold);
    color: #0f1419;
    ...
}
```
> Nota: `#0f1419` es la paleta "Twitter-style" del sidebar. Conservar intencional.

- [ ] **Step 6: Token-ify .related-card**

Antes:
```css
.related-card {
    display: flex;
    flex-direction: column;
    margin-bottom: 16px;
    background: #fff;
    border-radius: 10px;
    padding: 0;
    border: 1px solid #eef2f7;
    overflow: hidden;
    transition: box-shadow 0.2s;
}

.related-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.08);
}
```

Después:
```css
.related-card {
    display: flex;
    flex-direction: column;
    margin-bottom: var(--eii-space-4);
    background: var(--eii-surface);
    border-radius: var(--eii-radius-md);
    padding: 0;
    border: 1px solid var(--eii-border);
    overflow: hidden;
    transition: box-shadow var(--eii-transition-base);
}

.related-card:hover {
    box-shadow: var(--eii-shadow-md);
}
```

- [ ] **Step 7: Token-ify .tabs**

Antes:
```css
.tabs {
    margin-top: 26px;
    background: #fff;
    border: 1px solid #eef2f7;
    border-radius: 10px;
    overflow: hidden;
}
```

Después:
```css
.tabs {
    margin-top: var(--eii-space-6);
    background: var(--eii-surface);
    border: 1px solid var(--eii-border);
    border-radius: var(--eii-radius-md);
    overflow: hidden;
}
```

- [ ] **Step 8: Token-ify .pregunta-card**

Antes:
```css
.pregunta-card {
    background: #fff;
    border-radius: 8px;
    border: 1px solid #eef2f7;
    padding: 10px;
    margin-bottom: 10px;
    font-weight: 100;
    font-size: .95rem;
}
```

Después:
```css
.pregunta-card {
    background: var(--eii-surface);
    border-radius: var(--eii-radius-md);
    border: 1px solid var(--eii-border);
    padding: var(--eii-space-3);
    margin-bottom: var(--eii-space-3);
    font-weight: var(--eii-fw-normal);
    font-size: var(--eii-text-base);
}
```

- [ ] **Step 9: Token-ify .manual-block**

Antes:
```css
.manual-block {
    background: #fff;
    border-radius: 10px;
    border: 1px solid #eef2f7;
    padding: 12px;
    margin-bottom: 12px;
}
```

Después:
```css
.manual-block {
    background: var(--eii-surface);
    border-radius: var(--eii-radius-md);
    border: 1px solid var(--eii-border);
    padding: var(--eii-space-3);
    margin-bottom: var(--eii-space-3);
}
```

- [ ] **Step 10: Token-ify .categories-list .badge-cat**

Antes:
```css
.categories-list .badge-cat {
    display: inline-block;
    background: #f4edfc;
    color: #24527a;
    padding: 6px 10px;
    border-radius: 10px;
    font-size: .85rem;
}
```

Después:
```css
.categories-list .badge-cat {
    display: inline-block;
    background: var(--eii-primary-soft);
    color: var(--eii-primary);
    padding: var(--eii-space-2) var(--eii-space-3);
    border-radius: var(--eii-radius-md);
    font-size: var(--eii-text-sm);
}
```

- [ ] **Step 11: Token-ify .social-list .badge-cat**

Antes:
```css
.social-list .badge-cat {
    display: inline-block;
    background: #eef5ff;
    color: #24527a;
    padding: 6px 10px;
    border-radius: 999px;
    font-size: .85rem;
    margin: 6px 6px 0 0;
}
```

Después:
```css
.social-list .badge-cat {
    display: inline-block;
    background: var(--eii-primary-soft);
    color: var(--eii-primary);
    padding: var(--eii-space-2) var(--eii-space-3);
    border-radius: var(--eii-radius-full);
    font-size: var(--eii-text-sm);
    margin: var(--eii-space-2) var(--eii-space-2) 0 0;
}
```

- [ ] **Step 12: Token-ify .aviso-termino-box** (en detalle.css, usado en Termino.cshtml inline)

Localizar en el inline `<style>` de Termino.cshtml:
```css
.aviso-termino-box {
    background: #f5f3ff;
    border: 1px solid #ede9fe;
    border-radius: 10px;
    padding: 14px 16px;
    margin-bottom: 20px;
}

.aviso-termino-box .aviso-title {
    color: var(--eii-primary);
    font-weight: 700;
    font-size: 1.3rem;
    margin-bottom: 6px;
}

.aviso-termino-box .aviso-text {
    color: #4c1d95;
    font-size: .85rem;
    margin: 0;
}
```

Reemplazar por (dentro del inline `<style>` de Termino.cshtml, se migra en Task 4):
```css
.aviso-termino-box {
    background: var(--eii-primary-soft);
    border: 1px solid var(--eii-primary-border);
    border-radius: var(--eii-radius-md);
    padding: var(--eii-space-4);
    margin-bottom: var(--eii-space-5);
}

.aviso-termino-box .aviso-title {
    color: var(--eii-primary);
    font-weight: var(--eii-fw-semibold);
    font-size: var(--eii-text-md);
    margin-bottom: var(--eii-space-2);
}

.aviso-termino-box .aviso-text {
    color: var(--eii-primary-hover);
    font-size: var(--eii-text-sm);
    margin: 0;
}
```

- [ ] **Step 13: Token-ify article-index-sidebar hover shadow**

Antes:
```css
.article-index-sidebar:hover {
    box-shadow: 0 0 20px rgba(106, 78, 122, 0.2);
}
```

Después:
```css
.article-index-sidebar:hover {
    box-shadow: var(--eii-shadow-md);
}
```

- [ ] **Step 14: Agregar .editorial-section-h2 a detalle.css**

Al final del bloque de tipografía (después de la sección `.contenido-html`), agregar:
```css
/* ─── H2 de secciones editoriales (Termino: definición, relación, comunidad) ─── */
.editorial-section-h2 {
    font-size: var(--eii-text-xl);
    font-weight: var(--eii-fw-medium);
    color: var(--eii-text-heading);
    margin: 0 0 var(--eii-space-3) 0;
    line-height: var(--eii-leading-tight);
}
```

- [ ] **Step 15: Agregar .sidebar-thumb a detalle.css**

```css
/* Thumbnail en sidebar de artículos relacionados */
.sidebar-thumb {
    width: 64px;
    height: 48px;
    object-fit: cover;
    border-radius: var(--eii-radius-sm);
    flex-shrink: 0;
}
```

- [ ] **Step 16: Verificar build**

```
dotnet build --no-restore -v quiet 2>&1 | grep "error CS"
```
Salida esperada: ninguna línea (cero errores CS).

- [ ] **Step 17: Commit**

```bash
git add eiibd26/wwwroot/css/detalle.css
git commit -m "css: migrar detalle.css — eliminar conflictos page-title, tokens radios/sombras/colores"
```

---

## Task 4 — Termino.cshtml: eii-page-header + limpiar inline styles

**Files:**
- Modify: `eiibd26/Pages/Glosario/Termino.cshtml`

- [ ] **Step 1: Aplicar eii-page-header al título del Termino**

Localizar (líneas ~473–481):
```html
<!-- Título con pill inline -->
<div class="page-title">
    <div class="termino-title-row">
        <h1 class="eii-page-title">@Model.Term.Nombre</h1>
        <span class="termino-tipo-pill"
              style="background:@tipoColorBg;color:@tipoColor;border:1px solid @tipoBorder;">
            @tipoLabel
        </span>
    </div>
</div>
```

Reemplazar por:
```html
<div class="eii-page-header">
    <div class="eii-page-header__content">
        <div class="termino-title-row">
            <h1 class="eii-page-title">@Model.Term.Nombre</h1>
            <span class="termino-tipo-pill termino-tipo-pill--@(isSintoma ? "sintoma" : "tratamiento")">
                @tipoLabel
            </span>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Agregar modificadores BEM de termino-tipo-pill al inline `<style>`**

En el bloque `@section Styles { <style>...</style> }` de Termino.cshtml, localizar:
```css
.termino-tipo-pill {
    display: inline-flex;
    align-items: center;
    font-size: .72rem;
    font-weight: 600;
    padding: 2px 10px;
    border-radius: 999px;
    letter-spacing: .02em;
    vertical-align: middle;
    white-space: nowrap;
}
```

Agregar inmediatamente después:
```css
.termino-tipo-pill--sintoma {
    background: var(--eii-primary-soft);
    color: var(--eii-primary);
    border: 1px solid var(--eii-primary-border);
}
.termino-tipo-pill--tratamiento {
    background: var(--eii-success-soft);
    color: var(--eii-success);
    border: 1px solid var(--eii-success-border);
}
```

- [ ] **Step 3: Migrar .aviso-termino-box en el inline `<style>` de Termino**

Localizar en el `<style>` inline de Termino.cshtml (aproximadamente):
```css
.aviso-termino-box {
    background: #f5f3ff;
    border: 1px solid #ede9fe;
    border-radius: 10px;
    padding: 14px 16px;
    margin-bottom: 20px;
}

    .aviso-termino-box .aviso-title {
        color: var(--eii-primary);
        font-weight: 700;
        font-size: 1.3rem;
        margin-bottom: 6px;
    }

    .aviso-termino-box .aviso-text {
        color: #4c1d95;
        font-size: .85rem;
        margin: 0;
    }
```

Reemplazar por:
```css
.aviso-termino-box {
    background: var(--eii-primary-soft);
    border: 1px solid var(--eii-primary-border);
    border-radius: var(--eii-radius-md);
    padding: var(--eii-space-4);
    margin-bottom: var(--eii-space-5);
}

.aviso-termino-box .aviso-title {
    color: var(--eii-primary);
    font-weight: var(--eii-fw-semibold);
    font-size: var(--eii-text-md);
    margin-bottom: var(--eii-space-2);
}

.aviso-termino-box .aviso-text {
    color: var(--eii-primary-hover);
    font-size: var(--eii-text-sm);
    margin: 0;
}
```

- [ ] **Step 4: Reemplazar H2 inline de la definición médica**

Localizar (línea ~523):
```html
<h2 style="font-size: 2rem;font-weight: 500;margin:0 0 12px 0;">¿Qué es y cómo se siente?</h2>
```

Reemplazar por:
```html
<h2 class="editorial-section-h2">¿Qué es y cómo se siente?</h2>
```

- [ ] **Step 5: Reemplazar H2 inline de "Relación con EII"**

Localizar (línea ~546):
```html
<div class="contenido"><h2>Relación con EII</h2></div>
```

Reemplazar por:
```html
<h2 class="editorial-section-h2">Relación con EII</h2>
```

- [ ] **Step 6: Reemplazar H2 inline de "Comunidad"**

Localizar (línea ~810–811):
```html
<div class="contenido" style="display:flex;align-items:center;gap:10px;margin-bottom:8px;flex-wrap:wrap;">
    <h2 style="margin:0;">Comunidad</h2>
```

Reemplazar por:
```html
<div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
    <h2 class="editorial-section-h2 mb-0">Comunidad</h2>
```

- [ ] **Step 7: Migrar .btn-purple → eii-btn eii-btn--solid**

Localizar en Termino.cshtml (aparece en 2 lugares):

Uso 1 — botón "Crear cuenta gratis":
```html
<a href="..." class="btn btn-purple btn-sm">
    <i class="bi bi-person-plus me-1"></i> Crear cuenta gratis
</a>
```
Reemplazar por:
```html
<a href="..." class="eii-btn eii-btn--solid eii-btn--sm">
    <i class="bi bi-person-plus me-1"></i> Crear cuenta gratis
</a>
```

- [ ] **Step 8: Migrar .btn-outline-purple → eii-btn eii-btn--primary**

Uso — botón "Iniciar sesión":
```html
<a href="..." class="btn btn-outline-purple btn-sm">
    <i class="bi bi-box-arrow-in-right me-1"></i> Iniciar sesión
</a>
```
Reemplazar por:
```html
<a href="..." class="eii-btn eii-btn--primary eii-btn--sm">
    <i class="bi bi-box-arrow-in-right me-1"></i> Iniciar sesión
</a>
```

- [ ] **Step 9: Migrar .btn-outline-purple en botón "Agregar" del community header**

Localizar:
```html
<button type="button" id="btnAgregarMiMood" class="btn btn-outline-purple btn-sm ms-auto" ...>
    <i class="bi bi-plus-lg me-1"></i> Agregar
</button>
```
Reemplazar por:
```html
<button type="button" id="btnAgregarMiMood" class="eii-btn eii-btn--primary eii-btn--sm ms-auto" ...>
    <i class="bi bi-plus-lg me-1"></i> Agregar
</button>
```

- [ ] **Step 10: Reemplazar sidebar thumbnail inline style**

Localizar (línea ~1197):
```html
<img src="@c.ImagenDestacada" alt="@c.Titulo" style="width:64px;height:48px;object-fit:cover;border-radius:6px;" loading="lazy" decoding="async" />
```
Reemplazar por:
```html
<img src="@c.ImagenDestacada" alt="@c.Titulo" class="sidebar-thumb" loading="lazy" decoding="async" />
```

- [ ] **Step 11: Eliminar .btn-purple y .btn-outline-purple del inline `<style>` de Termino**

Localizar y eliminar de la sección `<style>` inline:
```css
.btn-purple {
    background: var(--eii-primary);
    color: #fff;
    border: 1px solid var(--eii-primary);
}
.btn-purple:hover, .btn-purple:focus {
    background: var(--eii-primary-hover);
    border-color: var(--eii-primary-hover);
    color: #fff;
}
.btn-outline-purple {
    background: transparent;
    color: var(--eii-primary);
    border: 1px solid var(--eii-primary);
}
.btn-outline-purple:hover, .btn-outline-purple:focus {
    background: var(--eii-primary-soft);
    color: var(--eii-primary-hover);
    border-color: var(--eii-primary-hover);
}
```

- [ ] **Step 12: Verificar que eii-btn--sm existe en eiibd-components.css**

```bash
grep -n "eii-btn--sm" "eiibd26/wwwroot/css/eiibd-components.css"
```

Si NO existe, agregar al final de la sección de tamaños en eiibd-components.css:
```css
.eii-btn--sm {
    height: 32px;
    padding: 0 var(--eii-space-3);
    font-size: var(--eii-text-sm);
    border-radius: var(--eii-radius-md);
}
```

- [ ] **Step 13: Verificar build**

```
dotnet build --no-restore -v quiet 2>&1 | grep "error CS"
```
Salida esperada: ninguna línea.

- [ ] **Step 14: Commit**

```bash
git add eiibd26/Pages/Glosario/Termino.cshtml eiibd26/wwwroot/css/eiibd-components.css
git commit -m "feat: termino.cshtml — eii-page-header, tipo-pill BEM, tokens, eii-btn, eliminar inline styles"
```

---

## Task 5 — Documentación: 03-estandar-editorial.html

**Files:**
- Create: `Documentation/ux-editorial/03-estandar-editorial.html`

- [ ] **Step 1: Crear 03-estandar-editorial.html** con el estándar visual editorial resultante, mostrando ejemplos en vivo de cada componente (sin depender de archivos externos).

El archivo es un HTML self-contained que demuestra visualmente el estándar. Estructura principal:

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <title>03 — Estándar Editorial EIIBD</title>
  <style>
    /* tokens inline */
    :root {
      --eii-primary:#6a4e7a; --eii-primary-hover:#5a3d69;
      --eii-primary-soft:#f5eefb; --eii-primary-border:#d4b8e6;
      --eii-success:#16a34a; --eii-success-soft:#f0fdf4; --eii-success-border:#86efac;
      --eii-text:#111827; --eii-text-heading:#172849; --eii-text-soft:#6b7280;
      --eii-border:#e5e7eb; --eii-border-soft:#f3f4f6; --eii-surface:#ffffff;
      --eii-surface-soft:#f9fafb;
      --eii-space-2:8px; --eii-space-3:12px; --eii-space-4:16px;
      --eii-space-5:20px; --eii-space-6:24px;
      --eii-radius-md:8px; --eii-radius-lg:12px; --eii-radius-xl:16px; --eii-radius-full:9999px;
      --eii-shadow-md:0 4px 12px rgba(0,0,0,.08),0 2px 4px rgba(0,0,0,.04);
      --eii-fw-semibold:600; --eii-fw-medium:500; --eii-fw-normal:400;
      --eii-text-sm:.875rem; --eii-text-base:1rem; --eii-text-md:1.125rem;
      --eii-text-lg:1.25rem; --eii-text-xl:1.5rem;
      --eii-leading-tight:1.25;
    }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif; max-width: 900px; margin: 0 auto; padding: 2rem; color: var(--eii-text); }
    
    /* Replicated components for demo */
    .eii-page-header { display:flex; align-items:flex-start; justify-content:space-between; gap:var(--eii-space-4); margin-bottom:var(--eii-space-6); flex-wrap:wrap; }
    .eii-page-title { font-size:2.1rem; font-weight:300; color:var(--eii-text-heading); letter-spacing:-.06em; line-height:var(--eii-leading-tight); padding-bottom:20px; margin:0; }
    .editorial-section-h2 { font-size:var(--eii-text-xl); font-weight:var(--eii-fw-medium); color:var(--eii-text-heading); margin:0 0 var(--eii-space-3) 0; line-height:var(--eii-leading-tight); }
    .termino-tipo-pill { display:inline-flex; align-items:center; font-size:.72rem; font-weight:600; padding:2px 10px; border-radius:var(--eii-radius-full); letter-spacing:.02em; vertical-align:middle; white-space:nowrap; }
    .termino-tipo-pill--sintoma { background:var(--eii-primary-soft); color:var(--eii-primary); border:1px solid var(--eii-primary-border); }
    .termino-tipo-pill--tratamiento { background:var(--eii-success-soft); color:var(--eii-success); border:1px solid var(--eii-success-border); }
    .aviso-termino-box { background:var(--eii-primary-soft); border:1px solid var(--eii-primary-border); border-radius:var(--eii-radius-md); padding:var(--eii-space-4); margin-bottom:var(--eii-space-5); }
    .aviso-termino-box .aviso-title { color:var(--eii-primary); font-weight:var(--eii-fw-semibold); font-size:var(--eii-text-md); margin:0 0 var(--eii-space-2) 0; }
    .aviso-termino-box .aviso-text { color:var(--eii-primary-hover); font-size:var(--eii-text-sm); margin:0; }
    .sidebar-section { background:var(--eii-surface); border:1px solid var(--eii-border-soft); border-radius:var(--eii-radius-xl); margin-bottom:var(--eii-space-6); overflow:hidden; }
    .sidebar-section-title { font-size:var(--eii-text-lg); font-weight:var(--eii-fw-semibold); color:#0f1419; margin:0; }
    .sidebar-section-header-static { padding:var(--eii-space-4); }
    .sidebar-section-content-static { padding:0 var(--eii-space-4) var(--eii-space-4); }
    .eii-btn { display:inline-flex; align-items:center; justify-content:center; gap:6px; padding:0 var(--eii-space-4); height:40px; border-radius:var(--eii-radius-md); font-size:var(--eii-text-base); font-weight:var(--eii-fw-medium); cursor:pointer; border:1.5px solid transparent; text-decoration:none; transition:all 150ms ease; }
    .eii-btn--solid { background:var(--eii-primary); color:#fff; border-color:var(--eii-primary); }
    .eii-btn--primary { background:#fff; color:var(--eii-primary); border-color:var(--eii-primary-border); }
    .eii-btn--sm { height:32px; padding:0 var(--eii-space-3); font-size:var(--eii-text-sm); }
    
    /* Doc styles */
    .sg-section { margin: 3rem 0; }
    .sg-section h2 { font-size:1rem; font-weight:600; border-bottom:1px solid var(--eii-border); padding-bottom:.5rem; margin-bottom:1.5rem; }
    .demo-box { background:var(--eii-surface-soft); border:1px solid var(--eii-border); border-radius:var(--eii-radius-md); padding:1.5rem; margin-bottom:1rem; }
    .demo-label { font-size:.75rem; color:var(--eii-text-soft); margin-bottom:.5rem; font-weight:600; text-transform:uppercase; letter-spacing:.04em; }
    .breadcrumbs { font-size:.875rem; color:var(--eii-text-soft); margin-bottom:var(--eii-space-4); }
    .breadcrumbs a { color:var(--eii-text); text-decoration:none; }
    .breadcrumbs .sep { margin:0 8px; color:var(--eii-text-soft); }
  </style>
</head>
<body>

<h1 style="font-size:1.5rem;font-weight:300;letter-spacing:-.04em;margin-bottom:.25rem;">Estándar Editorial EIIBD</h1>
<p style="color:var(--eii-text-soft);font-size:.875rem;margin:0 0 3rem;">Sistema visual unificado — Blog + Glosario · Sesión 2026-05-26</p>

<!-- H1 BLOG -->
<div class="sg-section">
  <h2>H1 — Título de artículo (Blog)</h2>
  <div class="demo-box">
    <div class="demo-label">eii-page-header > eii-page-header__content > h1.eii-page-title</div>
    <div class="eii-page-header">
      <div>
        <h1 class="eii-page-title">Colitis Ulcerosa: síntomas, diagnóstico y tratamiento</h1>
      </div>
    </div>
    <div class="breadcrumbs">
      <a href="#">Inicio</a><span class="sep">›</span>
      <a href="#">Info y Ayuda</a><span class="sep">›</span>
      <span>Colitis Ulcerosa</span>
    </div>
  </div>
  <code style="font-size:.8rem;">font-size: 2.1rem · font-weight: 300 · color: --eii-text-heading · letter-spacing: -0.06em</code>
</div>

<!-- H1 TERMINO -->
<div class="sg-section">
  <h2>H1 — Título de término (Glosario)</h2>
  <div class="demo-box">
    <div class="demo-label">eii-page-header > eii-page-header__content > termino-title-row</div>
    <div class="eii-page-header">
      <div>
        <div style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;">
          <h1 class="eii-page-title">Dolor abdominal</h1>
          <span class="termino-tipo-pill termino-tipo-pill--sintoma">Síntoma</span>
        </div>
      </div>
    </div>
    <div class="eii-page-header" style="margin-top:1rem;">
      <div>
        <div style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;">
          <h1 class="eii-page-title">Mesalazina</h1>
          <span class="termino-tipo-pill termino-tipo-pill--tratamiento">Tratamiento</span>
        </div>
      </div>
    </div>
  </div>
  <code style="font-size:.8rem;">Mismo H1. Pills: sintoma=primary-soft/primary, tratamiento=success-soft/success</code>
</div>

<!-- H2 SECCIONES -->
<div class="sg-section">
  <h2>H2 — Secciones editoriales (Termino)</h2>
  <div class="demo-box">
    <div class="demo-label">.editorial-section-h2</div>
    <h2 class="editorial-section-h2">¿Qué es y cómo se siente?</h2>
    <h2 class="editorial-section-h2">Relación con EII</h2>
    <h2 class="editorial-section-h2">Comunidad</h2>
  </div>
  <code style="font-size:.8rem;">font-size: --eii-text-xl (1.5rem) · font-weight: --eii-fw-medium (500) · color: --eii-text-heading</code>
</div>

<!-- AVISO MÉDICO -->
<div class="sg-section">
  <h2>Aviso médico</h2>
  <div class="demo-box">
    <div class="demo-label">.aviso-termino-box</div>
    <div class="aviso-termino-box">
      <p class="aviso-title">Aviso Importante</p>
      <p class="aviso-text">Esta información es educativa y no reemplaza el consejo de un médico profesional. <strong>Siempre consulta con tu médico</strong> sobre tu condición y tratamiento.</p>
    </div>
  </div>
</div>

<!-- SIDEBAR -->
<div class="sg-section">
  <h2>Sidebar — Card estático</h2>
  <div class="demo-box" style="max-width:280px;">
    <div class="demo-label">.sidebar-section.sidebar-static</div>
    <div class="sidebar-section">
      <div class="sidebar-section-header-static">
        <span class="sidebar-section-title">Calificar artículo</span>
      </div>
      <div class="sidebar-section-content-static">
        <p style="font-size:.875rem;color:var(--eii-text-soft);margin:0;">Título: font-size 1.25rem · font-weight 600 (antes 800)</p>
      </div>
    </div>
  </div>
  <code style="font-size:.8rem;">font-weight: --eii-fw-semibold (600) · font-size: --eii-text-lg (1.25rem)</code>
</div>

<!-- BOTONES -->
<div class="sg-section">
  <h2>Botones — Comunidad</h2>
  <div class="demo-box">
    <div class="demo-label">eii-btn (reemplaza btn-purple / btn-outline-purple)</div>
    <div style="display:flex;gap:1rem;flex-wrap:wrap;">
      <a class="eii-btn eii-btn--solid eii-btn--sm" href="#">
        ＋ Crear cuenta gratis
      </a>
      <a class="eii-btn eii-btn--primary eii-btn--sm" href="#">
        Iniciar sesión
      </a>
    </div>
  </div>
</div>

</body>
</html>
```

- [ ] **Step 2: Commit**

```bash
git add Documentation/ux-editorial/03-estandar-editorial.html
git commit -m "docs: estándar editorial visual — demos de H1, sidebar, botones, aviso"
```

---

## Task 6 — Playwright: validación visual + 05-validacion.html

**Files:**
- Create: `Documentation/ux-editorial/05-validacion-playwright.html`

Prerequisito: servidor corriendo en `https://localhost:7xxx` o puerto configurado.

- [ ] **Step 1: Navegar a un artículo real**

Usar Playwright MCP para:
1. Abrir el servidor local
2. Navegar a `/Contenidos` → clic en el primer artículo
3. Tomar screenshot de la zona del H1 + metadata
4. Tomar screenshot del sidebar
5. Inspeccionar computed styles de `.eii-page-title`: verificar `font-size ≈ 2.1rem`, `font-weight ≈ 300`
6. Verificar que `.page-title h1` ya no anula (`font-size` debe ser 2.1rem, no 3rem)

- [ ] **Step 2: Navegar a un término real**

1. Ir a `/Glosario` → clic en un término (síntoma)
2. Verificar H1: mismo tamaño/peso que el artículo
3. Verificar pill tipo-pill: fondo lila + texto morado (sintoma)
4. Ir a un tratamiento: verificar pill verde
5. Verificar secciones H2 (`.editorial-section-h2`): font-weight 500
6. Verificar sidebar-section-title: font-weight 600 (no 800)
7. Verificar botón "Crear cuenta" usa estilos eii-btn

- [ ] **Step 3: Crear 05-validacion-playwright.html** con resultados de validación.

El archivo incluye capturas embebidas como `<img>` si las screenshots están disponibles, o descripción textual de los resultados observados.

- [ ] **Step 4: Commit**

```bash
git add Documentation/ux-editorial/05-validacion-playwright.html
git commit -m "docs: validación playwright — editorial blog + termino"
```

---

## Task 7 — Documentación restante (04, 06) + Style Guide

**Files:**
- Create: `Documentation/ux-editorial/04-implementacion.html`
- Create: `Documentation/ux-editorial/06-resumen-final.html`
- Modify: `Documentation/style-guide.html`

- [ ] **Step 1: Crear 04-implementacion.html**

HTML que lista todos los cambios implementados: archivo, línea, antes/después.
Estructura de tabla con columnas: Archivo | Selector | Antes | Después | Motivo.

- [ ] **Step 2: Crear 06-resumen-final.html**

HTML que resume: objetivos cumplidos, métricas de consistencia (antes: N hardcodes → después: M), y checklist de validación.

- [ ] **Step 3: Actualizar style-guide.html — agregar sección 12: EDITORIAL SYSTEM**

Localizar el cierre del bloque `</main>` en `Documentation/style-guide.html` (línea ~1176). Insertar antes del cierre `</main>`:

```html
<!-- ══ 12. EDITORIAL SYSTEM ═══════════════════════════════════════════════ -->
<section class="sg-section" id="editorial">
  <div class="sg-section-header">
    <span class="sg-section-num">12</span>
    <h2>Editorial System</h2>
  </div>

  <p style="color:var(--eii-text-soft);font-size:.9rem;margin-bottom:1.5rem;">
    Sistema visual unificado para <strong>Blog (Detalle.cshtml)</strong> y <strong>Glosario (Termino.cshtml)</strong>.
    Ambos módulos comparten exactamente el mismo lenguaje visual.
  </p>

  <!-- H1 BLOG Y TERMINO -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">H1 — Título editorial (Blog + Termino)</h3>
  <div style="background:var(--eii-surface-soft);border:1px solid var(--eii-border);border-radius:var(--eii-radius-md);padding:1.25rem;margin-bottom:.75rem;">
    <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;margin-bottom:1rem;flex-wrap:wrap;">
      <div>
        <h1 style="font-size:2.1rem;font-weight:300;color:var(--eii-text-heading);letter-spacing:-.06em;line-height:1.25;margin:0 0 .5rem;">Título de artículo de ejemplo</h1>
      </div>
    </div>
    <div style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;">
      <h1 style="font-size:2.1rem;font-weight:300;color:var(--eii-text-heading);letter-spacing:-.06em;line-height:1.25;margin:0;">Dolor abdominal</h1>
      <span style="display:inline-flex;align-items:center;font-size:.72rem;font-weight:600;padding:2px 10px;border-radius:9999px;background:var(--eii-primary-soft);color:var(--eii-primary);border:1px solid var(--eii-primary-border);">Síntoma</span>
    </div>
  </div>
  <code style="font-size:.78rem;">.eii-page-title — font-size:2.1rem · font-weight:300 · color:--eii-text-heading · letter-spacing:-0.06em</code>

  <!-- H2 SECCIONES -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">H2 — Secciones editoriales</h3>
  <div style="background:var(--eii-surface-soft);border:1px solid var(--eii-border);border-radius:var(--eii-radius-md);padding:1.25rem;margin-bottom:.75rem;">
    <h2 style="font-size:1.5rem;font-weight:500;color:var(--eii-text-heading);margin:0 0 .75rem;line-height:1.25;">¿Qué es y cómo se siente?</h2>
    <h2 style="font-size:1.5rem;font-weight:500;color:var(--eii-text-heading);margin:0;line-height:1.25;">Relación con EII</h2>
  </div>
  <code style="font-size:.78rem;">.editorial-section-h2 — font-size:--eii-text-xl · font-weight:--eii-fw-medium (500) · color:--eii-text-heading</code>

  <!-- PILLS TIPO -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">Pills de tipo (Glosario)</h3>
  <div style="background:var(--eii-surface-soft);border:1px solid var(--eii-border);border-radius:var(--eii-radius-md);padding:1.25rem;margin-bottom:.75rem;display:flex;gap:1rem;flex-wrap:wrap;">
    <span style="display:inline-flex;align-items:center;font-size:.72rem;font-weight:600;padding:2px 10px;border-radius:9999px;background:var(--eii-primary-soft);color:var(--eii-primary);border:1px solid var(--eii-primary-border);">Síntoma</span>
    <span style="display:inline-flex;align-items:center;font-size:.72rem;font-weight:600;padding:2px 10px;border-radius:9999px;background:var(--eii-success-soft);color:var(--eii-success);border:1px solid var(--eii-success-border);">Tratamiento</span>
  </div>
  <code style="font-size:.78rem;">.termino-tipo-pill--sintoma / --tratamiento · BEM modifiers · sin inline styles</code>

  <!-- AVISO MÉDICO -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">Aviso médico</h3>
  <div style="background:var(--eii-surface-soft);border:1px solid var(--eii-border);border-radius:var(--eii-radius-md);padding:1.25rem;margin-bottom:.75rem;">
    <div style="background:var(--eii-primary-soft);border:1px solid var(--eii-primary-border);border-radius:var(--eii-radius-md);padding:var(--eii-space-4);">
      <p style="color:var(--eii-primary);font-weight:600;font-size:1.125rem;margin:0 0 8px;">Aviso Importante</p>
      <p style="color:var(--eii-primary-hover);font-size:.875rem;margin:0;">Esta información es educativa y no reemplaza el consejo de un médico profesional.</p>
    </div>
  </div>
  <code style="font-size:.78rem;">.aviso-termino-box · bg:--eii-primary-soft · border:--eii-primary-border · text:--eii-primary-hover</code>

  <!-- SIDEBAR TITLE -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">Sidebar — Título de sección</h3>
  <div style="background:var(--eii-surface-soft);border:1px solid var(--eii-border);border-radius:var(--eii-radius-md);padding:1.25rem;margin-bottom:.75rem;">
    <div style="background:var(--eii-surface);border:1px solid var(--eii-border-soft);border-radius:var(--eii-radius-xl);overflow:hidden;max-width:280px;">
      <div style="padding:1rem;">
        <span style="font-size:1.25rem;font-weight:600;color:#0f1419;">Calificar artículo</span>
      </div>
    </div>
  </div>
  <code style="font-size:.78rem;">.sidebar-section-title · font-weight:--eii-fw-semibold (600) · antes era 800 — reducido</code>

  <!-- REGLAS EDITORIALES -->
  <h3 style="font-size:.9rem;font-weight:700;margin:1.5rem 0 .75rem;">Reglas del sistema editorial</h3>
  <ul style="font-size:.875rem;line-height:2;color:var(--eii-text);">
    <li>Blog H1 y Termino H1 comparten <strong>exactamente</strong> el mismo selector: <code>.eii-page-title</code></li>
    <li>Ambos usan el wrapper <code>.eii-page-header > .eii-page-header__content</code></li>
    <li>Las H2 de secciones de Termino usan <code>.editorial-section-h2</code> (NO inline styles)</li>
    <li>Los pills de tipo usan clases BEM: <code>.termino-tipo-pill--sintoma</code> / <code>--tratamiento</code></li>
    <li>Todos los botones de comunidad usan <code>.eii-btn</code> (NO .btn-purple)</li>
    <li>Thumbnails de sidebar usan <code>.sidebar-thumb</code> (NO inline style)</li>
    <li>Aviso médico usa tokens: <code>--eii-primary-soft</code>, <code>--eii-primary-border</code></li>
    <li>Sidebar section title: <code>font-weight: var(--eii-fw-semibold)</code> — nunca 800</li>
  </ul>
</section>
```

Además actualizar el `<footer>` del style-guide para reflejar la versión actualizada:
```html
EIIBD Design System — Manual de Estilos v1.1 &nbsp;·&nbsp; Actualizado 2026-05-26 &nbsp;·&nbsp; ...
```

- [ ] **Step 4: Verificar build**

```
dotnet build --no-restore -v quiet 2>&1 | grep "error CS"
```

- [ ] **Step 5: Commit final**

```bash
git add Documentation/ux-editorial/ Documentation/style-guide.html
git commit -m "docs: editorial system — 04/06 implementacion/resumen, style-guide sección 12"
```

---

## Self-Review

### Cobertura del spec

| Requisito | Task |
|---|---|
| Fase 1 — Auditoría visual | Task 1 |
| Fase 2 — Inventario CSS | Task 2 |
| Fase 3 — Estándar editorial (H1, metadata, contenido, sidebars, botones, badges, espaciado, radios, sombras) | Tasks 3+4+5 |
| Fase 4 — Implementación | Tasks 3+4 |
| Fase 5 — Style guide | Task 7 Step 3 |
| Fase 6 — Validación Playwright | Task 6 |
| H1 Blog = H1 Termino | Tasks 3+4 |
| Sidebar title sin bold agresiva | Task 3 Step 5 |
| Eliminar inline styles | Task 4 |
| Badges token-ificados | Tasks 3+4 |
| Botones unificados | Task 4 Steps 7-11 |
| Sombras via token | Task 3 Steps 6,13 |
| Radios via token | Task 3 Steps 4,6,7,8,9,10,11 |

### Sin placeholders — verificado ✓
Todos los steps tienen código CSS/HTML completo.

### Consistencia de tipos — verificada ✓
- `eii-btn--sm` se verifica que existe antes de usarlo (Task 4 Step 12)
- `editorial-section-h2` se define en detalle.css (Task 3 Step 14) antes de usarse en Termino.cshtml (Task 4 Steps 4-6)
- `sidebar-thumb` se define en detalle.css (Task 3 Step 15) antes de usarse en Termino.cshtml (Task 4 Step 10)
- Modificadores BEM se definen en Termino inline `<style>` (Task 4 Step 2) y se usan en el HTML del mismo archivo (Task 4 Step 1)
