# 🚀 Guía de Implementación Rápida - Sistema de Calificación de Artículos

## ⚡ Pasos para Activar (5 minutos)

### 📋 **PASO 1: Ejecutar Script SQL** (2 min)

1. Abrir **SQL Server Management Studio** o **Azure Data Studio**
2. Conectar a tu base de datos
3. Abrir el archivo: `eiibd26\Data\Migrations\SQL\CreateArticleRatingsTable.sql`
4. Ejecutar el script completo (F5)
5. Verificar mensaje: "Tabla ArticleRatings creada exitosamente ✓"

---

### ✅ **PASO 2: Verificar la Tabla** (1 min)

Ejecutar el script de verificación:

```sql
-- Abrir: eiibd26\Data\Migrations\SQL\VerifyArticleRatings.sql
-- Ejecutar para ver:
-- ✓ Estructura de tabla
-- ✓ Índices creados
-- ✓ Foreign keys
-- ✓ Estadísticas iniciales
```

---

### 🔨 **PASO 3: Compilar el Proyecto** (1 min)

```bash
dotnet build eiibd26
```

**Resultado esperado:** Build succeeded ✅

---

### 🌐 **PASO 4: Iniciar la Aplicación** (30 seg)

```bash
dotnet run --project eiibd26
```

O presionar **F5** en Visual Studio

---

### 🧪 **PASO 5: Probar la Funcionalidad** (1 min)

#### Opción A: Navegador
1. Ir a cualquier artículo (ej: `/Contenidos/Detalle/1`)
2. Scroll al sidebar derecho
3. Buscar la sección **"Calificar artículo"**
4. Hacer clic en **👍 Me fue útil** o **👎 No me fue útil**
5. Verificar:
   - ✅ Mensaje: "¡Gracias por tu opinión!"
   - ✅ Contadores actualizados
   - ✅ Botón seleccionado resaltado

#### Opción B: API (Postman/curl)
```bash
# GET rating stats
curl -X GET https://localhost:7XXX/api/articles/1/rating

# POST rating (requiere autenticación)
curl -X POST https://localhost:7XXX/api/articles/1/rating \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"ratingType":"like"}'
```

---

## 🎯 Checklist de Verificación

### Base de Datos
- [ ] Tabla `ArticleRatings` creada
- [ ] Índice `IX_ArticleRatings_ArticleId` existe
- [ ] Índice `IX_ArticleRatings_UserId` existe
- [ ] Índice único `IX_ArticleRatings_UserId_ArticleId` existe
- [ ] Foreign key a `contenidos` configurada
- [ ] Foreign key a `AspNetUsers` configurada

### Backend
- [ ] Archivo `Models/RatingType.cs` existe
- [ ] Archivo `Models/ArticleRating.cs` existe
- [ ] `ApplicationDbContext.cs` tiene `DbSet<ArticleRating>`
- [ ] Archivo `Controllers/ArticleRatingsApiController.cs` existe
- [ ] Proyecto compila sin errores

### Frontend
- [ ] UI visible en sidebar de artículos
- [ ] Botones 👍 y 👎 funcionan
- [ ] Contadores se actualizan
- [ ] Mensaje de confirmación aparece
- [ ] CSS aplicado correctamente

### Funcionalidad
- [ ] Usuario autenticado puede votar
- [ ] Usuario puede cambiar su voto
- [ ] No se puede votar múltiples veces
- [ ] Contadores reflejan datos reales
- [ ] Usuario no autenticado ve mensaje de login

---

## 🔍 Troubleshooting

### ❌ "Tabla ArticleRatings no existe"
**Solución:** Ejecutar `CreateArticleRatingsTable.sql`

### ❌ "Error al compilar"
**Solución:** 
```bash
dotnet clean eiibd26
dotnet build eiibd26
```

### ❌ "API devuelve 404"
**Solución:** Verificar que el controller está en `Controllers/ArticleRatingsApiController.cs`

### ❌ "Botones no responden"
**Solución:** 
1. Abrir DevTools (F12)
2. Ver Console
3. Buscar errores JavaScript
4. Verificar que el script se cargó

### ❌ "Puedo votar múltiples veces"
**Solución:** Verificar índice único:
```sql
SELECT * FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.ArticleRatings') 
AND is_unique = 1;
```

---

## 📊 Consultas Útiles

### Ver todos los ratings
```sql
SELECT * FROM ArticleRatings ORDER BY CreatedAt DESC;
```

### Ver ratings de un artículo específico
```sql
SELECT 
    ar.*,
    u.UserName,
    CASE WHEN ar.RatingType = 1 THEN 'Like' ELSE 'Dislike' END AS Tipo
FROM ArticleRatings ar
LEFT JOIN AspNetUsers u ON ar.UserId = u.Id
WHERE ar.ArticleId = 1;
```

### Estadísticas por artículo
```sql
SELECT 
    c.Id,
    c.ContenidoTitulo,
    COUNT(*) AS TotalVotos,
    SUM(CASE WHEN ar.RatingType = 1 THEN 1 ELSE 0 END) AS Likes,
    SUM(CASE WHEN ar.RatingType = 0 THEN 1 ELSE 0 END) AS Dislikes
FROM contenidos c
LEFT JOIN ArticleRatings ar ON c.Id = ar.ArticleId
WHERE c.Eliminado = 0
GROUP BY c.Id, c.ContenidoTitulo
ORDER BY TotalVotos DESC;
```

### Limpiar ratings de prueba
```sql
DELETE FROM ArticleRatings WHERE UserId = 'tu-user-id-de-prueba';
```

---

## 🎨 Personalización Opcional

### Cambiar colores
Editar `wwwroot/css/detalle.css`:
```css
.rating-like.active {
    background: #TU_COLOR_AQUI;
    border-color: #TU_COLOR_AQUI;
}
```

### Cambiar textos
Editar `Pages/Contenidos/Detalle.cshtml`:
```html
<span class="rating-text">Tu texto aquí</span>
```

### Deshabilitar votos anónimos
Comentar en `ArticleRatingsApiController.cs`:
```csharp
// Líneas 105-115 - Bloque de usuarios anónimos
```

---

## 📈 Próximos Pasos (Opcional)

### Analytics Dashboard
Crear página de administración para ver:
- Artículos mejor calificados
- Artículos que necesitan mejoras
- Tendencias de calificación
- Gráficas de satisfacción

### Notificaciones
Enviar email al autor cuando:
- Un artículo recibe X dislikes
- Un artículo alcanza X likes
- Ratio de satisfacción baja del Y%

### Exportar Datos
```sql
-- Exportar a CSV
SELECT 
    c.ContenidoTitulo AS Articulo,
    SUM(CASE WHEN ar.RatingType = 1 THEN 1 ELSE 0 END) AS Likes,
    SUM(CASE WHEN ar.RatingType = 0 THEN 1 ELSE 0 END) AS Dislikes,
    COUNT(*) AS Total
FROM contenidos c
INNER JOIN ArticleRatings ar ON c.Id = ar.ArticleId
WHERE c.Eliminado = 0
GROUP BY c.ContenidoTitulo
ORDER BY Total DESC;
```

---

## ✅ Sistema Listo

Una vez completados los 5 pasos, el sistema está **100% funcional** y listo para producción.

**Total de archivos creados:** 7
**Total de archivos modificados:** 3
**Tiempo de implementación:** ~5 minutos

---

## 📞 Soporte

Si encuentras algún problema:
1. Revisar este documento
2. Consultar `ARTICLE-RATING-SYSTEM.md` para detalles técnicos
3. Verificar logs del servidor
4. Revisar DevTools Console

**¡Éxito con la implementación! 🎉**
