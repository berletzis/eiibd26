# DirectorioMedicos — Alineación visual al estándar de plataforma

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Alinear las 3 páginas de DirectorioMedicos al estándar visual/estructural de `/Identity/Usuario/usuarioPreguntasRespuestas`, habilitando el sidebar izquierdo y agregando la opción "Médicos" al menú.

**Architecture:** El layout (`Pages/Shared/_Layout.cshtml`) excluía explícitamente DirectorioMedicos del sidebar. La solución es: (1) eliminar esa exclusión, (2) agregar la entrada "Médicos" a ambos sidebars, (3) reemplazar el wrapper `conte-detail`/`detail-grid` (patrón página pública) por `container py-4` (patrón panel autenticado) en las 3 páginas, manteniendo toda la funcionalidad.

**Tech Stack:** ASP.NET Core 8 Razor Pages, Bootstrap 5, CSS custom (directorio-medicos.css, detalle.css, site.css)

---

## Análisis del patrón de referencia (`usuarioPreguntasRespuestas`)

| Elemento | Patrón de referencia | Patrón actual DirectorioMedicos |
|---|---|---|
| Wrapper externo | `<div class="container py-4">` | `<div class="conte-detail">` |
| Layout de contenido | Una sola columna o `.qp-wrapper-flex` | `detail-grid` (2 cols: content + right-panel) |
| Header de sección | `.crm-card` con `fs-4` + `.crm-label` | `.page-title h1` con `se-subtitle` |
| Cards | `card shadow-sm` Bootstrap estándar | `card shadow-sm` Bootstrap — OK |
| Sidebar izquierdo | Via `_Layout.cshtml` (showSidebar=true) | Excluido explícitamente |
| Tipografía títulos | `mb-2 fs-4` + `font-weight:600; color:#172849` | `h1.h3` / `h1.h4` |

## Archivos modificados

| Archivo | Acción |
|---|---|
| `Pages/Shared/_Layout.cshtml` | Eliminar exclusión `isDirectorioPage` |
| `Pages/Shared/_SidebarMenu.cshtml` | Agregar ítem "Médicos" (Paciente block) |
| `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` | Agregar ítem "Médicos" (Paciente block) |
| `Pages/DirectorioMedicos/Index.cshtml` | Reemplazar wrapper + header card |
| `Pages/DirectorioMedicos/Proponer.cshtml` | Reemplazar structure a `container py-4` + row/col |
| `Pages/DirectorioMedicos/Detalle.cshtml` | Reemplazar structure a `container py-4` + row/col |

---

## Task 1: Habilitar sidebar para DirectorioMedicos en el Layout

**Files:**
- Modify: `Pages/Shared/_Layout.cshtml:247-248`

- [ ] **Step 1: Eliminar la exclusión de isDirectorioPage**

En `_Layout.cshtml`, la lógica actual es:
```csharp
var isDirectorioPage = currentPageNorm.StartsWith("/directorio");
var showSidebar = isAuthenticated && !isHomePage && !isPreguntasPage && !iscontenidosPage && !isMapaPage && !isGlosarioPage && !isPublicProfilePage && !isDirectorioPage;
```

Cambiar a:
```csharp
var showSidebar = isAuthenticated && !isHomePage && !isPreguntasPage && !iscontenidosPage && !isMapaPage && !isGlosarioPage && !isPublicProfilePage;
```
(Eliminar la línea `var isDirectorioPage` y el `&& !isDirectorioPage` del showSidebar)

- [ ] **Step 2: Commit**
```
git add Pages/Shared/_Layout.cshtml
git commit -m "feat: habilitar sidebar en páginas DirectorioMedicos"
```

---

## Task 2: Agregar "Médicos" al sidebar (ambos archivos)

**Files:**
- Modify: `Pages/Shared/_SidebarMenu.cshtml`
- Modify: `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`

En ambos archivos, dentro del bloque `@if (User.IsInRole("Paciente"))`, después del `<li>` de "Mis P&R":

- [ ] **Step 1: Agregar entrada "Médicos" en `Pages/Shared/_SidebarMenu.cshtml`**

```html
<li class="nav-item mb-1">
    <a href="/DirectorioMedicos"
       class="nav-link text-dark d-flex align-items-center @(IsStartingWith("/DirectorioMedicos") ? "active" : "")"
       style="gap:.5rem; font-size:.9rem; font-weight:400;">
        <span class="d-inline-flex align-items-center justify-content-center rounded border me-2" style="width:30px;height:30px; border-width:2px;">
            <i class="bi bi-hospital" style="color:@mustard; font-size:1.3em"></i>
        </span>
        <span>Médicos</span>
    </a>
</li>
```

- [ ] **Step 2: Misma entrada en `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`**

Mismo bloque HTML (idéntico).

- [ ] **Step 3: Commit**
```
git add Pages/Shared/_SidebarMenu.cshtml Areas/Identity/Pages/Shared/_SidebarMenu.cshtml
git commit -m "feat: agregar opción Médicos al sidebar de usuario"
```

---

## Task 3: Refactorizar Index.cshtml

**Files:**
- Modify: `Pages/DirectorioMedicos/Index.cshtml`

**Cambio:** Reemplazar `conte-detail` por `container py-4` y adaptar el header al patrón `.crm-card`.

- [ ] **Step 1: Reemplazar wrapper externo y header**

Reemplazar:
```html
<div class="conte-detail">
    <div class="page-title">
        <h1>Directorio de médicos</h1>
        <div class="se-subtitle">Médicos identificados por pacientes con EII en México</div>
    </div>
    <div class="alert alert-info mb-4" role="note">
        ...
    </div>
    <!-- ... filtros, grid, paginacion ... -->
</div>
```

Por:
```html
<div class="container py-4">
    <div class="crm-card mb-4" style="background:#f7f9fd !important; border-radius:18px; box-shadow:0 2px 12px rgb(180 193 239 / 12%); border:none; padding:1.15rem 1rem 1rem 1rem;">
        <div class="d-flex flex-wrap justify-content-between align-items-center">
            <div>
                <div class="mb-1 fs-4" style="font-weight:600; color:#172849;">Directorio de médicos</div>
                <div class="crm-label" style="font-size:1.05rem; color:#888ca0; font-weight:400;">Médicos identificados por pacientes con EII en México</div>
            </div>
            <a asp-page="/DirectorioMedicos/Proponer" class="btn btn-primary btn-sm mt-2 mt-sm-0">
                <i class="bi bi-plus-lg me-1"></i> Agregar médico
            </a>
        </div>
    </div>

    <div class="alert alert-info mb-4" role="note">
        <i class="bi bi-info-circle me-2"></i>
        Este directorio es construido por la comunidad EII. La información refleja experiencias
        reportadas por pacientes y <strong>no constituye una recomendación médica oficial.</strong>
    </div>

    <!-- Filtros, grid, paginación — sin cambios funcionales -->
    ...
</div>
```

Nota: El botón "Agregar médico" se sube al header card (se elimina la barra de acciones duplicada debajo de los filtros).

- [ ] **Step 2: Commit**
```
git add Pages/DirectorioMedicos/Index.cshtml
git commit -m "feat: alinear Index DirectorioMedicos al patrón container py-4 + crm-card"
```

---

## Task 4: Refactorizar Proponer.cshtml

**Files:**
- Modify: `Pages/DirectorioMedicos/Proponer.cshtml`

**Cambio:** Reemplazar `conte-detail`/`detail-grid`/`content-panel`/`right-panel` por `container py-4` + Bootstrap `row g-4` con `col-lg-8` (form) + `col-lg-4` (aside).

- [ ] **Step 1: Reemplazar estructura**

```html
<div class="container py-4">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-page="/DirectorioMedicos/Index">Directorio</a></li>
            <li class="breadcrumb-item active" aria-current="page">Agregar médico</li>
        </ol>
    </nav>

    <div class="row g-4">
        <div class="col-lg-8">
            <!-- header card + form (exactamente igual que antes) -->
        </div>
        <div class="col-lg-4">
            <!-- right-panel content (¿Cómo funciona?, Privacidad, Directorio) -->
        </div>
    </div>
</div>
```

- [ ] **Step 2: Commit**
```
git add Pages/DirectorioMedicos/Proponer.cshtml
git commit -m "feat: alinear Proponer DirectorioMedicos al patrón container py-4"
```

---

## Task 5: Refactorizar Detalle.cshtml

**Files:**
- Modify: `Pages/DirectorioMedicos/Detalle.cshtml`

**Cambio:** Igual que Proponer — `conte-detail`/`detail-grid` → `container py-4` + `row g-4` con `col-lg-8` + `col-lg-4`.

- [ ] **Step 1: Reemplazar estructura**

```html
<div class="container py-4">
    <nav aria-label="breadcrumb" class="mb-3">...</nav>
    
    <div class="row g-4">
        <div class="col-lg-8">
            <!-- tarjeta médico, confirmaciones, áreas, form confirmar -->
        </div>
        <div class="col-lg-4">
            <!-- ¿Conoces otro médico? + Aviso -->
        </div>
    </div>
</div>
```

- [ ] **Step 2: Commit**
```
git add Pages/DirectorioMedicos/Detalle.cshtml
git commit -m "feat: alinear Detalle DirectorioMedicos al patrón container py-4"
```

---

## Criterios de aceptación

- [ ] Las 3 páginas muestran sidebar izquierdo cuando el usuario está autenticado
- [ ] La opción "Médicos" aparece en el sidebar y navega a `/DirectorioMedicos`
- [ ] El header de Index usa el patrón `.crm-card` con tipografía `fs-4 fw-semibold`
- [ ] Proponer y Detalle usan `row g-4` con `col-lg-8`/`col-lg-4` en lugar de `detail-grid`
- [ ] Todos los filtros, formularios y acciones siguen funcionando
- [ ] No se rompe ninguna ruta existente
- [ ] Responsive: en móvil el sidebar va al offcanvas, el row se apila verticalmente
