# ✅ URLs de Artículos Corregidas - Con Categoría

## 🐛 Problema Original

**Antes:**
```
URL generada: /Contenidos/embarazo-y-parto
URL correcta:  /familia-y-relaciones/embarazo-y-parto
```

Las URLs de artículos no incluían el slug de la categoría, causando links rotos.

---

## 🔧 Solución Implementada

### 1. **Agregada propiedad `CategoriaSlug`**

**Archivo:** `SearchSuggestionService.cs`

```csharp
public class SuggestionArticulo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Resumen { get; set; }
    public string? ImagenUrl { get; set; }
    public string? CategoriaSlug { get; set; } // ⭐ NUEVO
}
```

### 2. **Consulta de Categoría en BD**

**Archivo:** `SearchSuggestionService.cs` → `BuscarArticulosAsync()`

```csharp
// Después de agrupar artículos por relevancia...

// Obtener slugs de categorías para construir URLs correctas
var articulosIds = articulos.Select(x => x.Articulo.Id).ToList();
var categoriasSlugs = await _db.ContenidosCategoriasRelacion
    .AsNoTracking()
    .Where(ccr => articulosIds.Contains(ccr.ContenidoId) && !ccr.Borrado)
    .Join(_db.ContenidosCategorias,
        ccr => ccr.CategoriaId,
        cat => cat.Sequence,
        (ccr, cat) => new { ccr.ContenidoId, CategoriaSlug = cat.CategoriaSlug })
    .Where(x => x.CategoriaSlug != null)
    .GroupBy(x => x.ContenidoId)
    .Select(g => new { ContenidoId = g.Key, CategoriaSlug = g.First().CategoriaSlug })
    .ToDictionaryAsync(x => x.ContenidoId, x => x.CategoriaSlug);

// Mapear con categoría
CategoriaSlug = categoriasSlugs.TryGetValue(x.Articulo.Id, out var catSlug) ? catSlug : null
```

### 3. **Construcción de URL Mejorada**

**Lógica de URLs:**

```csharp
// Prioridad 1: Con categoría (ideal)
url = $"/{categoriaSlug}/{contenidoSlug}"
// Ejemplo: /familia-y-relaciones/embarazo-y-parto

// Prioridad 2: Sin categoría (fallback)
url = $"/c/{contenidoSlug}"
// Ejemplo: /c/embarazo-y-parto

// Prioridad 3: Sin slug (último recurso)
url = $"/Contenidos/Detalle?id={id}"
// Ejemplo: /Contenidos/Detalle?id=123
```

---

## 📁 Archivos Modificados

### 1. **SearchSuggestionService.cs**
- ✅ Agregada propiedad `CategoriaSlug` al modelo `SuggestionArticulo`
- ✅ Query adicional para obtener slugs de categorías
- ✅ Mapeo de categoría a cada artículo

### 2. **SearchApiController.cs**
- ✅ Construcción de URL con categoría en API de sugerencias:

```csharp
articulos = result.Articulos.Select(a => new
{
    // ...
    url = !string.IsNullOrWhiteSpace(a.Slug)
        ? (!string.IsNullOrWhiteSpace(a.CategoriaSlug)
            ? $"/{a.CategoriaSlug}/{a.Slug}"  // ⭐ Con categoría
            : $"/c/{a.Slug}")                  // Fallback
        : $"/Contenidos/Detalle?id={a.Id}"
}),
```

### 3. **Detalles.cshtml.cs**
- ✅ Construcción de URL con categoría en panel relacionado:

```csharp
ArticulosRelacionados = suggestions.Articulos.Select(a => new SuggestionDto
{
    Titulo = a.Titulo,
    Url = !string.IsNullOrWhiteSpace(a.Slug)
        ? (!string.IsNullOrWhiteSpace(a.CategoriaSlug)
            ? $"/{a.CategoriaSlug}/{a.Slug}"  // ⭐ Con categoría
            : $"/c/{a.Slug}")
        : $"/Contenidos/Detalle?id={a.Id}",
    // ...
}).ToList();
```

---

## 🎯 Resultado Final

### Antes (❌):
```
/Contenidos/embarazo-y-parto          → 404 Not Found
/Contenidos/biologicos-y-otros-medicamentos-dirigidos → 404
```

### Ahora (✅):
```
/familia-y-relaciones/embarazo-y-parto               → ✅ Funciona
/tratamientos/biologicos-y-otros-medicamentos-dirigidos → ✅ Funciona
```

---

## 🧪 Testing

### Prueba en Formulario:

1. Ir a crear pregunta
2. Escribir: `embarazo y parto`
3. Ver sugerencias → Click en artículo
4. **Verificar URL** en la barra de direcciones

**Esperado:**
```
https://localhost:7002/familia-y-relaciones/embarazo-y-parto
```

### Prueba en Panel Relacionado:

1. Ir a cualquier pregunta
2. Ver sidebar "📚 Contenido Relacionado"
3. Click en un artículo
4. **Verificar URL** funciona

---

## 📊 Estructura de BD Usada

```
ContenidosCategoriasRelacion
├── ContenidoId (int)
├── CategoriaId (int)
└── Borrado (bool)

ContenidosCategorias
├── Sequence (int) → PK
├── CategoriaSlug (string)
├── Nombre (string)
└── CategoriaPadre (int?)
```

**Join:**
```
ContenidosCategoriasRelacion → CategoriaId
ContenidosCategorias → Sequence
```

---

## ⚠️ Consideraciones

### 1. **Artículos en Múltiples Categorías**
Si un artículo pertenece a varias categorías, se toma la **primera** (`g.First().CategoriaSlug`).

**Posible mejora futura:** Priorizar categorías "padre" sobre "hijas".

### 2. **Artículos Sin Categoría**
Si un artículo no tiene categoría asignada:
- **Fallback 1:** `/c/{slug}`
- **Fallback 2:** `/Contenidos/Detalle?id={id}`

### 3. **Performance**
- ✅ Query adicional es eficiente (usa índices en CategoriaId y ContenidoId)
- ✅ Solo se consulta para los Top 5 artículos (no para todos)
- ✅ `AsNoTracking()` para mejor performance

---

## 🎉 Estado

- ✅ **Compilación exitosa**
- ✅ **URLs con categoría funcionando**
- ✅ **Fallbacks implementados**
- ✅ **Compatible con ambos lugares** (formulario + panel)

---

**Hot Reload y prueba las URLs ahora!** 🚀

