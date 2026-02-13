# 🎯 Plan Simplificado - Admin/Contenidos/Index

## Estado Actual
- ❌ Código demasiado complejo con múltiples intentos de fix
- ❌ Handler duplicado, código roto
- ❌ No compila

## ✅ Solución: Empezar de Cero - Paso por Paso

### **PASO 1: Restaurar a Versión Funcional Anterior**

Usar Git para volver a una versión que funcionaba ANTES de todos los cambios de GridData:

```powershell
# Ver historial de commits
git log --oneline Areas/Identity/Pages/Admin/Contenidos/Index.cshtml.cs

# Restaurar a un commit anterior que funcionaba
git checkout <commit-hash> -- Areas/Identity/Pages/Admin/Contenidos/Index.cshtml.cs
git checkout <commit-hash> -- Areas/Identity/Pages/Admin/Contenidos/Index.cshtml
```

**O mejor: Usa el backup que creaste con `create-backups.ps1`**

```powershell
# Restaurar desde backup
.\restore-backups.ps1
# Selecciona solo Index.cshtml e Index.cshtml.cs
```

---

### **PASO 2: Implementación Mínima Viable**

Una vez restaurado, crea una versión ULTRA SIMPLE del handler:

#### `Index.cshtml.cs` - Handler Básico

```csharp
[HttpGet]
public async Task<IActionResult> OnGetGridData()
{
    try
    {
        var draw = int.TryParse(Request.Query["draw"], out var d) ? d : 1;
        var start = int.TryParse(Request.Query["start"], out var s) ? s : 0;
        var length = int.TryParse(Request.Query["length"], out var l) ? l : 10;
        
        var query = _db.Contenidos
            .AsNoTracking()
            .Where(c => !c.Eliminado && c.EstadoPublicacion > 0)
            .OrderByDescending(c => c.FechaCreado);

        var total = await query.CountAsync();

        var items = await query
            .Skip(start)
            .Take(length)
            .Select(c => new
            {
                id = c.Id,
                titulo = c.ContenidoTitulo ?? "",
                imagenUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) 
                    ? null 
                    : "/uploads/contenidos/" + c.URLImagenPrincipal
            })
            .ToListAsync();

        var data = items.Select(p => new
        {
            p.id,
            p.titulo,
            p.imagenUrl,
            actions = $"<a href='/Identity/Admin/Contenidos/Detalle?id={p.id}'>Editar</a>"
        }).ToList();

        return new JsonResult(new
        {
            draw,
            recordsTotal = total,
            recordsFiltered = total,
            data
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en GridData");
        return new JsonResult(new 
        { 
            draw = 0, 
            recordsTotal = 0, 
            recordsFiltered = 0, 
            data = new object[0]
        });
    }
}
```

#### `Index.cshtml` - URL Simple

```razor
@{
    var gridDataUrl = "/Identity/Admin/Contenidos/Index?handler=GridData";
}

@section Scripts {
    <script>
        const gridDataUrl = '@gridDataUrl';
        
        $('#contenidosGrid').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: gridDataUrl,
                type: 'GET'
            },
            columns: [
                { data: 'imagenUrl' },
                { data: 'titulo' },
                { data: 'actions', orderable: false }
            ]
        });
    </script>
}
```

---

### **PASO 3: Probar en Local**

1. **Compilar:** `dotnet build`
2. **Ejecutar:** `dotnet run`
3. **Probar:** `https://localhost:5001/Identity/Admin/Contenidos/Index`
4. **Verificar:** DataTable carga con datos básicos

---

### **PASO 4: Agregar Funcionalidad Gradualmente**

Una vez que funciona lo BÁSICO, agregar de a poco:

#### 4.1 Agregar más columnas
```csharp
descripcion = (c.ContenidoTextoC ?? "").Substring(0, Math.Min(100, (c.ContenidoTextoC ?? "").Length)),
autor = c.Autor ?? "",
fechaCreado = c.FechaCreado
```

#### 4.2 Agregar botón Eliminar con JavaScript
```javascript
function eliminarContenido(id) {
    if (!confirm('¿Eliminar este contenido?')) return;
    
    fetch(`/Identity/Admin/Contenidos/Index?handler=Eliminar&id=${id}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            table.ajax.reload();
        } else {
            alert(data.message || 'Error eliminando');
        }
    });
}
```

#### 4.3 Agregar botón Clonar
Similar al de Eliminar

#### 4.4 Agregar filtros básicos
```javascript
data: function(d) {
    d.search = $('#searchBox').val();
}
```

---

### **PASO 5: Deploy a Producción**

Solo cuando funcione **PERFECTAMENTE en local**:

```powershell
dotnet publish -c Release -o ../publish
# Subir via FTP
# Reiniciar aplicación
```

---

## 🎓 Lecciones Aprendidas

1. ❌ **NO** intentar arreglar código que ya está roto agregando más código
2. ✅ **SÍ** volver a una versión funcional y empezar limpio
3. ❌ **NO** hacer cambios complejos directamente en producción
4. ✅ **SÍ** probar TODO en local primero
5. ❌ **NO** tener múltiples soluciones (Page Handler + API Controller) al mismo tiempo
6. ✅ **SÍ** una sola solución simple que funcione

---

## 📋 Checklist de Implementación

- [ ] **1. Restaurar archivos desde backup/git**
- [ ] **2. Crear handler básico OnGetGridData**
- [ ] **3. Crear JavaScript básico con DataTable**
- [ ] **4. Compilar sin errores**
- [ ] **5. Probar en local - ver registros básicos**
- [ ] **6. Agregar columnas una por una**
- [ ] **7. Agregar botón Editar (solo link)**
- [ ] **8. Agregar botón Eliminar (con confirmación)**
- [ ] **9. Agregar botón Clonar**
- [ ] **10. Agregar filtros básicos**
- [ ] **11. Probar TODO en local**
- [ ] **12. Deploy a producción**
- [ ] **13. Verificar en eiibd.com**

---

## ⚠️ Si Algo Falla

1. **NO** agregar más código
2. **SÍ** revisar console del navegador (F12)
3. **SÍ** revisar logs del servidor
4. **SÍ** simplificar aún más
5. **SÍ** pedir ayuda con el error ESPECÍFICO

---

**Siguiente Acción:** Decide si quieres:
- **Opción A:** Restaurar desde backup y empezar limpio
- **Opción B:** Arreglar el archivo actual (más complejo, arriesgado)

**Recomendación:** Opción A - Restaurar y empezar limpio. Es más rápido y seguro.
