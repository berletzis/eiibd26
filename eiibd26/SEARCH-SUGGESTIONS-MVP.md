# ✅ Sistema de Sugerencias durante Creación de Preguntas - MVP

## 🎯 Funcionalidad Implementada

Sistema de sugerencias automáticas que muestra contenido relacionado mientras el usuario escribe su pregunta, **SIN usar IA externa**, solo búsqueda en la base de datos local.

---

## 📁 Archivos Creados

### 1. **`eiibd26/Services/SearchSuggestionService.cs`**
Servicio principal que implementa la búsqueda de contenido relacionado.

**Funciones:**
- Búsqueda por keywords (sin stopwords)
- Cache de 60 segundos para optimizar performance
- Busca en:
  - ✅ Preguntas (título + cuerpo)
  - ✅ Artículos (título + resumen)
  - ✅ Respuestas destacadas (por puntuación)
- Soporta filtro opcional por `condicionId`
- Top 5 resultados por categoría

**Stopwords ignoradas:**
```csharp
"el", "la", "de", "que", "es", "un", "con", "para", "por", etc.
```

---

### 2. **`eiibd26/Controllers/SearchApiController.cs`**
Controlador API REST para exponer el servicio.

**Endpoint:**
```
GET /api/search/suggestions?q=texto&condicionId=1
```

**Response:**
```json
{
  "ok": true,
  "preguntas": [
    {
      "id": "guid",
      "titulo": "¿Es normal tener urgencias en la mañana?",
      "slug": "es-normal-tener-urgencias-manana",
      "respuestasCount": 5,
      "url": "/Preguntas/es-normal-tener-urgencias-manana"
    }
  ],
  "articulos": [
    {
      "id": 123,
      "titulo": "Síntomas comunes de colitis activa",
      "slug": "sintomas-colitis-activa",
      "url": "/Contenidos/sintomas-colitis-activa"
    }
  ],
  "respuestas": [
    {
      "id": "guid",
      "preguntaTitulo": "¿Cómo controlar las urgencias?",
      "cuerpoPreview": "En mi experiencia lo que mejor me funciona...",
      "puntuacion": 8,
      "url": "/Preguntas/slug#respuesta-id"
    }
  ]
}
```

---

### 3. **Modificaciones en `Program.cs`**
Registro del servicio:
```csharp
builder.Services.AddScoped<eiibd26.Services.SearchSuggestionService>();
```

---

### 4. **Modificaciones en `UusuarioPreguntaDetalle.cshtml`**

#### A. HTML - Contenedor de Sugerencias
Agregado después del campo `Título`:
```html
<div id="suggestionBox" style="display:none;">
    <div>💡 Tal vez esto ya se ha preguntado o puede ayudarte:</div>
    <div id="suggestionContent"></div>
</div>
```

#### B. JavaScript - Sugerencias en Tiempo Real
- **Trigger**: keyup en campo título
- **Debounce**: 400ms
- **Mínimo**: 20 caracteres
- **Fetch**: `/api/search/suggestions?q=...`
- **Renderiza**: Preguntas, artículos y respuestas
- **Links**: Abren en nueva pestaña (`target="_blank"`)

#### C. JavaScript - Validación Obligatoria
- **Valida**: Al menos 1 condición seleccionada antes de enviar formulario
- **Error UX**: Alert + scroll automático a card de condiciones
- **Highlight**: Borde rojo temporal (3 segundos)

---

## 🧪 Testing

### Test 1: Sugerencias Básicas

1. Ir a crear pregunta: `/Identity/Usuario/UusuarioPreguntaDetalle`
2. Escribir en título: `¿Es normal tener urgencias en la mañana?` (>20 caracteres)
3. Esperar 400ms
4. ✅ Debe aparecer bloque gris con sugerencias
5. ✅ Debe mostrar preguntas/artículos/respuestas relacionadas

### Test 2: Condición Obligatoria

1. Llenar título y cuerpo
2. NO seleccionar ninguna condición
3. Hacer clic en guardar
4. ✅ Debe mostrar alert: "Selecciona al menos una condición..."
5. ✅ Debe hacer scroll automático a card de condiciones
6. ✅ Card debe tener borde rojo temporal

### Test 3: Filtro por Condición

1. Seleccionar "Colitis ulcerosa"
2. Escribir: `¿Cómo controlar las urgencias?`
3. ✅ Sugerencias deben priorizarse por contenido relacionado con Colitis

### Test 4: Cache

1. Buscar: `urgencias matinales`
2. Borrar y volver a escribir: `urgencias matinales`
3. ✅ Segunda búsqueda debe ser instantánea (cache hit)
4. Ver logs del servidor: debe aparecer "Cache hit"

---

## 📊 Performance

### Optimizaciones Implementadas:

1. **Debounce 400ms**: Evita llamadas innecesarias mientras escribe
2. **Cache 60s**: Queries repetidas usan cache en memoria
3. **TOP 5**: Solo trae 5 resultados por categoría
4. **AsNoTracking**: Consultas EF Core sin tracking
5. **Mínimo 20 caracteres**: No busca con texto muy corto

### Métricas Esperadas:

- **Tiempo respuesta API**: < 200ms (sin cache)
- **Tiempo respuesta API**: < 10ms (con cache)
- **Keywords extraídas**: 2-5 por query típica

---

## 🔍 Algoritmo de Búsqueda

### Paso 1: Normalización
```
Input: "¿Es normal tener urgencias en la mañana?"
↓
Lowercase: "¿es normal tener urgencias en la mañana?"
↓
Sin caracteres especiales: "es normal tener urgencias en la mañana"
↓
Output: "es normal tener urgencias en la mañana"
```

### Paso 2: Extracción de Keywords
```
Palabras: ["es", "normal", "tener", "urgencias", "en", "la", "mañana"]
↓
Filtro: >= 3 caracteres && !stopword
↓
Keywords: ["normal", "tener", "urgencias", "mañana"]
```

### Paso 3: Búsqueda SQL
```sql
SELECT TOP 5 * FROM Preguntas
WHERE !Eliminado
AND (Titulo LIKE '%normal%' OR Cuerpo LIKE '%normal%')
AND (Titulo LIKE '%tener%' OR Cuerpo LIKE '%tener%')
AND (Titulo LIKE '%urgencias%' OR Cuerpo LIKE '%urgencias%')
AND (Titulo LIKE '%mañana%' OR Cuerpo LIKE '%mañana%')
ORDER BY FechaCreacion DESC
```

---

## 💡 Casos de Uso

### ✅ Caso 1: Usuario encuentra respuesta sin preguntar
1. Usuario empieza a escribir pregunta
2. Ve sugerencia con pregunta similar ya respondida
3. Hace clic en la sugerencia
4. Lee respuestas existentes
5. **Resultado**: No crea pregunta duplicada ✅

### ✅ Caso 2: Usuario mejora su pregunta
1. Usuario escribe pregunta vaga
2. Ve artículos relacionados en sugerencias
3. Lee artículo para entender mejor el tema
4. Reformula pregunta con más detalle
5. **Resultado**: Pregunta de mejor calidad ✅

### ✅ Caso 3: Usuario descubre contenido relacionado
1. Usuario pregunta sobre síntomas
2. Sugerencias muestran artículos educativos
3. Usuario aprende más sobre su condición
4. **Resultado**: Mayor engagement con contenido existente ✅

---

## 📈 Impacto Esperado

### Reducción de Preguntas Duplicadas
- **Objetivo**: -20% en preguntas duplicadas
- **Medición**: Comparar ratio duplicados mes anterior vs mes actual

### Mayor Uso de Contenido Existente
- **Objetivo**: +30% clics en artículos desde sugerencias
- **Medición**: Tracking de clics en links de sugerencias

### Mejor Calidad de Preguntas
- **Objetivo**: +15% preguntas con contexto detallado
- **Medición**: Longitud promedio del cuerpo de pregunta

---

## 🔧 Configuración

### Ajustar Stopwords

Editar en `SearchSuggestionService.cs`:
```csharp
private static readonly HashSet<string> StopWords = new(...)
{
    "palabra", "a", "ignorar", ...
};
```

### Ajustar Cache Duration

Cambiar en `SearchSuggestionService.cs`:
```csharp
_cache.Set(cacheKey, result, TimeSpan.FromSeconds(120)); // 2 minutos
```

### Ajustar Top Resultados

Cambiar `.Take(5)` a `.Take(10)` en cada método de búsqueda.

### Ajustar Debounce

Cambiar en `UusuarioPreguntaDetalle.cshtml`:
```javascript
}, 600); // 600ms en lugar de 400ms
```

---

## 🚀 Estado

- ✅ Servicio de búsqueda creado
- ✅ Endpoint API expuesto
- ✅ UI de sugerencias agregada
- ✅ JavaScript de sugerencias en tiempo real
- ✅ Validación de condición obligatoria
- ✅ Cache implementado
- ✅ Compilación exitosa
- ✅ Listo para testing

---

## 📝 Próximos Pasos (Futuro)

### Fase 2 - Mejoras (Opcional)
1. **Ranking más inteligente**: Priorizar por relevancia real
2. **Búsqueda semántica**: Usar embeddings para similitud conceptual
3. **A/B Testing**: Medir impacto real en métricas
4. **Analytics**: Tracking de clics en sugerencias
5. **UI mejorada**: Cards visuales en lugar de lista

### Fase 3 - IA Contextual (Futuro)
1. Integrar con sistema NINA para sugerencias generadas
2. Combinar búsqueda local + sugerencias IA
3. Aprendizaje de qué sugerencias son más útiles

---

## 🐛 Troubleshooting

### Problema: No aparecen sugerencias
- ✅ Verificar que escribiste **≥ 20 caracteres**
- ✅ Abrir DevTools Console, buscar logs `[Suggestions]`
- ✅ Verificar que endpoint `/api/search/suggestions` responde OK

### Problema: Búsqueda muy lenta
- ✅ Verificar índices en BD: `Preguntas.Titulo`, `Contenidos.ContenidoTitulo`
- ✅ Revisar logs de servidor para tiempo de respuesta
- ✅ Considerar aumentar cache duration

### Problema: Validación no funciona
- ✅ Verificar que form tiene `id="preguntaDetForm"`
- ✅ Verificar que checkboxes tienen class `rel-check-condicion`
- ✅ Abrir DevTools Console para ver errores JS

---

## 📊 SQL Queries Útiles

### Ver queries más buscadas (requiere logging adicional)
```sql
-- Por ahora no hay tabla de logs de búsqueda
-- Se puede agregar en futuro si se necesita analítica
```

### Ver preguntas duplicadas (para medir impacto)
```sql
SELECT Titulo, COUNT(*) as Cantidad
FROM Preguntas
WHERE !Eliminado
GROUP BY Titulo
HAVING COUNT(*) > 1
ORDER BY Cantidad DESC;
```

---

**¡Sistema de Sugerencias MVP implementado y listo para usar!** 🚀

---

## 📞 Documentación Adicional

- **Servicio**: `eiibd26/Services/SearchSuggestionService.cs` (comentarios inline)
- **API**: `eiibd26/Controllers/SearchApiController.cs` (comentarios inline)
- **UI**: `eiibd26/Areas/Identity/Pages/Usuario/UusuarioPreguntaDetalle.cshtml` (comentarios inline)
