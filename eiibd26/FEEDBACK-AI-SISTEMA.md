# ✅ Sistema de Feedback para Respuestas IA - IMPLEMENTADO

## 🎯 Funcionalidad Implementada

Sistema completo de feedback 👍/👎 con comentarios opcionales para evaluar la utilidad de las respuestas de NINA (IA).

---

## 📁 Archivos Creados

### 1. **Modelo de Datos**: `eiibd26/Models/RespuestaAIFeedback.cs`
- ✅ Propiedades: Id, RespuestaId, UsuarioId, EsUtil, Comentario, FechaCreacion
- ✅ Navigation properties a Respuesta y Usuario
- ✅ Constraint único: un usuario solo puede dar feedback una vez por respuesta (pero puede modificarlo)

### 2. **DbSet en ApplicationDbContext**: `eiibd26/Data/ApplicationDbContext.cs`
- ✅ Agregado: `public DbSet<RespuestaAIFeedback> RespuestaAIFeedbacks { get; set; }`

### 3. **API Controller**: `eiibd26/Controllers/RespuestaFeedbackApiController.cs`

#### Endpoints Creados:

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/api/respuestas/{respuestaId}/feedback` | ✅ Required | Dar o actualizar feedback |
| GET | `/api/respuestas/{respuestaId}/feedback` | ❌ Public | Obtener estadísticas |
| DELETE | `/api/respuestas/{respuestaId}/feedback` | ✅ Required | Eliminar propio feedback |

---

## 🗃️ Base de Datos

### Tabla Creada: `RespuestaAIFeedback`

```sql
-- ✅ YA EJECUTADO
CREATE TABLE [dbo].[RespuestaAIFeedback] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [RespuestaId] UNIQUEIDENTIFIER NOT NULL,
    [UsuarioId] UNIQUEIDENTIFIER NOT NULL,
    [EsUtil] BIT NOT NULL,
    [Comentario] NVARCHAR(500) NULL,
    [FechaCreacion] DATETIMEOFFSET NOT NULL,
    -- Constraints y FKs...
);
```

### Índices Creados:
- ✅ `IX_RespuestaAIFeedback_RespuestaId` (buscar por respuesta)
- ✅ `IX_RespuestaAIFeedback_UsuarioId` (buscar por usuario)
- ✅ `IX_RespuestaAIFeedback_EsUtil` (estadísticas)
- ✅ `IX_RespuestaAIFeedback_Comentario` (feedback con comentarios)

---

## 🔌 API Endpoints - Ejemplos de Uso

### 1. Dar Feedback (Like)

```javascript
// Usuario hace clic en 👍
fetch('/api/respuestas/abc-123-def/feedback', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        esUtil: true,
        comentario: null // Opcional
    })
});

// Response:
{
    "ok": true,
    "message": "Feedback guardado correctamente",
    "estadisticas": {
        "total": 15,
        "likes": 12,
        "dislikes": 3,
        "porcentajeLikes": 80.0
    }
}
```

### 2. Dar Feedback Negativo con Comentario

```javascript
// Usuario hace clic en 👎 y escribe comentario
fetch('/api/respuestas/abc-123-def/feedback', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        esUtil: false,
        comentario: 'La respuesta fue muy genérica y no respondió mi pregunta específica'
    })
});
```

### 3. Obtener Estadísticas

```javascript
// Cargar al mostrar la pregunta
fetch('/api/respuestas/abc-123-def/feedback')
    .then(r => r.json())
    .then(data => {
        console.log(data.estadisticas); // { total: 15, likes: 12, ... }
        console.log(data.feedbackUsuario); // null si no autenticado, o { esUtil: true, ... }
    });

// Response:
{
    "ok": true,
    "estadisticas": {
        "total": 15,
        "likes": 12,
        "dislikes": 3,
        "porcentajeLikes": 80.0
    },
    "feedbackUsuario": {
        "esUtil": true,
        "comentario": null,
        "fechaCreacion": "2024-01-15T10:30:00Z"
    }
}
```

### 4. Eliminar Feedback

```javascript
// Usuario quiere quitar su voto
fetch('/api/respuestas/abc-123-def/feedback', {
    method: 'DELETE'
});

// Response:
{
    "ok": true,
    "message": "Feedback eliminado"
}
```

---

## 🎨 Comportamiento del Sistema

### Flujo de Usuario:

1. **Ver respuesta de IA** → Se muestran estadísticas generales (12 👍, 3 👎)
2. **Usuario autenticado** → Botones habilitados (👍/👎)
3. **Usuario NO autenticado** → Solo ver estadísticas
4. **Hacer clic en 👍** → Se guarda, botón se resalta, contador actualiza
5. **Cambiar a 👎** → Se actualiza el feedback existente (no se duplica)
6. **Agregar comentario** → Modal/textarea aparece, se guarda junto con el voto

### Reglas de Negocio:

- ✅ **Un voto por usuario por respuesta** (constraint en BD)
- ✅ **Puede cambiar su voto** (actualiza en lugar de crear nuevo)
- ✅ **Comentario es opcional** (pero útil para dislikes)
- ✅ **Comentario máx 500 caracteres**
- ✅ **Solo usuarios autenticados** pueden votar
- ✅ **Estadísticas son públicas** (todos pueden ver)
- ✅ **Puede eliminar su voto** si cambia de opinión

---

## 📊 Queries SQL Útiles para Análisis

### Resumen General

```sql
-- Ver estadísticas globales
SELECT 
    COUNT(*) AS TotalFeedbacks,
    SUM(CASE WHEN EsUtil = 1 THEN 1 ELSE 0 END) AS TotalLikes,
    SUM(CASE WHEN EsUtil = 0 THEN 1 ELSE 0 END) AS TotalDislikes,
    CAST(SUM(CASE WHEN EsUtil = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PorcentajeLikes
FROM RespuestaAIFeedback;
```

### Top Respuestas con Más Likes

```sql
SELECT TOP 10
    r.Id,
    p.Titulo,
    COUNT(CASE WHEN f.EsUtil = 1 THEN 1 END) AS Likes,
    COUNT(CASE WHEN f.EsUtil = 0 THEN 1 END) AS Dislikes
FROM RespuestaAIFeedback f
INNER JOIN Respuestas r ON f.RespuestaId = r.Id
INNER JOIN Preguntas p ON r.PreguntaId = p.Id
GROUP BY r.Id, p.Titulo
ORDER BY Likes DESC;
```

### Comentarios Negativos para Revisar

```sql
-- Ver feedback negativo con comentarios (para mejorar NINA)
SELECT TOP 20
    f.FechaCreacion,
    p.Titulo,
    f.Comentario,
    u.UserName
FROM RespuestaAIFeedback f
INNER JOIN Respuestas r ON f.RespuestaId = r.Id
INNER JOIN Preguntas p ON r.PreguntaId = p.Id
INNER JOIN AspNetUsers u ON f.UsuarioId = u.Id
WHERE f.EsUtil = 0 
  AND f.Comentario IS NOT NULL
ORDER BY f.FechaCreacion DESC;
```

---

## 🧪 Testing del Backend

### Probar con cURL

```bash
# 1. Dar like (requiere autenticación)
curl -X POST https://localhost:5001/api/respuestas/{respuestaId}/feedback \
  -H "Content-Type: application/json" \
  -H "Cookie: .AspNetCore.Identity.Application=..." \
  -d '{"esUtil": true, "comentario": null}'

# 2. Obtener estadísticas (público)
curl https://localhost:5001/api/respuestas/{respuestaId}/feedback

# 3. Eliminar feedback
curl -X DELETE https://localhost:5001/api/respuestas/{respuestaId}/feedback \
  -H "Cookie: .AspNetCore.Identity.Application=..."
```

### Probar con Postman

1. **POST** `/api/respuestas/{respuestaId}/feedback`
   - Body: `{ "esUtil": true, "comentario": "Muy útil" }`
   - Headers: Cookie con sesión autenticada
   
2. **GET** `/api/respuestas/{respuestaId}/feedback`
   - No requiere auth
   
3. **DELETE** `/api/respuestas/{respuestaId}/feedback`
   - Headers: Cookie con sesión autenticada

---

## ⚠️ Validaciones Implementadas

### En el Controller:

1. ✅ **Respuesta existe y es de IA** → `NotFound` si no
2. ✅ **Usuario autenticado** → `Unauthorized` si no
3. ✅ **Feedback duplicado** → Actualiza en lugar de error
4. ✅ **Comentario trimmed** → Quita espacios al inicio/fin
5. ✅ **Errores capturados** → Log + status 500

### En la BD:

1. ✅ **Constraint único** → `UQ_RespuestaAIFeedback_Usuario_Respuesta`
2. ✅ **FK con CASCADE** → Si se elimina respuesta, se elimina feedback
3. ✅ **FK sin CASCADE** → Si se elimina usuario, NO se elimina su feedback (para stats)

---

## 📈 Métricas Clave

### KPIs para Monitorear:

1. **Tasa de Aprobación** = Likes / (Likes + Dislikes)
   - Meta: > 70%
   
2. **Engagement** = Total Feedbacks / Total Respuestas IA
   - Meta: > 30%
   
3. **Feedback con Comentarios** = Comentarios / Total Feedbacks
   - Útil para: Mejorar NINA basado en retroalimentación detallada

### Dashboard Sugerido:

```sql
-- Reporte semanal
SELECT 
    DATEPART(WEEK, FechaCreacion) AS Semana,
    COUNT(*) AS TotalFeedbacks,
    SUM(CASE WHEN EsUtil = 1 THEN 1 ELSE 0 END) AS Likes,
    SUM(CASE WHEN EsUtil = 0 THEN 1 ELSE 0 END) AS Dislikes,
    CAST(SUM(CASE WHEN EsUtil = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS TasaAprobacion
FROM RespuestaAIFeedback
WHERE FechaCreacion >= DATEADD(WEEK, -4, GETDATE())
GROUP BY DATEPART(WEEK, FechaCreacion)
ORDER BY Semana DESC;
```

---

## 🚀 Estado Actual

- ✅ **Modelo C# creado**
- ✅ **Tabla SQL creada** (con índices)
- ✅ **DbSet registrado**
- ✅ **API Controller completo**
- ✅ **Endpoints testeables**
- ✅ **Logging implementado**
- ✅ **Compilación exitosa**

## 🎯 Próximos Pasos (Fase UI)

1. Modificar `Preguntas/Detalles.cshtml` para agregar:
   - Panel de feedback (👍/👎) debajo de respuesta IA
   - Mostrar estadísticas
   - Modal para comentario opcional
   
2. JavaScript para:
   - Llamar a API endpoints
   - Actualizar UI sin recargar
   - Manejar estados (loading, success, error)

3. Panel de contenido relacionado (usar `SearchSuggestionService`)

---

**¿Todo claro? ¿Continuamos con la UI?** 🎨

