# ✅ FIX: Nombres de Autores Inconsistentes en Cards

## 🐛 Problema Reportado

Los nombres de autores en las tarjetas (cards) de contenidos aparecían diferentes en:
- `/Contenidos/Index`
- `/Contenidos/porCategoria`
- `/Contenidos/Detalle` (página individual)
- AJAX "Cargar más"

**Causa:** Faltaba `.Include(c => c.AutorPerfil)` en 3 archivos, causando que EF Core NO cargara la navegación y siempre usara el fallback del campo `Contenidos.Autor`.

---

## ✅ Solución Implementada

### Archivos Modificados:

#### 1. **eiibd26/Pages/Contenidos/Index.cshtml.cs**
```csharp
// Línea ~201: Agregado .Include(c => c.AutorPerfil)
var contentsQuery = _db.Contenidos
    .AsNoTracking()
    .Include(c => c.AutorPerfil)  // ✅ NUEVO
    .Where(c => idsQuery.Contains(c.Id))
    .OrderByDescending(c => c.FechaCreado);
```

#### 2. **eiibd26/Pages/Home/BlogMore.cshtml.cs**
```csharp
// Línea ~151: Agregado .Include(c => c.AutorPerfil)
var contentsQuery = _db.Contenidos
    .AsNoTracking()
    .Include(c => c.AutorPerfil)  // ✅ NUEVO
    .Where(c => idsQuery.Contains(c.Id))
    .OrderByDescending(c => c.FechaCreado);
```

#### 3. **eiibd26/Pages/Contenidos/Detalle.cshtml.cs** ⭐ NUEVO
```csharp
// Líneas ~59-68: Agregado .Include(c => c.AutorPerfil) en AMBOS queries
entity = await _db.Contenidos.AsNoTracking()
    .Include(c => c.AutorPerfil)  // ✅ NUEVO
    .Where(c => !c.Eliminado && c.ContenidoTituloSlug == slug)
    .FirstOrDefaultAsync();

// También en la búsqueda por ID:
entity = await _db.Contenidos.AsNoTracking()
    .Include(c => c.AutorPerfil)  // ✅ NUEVO
    .Where(c => !c.Eliminado && c.Id == id.Value)
    .FirstOrDefaultAsync();
```

#### 4. **eiibd26/Pages/Contenidos/porCategoria.cshtml.cs**
```csharp
// ✅ Ya estaba correcto (línea ~263):
var items = await _db.Contenidos
    .AsNoTracking()
    .Include(c => c.AutorPerfil)  // ✓ YA EXISTÍA
    .Where(...)
```

---

## 📚 Explicación Técnica

### Modelo de Datos:

**Tabla `Contenidos`:**
- `IdAutor` (Guid) → FK a `AspNetUsers`
- `Autor` (string) → Nombre almacenado en texto plano (FALLBACK)

**Tabla `Perfil`:**
- `idUser` (Guid) → FK a `AspNetUsers`
- `Nombre` (string) → Nombre del usuario desde su perfil
- `PrimerApellido` (string)
- `Avatar` (string)
- `slug` (string)

**Navegación en Modelo `Contenido`:**
```csharp
[ForeignKey("IdAutor")]
public virtual Perfil AutorPerfil { get; set; }
```

### Lógica del Query (ya existente, solo faltaba el Include):

```csharp
Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre)) 
    ? c.AutorPerfil.Nombre                           // ← PRIORIDAD: nombre del perfil
    : (string.IsNullOrWhiteSpace(c.Autor) 
        ? "Autor"                                     // ← Fallback si ambos están vacíos
        : c.Autor)                                    // ← Fallback: campo texto en Contenidos
```

**Sin `.Include()`:** EF Core NO ejecuta el JOIN y `c.AutorPerfil` es siempre `null` → usa `c.Autor`.

**Con `.Include()`:** EF Core ejecuta:
```sql
SELECT ...
FROM contenidos c
INNER JOIN Perfil p ON c.IdAutor = p.idUser
```
→ `c.AutorPerfil` se llena correctamente → usa `p.Nombre`.

---

## 🧪 Pruebas

### Verificar en SQL (opcional):
```sql
-- Ver contenidos con sus autores
SELECT 
    c.Id,
    c.ContenidoTitulo,
    c.Autor AS AutorCampoTexto,
    p.Nombre AS AutorPerfil,
    p.PrimerApellido,
    u.Email
FROM contenidos c
INNER JOIN AspNetUsers u ON c.IdAutor = u.Id
LEFT JOIN Perfil p ON u.Id = p.idUser
WHERE c.Eliminado = 0
ORDER BY c.FechaCreado DESC;
```

### Verificar en Frontend:

1. **Página Principal:**
```
https://localhost:7002/contenidos
```
✅ Todos los autores deben mostrar el mismo formato (nombre del perfil)

2. **Página de Categoría:**
```
https://localhost:7002/alimentacion-y-nutricion
```
✅ Autores consistentes con página principal

3. **Página de Detalle Individual:** ⭐ NUEVO
```
https://localhost:7002/alimentacion-y-nutricion/recetas-saludables
```
✅ Autor debe mostrar nombre del perfil, no campo texto

4. **AJAX "Cargar Más":**
```
Scroll down en /contenidos → Click "Cargar más"
```
✅ Nuevos contenidos tienen el nombre correcto

5. **Verificar Avatar:**
```html
<!-- En el HTML renderizado debe aparecer -->
<img src="/path/to/avatar.jpg" alt="Nombre Autor" />
```
✅ Avatar debe cargar desde `Perfil.Avatar`, no placeholder

---

## 📊 Comparativa Antes/Después

### ANTES del Fix:

| Archivo | Query | Resultado |
|---------|-------|-----------|
| `Index.cshtml.cs` | ❌ Sin `.Include()` | Usa `c.Autor` (campo texto) |
| `BlogMore.cshtml.cs` | ❌ Sin `.Include()` | Usa `c.Autor` (campo texto) |
| `Detalle.cshtml.cs` | ❌ Sin `.Include()` | Usa `c.Autor` (campo texto) |
| `porCategoria.cshtml.cs` | ✅ Con `.Include()` | Usa `AutorPerfil.Nombre` ✓ |

**Resultado:** Nombres inconsistentes entre páginas.

### DESPUÉS del Fix:

| Archivo | Query | Resultado |
|---------|-------|-----------|
| `Index.cshtml.cs` | ✅ Con `.Include()` | Usa `AutorPerfil.Nombre` ✓ |
| `BlogMore.cshtml.cs` | ✅ Con `.Include()` | Usa `AutorPerfil.Nombre` ✓ |
| `Detalle.cshtml.cs` | ✅ Con `.Include()` | Usa `AutorPerfil.Nombre` ✓ |
| `porCategoria.cshtml.cs` | ✅ Con `.Include()` | Usa `AutorPerfil.Nombre` ✓ |

**Resultado:** Nombres consistentes en todas las páginas.

---

## 🚀 Deploy

### 1. Build Exitoso:
```
Build successful ✅
```

### 2. Reiniciar Aplicación (REQUERIDO):
```
Hot Reload NO funciona con cambios en queries.
Detén debugging (Shift+F5) e inicia de nuevo (F5).
```

### 3. Verificar en Browser:
- Abre DevTools (F12) → Network
- Recarga `/contenidos`
- Verifica que los nombres de autores sean iguales
- Click "Cargar más" y verifica que nuevos contenidos sean consistentes

---

## 🐛 Troubleshooting

### Problema: Autores siguen siendo diferentes

**Causa posible:** Datos inconsistentes en la tabla `Perfil`.

**Verificar:**
```sql
-- Ver usuarios sin perfil
SELECT u.Id, u.Email, u.UserName
FROM AspNetUsers u
LEFT JOIN Perfil p ON u.Id = p.idUser
WHERE p.idUser IS NULL;
```

**Solución:** Crear perfiles para estos usuarios.

### Problema: Algunos autores muestran "Autor"

**Causa:** `Perfil.Nombre` está vacío o NULL.

**Verificar:**
```sql
SELECT p.idUser, p.Nombre, p.PrimerApellido
FROM Perfil p
WHERE p.Nombre IS NULL OR p.Nombre = '';
```

**Solución:** Actualizar el perfil con nombre válido.

---

## 📝 Conclusión

El problema se debió a **falta de Eager Loading** en 2 archivos. Con `.Include(c => c.AutorPerfil)`, EF Core ahora carga correctamente la navegación y todos los nombres de autores son consistentes.

**Estado:**
- ✅ Index.cshtml.cs: CORREGIDO
- ✅ BlogMore.cshtml.cs: CORREGIDO
- ✅ Detalle.cshtml.cs: CORREGIDO ⭐ NUEVO
- ✅ porCategoria.cshtml.cs: YA ESTABA CORRECTO

**Próximo paso:** Reiniciar la app y probar en frontend.
