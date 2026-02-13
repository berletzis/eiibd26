# ✅ Checklist de Pruebas - Contenidos/Index Simplificado

## 🎯 Objetivo
Verificar que el patrón simplificado (copiado de Condiciones) funciona correctamente en LOCAL antes de deployar a producción.

---

## 📋 Pre-requisitos
- [ ] Build exitoso ✅ (ya confirmado)
- [ ] Servidor local corriendo (F5 en Visual Studio)
- [ ] Usuario logueado con rol "Administrador"

---

## 🧪 Tests Funcionales

### 1️⃣ Carga Inicial
- [ ] Navegar a: `https://localhost:XXXX/Identity/Admin/Contenidos/Index`
- [ ] ¿Se carga la página sin errores?
- [ ] ¿Aparece la grid con contenidos?
- [ ] ¿Se ve el título "Gestión de Contenidos"?
- [ ] **CRÍTICO:** Abrir Console (F12) - ¿Sin errores 404?

**Resultado esperado:**
```
✅ Grid cargada con datos
✅ Console limpio (sin 404 de GridData)
✅ Botones visibles: Editar, Eliminar, Clonar, Ver
```

---

### 2️⃣ Paginación
- [ ] Cambiar "Mostrar X registros" a 25
- [ ] ¿Se recargan los datos?
- [ ] Ir a página siguiente →
- [ ] ¿Cambia el contenido?
- [ ] Regresar a página anterior ←
- [ ] ¿Vuelve al contenido anterior?

**Resultado esperado:**
```
✅ Paginación funcional
✅ Sin errores en Console
✅ Datos se actualizan correctamente
```

---

### 3️⃣ Filtro Categoría Padre
- [ ] Seleccionar una categoría padre (ej: "Salud")
- [ ] ¿Se filtran los contenidos?
- [ ] ¿Se habilita el selector de subcategoría?
- [ ] ¿Aparecen las subcategorías correctas?
- [ ] Cambiar a "(Todas)"
- [ ] ¿Se muestran todos los contenidos otra vez?

**Resultado esperado:**
```
✅ Filtro funciona
✅ Subcategorías se populan dinámicamente
✅ Grid se actualiza al cambiar filtro
```

---

### 4️⃣ Filtro Subcategoría
- [ ] Seleccionar categoría padre primero
- [ ] Seleccionar una subcategoría
- [ ] ¿Se filtran los contenidos a esa subcategoría?
- [ ] Cambiar a otra subcategoría
- [ ] ¿Cambia el filtro?

**Resultado esperado:**
```
✅ Subcategorías filtran correctamente
✅ Grid se actualiza
```

---

### 5️⃣ Switches de Filtro
- [ ] Activar "Mostrar eliminados"
- [ ] ¿Aparecen contenidos marcados como eliminados?
- [ ] Desactivar switch
- [ ] ¿Desaparecen los eliminados?
- [ ] Activar "Mostrar borradores"
- [ ] ¿Aparecen borradores (EstadoPublicacion = 0)?
- [ ] Activar "Mostrar imágenes"
- [ ] ¿Afecta la visualización? (puede no tener efecto visible)

**Resultado esperado:**
```
✅ Cada switch afecta el filtrado
✅ Grid se recarga al cambiar switch
```

---

### 6️⃣ Búsqueda de Texto
- [ ] Escribir en el campo "Buscar..."
- [ ] Ingresar texto (ej: "salud", "diabetes", etc.)
- [ ] ¿Se filtran los resultados?
- [ ] Borrar texto
- [ ] ¿Vuelven todos los resultados?

**Resultado esperado:**
```
✅ Búsqueda funciona
✅ Filtra por título, descripción, autor
```

---

### 7️⃣ Botón Editar
- [ ] Click en botón Editar (ícono lápiz) de cualquier fila
- [ ] ¿Abre la página Detalle?id=X?
- [ ] ¿URL es `/Identity/Admin/Contenidos/Detalle?id=123`?
- [ ] ¿Se carga el contenido para editar?

**Resultado esperado:**
```
✅ Redirige a página de detalle
✅ URL correcta con ID
```

---

### 8️⃣ Botón Eliminar
- [ ] Click en botón Eliminar (ícono basura) de una fila
- [ ] ¿Aparece confirmación "¿Eliminar contenido?"?
- [ ] Click "Aceptar"
- [ ] ¿Desaparece el contenido de la grid?
- [ ] Activar "Mostrar eliminados"
- [ ] ¿Aparece el contenido con badge "Eliminado: Sí"?

**Resultado esperado:**
```
✅ Eliminación funciona
✅ Grid se recarga automáticamente
✅ Contenido marcado como eliminado (soft delete)
```

---

### 9️⃣ Botón Clonar
- [ ] Click en botón Clonar (ícono archivos) de una fila
- [ ] ¿Aparece confirmación "¿Clonar este contenido?"?
- [ ] Click "Aceptar"
- [ ] ¿Redirige a página de Detalle del contenido clonado?
- [ ] ¿El contenido tiene datos copiados del original?

**Resultado esperado:**
```
✅ Clonación funciona
✅ Redirige a editor del nuevo contenido
✅ Datos copiados correctamente
```

---

### 🔟 Botón Refresh Sitemap
- [ ] Click en botón "Actualizar sitemap"
- [ ] ¿Cambia a "Actualizando..." con spinner?
- [ ] ¿Aparece mensaje de éxito con contadores?
- [ ] Verificar en Console: ¿Request a `/admin/sitemap/refresh`?

**Resultado esperado:**
```
✅ Botón ejecuta refresh
✅ Muestra resultado (contenidos: X, preguntas: Y, categorías: Z)
```

---

## 🔍 Tests de Console (F12)

### Red (Network)
- [ ] Abrir pestaña Network
- [ ] Recargar página
- [ ] Buscar request a `GridData`
- [ ] ¿Estado 200 OK?
- [ ] ¿Response tiene datos JSON?
- [ ] Click en siguiente página
- [ ] ¿Nuevo request a GridData con start/length correctos?

**Resultado esperado:**
```
✅ Request: GET /Identity/Admin/Contenidos/Index?handler=GridData
✅ Status: 200
✅ Response: JSON con {draw, recordsTotal, recordsFiltered, data[]}
✅ Sin errores 404
```

### Console
- [ ] Abrir pestaña Console
- [ ] ¿Sin errores rojos?
- [ ] ¿Sin warnings de "Failed to fetch"?
- [ ] ¿Sin errores 404?

**Resultado esperado:**
```
✅ Console limpio
✅ Sin errores JavaScript
✅ Sin errores 404 de GridData ⭐⭐⭐
```

---

## 🎯 Test Crítico (URL Simple)

### Verificar URL Simple
- [ ] Inspeccionar código fuente (Ctrl+U)
- [ ] Buscar texto: `@Url.Page(null, "GridData")`
- [ ] ¿Está presente en el HTML?
- [ ] En Console, ejecutar:
  ```javascript
  $('#contenidosGrid').DataTable().ajax.url()
  ```
- [ ] ¿URL termina con `?handler=GridData` (sin otros params)?

**Resultado esperado:**
```
✅ URL generada: /Identity/Admin/Contenidos/Index?handler=GridData
✅ Sin parámetros adicionales en URL base
✅ Parámetros se envían en request body (data function)
```

---

## ✅ Checklist Final

### Local Funcional
- [ ] Todos los tests anteriores ✅
- [ ] Console sin errores 404 ⭐
- [ ] Grid carga datos correctamente
- [ ] Filtros funcionan
- [ ] Botones funcionan (Editar, Eliminar, Clonar)
- [ ] Paginación funciona

### Listo para Producción
- [ ] Local 100% funcional
- [ ] Sin cambios en backend (Index.cshtml.cs)
- [ ] Build exitoso
- [ ] Backup realizado (Git commit)

---

## 🚀 Si TODO está ✅

### Pasos para Deploy:
1. Hacer commit en Git:
   ```
   git add .
   git commit -m "Simplificado Contenidos/Index - patrón Condiciones sin URL state"
   ```

2. Publicar proyecto:
   ```
   Botón derecho en proyecto → Publish
   Seleccionar perfil → Publish
   ```

3. Subir por FTP a eiibd.com:
   - Carpeta completa publish
   - Sobrescribir archivos

4. Reiniciar aplicación en servidor

5. Probar en producción:
   - Abrir https://eiibd.com/Identity/Admin/Contenidos/Index
   - Repetir tests críticos (especialmente #1 y #11)
   - **VERIFICAR Console sin 404** ⭐⭐⭐

---

## 📞 Si algo falla

### En Local:
1. Revisar Console (F12)
2. Verificar request en Network tab
3. Verificar que handler existe en .cshtml.cs

### En Producción:
1. Verificar que archivos se subieron correctamente
2. Verificar permisos de archivos
3. Reiniciar aplicación/pool
4. Revisar logs del servidor
5. Si nada funciona: Restore from backup

---

## 📊 Comparación con Estado Anterior

| Aspecto | ANTES (Con URL state) | AHORA (Sin URL state) |
|---------|----------------------|----------------------|
| **Console 404** | ❌ Error en producción | ✅ Sin errores esperado |
| **Complejidad** | ~310 líneas JS | ~250 líneas JS |
| **Mantenibilidad** | Media | Alta |
| **Debugging** | Difícil | Fácil |
| **URL compartible** | ✅ Sí (con filtros) | ❌ No (sin filtros) |
| **Estado persistente** | ✅ Sí | ❌ No |
| **Funcionalidad core** | ✅ Completa | ✅ Completa |
| **Patrón probado** | ❌ Custom | ✅ Copia de Condiciones |

---

**NOTA IMPORTANTE:** El objetivo principal es **eliminar el error 404 de GridData en producción**. 
La pérdida de estado en URL es un trade-off aceptable por tener código más simple y funcional.

---

**Creado:** 2025  
**Estrategia:** Copiar patrón probado (Condiciones) que SÍ funciona  
**Prioridad:** Funcionalidad > Features
