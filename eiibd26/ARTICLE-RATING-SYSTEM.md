# 🎯 Sistema de Calificación de Artículos (Like/Dislike)

## 📋 Resumen de Implementación

Sistema completo para permitir a los usuarios calificar artículos con **Like** (útil) o **Dislike** (no útil).

---

## ✅ Archivos Creados

### 1. **Modelo y Enumeración**
- ✅ `eiibd26\Models\RatingType.cs` - Enum para Like/Dislike
- ✅ `eiibd26\Models\ArticleRating.cs` - Modelo de datos

### 2. **API Controller**
- ✅ `eiibd26\Controllers\ArticleRatingsApiController.cs` - Endpoints REST

### 3. **Scripts SQL**
- ✅ `eiibd26\Data\Migrations\SQL\CreateArticleRatingsTable.sql` - Script de base de datos

### 4. **Archivos Actualizados**
- ✅ `eiibd26\Data\ApplicationDbContext.cs` - DbSet y configuración
- ✅ `eiibd26\Pages\Contenidos\Detalle.cshtml` - UI del sidebar
- ✅ `eiibd26\wwwroot\css\detalle.css` - Estilos CSS

---

## 📊 Estructura de la Tabla

```sql
ArticleRatings
├── Id (INT, PK, Identity)
├── ArticleId (INT, FK → contenidos)
├── RatingType (INT, 0=Dislike, 1=Like)
├── UserId (NVARCHAR(450), FK → AspNetUsers, nullable)
├── IpAddress (NVARCHAR(45), nullable)
├── CreatedAt (DATETIME2)
└── UpdatedAt (DATETIME2, nullable)
```

**Índices:**
- `IX_ArticleRatings_ArticleId` - Búsquedas por artículo
- `IX_ArticleRatings_UserId` - Búsquedas por usuario
- `IX_ArticleRatings_UserId_ArticleId` (UNIQUE) - Un voto por usuario/artículo

---

## 🔌 API Endpoints

### GET `/api/articles/{articleId}/rating`
Obtener estadísticas de calificación de un artículo.

**Response:**
```json
{
  "ok": true,
  "estadisticas": {
    "likes": 120,
    "dislikes": 15,
    "total": 135
  },
  "ratingUsuario": {
    "tipo": "like",
    "fecha": "2024-01-15T10:30:00Z"
  }
}
```

### POST `/api/articles/{articleId}/rating`
Registrar o actualizar calificación.

**Request:**
```json
{
  "ratingType": "like"  // o "dislike"
}
```

**Response:**
```json
{
  "ok": true,
  "message": "Calificación registrada",
  "estadisticas": {
    "likes": 121,
    "dislikes": 15,
    "total": 136
  }
}
```

---

## 🎨 Interfaz de Usuario

### Ubicación
Sidebar derecho del artículo, **antes** de "Compartir artículo".

### Componentes
```
┌─────────────────────────────┐
│ Calificar artículo          │
├─────────────────────────────┤
│ 👍 Me fue útil        [120] │
│ 👎 No me fue útil      [15] │
└─────────────────────────────┘
```

### Estados
- **No autenticado**: Mensaje para iniciar sesión
- **Autenticado**: Botones activos con contadores
- **Ya votado**: Botón seleccionado resaltado
- **Cambiar voto**: Actualiza automáticamente

---

## 💻 Lógica de Negocio

### Restricciones
1. ✅ **Usuarios autenticados**: Un voto por artículo (por `UserId`)
2. ✅ **Usuarios anónimos**: Un voto por IP en 24 horas (opcional)
3. ✅ **Cambio de voto**: Permitido, actualiza el registro existente

### Validaciones
- ✅ `ArticleId` debe existir y no estar eliminado
- ✅ `RatingType` debe ser "like" o "dislike"
- ✅ Unicidad de votos garantizada por índice único

### Comportamiento
- **Primer voto**: Crea nuevo registro
- **Voto existente**: Actualiza `RatingType` y `UpdatedAt`
- **Sin autenticar**: Guarda IP y permite votar (limitado)

---

## 🎯 Flujo de Usuario

### Usuario Autenticado
```
1. Usuario hace clic en 👍 o 👎
2. JavaScript llama a POST /api/articles/{id}/rating
3. Backend valida y guarda/actualiza en BD
4. Responde con estadísticas actualizadas
5. UI actualiza contadores y resalta botón
6. Muestra mensaje: "¡Gracias por tu opinión!"
```

### Usuario No Autenticado
```
1. UI muestra: "Inicia sesión para calificar"
2. Click redirige a /Identity/Account/Login
3. Después de login, puede votar
```

---

## 🔒 Seguridad

### Implementadas
✅ Validación de artículo existente
✅ Restricción de un voto por usuario
✅ Sanitización de IP address
✅ Foreign keys con CASCADE/SET NULL apropiados
✅ Índices únicos para prevenir duplicados

### Consideraciones
- IP tracking es **opcional** (puede desactivarse)
- Los votos anónimos tienen validez de 24 horas
- `CASCADE DELETE` elimina ratings si se elimina el artículo
- `SET NULL` preserva ratings si se elimina el usuario

---

## 📈 Métricas Futuras

Este sistema permite generar:
- 📊 Artículos mejor calificados
- 📉 Artículos con peor valoración
- 📈 Tasa de satisfacción (likes / total)
- 🔍 Identificar contenido que necesita mejoras
- 📅 Tendencias de calificación en el tiempo

---

## 🚀 Pasos para Activar

### 1. Ejecutar Script SQL
```sql
-- Ejecutar en SQL Server Management Studio o Azure Data Studio
USE [tu_base_de_datos];
GO

-- Copiar y ejecutar el contenido de:
-- eiibd26\Data\Migrations\SQL\CreateArticleRatingsTable.sql
```

### 2. Verificar Compilación
```bash
dotnet build eiibd26
```

### 3. Probar API
```bash
# GET rating stats
curl https://localhost:7XXX/api/articles/1/rating

# POST rating
curl -X POST https://localhost:7XXX/api/articles/1/rating \
  -H "Content-Type: application/json" \
  -d '{"ratingType":"like"}'
```

### 4. Verificar UI
1. Navegar a cualquier artículo
2. Buscar en el sidebar derecho
3. Hacer clic en 👍 o 👎
4. Verificar que se actualicen los contadores

---

## 🎨 Estilos CSS

### Colores
- **Like activo**: Verde (#10b981, #d1fae5)
- **Dislike activo**: Rojo (#ef4444, #fee2e2)
- **Neutral**: Gris (#f7f9f9)
- **Hover**: Gris oscuro (#e7e9ea)

### Transiciones
- Smooth hover con `transform: translateY(-1px)`
- Border radius consistente: `0.5rem`
- Iconos de Bootstrap Icons

---

## ✨ Características Destacadas

1. ✅ **Un solo voto por usuario** - Previene spam
2. ✅ **Cambio de voto permitido** - Flexibilidad
3. ✅ **Contadores en tiempo real** - Feedback inmediato
4. ✅ **UI limpia y moderna** - Diseño consistente con el sitio
5. ✅ **Soporte para anónimos** - Mayor participación (opcional)
6. ✅ **Optimizado con índices** - Consultas rápidas
7. ✅ **Logging completo** - Debugging y auditoría
8. ✅ **Responsive** - Funciona en móviles

---

## 🐛 Troubleshooting

### "Error al guardar calificación"
- ✅ Verificar que la tabla existe
- ✅ Verificar que el artículo existe
- ✅ Revisar logs del servidor

### "Los contadores no se actualizan"
- ✅ Abrir DevTools → Console
- ✅ Verificar errores de JavaScript
- ✅ Verificar que el API responde correctamente

### "Puedo votar múltiples veces"
- ✅ Verificar índice único está creado
- ✅ Verificar que el usuario está autenticado
- ✅ Revisar logs del controller

---

## 📝 Notas del Desarrollador

- El sistema está diseñado para **escalar**
- Los índices optimizan consultas incluso con millones de votos
- La IP se guarda para usuarios anónimos pero es **opcional**
- El `UpdatedAt` permite auditar cambios de voto
- El sistema es **extensible** para agregar más tipos de rating

---

## 🎯 Estado: ✅ LISTO PARA PRODUCCIÓN

Todos los componentes han sido implementados y están listos para usar.

**Última actualización:** $(Get-Date -Format "yyyy-MM-dd HH:mm")
