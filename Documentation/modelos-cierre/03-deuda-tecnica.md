# Deuda Técnica — Auditoría 05modelos

Fecha de registro: 2026-05-26  
Fase: Modelos de Datos (05modelos.html)

Estos findings son reales pero **no se corrigen en esta iteración** porque requieren migraciones de BD, refactors con impacto transversal, o análisis adicional que excede el alcance mínimo autorizado.

---

## MDL-001 — 8+ entidades con nombres camelCase/lowercase

**Severidad:** HIGH  
**Por qué no se corrige ahora:** Renombrar clases de modelo (`condiciones` → `Condiciones`) requiere actualizar todos los usages en Controllers, Services, Razor Pages y Seeds. El cambio es masivo y viola la regla de "cambios mínimos y localizados". Además requeriría agregar `[Table("condiciones")]` a cada entidad para no romper las tablas de BD.

**Prerequisito para corregir:** Sprint dedicado de refactor con cobertura de tests completa.

---

## MDL-002 — Typo en nombre de archivo `estuduiLabUsuario.cs`

**Severidad:** HIGH  
**Por qué no se corrige ahora:** Renombrar el archivo puede afectar git history, referencias de `#include`, y es ruido sin impacto funcional. La clase interna `estudiosLabUsuario` no coincide con el nombre del archivo, pero el compilador no depende del nombre del archivo.

**Prerequisito para corregir:** Solo si se hace un sprint de limpieza de nomenclatura.

---

## MDL-003 — `estudioLab.idPadre` tipado como `string` en lugar de `int?`

**Severidad:** HIGH  
**Por qué no se corrige ahora:** Cambiar el tipo de `string` a `int?` requiere:
1. Migración de BD: `ALTER COLUMN idPadre INT NULL`
2. Verificar que los datos actuales en la columna son convertibles a `int`
3. Actualizar todos los consumers que asignan o leen `idPadre`

Sin instrucción explícita de modificar BD, no se actúa.

---

## MDL-004 — `AIRequestLog` sin `DbSet` en ApplicationDbContext

**Severidad:** HIGH  
**Por qué no se corrige ahora:** Agregar `DbSet<AIRequestLog>` requiere crear la tabla en BD via migración. La tabla no existe en producción según la auditoría. Agregar el DbSet sin migración causaría error en EF al intentar materializar.

**Prerequisito para corregir:** Instrucción explícita de crear tabla `AIRequestLog` en BD.

---

## MDL-005 — `SintomasNotas` y `TratamientosNotas` sin `DbSet`

**Severidad:** HIGH  
**Por qué no se corrige ahora:** La auditoría indica que las tablas "existen en BD pero son inaccesibles". Antes de agregar los DbSets, se requiere:
1. Verificar en producción que las tablas existen con el schema correcto
2. Confirmar que no hay datos críticos que dependan del acceso controlado actual
3. Instrucción explícita de habilitar acceso EF a esas tablas

---

## MDL-008 — `FechaCreacion` + `FechaCreado` duplicados en `Perfil`

**Severidad:** MEDIUM  
**Por qué no se corrige ahora:** Consolidar requiere decidir cuál campo es canónico, migrar datos, y actualizar todos los writers/readers. Riesgo de perder datos de auditoría si la migración es incorrecta.

---

## MDL-009 — `[Required] + string?` en catálogos clínicos (condiciones/sintomas/tratamientos)

**Severidad:** MEDIUM  
**Resultado de verificación:** Se realizó análisis completo de usages:
- `MedicalDataAdapter.cs:36` — `Nombre = s.nombre ?? ""`
- `MedicalDataAdapter.cs:70` — `Nombre = t.nombre ?? ""`
- `MedicalSummaryService.cs:197` — ternario que puede retornar `null`
- `_EstadoAnimoModal.cshtml` — `?.nombre` con optional chaining

El codebase trata `nombre` como nullable en múltiples puntos de materialización. La BD no tiene NULLs (verificado en producción), pero cambiar el tipo del modelo requeriría actualizar todos los consumers que manejan el valor como potencialmente nullable.

**Prerequisito para corregir:** Actualizar MedicalDataAdapter.cs, MedicalSummaryService.cs y los templates Razor que usan `?.nombre` antes o en paralelo con el cambio de modelo. Hacerlo de forma atómica.

---

## MDL-011 — `ContenidoRespuesta` strings sin `MaxLength`

**Severidad:** MEDIUM  
**Por qué no se corrige ahora:** Agregar `[MaxLength]` a campos que actualmente son `NVARCHAR(MAX)` requiere migración de BD que trunca el tipo de columna. Si hay datos existentes que excedan el nuevo límite, la migración fallará.

---

## MDL-028 — Tablas de relación clínica sin campos de auditoría ni soft delete

**Severidad:** MEDIUM  
**Por qué no se corrige ahora:** Agregar `Eliminado`, `FechaModificado` a las 4 tablas clínicas requiere:
1. Migraciones de BD para agregar columnas
2. Actualizar todos los queries que hacen hard-delete en esas tablas
3. Actualizar los query filters en DbContext

Cambio transversal que excede el alcance de esta auditoría.
