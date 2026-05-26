# Análisis — Granularidad EII en Confirmaciones Comunitarias

**Fecha:** 2026-05-25  
**Estado actual:** Fase 2 completada. Fuente única `ConfirmacionesComunitarias`. Sin granularidad EII por confirmación.

---

## 1. Taxonomías existentes

### `AreaExperienciaEii` (tabla `AreaExperienciaEii`)
Taxonomía oficial de áreas EII del directorio. Ya existe en BD.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int PK | — |
| `Nombre` | string(100) | "CUCI", "Crohn", "Pediátrico", etc. |
| `Descripcion` | string(300)? | — |
| `Orden` | int | Orden de display |
| `Activo` | bool | — |

Las 10 áreas del sistema antiguo (booleanos en `DirectorioMedicoConfirmacion`):

| # | Campo antiguo | Nombre en `AreaExperienciaEii` |
|---|---------------|-------------------------------|
| 1 | `ExpCUCI` | CUCI |
| 2 | `ExpCrohn` | Crohn |
| 3 | `ExpPediatrico` | Pediátrico |
| 4 | `ExpOstomias` | Ostomías |
| 5 | `ExpBiologicos` | Biológicos |
| 6 | `ExpEmbarazoEII` | Embarazo+EII |
| 7 | `ExpManejoBrotes` | Manejo brotes |
| 8 | `ExpSegundaOpinion` | Segunda opinión |
| 9 | `ExpCirugia` | Cirugía |
| 10 | `ExpSeguimientoProlongado` | Seguimiento |

> **Nota crítica:** La tabla `AreaExperienciaEii` ya existe y contiene estas áreas. No se necesita crear taxonomía nueva.

### `TipoConfirmacion` (tabla `TipoConfirmacion`)
Captura el ROL del confirmador: "Paciente atendido", "Familiar atendido", etc.
Es una dimensión ortogonal a las áreas EII — no sirve para codificar áreas.

### `MedicoExperienciaEii` (tabla `MedicoExperienciaEii`)
Relación médico ↔ área EII a nivel agregado (por médico, no por confirmación).
Actualmente usada para mostrar las áreas de experiencia en la tarjeta de detalle.

### `MedicoAreaEii` (tabla `MedicoAreaEii`)
Áreas declaradas por el médico en su perfil (`MedicoPerfilExtendido`). Diferente semántica: autodeclaración vs. confirmación comunitaria.

---

## 2. Matriz de pantallas que usan granularidad EII

### Por confirmación (nivel de fila)

| Pantalla | Campo | Uso actual (Fase 2) | Criticidad |
|----------|-------|---------------------|------------|
| Dashboard médico — lista confirmaciones | `rec.ExpCUCI/ExpCrohn/ExpPediatrico/ExpBiologicos` en `RecomendacionDashboardVm` | Siempre `false` → fila sin áreas | **ALTA** |
| Admin detalle médico — `confirmadores` | `exps: [string]` en JSON | Muestra `TipoConfirmacion.Nombre` (no áreas) | **MEDIA** |

### Por médico (nivel agregado)

| Pantalla | Campo | Uso actual (Fase 2) | Criticidad |
|----------|-------|---------------------|------------|
| Admin detalle médico — `expContadores` | Conteo por área (CUCI:N, Crohn:N …) | Todos en 0 | **MEDIA** |
| Index público — `MedicosConEII` | Flag bool por médico | Funciona (cualquier confirmación = EII) | BAJA |
| Listado tarjetas — `TotalConfirmaciones` | Conteo total | Funciona | BAJA |
| Badge `activo_comunidad` | Conteo ≥ 5 | Funciona | BAJA |
| `RecalcularNivelAsync` | `tieneEII = total > 0` | Funciona | BAJA |

### Sin impacto

| Pantalla | Motivo |
|----------|--------|
| Detalle médico público (`Detalle.cshtml`) | Muestra `TotalConfirmaciones` (conteo, sin áreas) |
| Activar / ReclamarPerfil | Solo flujo de vinculación, sin áreas |
| `RecalcularNivelConfianzaAsync` en `MedicoDirectorioService` | Solo conteo |

---

## 3. Qué se perdió y qué sigue funcionando

| Funcionalidad | Estado |
|---------------|--------|
| Conteo total de confirmaciones | ✅ Funciona |
| Badge `activo_comunidad` (≥5 confirmaciones) | ✅ Funciona |
| `tieneEII` para nivel confianza | ✅ Funciona (todo confirma EII en esta plataforma) |
| Lista de confirmadores en admin (email + fecha) | ✅ Funciona |
| Tags de área EII por confirmación en Dashboard | ❌ Perdido (siempre vacío) |
| `expContadores` por área en admin | ❌ Perdido (siempre 0) |
| `exps` por confirmación en admin (áreas específicas) | ❌ Perdido (muestra TipoConfirmacion en su lugar) |

---

## 4. Conclusión del análisis

Las áreas EII perdidas son **datos de confirmación por fila** — lo que el paciente declara haber experimentado con ese médico. Esto es ortogonal al tipo de confirmador (`TipoConfirmacion`).

La taxonomía existe: `AreaExperienciaEii`. Solo falta el puente entre `ConfirmacionComunitaria` y `AreaExperienciaEii`.

Las opciones de diseño se evalúan en `02-modelo-propuesto.md`.
