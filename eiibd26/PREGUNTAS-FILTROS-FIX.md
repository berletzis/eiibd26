# ✅ Correcciones en Página de Preguntas - Resumen

## 🎯 Problemas Corregidos

### 1. ✅ Combo de Tamaño Ahora Solo Tiene 15, 50, 100

**Antes:**
```csharp
var sizes2 = new[] { 10, 12, 30, 60, 100 };
```

**Ahora:**
```csharp
var sizes2 = new[] { 15, 50, 100 };
```

**También se cambió el valor por defecto:**
- Antes: `PageSize = 12`
- Ahora: `PageSize = 15`

---

### 2. ✅ Paginación Ahora Preserva Filtro de Orden

**Antes:** Al cambiar de página, se perdía el filtro de ordenamiento (Activas/Recientes/Votadas)

```csharp
Func<int, string> pageUrl = p => Url.Page("/Preguntas", null, 
    new { search = baseSearch, pageSize = Model.PageSize, pageNumber = p }, 
    null) ?? "#";
```

**Ahora:** Se preserva el filtro de orden al paginar

```csharp
Func<int, string> pageUrl = p => Url.Page("/Preguntas", null, 
    new { search = baseSearch, pageSize = Model.PageSize, pageNumber = p, orden = Model.Orden }, 
    null) ?? "#";
```

---

### 3. ✅ Combo de Tamaño Ahora Preserva Filtro de Orden

**Antes:** Al cambiar tamaño de página, se perdía el filtro de ordenamiento

```html
<form method="get" class="pag-size-inline" id="inlineSizeForm">
    <input type="hidden" name="search" value="@Model.Search" />
    <input type="hidden" name="pageNumber" value="1" />
    <!-- ❌ Faltaba el orden -->
    ...
</form>
```

**Ahora:** Se incluye el filtro de orden

```html
<form method="get" class="pag-size-inline" id="inlineSizeForm">
    <input type="hidden" name="search" value="@Model.Search" />
    <input type="hidden" name="orden" value="@Model.Orden" />
    <input type="hidden" name="pageNumber" value="1" />
    <!-- ✅ Ahora preserva el orden -->
    ...
</form>
```

---

## 📁 Archivos Modificados

1. **`eiibd26/Pages/Preguntas.cshtml`** (2 cambios)
   - Línea ~238: Agregado `orden = Model.Orden` en función `pageUrl` (primer bloque paginación)
   - Línea ~276: Agregado `<input type="hidden" name="orden" value="@Model.Orden" />` en form de tamaño
   - Línea ~280: Cambiado array de tamaños a `new[] { 15, 50, 100 }`
   - Línea ~520: Agregado `orden = Model.Orden` en función `pageUrl` (segundo bloque paginación)
   - Línea ~558: Agregado `<input type="hidden" name="orden" value="@Model.Orden" />` en form de tamaño
   - Línea ~562: Cambiado array de tamaños a `new[] { 15, 50, 100 }`

2. **`eiibd26/Pages/Preguntas.cshtml.cs`** (2 cambios)
   - Línea 62: Cambiado `PageSize = 12` → `PageSize = 15`
   - Línea 83: Cambiado `PageSize = 12` → `PageSize = 15`

---

## 🧪 Testing

### Test 1: Combo de Tamaño
1. Ir a `/Preguntas`
2. Abrir combo "Tamaño"
3. ✅ Verificar que solo tiene opciones: **15, 50, 100**

### Test 2: Paginación con Filtro de Orden
1. Ir a `/Preguntas`
2. Hacer clic en tab "**Recientes**"
3. Hacer clic en "**Página 2**"
4. ✅ Verificar que sigue mostrando tab "Recientes" activo
5. ✅ Verificar URL: `/Preguntas?pageNumber=2&pageSize=15&search=&orden=Recientes`

### Test 3: Combo de Tamaño con Filtro de Orden
1. Ir a `/Preguntas`
2. Hacer clic en tab "**Más votadas**"
3. Cambiar tamaño a "**50**"
4. ✅ Verificar que sigue mostrando tab "Más votadas" activo
5. ✅ Verificar URL: `/Preguntas?search=&orden=Votadas&pageNumber=1&pageSize=50`

### Test 4: Búsqueda + Orden + Paginación
1. Ir a `/Preguntas`
2. Buscar: "**diabetes**"
3. Hacer clic en tab "**Recientes**"
4. Cambiar tamaño a "**100**"
5. Hacer clic en "**Página 2**" (si hay)
6. ✅ Verificar que todos los filtros se mantienen
7. ✅ Verificar URL: `/Preguntas?search=diabetes&orden=Recientes&pageNumber=2&pageSize=100`

---

## ✅ Estado

- ✅ Código compilado sin errores
- ✅ Combo de tamaño: **15, 50, 100** solamente
- ✅ Filtros se preservan al paginar
- ✅ Filtros se preservan al cambiar tamaño
- ✅ Búsqueda + Orden + Tamaño + Paginación funcionan juntos

---

## 🎯 Resumen de Comportamiento

**Todos los filtros ahora funcionan correctamente juntos:**

| Acción | Búsqueda | Orden | Tamaño | Página |
|--------|----------|-------|--------|--------|
| Buscar texto | ✅ Mantiene | ✅ Mantiene | ✅ Mantiene | → Reset a 1 |
| Cambiar tab orden | ✅ Mantiene | ✅ Cambia | ✅ Mantiene | → Reset a 1 |
| Cambiar tamaño | ✅ Mantiene | ✅ Mantiene | ✅ Cambia | → Reset a 1 |
| Cambiar página | ✅ Mantiene | ✅ Mantiene | ✅ Mantiene | ✅ Cambia |

**Todos los escenarios mantienen el estado de filtros correctamente** ✅

---

## 📝 Notas Técnicas

1. **Hay 2 bloques de paginación** en el archivo `.cshtml`:
   - Uno al inicio (antes de la lista)
   - Uno al final (después de la lista)
   - Ambos fueron actualizados para consistencia

2. **PageSize por defecto**: Ahora es `15` en lugar de `12`

3. **Orden por defecto**: `Activas` (ordenar por última actividad)

---

**¡Filtros, paginación y tamaño ahora funcionan perfectamente!** 🚀
