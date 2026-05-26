# 01 – Análisis: Friendly URLs rotas en módulo Términos

## Fecha
2026-05-25

## Alcance
Módulo afectado: `Pages/Glosario/Termino.cshtml` y sus secciones de contenido relacionado.

---

## Pantallas afectadas

| Pantalla | Descripción |
|---|---|
| **Término** | Página de detalle del término del glosario (`/Termino/{slug}`) |
| **Artículos relacionados** | Sección de tabs dentro de Termino.cshtml y sidebar |
| **Preguntas relacionadas** | Sección de tabs dentro de Termino.cshtml y sidebar |

---

## Archivos relevantes identificados

### Página principal
| Archivo | Rol |
|---|---|
| `Pages/Glosario/Termino.cshtml` | Vista principal del término, genera los links |
| `Pages/Glosario/Termino.cshtml.cs` | PageModel, resuelve el término por slug |

### Servicio
| Archivo | Rol |
|---|---|
| `Services/Glossary/GlossaryService.cs` | Orquesta la carga del término, artículos y preguntas |
| `Services/Glossary/IGlossaryService.cs` | Interfaz del servicio |

### DTOs
| Archivo | Rol |
|---|---|
| `Services/Glossary/DTOs/GlossaryTermDetailDto.cs` | DTO del detalle del término (contiene listas de artículos y preguntas) |
| `Services/Glossary/DTOs/RelatedContentDto.cs` | DTO para artículos relacionados (tiene campo `Slug`) |
| `Services/Glossary/DTOs/RelatedQuestionDto.cs` | DTO para preguntas relacionadas (tiene campo `Slug`) |

### Helper de slugs
| Archivo | Rol |
|---|---|
| `Helpers/SlugHelper.cs` | Única fuente de verdad para generación de slugs |

### Modelos fuente
| Archivo | Campo Slug |
|---|---|
| `Models/Glossary/GlossaryTerm.cs` | `public string Slug { get; set; }` |
| `Models/Pregunta.cs` | `public string Slug { get; set; }` |
| `Models/Contenido.cs` | `public string? ContenidoTituloSlug { get; set; }` |

### Páginas de destino (resolución de ruta)
| Archivo | Directiva `@page` | Ruta resultante |
|---|---|---|
| `Pages/Contenidos/Detalle.cshtml` | `@page "{slug?}"` | `/Contenidos/Detalle/{slug}` → **Ambigua** |
| `Pages/Preguntas/Detalles.cshtml` | `@page "/Preguntas/Detalles/{slug?}"` | `/Preguntas/Detalles/{slug}` |
| `Pages/Glosario/Termino.cshtml` | `@page "/Termino/{slug}"` | `/Termino/{slug}` |

---

## Diagnóstico: ¿Dónde se rompen los links?

### 1. Artículos relacionados – ruta hardcodeada incorrecta

**Líneas 750 y 1200 de `Termino.cshtml`:**
```html
<a href="/Contenidos/@articulo.Slug" ...>
```

**Problema:**  
La página de destino `Pages/Contenidos/Detalle.cshtml` tiene `@page "{slug?}"`, que Razor Pages traduce a la ruta convencional `/Contenidos/Detalle/{slug}`.  
La URL generada `/Contenidos/{slug}` **no existe** → 404.

**Raíz del error:** La ruta hardcodeada `/Contenidos/` apunta a la carpeta, no a la página `Detalle`. La ruta correcta es `/Contenidos/Detalle/{slug}`.

---

### 2. Preguntas relacionadas – ruta hardcodeada inconsistente

**Líneas 786 y 1169 de `Termino.cshtml`:**
```html
<a href="/Preguntas/@preg.Slug" ...>
```

**Problema:**  
La página de destino `Pages/Preguntas/Detalles.cshtml` tiene `@page "/Preguntas/Detalles/{slug?}"`, resultando en `/Preguntas/Detalles/{slug}`.  
La URL generada `/Preguntas/{slug}` **no existe** → 404.  
`Pages/Preguntas.cshtml` (`@page`) es la lista de preguntas, no el detalle de una pregunta.

**Raíz del error:** La ruta hardcodeada omite el segmento `/Detalles/` del path de la página de detalle.

---

### 3. Término – funciona correctamente

La página `Pages/Glosario/Termino.cshtml` (`@page "/Termino/{slug}"`) resuelve correctamente a `/Termino/{slug}` y el PageModel usa `_glossaryService.GetTermBySlugAsync(slug)`. **Sin problemas aquí.**

---

### 4. Slugs con tildes / caracteres especiales – no hay problema en generación

`SlugHelper.GenerateSlug` normaliza Unicode y elimina diacríticos. Ejemplo:
- `"Colitis Ulcerosa"` → `"colitis-ulcerosa"` ✅
- `"Crohn y Colitis"` → `"crohn-y-colitis"` ✅
- `"Mesalazina"` → `"mesalazina"` ✅

El slug almacenado en base de datos es correcto. El fallo es exclusivamente en la construcción del href.

---

### 5. Slug vacío/nulo en artículos CMS

En `GlossaryService.GetRelatedContentsAsync`:
```csharp
Slug = c.ContenidoTituloSlug ?? "",
```

Si `ContenidoTituloSlug` es nulo o vacío en la base de datos, el link generado será `/Contenidos/Detalle/` (sin slug), lo cual puede producir resultados inesperados. **Riesgo secundario identificado.**

---

## Resumen de problemas

| # | Problema | Archivo | Línea(s) | Gravedad |
|---|---|---|---|---|
| 1 | Ruta artículo apunta a `/Contenidos/{slug}` en vez de `/Contenidos/Detalle/{slug}` | `Termino.cshtml` | 750, 1200 | 🔴 Crítico |
| 2 | Ruta pregunta apunta a `/Preguntas/{slug}` en vez de `/Preguntas/Detalles/{slug}` | `Termino.cshtml` | 786, 1169 | 🔴 Crítico |
| 3 | Slug vacío en artículo CMS no se guarda → link roto silencioso | `GlossaryService.cs` | 548 | 🟡 Medio |
