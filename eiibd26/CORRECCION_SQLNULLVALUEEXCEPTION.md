# ✅ CORRECCIÓN - Error SqlNullValueException en Síntomas/Tratamientos Grid

## Problema

**Error:** 
```
System.Data.SqlTypes.SqlNullValueException: Data is Null. 
This method or property cannot be called on Null values.
```

**Ubicación:** Grid Data de Síntomas y Tratamientos

**Causa:** Después de agregar los nuevos campos NULL (`DescripcionIA`, `ValidadoIA`, etc.), EF Core intentaba cargar toda la entidad incluyendo esos campos, pero fallaba al mapear valores NULL a tipos que no los aceptaban.

---

## Solución Aplicada

### Cambios en ambos archivos:

1. **Agregar `.AsNoTracking()`** - Indica a EF Core que no necesita rastrear los cambios
2. **Usar `.Select()`** - Proyectar solo las columnas necesarias para el grid
3. **Excluir columnas nuevas** - No incluir los campos NULL que causan el error en el query inicial

### Archivos Corregidos:

- ✅ `Areas/Identity/Pages/Admin/Sintomas/Index.cshtml.cs`
- ✅ `Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml.cs`

---

## Cambio Específico

**ANTES:**
```csharp
var baseQuery = _db.sintomas.Where(s => mostrarEliminados || !s.Eliminado);

var filtered = string.IsNullOrEmpty(searchValue)
    ? await baseQuery.ToListAsync()  // ❌ Intenta cargar TODAS las columnas
    : await baseQuery...ToListAsync();
```

**DESPUÉS:**
```csharp
var baseQuery = _db.sintomas
    .AsNoTracking()
    .Where(s => mostrarEliminados || !s.Eliminado)
    .Select(s => new  // ✅ Proyecta solo estas columnas
    {
        s.id,
        s.nombre,
        s.idPadre,
        s.idIdioma,
        s.Eliminado,
        s.icono
        // ❌ NO incluye: DescripcionIA, ValidadoIA, etc.
    });
```

---

## ¿Por qué funciona?

1. **`.AsNoTracking()`** - Más rápido porque no guarda cambios
2. **`.Select()`** - Solo carga lo que necesita el grid
3. **Evita NULL values** - Los campos nuevos no son incluidos en el query inicial

---

## Próximo Paso

Los campos nuevos (ValidadoIA, ValidadoHumano, RelacionEII) se cargarán cuando:
- Se abre el formulario de edición
- Se usa un endpoint específico GET para obtener los detalles

---

## Verificación

Intenta nuevamente con:
```
https://localhost:7002/Identity/Admin/Sintomas?handler=GridData
```

Debería funcionar sin errores ahora. ✅

