# 📐 Estructura Final del Código - Referencia Rápida

## Flujo Completo de OnGetAsync()

```
OnGetAsync()
│
├─ Step 1: Validar parámetros
│  └─ PageNumber, PageSize, SearchQuery
│
├─ Step 2: Cargar filtros de usuario (si autenticado)
│  ├─ AvailableConditions
│  ├─ AvailableSintomas
│  └─ AvailableTratamientos
│
├─ Step 3: Construir query base
│  ├─ WHERE !Eliminado AND EstadoPublicacion IN (1,2,3)
│  └─ Si hay búsqueda: LIKE Título OR LIKE ContenidoCorto OR LIKE ContenidoLargo
│
├─ Step 4: Aplicar filtros (AND)
│  ├─ IF Condiciones → contenidos con esas condiciones
│  ├─ IF Síntomas → contenidos con esos síntomas
│  └─ IF Tratamientos → contenidos con esos tratamientos
│
├─ Step 5: Cargar TODOS los resultados en memoria
│  └─ await _db.Contenidos.Where(c => idsQuery.Contains(c.Id)).ToListAsync()
│
├─ Step 6: ✅ Aplicar scoring si hay búsqueda
│  ├─ FOR EACH contenido:
│  │  ├─ CalculateRelevanceScore(content, searchTerm)
│  │  │  ├─ Check título (10000, 5000, 2000, 1000)
│  │  │  ├─ Check contenido corto (100)
│  │  │  └─ Check contenido largo (10)
│  │  └─ Retorna: 10-10,110
│  │
│  ├─ OrderByDescending(score) → Primario
│  ├─ ThenByDescending(fecha) → Secundario
│  └─ Re-asignar allContents ordenada
│
├─ Step 7: Calcular total
│  └─ TotalCount = allContents.Count
│
├─ Step 8: ✅ PAGINAR (sobre lista YA ordenada)
│  └─ .Skip(skip).Take(PageSize)
│
├─ Step 9: Proyectar a BlogItemVm
│  └─ Select(c => new BlogItemVm { ... })
│
├─ Step 10: Cargar metadata en batch
│  ├─ ContenidoCondiciones → Conditions
│  ├─ ContenidoSintomas → Symptoms
│  ├─ ContenidoTratamientos → Treatments
│  ├─ ContenidosPreguntasRelacion → RelatedQuestionsCount
│  └─ ContenidosCategoriasRelacion → Category
│
└─ Step 11: Return Page()
```

---

## Método CalculateRelevanceScore()

### Lógica de Decisión
```
Input: Contenido c, string searchTerm
Output: int score (10-10,110)

INICIO:
│
├─ score = 0
├─ term = searchTerm.ToLower().Trim()
│
├─ IF c.ContenidoTitulo exists:
│  │  titleLower = c.ContenidoTitulo.ToLower()
│  │
│  ├─ IF titleLower == term
│  │  └─ score += 10000 ✅ EXACTO
│  │
│  ├─ ELSE IF titleLower.StartsWith(term + " ")
│  │  └─ score += 5000 ✅ PREFIJO
│  │
│  ├─ ELSE IF titleLower.Contains([" " + term + " "] OR [" " + term] OR [term + " "])
│  │  └─ score += 2000 ✅ LÍMITE DE PALABRA
│  │
│  └─ ELSE IF titleLower.Contains(term)
│     └─ score += 1000 ✅ SUBSTRING
│
├─ IF c.ContenidoTextoC exists AND contains(term)
│  └─ score += 100 ✅ CONTENIDO CORTO
│
├─ IF c.ContenidoTextoL exists AND contains(term)
│  └─ score += 10 ✅ CONTENIDO LARGO
│
└─ RETURN score (rango: 10-10,110)
```

---

## Ordenamiento en OnGetAsync()

### Antes (Antigua Lógica)
```csharp
// ❌ PROBLEMA: Ordenamiento sin escala adecuada
score range: 1-100
problemas:
  - 100 (exacto) vs 100 (múltiples hits en contenido)
  - Desempate por fecha → exacto podría NO estar primero
  - Resultado: en página 5
```

### Después (Nueva Lógica)
```csharp
// ✅ SOLUCIÓN: Escalado y ordering correcto
var contentWithScores = allContents
    .Select(c => new
    {
        Content = c,
        RelevanceScore = CalculateRelevanceScore(c, searchTerm)
        // Rango: 10-10,110
    })
    .OrderByDescending(x => x.RelevanceScore)      // ← Primario (rango amplio)
    .ThenByDescending(x => x.Content.FechaCreado)  // ← Secundario
    .ToList();

// 10,000 (exacto) siempre > 100 (contenido)
// ✅ Resultado: en página 1
```

---

## Ejemplo de Ejecución

### Búsqueda: "diabetes"

#### Input:
```csharp
SearchQuery = "diabetes"
PageNumber = 1
PageSize = 9
```

#### Step 1-5: Cargar y Filtrar
```
Contenidos encontrados en BD: 150
├─ "diabetes" en título/contenido
└─ Cargados en memoria: allContents
```

#### Step 6: Scoring
```
Content 1: "Diabetes"
  ├─ Título.ToLower() == "diabetes" → 10,000 ✅
  └─ Score = 10,000

Content 2: "Diabetes tipo 2"
  ├─ Título.ToLower().StartsWith("diabetes ") → 5,000 ✅
  └─ Score = 5,000

Content 3: "Síntomas de diabetes"
  ├─ Título contiene " diabetes" → 2,000 ✅
  └─ Score = 2,000

Content 4: "Artículo sobre hidratación que menciona diabetes solo en contenido"
  ├─ Contenido corto contains "diabetes" → 100 ✅
  └─ Score = 100

... (más contenidos)

Total: 150 contenidos con scores calculados
```

#### Step 6b: Ordenamiento
```
Sorted by RelevanceScore DESC, then FechaCreado DESC:

[1] Score 10,000 - Diabetes
[2] Score 5,000  - Diabetes tipo 2
[3] Score 5,000  - Diabetes infantil
[4] Score 5,000  - Diabetes gestacional
[5] Score 2,000  - Síntomas de diabetes
[6] Score 2,000  - Tratamiento diabetes
[7] Score 1,000  - Complicaciones diabéticas
[8] Score 1,000  - Alimentos y diabetes
[9] Score 100    - Hidratación en diabetes
... (resto)
```

#### Step 7-8: Paginar
```
allContents.Skip(0).Take(9)

Página 1 Resultados (posiciones 0-8):
[1] Diabetes
[2] Diabetes tipo 2
[3] Diabetes infantil
[4] Diabetes gestacional
[5] Síntomas de diabetes
[6] Tratamiento diabetes
[7] Complicaciones diabéticas
[8] Alimentos y diabetes
[9] Hidratación en diabetes

✅ Resultado exacto: POSICIÓN 1 ✓
```

---

## Diferencias en Rendering

### Valores en BlogItemVm
```csharp
public class BlogItemVm
{
    public int Id { get; set; }
    public string Title { get; set; }                    // "Diabetes"
    public string Slug { get; set; }                     // "diabetes"
    public string Excerpt { get; set; }                  // Texto corto
    public string ImageUrl { get; set; }                 // "/uploads/.../..."
    public string Author { get; set; }                   // "Dr. X"
    public string AuthorImageUrl { get; set; }           // Avatar
    public string AuthorSlug { get; set; }               // "@drx"
    public Guid? AuthorId { get; set; }                  // GUID
    public DateTime CreatedAt { get; set; }              // Fecha
    public List<string> Conditions { get; set; }         // ["Diabetes", ...]
    public List<string> Symptoms { get; set; }           // ["Polidipsia", ...]
    public List<string> Treatments { get; set; }         // ["Metformina", ...]
    public int RelatedQuestionsCount { get; set; }       // 5
    public string Category { get; set; }                 // "<a href=...>Categoría</a>"
    public string CategorySlug { get; set; }             // "salud"
}
```

---

## Debug Output

### Console Output (Visual Studio)
```
🔍 SEARCH: 'diabetes' | Found 150 results | PAGE 1
  → Score 10000: Diabetes
  → Score 5000: Diabetes tipo 2
  → Score 5000: Diabetes infantil
  → Score 5000: Diabetes gestacional
  → Score 2000: Síntomas de diabetes
```

### Verificación
```csharp
System.Diagnostics.Debug.WriteLine($"🔍 SEARCH: '{searchTerm}' | Found {allContents.Count} results | PAGE {PageNumber}");
foreach (var r in debugResults)
{
    System.Diagnostics.Debug.WriteLine($"  → Score {r.Score}: {r.Title}");
}
```

---

## Caché (Si Aplica)

```csharp
// Nota: El sistema actual NO usa caché para búsquedas
// Si se implementara caché, debería ser por:
// - Clave única: searchTerm + pageNumber + filtros
// - Invalidación: cuando se publican nuevos contenidos
```

---

## Performance

| Operación | Tiempo | Notas |
|-----------|--------|-------|
| BD Query | ~50-100ms | Dependiendo de índices |
| Load Memory | ~10-50ms | Para 150-1000 resultados |
| Scoring | ~5-20ms | En memoria, O(n) |
| Sort | ~5-10ms | Array sort, O(n log n) |
| **Total** | **~70-180ms** | Rápido ✓ |

---

## Variables Clave

```csharp
// Búsqueda
string searchTerm = "diabetes"
string searchPattern = "%diabetes%"
bool hasSearch = true

// Ordenamiento
int[] scores = { 10000, 5000, 2000, 1000, 100, 10 }

// Paginación
int skip = (PageNumber - 1) * PageSize       // (1-1)*9 = 0
int take = PageSize                          // 9
int totalCount = 150

// Resultado
List<BlogItemVm> items                       // 9 items en página 1
```

---

## Próximas Optimizaciones (Opcional)

```csharp
// 1. Full-text search en SQL Server
SELECT * FROM Contenidos
WHERE CONTAINS((ContenidoTitulo, ContenidoTextoC), '"diabetes"')

// 2. Índices especializados
CREATE FULLTEXT INDEX ON Contenidos(ContenidoTitulo, ContenidoTextoC)

// 3. Caché de búsquedas frecuentes
_cache.Set($"search:{searchTerm}", results)

// 4. Búsqueda booleana
WHERE CONTAINS(..., 'diabetes AND -infantil')
```

