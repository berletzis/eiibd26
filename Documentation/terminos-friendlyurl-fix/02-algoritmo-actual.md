# 02 – Algoritmo actual de Friendly URLs

## Fecha
2026-05-25

---

## Fuente única de generación de slugs

### `Helpers/SlugHelper.cs` – `GenerateSlug(string text)`

```
INPUT:  texto libre (nombre del término, título del artículo, título de la pregunta)
OUTPUT: slug SEO-friendly en minúsculas, solo a-z 0-9 y guiones, máx. 80 chars
```

**Pasos del algoritmo:**
1. Guard: si nulo/vacío → devuelve `"pregunta"` (fallback hardcodeado)
2. Normalización Unicode: `NormalizationForm.FormD`
3. Filtrado de diacríticos: elimina caracteres con categoría `NonSpacingMark`
4. Recomposición: `NormalizationForm.FormC`
5. Lowercase: `ToLowerInvariant()`
6. Trim de espacios extremos
7. Regex `[^a-z0-9\s-]` → elimina todo carácter no alfanumérico ni espacio ni guión
8. Regex `\s+` → reemplaza espacios por `-`
9. Regex `-+` → colapsa guiones múltiples a uno
10. `Trim('-')` → elimina guiones extremos
11. Límite a 80 caracteres, `TrimEnd('-')` para no cortar en guión
12. Guard final: si vacío → devuelve `"pregunta"` (fallback)

**Ejemplos verificados:**

| Entrada | Salida esperada | Salida real |
|---|---|---|
| `"Colitis Ulcerosa"` | `colitis-ulcerosa` | `colitis-ulcerosa` ✅ |
| `"Crohn y Colitis"` | `crohn-y-colitis` | `crohn-y-colitis` ✅ |
| `"Mesalazina"` | `mesalazina` | `mesalazina` ✅ |
| `"Biológicos"` | `biologicos` | `biologicos` ✅ |
| `"¿Cómo saber si tengo colitis?"` | `como-saber-si-tengo-colitis` | `como-saber-si-tengo-colitis` ✅ |
| `"Síndrome del intestino irritable"` | `sindrome-del-intestino-irritable` | `sindrome-del-intestino-irritable` ✅ |

---

## Almacenamiento de slugs

| Entidad | Modelo | Campo | ¿Siempre poblado? |
|---|---|---|---|
| Término del glosario | `GlossaryTerm` | `Slug` (`string`, NOT NULL default `""`) | Sí (requerido por constraint) |
| Pregunta | `Pregunta` | `Slug` (`string`, NOT NULL default `""`) | Sí (requerido) |
| Artículo CMS | `Contenido` | `ContenidoTituloSlug` (`string?`, nullable) | **No garantizado** – puede ser nulo |

---

## Unicidad de slugs

| Entidad | Mecanismo de unicidad |
|---|---|
| Pregunta | `SlugHelper.GenerateUniqueSlugForPregunta`: verifica colisiones en `db.Preguntas` y añade sufijo `-N` |
| Término (GlossaryTerm) | No se encontró método explícito de unicidad en `SlugHelper`; se asume unicidad por nombre único |
| Artículo CMS | No se encontró validación de unicidad; depende del CMS |

---

## Resolución de rutas (routing)

### Flujo de resolución actual

```
Request: /Termino/colitis-ulcerosa
  → Pages/Glosario/Termino.cshtml (@page "/Termino/{slug}")
  → Termino.cshtml.cs OnGetAsync(string slug)
  → GlossaryService.GetTermBySlugAsync("colitis-ulcerosa")
  → db.GlossaryTerms WHERE Slug == "colitis-ulcerosa" AND Activo == true
  → ✅ Funciona correctamente
```

```
Request: /Contenidos/que-es-la-colitis     ← URL generada HOY en Termino.cshtml
  → Razor Pages busca Pages/Contenidos/Index.cshtml o fallback
  → ❌ No existe → 404

URL CORRECTA esperada: /Contenidos/Detalle/que-es-la-colitis
  → Pages/Contenidos/Detalle.cshtml (@page "{slug?}")
  → ✅ Existe y resuelve por slug
```

```
Request: /Preguntas/como-saber-si-tengo-colitis     ← URL generada HOY en Termino.cshtml
  → Pages/Preguntas.cshtml (@page) → lista de preguntas, sin parámetro slug
  → ❌ El slug se ignora → no llega al detalle

URL CORRECTA esperada: /Preguntas/Detalles/como-saber-si-tengo-colitis
  → Pages/Preguntas/Detalles.cshtml (@page "/Preguntas/Detalles/{slug?}")
  → ✅ Existe y resuelve por slug
```

---

## Tabla de rutas definitiva

| Entidad | Ruta en `@page` | URL de acceso | Parámetro |
|---|---|---|---|
| Término | `/Termino/{slug}` | `/Termino/colitis-ulcerosa` | `slug` |
| Artículo (detalle) | `{slug?}` (convencional) | `/Contenidos/Detalle/que-es-la-colitis` | `slug` |
| Pregunta (detalle) | `/Preguntas/Detalles/{slug?}` | `/Preguntas/Detalles/como-saber-si-tengo-colitis` | `slug` |

---

## Dónde se generan los links hoy (hardcoded incorrecto)

```
// INCORRECTO – líneas 750, 1200
<a href="/Contenidos/@articulo.Slug">         → /Contenidos/{slug}         → 404

// INCORRECTO – líneas 786, 1169
<a href="/Preguntas/@preg.Slug">              → /Preguntas/{slug}           → 404
```

---

## Evaluación del algoritmo de generación

El algoritmo de `SlugHelper.GenerateSlug` **es correcto, robusto, y es la única fuente de verdad**.

**El problema NO está en la generación del slug.**  
**El problema está en la construcción del href en el template Razor.**

Los slugs se generan y almacenan correctamente. La URL se rompe en el momento de renderizar el link porque las rutas hardcodeadas en `Termino.cshtml` no coinciden con las directivas `@page` de las páginas de destino.
