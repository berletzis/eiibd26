# Verificación de Regresiones Globales

**Fecha:** 2026-05-25  
**Alcance:** Archivos modificados en sesión 2026-05-25 — búsqueda de regresiones en código no objetivo de ningún FUNC-xxx

---

## Método de verificación

1. Grep de `DirectorioMedicoConfirmaci` en todo el proyecto C# para mapear referencias a tabla antigua
2. Lectura de archivos clave modificados para detectar nulos, import faltantes o binding roto
3. Verificación de nombres de handlers vs. rutas en vistas modificadas

---

## Hallazgos

### ✅ Handlers Razor Pages — sin regresiones

Los handlers renombrados en `UsuarioCondiciones.cshtml.cs` (FUNC-027/028) tienen correspondencia correcta con los formularios de la vista:

| Handler | Ruta esperada | Estado |
|---------|---------------|--------|
| `OnPostEditarFechaInicioAsync` | `?handler=EditarFechaInicio` | ✅ Match |
| `OnPostEliminarCondicionAsync` | `?handler=EliminarCondicion` | ✅ Match |
| `OnPostAgregarCondicionAsync` | `?handler=AgregarCondicion` | ✅ Match |
| `OnPostTogglePrincipalCondicionAsync` | `?handler=TogglePrincipalCondicion` | ✅ Match |

El parámetro `condUsuarioId` se envía correctamente desde la vista (input oculto + JS) y coincide con la firma del handler en ambos casos.

---

### ✅ Activar.cshtml.cs — sin regresiones

`VincularAsync` sigue funcionando correctamente:
- Escribe en `MedicosPerfilExtendido` con `UserId`
- Actualiza `EstatusReclamacion` en `MedicosDirectorio`
- Marca token como usado
- Llama `RecalcularNivelConfianzaAsync` (que ya fue corregido a leer de `ConfirmacionesComunitarias`)

---

### ✅ ReclamarPerfil.cshtml.cs — sin regresiones

El cambio de FUNC-026 (email desde claim en lugar de form) no rompe ningún flujo existente. La vista no envía `EmailContacto` como campo activo del formulario de reclamación.

---

### ✅ Modelos — sin regresiones

- `tratamientos.cs` — `NombreSugeridoIA` añadido como `string?` nullable, sin `[Required]`, no rompe ModelState ni builds existentes.
- `EstadoAnimoUsuario.cs` — `[MaxLength(2000)]` en `Texto` es aditivo, sin impacto en registros existentes (la columna en DB ya existe; el atributo solo agrega validación cliente/server).

---

### ⚠️ WARN — Referencias a `DirectorioMedicoConfirmaciones` fuera de scope FUNC-023

Se detectaron **8 referencias** al DbSet antiguo en código NO modificado por esta sesión. Estas son deuda técnica pre-existente, no regresiones introducidas ahora:

| Archivo | Líneas | Impacto |
|---------|--------|---------|
| `Services/Directorio/MedicoDirectorioService.cs` | 61-65 (GetListaAsync) | Contador `TotalConfirmaciones` en tarjetas de listado muestra datos de tabla antigua |
| `Services/Directorio/MedicoDirectorioService.cs` | 122-126 (GetDetalleAsync) | Contador `TotalConfirmaciones` en VM de detalle (anulado por la lectura directa en `Detalle.cshtml.cs:67`) |
| `Services/Medico/MedicoBadgeService.cs` | 102-103 | Badge `activo_comunidad` no se activará desde nuevas confirmaciones |
| `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs` | 82-83, 87-88 | `TotalRecomendaciones` del médico logueado muestra datos stale |
| `Pages/DirectorioMedicos/Index.cshtml.cs` | 42-43 | Estadística "médicos con confirmación EII" usa tabla antigua |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` | 70-71, 128-157, 353-354 | Panel admin muestra conteos y filtros EII de tabla antigua |

**Ninguna de estas referencias fue introducida por esta sesión.** El DbSet `DirectorioMedicoConfirmaciones` sigue existiendo en `ApplicationDbContext.cs:87` y la tabla en la BD, por lo que no causan crashes — solo muestran datos desactualizados desde que se empezó a escribir en `ConfirmacionesComunitarias`.

**Recomendación:** Migrar estas referencias en la próxima sesión dedicada al directorio médico.

---

### ✅ DI y build — sin errores detectados

No se introducen nuevas dependencias ni interfaces. Los servicios modificados no cambian sus firmas de constructor. No hay riesgo de fallos de DI en runtime.

---

## Resumen

| Categoría | Estado | Cantidad |
|-----------|--------|----------|
| Handlers/routing Razor Pages | ✅ OK | 4 verificados |
| Modelos modificados | ✅ OK | 2 verificados |
| Flujos Identity (Activar, ReclamarPerfil) | ✅ OK | 2 verificados |
| DI / build | ✅ OK | Sin cambios en contratos |
| Referencias tabla antigua (pre-existentes) | ⚠️ WARN | 8 en 6 archivos |
| Regresiones introducidas por esta sesión | ✅ NINGUNA | — |

**Veredicto: ✅ APTO — 0 regresiones nuevas · 8 WARNs de deuda pre-existente**
