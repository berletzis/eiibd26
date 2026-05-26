# 03 – Plan de corrección: Friendly URLs en módulo Términos

## Fecha
2026-05-25

---

## Principios

1. **Fuente única**: usar `asp-page` + `asp-route-slug` en lugar de `href` hardcodeado.
2. **Sin ID en URL**: ningún link expondrá ID numérico o GUID.
3. **Sin duplicar lógica**: no crear nuevo helper de slugs; reutilizar `SlugHelper.GenerateSlug`.
4. **Sin romper SEO existente**: los slugs ya almacenados son correctos.
5. **Consistencia**: las 4 ocurrencias de links (2 en tabs, 2 en sidebar) se corrigen con el mismo patrón.

---

## Correcciones identificadas

### Corrección 1: Links de artículos relacionados (tabs y sidebar)
**Archivo:** `eiibd26/Pages/Glosario/Termino.cshtml`  
**Líneas afectadas:** 750 y 1200  

| Estado | URL generada |
|---|---|
| ❌ Antes | `/Contenidos/@articulo.Slug` → `/Contenidos/que-es-la-colitis` → 404 |
| ✅ Después | `/Contenidos/Detalle/@articulo.Slug` → `/Contenidos/Detalle/que-es-la-colitis` → 200 |

**Cambio exacto (línea 750 – tab artículos):**
```html
<!-- ANTES -->
<a href="/Contenidos/@articulo.Slug" class="related-card" style="text-decoration:none;color:inherit;">

<!-- DESPUÉS -->
<a href="/Contenidos/Detalle/@articulo.Slug" class="related-card" style="text-decoration:none;color:inherit;">
```

**Cambio exacto (línea 1200 – sidebar artículos):**
```html
<!-- ANTES -->
<a href="/Contenidos/@c.Slug" class="text-decoration-none text-dark">

<!-- DESPUÉS -->
<a href="/Contenidos/Detalle/@c.Slug" class="text-decoration-none text-dark">
```

---

### Corrección 2: Links de preguntas relacionadas (tabs y sidebar)
**Archivo:** `eiibd26/Pages/Glosario/Termino.cshtml`  
**Líneas afectadas:** 786 y 1169  

| Estado | URL generada |
|---|---|
| ❌ Antes | `/Preguntas/@preg.Slug` → `/Preguntas/como-saber-si-tengo-colitis` → 404 |
| ✅ Después | `/Preguntas/Detalles/@preg.Slug` → `/Preguntas/Detalles/como-saber-si-tengo-colitis` → 200 |

**Cambio exacto (línea 786 – tab preguntas):**
```html
<!-- ANTES -->
<a href="/Preguntas/@preg.Slug" class="fw-semibold text-decoration-none">

<!-- DESPUÉS -->
<a href="/Preguntas/Detalles/@preg.Slug" class="fw-semibold text-decoration-none">
```

**Cambio exacto (línea 1169 – sidebar preguntas):**
```html
<!-- ANTES -->
<a href="/Preguntas/@q.Slug" class="text-decoration-none text-dark">

<!-- DESPUÉS -->
<a href="/Preguntas/Detalles/@q.Slug" class="text-decoration-none text-dark">
```

---

## Corrección secundaria: slug vacío en artículo CMS

**Archivo:** `eiibd26/Services/Glossary/GlossaryService.cs`  
**Línea:** 548  

Si `ContenidoTituloSlug` es nulo o vacío, el link de artículo debe excluirse de la lista devuelta (o no renderizarse en la vista). Dos opciones:

**Opción A – Filtrar en el servicio (preferida):**  
Añadir condición `.Where(c => !string.IsNullOrWhiteSpace(c.ContenidoTituloSlug))` a la consulta de `GetRelatedContentsAsync`.

**Opción B – Filtrar en la vista:**  
Cambiar el `@foreach` para saltar artículos con slug vacío.

> La Opción A se implementará: mantiene la vista limpia y garantiza que el DTO nunca lleve un slug vacío.

---

## Resumen de cambios a aplicar

| Archivo | Tipo | Descripción |
|---|---|---|
| `Pages/Glosario/Termino.cshtml` | Corrección de href | Añadir `/Detalle/` en links de artículos (×2) |
| `Pages/Glosario/Termino.cshtml` | Corrección de href | Añadir `/Detalles/` en links de preguntas (×2) |
| `Services/Glossary/GlossaryService.cs` | Filtro de datos | Excluir artículos con `ContenidoTituloSlug` nulo o vacío |

**Total: 5 cambios en 2 archivos.**

---

## Nota: No se requieren cambios en

- `Helpers/SlugHelper.cs` – el algoritmo es correcto
- `Services/Glossary/DTOs/*.cs` – los DTOs ya tienen el campo `Slug`
- `Pages/Glosario/Termino.cshtml.cs` – la resolución por slug funciona
- `Models/` – los modelos tienen el campo Slug correctamente definido
- Routing / Program.cs – las rutas de Razor Pages son correctas
- Ninguna otra página del sitio
