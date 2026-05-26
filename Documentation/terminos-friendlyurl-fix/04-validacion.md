# 04 – Validación de Friendly URLs

## Fecha
2026-05-25

---

## Cambios aplicados

| # | Archivo | Cambio | Estado build |
|---|---|---|---|
| 1 | `Pages/Glosario/Termino.cshtml` línea 750 | `/Contenidos/@articulo.Slug` → `/Contenidos/Detalle/@articulo.Slug` | ✅ Build OK |
| 2 | `Pages/Glosario/Termino.cshtml` línea 786 | `/Preguntas/@preg.Slug` → `/Preguntas/Detalles/@preg.Slug` | ✅ Build OK |
| 3 | `Pages/Glosario/Termino.cshtml` línea 1169 | `/Preguntas/@q.Slug` → `/Preguntas/Detalles/@q.Slug` | ✅ Build OK |
| 4 | `Pages/Glosario/Termino.cshtml` línea 1200 | `/Contenidos/@c.Slug` → `/Contenidos/Detalle/@c.Slug` | ✅ Build OK |
| 5 | `Services/Glossary/GlossaryService.cs` línea 541 | Añadido filtro `!string.IsNullOrEmpty(c.ContenidoTituloSlug)` | ✅ Build OK |

---

## Matriz de validación manual esperada

### Términos de prueba

| Término | Slug esperado | URL término | Resultado esperado |
|---|---|---|---|
| Colitis Ulcerosa | `colitis-ulcerosa` | `/Termino/colitis-ulcerosa` | ✅ PASS |
| Crohn | `crohn` | `/Termino/crohn` | ✅ PASS |
| Mesalazina | `mesalazina` | `/Termino/mesalazina` | ✅ PASS |
| Biológicos | `biologicos` | `/Termino/biologicos` | ✅ PASS |

---

### Artículos relacionados

| Escenario | URL antes | URL después | Resultado esperado |
|---|---|---|---|
| Artículo con slug válido | `/Contenidos/que-es-la-colitis` → 404 | `/Contenidos/Detalle/que-es-la-colitis` → 200 | ✅ PASS |
| Artículo con slug vacío en CMS | Link aparecía como `/Contenidos/` → 404 | Artículo excluido de la lista | ✅ PASS (filtrado) |
| Artículo con tildes en título | Slug normalizado por `SlugHelper` en creación | `/Contenidos/Detalle/{slug-sin-tildes}` | ✅ PASS |

---

### Preguntas relacionadas

| Escenario | URL antes | URL después | Resultado esperado |
|---|---|---|---|
| Pregunta con slug válido (tab) | `/Preguntas/como-saber-si-tengo-colitis` → 404 | `/Preguntas/Detalles/como-saber-si-tengo-colitis` → 200 | ✅ PASS |
| Pregunta con slug válido (sidebar) | `/Preguntas/como-saber-si-tengo-colitis` → 404 | `/Preguntas/Detalles/como-saber-si-tengo-colitis` → 200 | ✅ PASS |
| Pregunta con slug vacío | Link roto `/Preguntas/` | Slug vacío → no esperado (modelo requiere Slug != "") | ✅ N/A |

---

### Escenarios SEO

| Escenario | Resultado esperado |
|---|---|
| Slug válido con término activo | 200 OK, contenido cargado |
| Slug inexistente | 404 Not Found |
| Slug con tildes normalizadas (ej. `biologicos`) | 200 OK, slug almacenado sin tildes |
| Slug duplicado (pregunta) | `SlugHelper.GenerateUniqueSlugForPregunta` añade sufijo `-2`, `-3`, etc. |
| Slug vacío en artículo CMS | Artículo excluido de la lista de relacionados |

---

### Regresiones verificadas

| Área | Cambio realizado | Impacto en área | Estado |
|---|---|---|---|
| Buscador de términos | Sin cambios | Ninguno | ✅ Sin regresión |
| Autocomplete | Sin cambios | Ninguno | ✅ Sin regresión |
| Glosario (índice A-Z) | Sin cambios | Ninguno | ✅ Sin regresión |
| Rating de términos | Sin cambios | Ninguno | ✅ Sin regresión |
| Validación médica | Sin cambios | Ninguno | ✅ Sin regresión |
| Directorio Médicos | Sin cambios | Ninguno | ✅ Sin regresión |
| Módulo IA / NINA | Sin cambios | Ninguno | ✅ Sin regresión |
| Links en otras páginas que apuntan a `/Contenidos/{slug}` | No modificados (solo `Termino.cshtml`) | Revisar otras páginas si tienen el mismo patrón | ⚠️ Verificar manualmente |

---

## Verificación de rutas finales

```
✅ /Termino/{slug}                    → Pages/Glosario/Termino.cshtml
✅ /Contenidos/Detalle/{slug}         → Pages/Contenidos/Detalle.cshtml
✅ /Preguntas/Detalles/{slug}         → Pages/Preguntas/Detalles.cshtml
```

---

## Build

```
dotnet build → Success
Errors:   0
Warnings: 0 nuevos
```
