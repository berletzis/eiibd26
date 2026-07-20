# REQ — Vista admin de estadísticas de tolerancia

**Fecha:** 17 JUL 2026
**Objetivo:** que el admin pueda VER los votos de las encuestas `/tolero/{slug}` sin meterse a la BD por SQL. Hoy los datos se recolectan pero no hay ninguna vista — este es el hueco de reporting del MVP de encuestas.
**Reglas del repo:** Razor Pages (`string?`/`int?`), reusar el patrón de grid admin ya estandarizado (`admin-grid.js/.css`), solo tocar el proyecto web, diff antes de aplicar. Es página nueva (`.cshtml`+`.cs`) → rebuild en VS. **Solo lectura** — no edita ni borra votos, no cambia esquema.

## La página
Nueva página admin, ej. `Areas/Identity/Pages/Admin/Platillos/EstadisticasTolerancia.cshtml(.cs)`, ruta `/Identity/Admin/Platillos/EstadisticasTolerancia`. Rol `Administrador`, dentro del área Identity (autorización por convención que ya cubre `/Admin`).

### Tabla (grid estándar, DataTables client-side — dataset chico)
Una fila por ingrediente **con al menos 1 voto** (con un toggle "mostrar todos" que incluya los de 0, para ver cobertura). Columnas:

| Ingrediente | Sí | A veces | No | Total (n) | % público |
|---|---|---|---|---|---|

- **Sí / A veces / No:** conteos crudos (la verdad, lo que el admin necesita).
- **Total (n):** suma de respuestas.
- **% público:** el MISMO cálculo que ve el paciente — Laplace `(Sí+1)/((Sí+No)+2)`, y si `n < 10` mostrar "—" o "insuficiente" (respetar el guard, para que el admin sepa qué se está mostrando realmente).
- Orden por defecto: `Total` desc (lo más votado primero). Sortable por cualquier columna.

### Query
Agregado de `PlatTolerVoto` agrupado por `IngredienteId`, contando por `Tolera` (1/2/3), join lógico a `PlatIngrediente` para el nombre. `AsNoTracking`, proyección `.Select` (no materializar entidades). Reusa el índice `IX_PlatTolerVoto_Ingrediente`.

### Filtro por tipo de EII (el pago de haber capturado TipoEII)
Un combo: **Todos / CUCI / Crohn**. Al filtrar, los conteos se recalculan solo con votos de ese `TipoEII`. Es el precursor visible del #16 (consenso por tipo). Si complica el MVP, va como fase-2 — pero la data ya está, así que idealmente entra.

### Extra útil (opcional): export CSV
Un botón "Exportar CSV" con las filas visibles — para análisis externo y para alimentar el #16 cuando se construya.

## Navegación
Agregar el link en la sección admin del sidebar. **OJO:** por el duplicado de sidebars (ver `Documentation/TAREA-sidebars-separar-admin-paciente.md`), agrégalo donde vive la sección admin (`Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`, junto a los otros de Platillos admin). Si algún día se unifican los sidebars, este link viaja con ellos.

## Fuera de alcance
- Editar/borrar votos desde el admin (solo lectura).
- El bayesiano completo (prior Beta, intervalo creíble) → tarea #16. Esta vista muestra el consenso crudo + el % público, que es suficiente para operar la viralidad.
- Vista "mis votos" del paciente → anotada aparte, menor prioridad.

## Aceptación
- El admin ve, por ingrediente, cuánta gente votó y el consenso (Sí/A veces/No, n, % público).
- Ordenable; filtro por tipo de EII funciona (o anotado como fase-2 con la data ya lista).
- Solo lectura, sin cambios de esquema, reusa el grid estándar y `eii-*`.
- Diff antes de aplicar; rebuild en VS por el `.cs` nuevo.
