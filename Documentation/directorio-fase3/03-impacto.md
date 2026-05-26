# Impacto de Implementación — Opción B (`ConfirmacionComunitariaArea`)

**Fecha:** 2026-05-25  
**Modelo seleccionado:** Nueva tabla relacional `ConfirmacionComunitariaArea`

---

## 1. Cambios de esquema BD (SQL directo)

| Artefacto | Operación | Riesgo |
|-----------|-----------|--------|
| Tabla `ConfirmacionComunitariaArea` | CREATE (idempotente) | Ninguno — nueva tabla |
| Índice `IX_CCA_ConfirmacionId` | CREATE | Ninguno |
| Constraint `UQ_CCA_Conf_Area` | Dentro del CREATE | Ninguno |
| Tablas existentes | Sin tocar | N/A |

**Sin ALTER TABLE en tablas existentes. Sin DROP. Sin migración de datos.**

---

## 2. Archivos C# a modificar

| Archivo | Tipo de cambio | Descripción |
|---------|---------------|-------------|
| `Models/Directorio/ConfirmacionComunitariaArea.cs` | CREAR | Nuevo modelo |
| `Models/Directorio/ConfirmacionComunitaria.cs` | AGREGAR | Navigation property `ICollection<ConfirmacionComunitariaArea> Areas` |
| `Data/ApplicationDbContext.cs` | AGREGAR | `DbSet<ConfirmacionComunitariaArea>` |
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | MODIFICAR | `OnPostConfirmarSimpleAsync` guarda áreas seleccionadas |
| `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs` | MODIFICAR | Load con `.Include(c => c.Areas).ThenInclude(a => a.Area)`, populate `Exp*` from areas |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` | MODIFICAR | `OnGetMedicoAsync`: `expContadores` desde areas; `confirmadores.exps` desde areas |

---

## 3. Archivos de UI a modificar

| Archivo | Tipo de cambio | Descripción |
|---------|---------------|-------------|
| `Pages/DirectorioMedicos/Detalle.cshtml` | AGREGAR | Checkboxes de áreas EII en formulario de confirmación |

**Nota:** `Dashboard.cshtml` y `Admin/Index.cshtml` **no requieren cambios** — el backend popula los mismos campos del VM (`ExpCUCI`, `expContadores`, `exps`) que la vista ya consume.

---

## 4. Impacto por pantalla

### Dashboard médico — Lista de confirmaciones
- **Antes (Fase 1):** `ExpCUCI=true`, `ExpCrohn=true` etc. desde campos booleanos
- **Después de Fase 2:** `ExpCUCI=false` siempre → fila sin áreas
- **Después de Fase 3:** `ExpCUCI = c.Areas.Any(a => a.Area.Nombre == "CUCI")`
- **Vista:** Sin cambios — sigue leyendo los bool fields del VM
- **Comportamiento:** Tags "CUCI", "Crohn" reaparecen para nuevas confirmaciones; históricas permanecen sin tags (correcta — no tuvieron granularidad por área)

### Admin — `expContadores`
- **Después de Fase 2:** Todos en 0
- **Después de Fase 3:** `confs.Count(c => c.Areas.Any(ar => ar.Area.Nombre == nombreArea))`
- **Vista JS:** Sin cambios — consume mismo JSON `{nombre: string, total: int}[]`
- **Comportamiento:** Conteos reales por área para confirmaciones nuevas; históricas cuentan 0 por área (correcto)

### Admin — `confirmadores[].exps`
- **Después de Fase 2:** `[TipoConfirmacion.Nombre]` (ej: `["Paciente atendido"]`)
- **Después de Fase 3:** `c.Areas.Select(a => a.Area.Nombre).ToList()` (ej: `["CUCI", "Crohn"]`)
- **Vista JS:** Sin cambios — array de strings
- **Comportamiento:** Tags de área correctos para confirmaciones nuevas

### Formulario de confirmación (`Detalle.cshtml`)
- **Cambio requerido:** Agregar checkboxes de `AreaExperienciaEii` (cargados desde BD) al formulario
- **El paciente puede seleccionar**: 0 o más áreas (opcionales)
- **Backend:** Al POST, parsear IDs seleccionados e insertar en `ConfirmacionComunitariaArea`
- **Compatibilidad:** 0 áreas seleccionadas es válido (confirmación sin área = comportamiento actual Fase 2)

### Nivel de confianza (`RecalcularNivelAsync`, `RecalcularNivelConfianzaAsync`)
- **Sin impacto.** La lógica usa solo `total > 0`, no áreas.

### Badge `activo_comunidad`
- **Sin impacto.** Solo cuenta `total >= 5`.

### Listado tarjetas / `TotalConfirmaciones`
- **Sin impacto.** Solo conteo.

---

## 5. Compatibilidad hacia atrás

| Escenario | Comportamiento |
|-----------|----------------|
| Confirmaciones antiguas (antes de Fase 3) | `c.Areas` = lista vacía → `Exp* = false` → sin tags (igual que Fase 2) |
| Nuevas confirmaciones sin áreas seleccionadas | Ídem — el campo es opcional |
| Nuevas confirmaciones con áreas | Tags aparecen en Dashboard y Admin |
| Datos en `DirectorioMedicoConfirmacion` (tabla histórica) | No se toca — datos archivados sin uso activo |

**Sin pérdida de datos. Sin rotura de funcionalidades existentes.**

---

## 6. Tareas de implementación (ordenadas)

| # | Tarea | Archivo(s) | Tipo |
|---|-------|-----------|------|
| 1 | Script SQL crear `ConfirmacionComunitariaArea` | `SQL/YYYY-MM-DD-confirmacion-comunitaria-area.sql` | SQL |
| 2 | Modelo `ConfirmacionComunitariaArea.cs` | `Models/Directorio/` | CREAR |
| 3 | Navigation en `ConfirmacionComunitaria` | `Models/Directorio/ConfirmacionComunitaria.cs` | AGREGAR |
| 4 | DbSet en `ApplicationDbContext` | `Data/ApplicationDbContext.cs` | AGREGAR |
| 5 | Cargar áreas en `Detalle.cshtml.cs` OnGet | `Pages/DirectorioMedicos/Detalle.cshtml.cs` | MODIFICAR |
| 6 | Checkboxes de áreas en formulario confirmación | `Pages/DirectorioMedicos/Detalle.cshtml` | AGREGAR |
| 7 | Guardar áreas en `OnPostConfirmarSimpleAsync` | `Pages/DirectorioMedicos/Detalle.cshtml.cs` | MODIFICAR |
| 8 | Include áreas en Dashboard query | `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs` | MODIFICAR |
| 9 | Populate `Exp*` del VM desde areas | `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs` | MODIFICAR |
| 10 | `expContadores` y `exps` desde areas en Admin | `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` | MODIFICAR |

---

## 7. Riesgos y mitigaciones

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| N+1 queries en Dashboard al cargar áreas | MEDIA | Usar `.Include().ThenInclude()` — EF Core genera JOIN único |
| Nombre de área en BD no coincide con string hardcoded ("CUCI" vs "CUCI ") | BAJA | Normalizar con `.Trim()` al comparar; o usar comparación case-insensitive |
| Formulario de confirmación nuevo (Detalle) rompe UX | BAJA | Checkboxes opcionales — no bloquean el submit |
| Datos históricos sin áreas muestran tags vacíos | NO es riesgo | Es el comportamiento correcto y esperado |
