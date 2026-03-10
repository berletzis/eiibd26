# RESUMEN EJECUTIVO - Mejoras a Síntomas y Tratamientos

## 📋 OVERVIEW

Se van a agregar capacidades de IA a la administración de Síntomas y Tratamientos con:
- Generación automática de descripciones de pacientes
- Validación de relación con EII
- Sistema de notas colaborativas
- Cambio de UI: Modal → Panel Lateral

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### FASE 1: Modelos y Base de Datos (COMPLETADA)
- [x] Crear modelo `SintomasNotas.cs`
- [x] Crear modelo `TratamientosNotas.cs`
- [x] Actualizar modelo `sintomas.cs` con campos nuevos
- [x] Actualizar modelo `tratamientos.cs` con campos nuevos

### FASE 2: Migraciones (NEXT)
- [ ] Ejecutar: `Add-Migration AgregaSintomasYTratamientosIA`
- [ ] Ejecutar: `Update-Database`
- [ ] Verificar tablas en SQL Server

### FASE 3: Backend API (NEXT)
- [ ] Crear endpoint POST: `/api/admin/sintomas/{id}/generate-ia-description`
- [ ] Crear endpoint POST: `/api/admin/tratamientos/{id}/generate-ia-description`
- [ ] Integrar con Claude API (reutilizar servicio existente)
- [ ] Crear endpoint GET: `/api/admin/sintomas/{id}` (para cargar datos)
- [ ] Crear endpoint PUT: `/api/admin/sintomas/{id}` (para guardar)
- [ ] Crear endpoint GET: `/api/admin/tratamientos/{id}` (para cargar datos)
- [ ] Crear endpoint PUT: `/api/admin/tratamientos/{id}` (para guardar)

### FASE 4: Frontend - Grid (NEXT)
- [ ] Agregar columnas al grid:
  - [ ] `ValidadoIA` (checkbox icon)
  - [ ] `ValidadoHumano` (checkbox icon)
  - [ ] `RelacionEII` (texto corto)
- [ ] Cambiar botón "Editar" para abrir panel lateral
- [ ] Actualizar estilos del grid

### FASE 5: Frontend - Panel Lateral (NEXT)
- [ ] Agregar CSS para panel lateral (65/35 layout)
- [ ] Crear formulario de edición (reemplazar modal)
- [ ] Implementar logica de abrir/cerrar panel
- [ ] Agregar botón "Generar Descripción IA"
- [ ] Conectar con API de generación de IA
- [ ] Auto-guardar después de generar descripción

### FASE 6: Testing (NEXT)
- [ ] Probar generación de descripciones
- [ ] Probar guardado de datos
- [ ] Probar validación de campos
- [ ] Probar en diferentes navegadores

---

## 📁 ARCHIVOS CREADOS/MODIFICADOS

### Nuevos Archivos
```
✓ eiibd26/Models/SintomasNotas.cs
✓ eiibd26/Models/TratamientosNotas.cs
✓ eiibd26/MIGRACION_SINTOMAS_TRATAMIENTOS.md
✓ eiibd26/INSTRUCCIONES_MIGRACION.md
✓ eiibd26/ENDPOINT_IA_DESCRIPCION.md
✓ eiibd26/CAMBIO_MODAL_A_PANEL_LATERAL.md
```

### Archivos Modificados
```
✓ eiibd26/Models/sintomas.cs (agregados campos IA)
✓ eiibd26/Models/tratamientos.cs (agregados campos IA)
```

### Archivos por Modificar
```
- eiibd26/Areas/Identity/Pages/Admin/Sintomas/Index.cshtml
- eiibd26/Areas/Identity/Pages/Admin/Sintomas/Index.cshtml.cs
- eiibd26/Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml
- eiibd26/Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml.cs
- eiibd26/Controllers/AdminController.cs (nuevo) o API existente
```

---

## 🎯 CAMPOS AGREGADOS A MODELOS

### sintomas / tratamientos
```
- DescripcionIA (string, NULL)
- ValidadoIA (bool, default=false)
- ValidadoHumano (bool, default=false)
- RelacionEII (string, NULL)
- FechaActualizacionIA (DateTime?, NULL)
```

### SintomasNotas
```
- id (int, PK)
- SintomaId (int, FK)
- UsuarioId (Guid?, FK nullable)
- Nota (string)
- EsNotaIA (bool, default=false)
- FechaCreado (DateTime)
- FechaModificado (DateTime)
- Eliminado (bool)
```

### TratamientosNotas
```
- id (int, PK)
- TratamientoId (int, FK)
- UsuarioId (Guid?, FK nullable)
- Nota (string)
- EsNotaIA (bool, default=false)
- FechaCreado (DateTime)
- FechaModificado (DateTime)
- Eliminado (bool)
```

---

## 📊 GRID UPDATES

### Columnas Nuevas (en ambos grids)
| Columna | Tipo | Descripción |
|---------|------|-------------|
| ValidadoIA | Icon | ✅ si true, ❌ si false |
| ValidadoHumano | Icon | ✅ si true, ❌ si false |
| RelacionEII | Texto corto | "Sí" / "No" / "..." |
| Acciones | Button | Editar (abre panel) |

---

## 🤖 INTEGRACIÓN CON CLAUDE API

El sistema reutilizará:
- Servicio existente `IClaudeAiService`
- Configuración existente de API Key
- Patrón de integración actual

**Nuevos prompts incluidos:** Ver `ENDPOINT_IA_DESCRIPCION.md`

---

## 🎨 UI/UX CHANGES

### Antes (Modal)
```
Grid completo → Click Edit → Modal emerge → Interfiere con grid
```

### Después (Panel Lateral)
```
├─ Grid (65%) │ Panel (35%)
│             │ • Siempre visible
│             │ • No interfiere
│             │ • Mejor UX
```

---

## 📝 PROMPTS DE IA

### Para Síntomas
- Descripción en lenguaje simple (pacientes, no médicos)
- 4 ejemplos cotidianos
- Max 120 palabras
- Determina relación EII

### Para Tratamientos
- Descripción de propósito y forma de uso
- 3-4 ejemplos de administración
- Max 120 palabras
- Determina relación EII

---

## 🚀 PRÓXIMOS PASOS

1. **Primero:** Ejecutar migraciones EF Core
2. **Segundo:** Crear los endpoints API
3. **Tercero:** Actualizar views y HTML
4. **Cuarto:** Agregar CSS y JavaScript
5. **Quinto:** Testing y ajustes

---

## 💡 NOTAS IMPORTANTES

- Los campos `DescripcionIA` permiten NULL (para items sin descripción generada)
- `ValidadoIA` se marca automáticamente cuando se genera descripción
- `ValidadoHumano` se marca manualmente por administradores
- Las notas son opcionales y colaborativas
- El sistema es **non-blocking**: si Claude API falla, el admin puede intentar de nuevo

---

## 📚 DOCUMENTACIÓN DE REFERENCIA

Consulta estos archivos para más detalles:

1. **MIGRACION_SINTOMAS_TRATAMIENTOS.md** - SQL queries y estructura
2. **INSTRUCCIONES_MIGRACION.md** - Paso a paso para EF Core
3. **ENDPOINT_IA_DESCRIPCION.md** - API endpoints y prompts
4. **CAMBIO_MODAL_A_PANEL_LATERAL.md** - CSS y HTML para UI

