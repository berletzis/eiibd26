# 05 – Performance: SearchSuggestionService

**Fecha:** 2025-07-10  
**Issues:** PERF-001, PERF-002, PERF-003

---

## Estado Pre-Remediación

El servicio ejecutaba un loop por keyword, generando **N queries independientes** por búsqueda:

```
keyword "Crohn" → 1 query preguntas + 1 query artículos + 1 query respuestas
keyword "inflamatoria" → 1 query preguntas + 1 query artículos + ...
keyword "intestinal" → ...
```

Con 3-5 keywords típicos en una query de EII → **9-15+ roundtrips** a base de datos por búsqueda.

---

## Estado Post-Remediación

### PERF-001: BuscarPreguntasAsync — Un único query OR dinámico

```csharp
// Un solo query SQL con OR dinámico para todos los keywords
var kws = keywords.Take(5).ToList();
query = query.Where(p =>
	kws.Any(k => p.Titulo.Contains(k) || p.Cuerpo.Contains(k)));
```

Luego, conteo de respuestas sin N+1:

```csharp
// PERF-001: Conteo en un solo query para todas las preguntas seleccionadas
var respuestasCounts = await _db.Respuestas
	.AsNoTracking()
	.Where(r => preguntaIds.Contains(r.PreguntaId) && !r.Eliminado)
	.GroupBy(r => r.PreguntaId)
	.ToDictionaryAsync(...);
```

**Queries por búsqueda ANTES:** N (un loop) + N (conteo por pregunta) = 2N queries  
**Queries por búsqueda DESPUÉS:** 1 (preguntas OR) + 1 (conteo batch) = **2 queries**

### PERF-002: BuscarArticulosAsync — Un único query OR dinámico

```csharp
query = query.Where(c =>
	kws.Any(k =>
		(c.ContenidoTitulo != null && c.ContenidoTitulo.Contains(k)) ||
		(c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(k)) ||
		(c.ContenidoTextoL != null && c.ContenidoTextoL.Contains(k))));
```

Slugs de categorías también consolidados en un único JOIN query.

### PERF-003: Caching en memoria (60 segundos)

```csharp
var cacheKey = $"suggestions_{normalizedQuery}_{condicionId}";
if (_cache.TryGetValue<SuggestionResult>(cacheKey, out var cached))
	return cached;
// ...
_cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
```

Queries repetidas dentro de 60 segundos → 0 roundtrips DB.

---

## Análisis de Queries Post-Remediación

| Método | Queries DB | AsNoTracking | Proyección Select | Take() |
|--------|-----------|-------------|-------------------|--------|
| `BuscarPreguntasAsync` | 2 (1 + 1 batch count) | ✅ | ✅ | Take(20) → Take(5) |
| `BuscarArticulosAsync` | 2 (1 OR + 1 slug JOIN) | ✅ | ✅ | Take(20) → Take(5) |
| `BuscarRespuestasAsync` | 1 (OR + Include Pregunta) | ✅ | ✅ | Take(10) |
| **Total por llamada sin cache** | **~5** | — | — | — |

---

## Benchmark Estático (sin ejecución runtime)

> ⚠️ **Limitación:** No se ejecutó benchmark de runtime en este entorno. Los datos siguientes son estimaciones conservadoras basadas en análisis de código y modelo de costo de queries.

### Keyword: "Crohn" (típico dominio EII)

| Métrica | ANTES (estimado) | DESPUÉS (medido estático) |
|---------|-----------------|--------------------------|
| Queries a DB | 9–15 (N per keyword × 3 types) | **~5** (constante) |
| Roundtrips red | 9–15 | **~5** |
| Rows candidatas | ~20 × N = 100+ | **20 capped por tipo** |
| Cache hit (60s) | ❌ Sin cache | ✅ 0 queries en hit |

### Proyección

Reducción esperada de roundtrips: **~70%** (de ~15 a ~5).  
Con cache activo: **~100%** (0 DB para queries repetidas).

---

## Validación de Patrones

| Patrón | Estado |
|--------|--------|
| `AsNoTracking()` en todas las queries de lectura | ✅ |
| `Select()` con proyección (no `Select(x => x)`) | ✅ |
| `Include()` solo donde necesario | ✅ (respuestas incluyen pregunta para enlace) |
| `ToListAsync()` con CancellationToken | ✅ |
| Sin bucle `foreach` con queries internas | ✅ |
| `Take()` antes de materializar | ✅ (Take(20) en DB, Take(5) en memoria) |

---

## Riesgos Residuales

| ID | Riesgo | Severidad |
|----|--------|-----------|
| R-PERF-01 | `kws.Any(k => ...)` con EF Core puede generar `OR` largo en SQL dependiendo del proveedor. Verificar plan de ejecución en producción. | 🟡 Bajo |
| R-PERF-02 | Cache key usa `normalizedQuery` completa — colisiones posibles si queries largas son similares pero no idénticas. | 🟡 Bajo |
| R-PERF-03 | Sin benchmark real de ejecución — la reducción de roundtrips es validada por análisis estático, no por medición. | 🟡 Informativo |

---

## Veredicto Fase 5

| Criterio | Estado |
|----------|--------|
| Loop N queries eliminado | ✅ PASS |
| Query consolidado OR dinámico | ✅ PASS |
| Conteo batch sin N+1 | ✅ PASS |
| AsNoTracking en reads | ✅ PASS |
| Cache en memoria 60s | ✅ PASS |
| Benchmark runtime ejecutado | ⚠️ NO (requiere acceso a DB de staging) |
| **VEREDICTO** | ✅ **PASS** (con deuda de benchmark en staging) |
