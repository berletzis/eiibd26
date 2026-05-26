# 04 - Performance

## PERF-001/002/003: SearchSuggestionService — 16+ queries por búsqueda

### Problema
`SearchSuggestionService.GetSuggestionsAsync()` realizaba un loop por cada keyword extrayendo:
- Una query por keyword para preguntas
- Una query por keyword para artículos
- Una query adicional por cada resultado para contar respuestas (N+1)

Con 8 keywords y N resultados, se generaban 16+ roundtrips por búsqueda.

### Causa raíz
Arquitectura de loop-per-keyword sin batching ni consolidación de queries.

### Solución
- `BuscarPreguntasAsync()`: una sola query con filtro OR entre keywords, con `AsNoTracking()`. Conteo de respuestas en una segunda query batched por IDs.
- `BuscarArticulosAsync()`: una sola query con filtro OR entre keywords, con `AsNoTracking()`.
- `BuscarRespuestasAsync()`: una sola query con filtro OR entre keywords y join a preguntas, con `AsNoTracking()`.
- Total: máximo 3 roundtrips por búsqueda (uno por tipo de contenido).
- Ranking por relevancia (conteo de keywords en el título) procesado en memoria.

### Impacto medido
- Antes: 16+ queries / búsqueda (estimado a partir del loop de keywords).
- Después: ≤ 3 queries / búsqueda.
- `AsNoTracking()` aplicado: sin change tracking overhead para resultados de solo lectura.

### Archivos modificados
- `eiibd26/Services/SearchSuggestionService.cs`

---

## DB-001/002/003: Tablas clínicas sin índices FK Usuario

### Problema
Las tablas `condicionUsuario`, `sintomasUsuario`, `tratamientoUsuario`, `EstadoAnimoUsuario` y `TrackingSintomaUsuario` no tenían índices sobre `IdUsuario`/`UsuarioId`, causando table scans en cada carga del dashboard.

### Causa raíz
Los índices no fueron incluidos en la configuración inicial del modelo EF Core.

### Solución
- Añadidos índices en `ApplicationDbContext.OnModelCreating()`:
  - `condicionUsuario.idUsuario`
  - `sintomasUsuario.idUsuario`
  - `tratamientoUsuario.idUsuario`
  - `EstadoAnimoUsuario.IdUsuario`
  - `EstadoAnimoUsuario (IdUsuario, FechaRegistro)` — índice compuesto para queries de historial
  - `TrackingSintomaUsuario (IdUsuario, Fecha)`
- Script SQL idempotente en `docs/sql/indices_tablas_clinicas.sql` para aplicación directa sin migración EF.

### Política aplicada
Sin migración EF. Script SQL directo como fuente de verdad del cambio de esquema.

### Archivos modificados
- `eiibd26/Data/ApplicationDbContext.cs`
- `docs/sql/indices_tablas_clinicas.sql` (creado)
