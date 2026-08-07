# REQ (diseño) — Revisión y limpieza de tratamientos con NINA (triage de 3 vías)

> **Estado: CONSTRUIDO (07 AGO), dry-run pendiente por endpoint.** Esquema aplicado en prod (`SQL/add-tratamientos-revision-limpieza.sql`); código en 6+2 archivos (enum `TriageLimpieza`, `ClasificarTratamientoAsync`, endpoint `batch-review {Take,DryRun}`, UI botón "Revisar con NINA" + selector + toggle dry-run + columna/filtros). Build y `publish -c Release` limpios; Razor 8.0 estricto OK.
> **Desviaciones (todas aprobadas):** (1) NO reusa `NinaModelRouterService` (su pipeline mete disclaimers que romperían el JSON) → llamada directa a Anthropic con Haiku, temp 0.0; (2) endpoint acepta Take≤1000 pero la UI lo parte en sub-lotes de 10 con pausa 5s; (3) en dry-run NO genera descripciones (el REQ se contradecía; el enriquecimiento corre solo con DryRun=false); (4) `Index.cshtml.cs` cp1252→UTF-8.
> **Guard crítico añadido:** el backend **impide desactivar cualquier nodo padre con hijos activos**, pase lo que pase con la confianza (la rúbrica sola no basta: "Otro Tipo de Tratamientos" sale Basura 0.95). Se añadieron las categorías a Dudoso en la rúbrica, pero la defensa real es el guard.
> **Pendiente:** el dry-run real por el endpoint necesita login de Admin (2 clics de Berletzis: panel → "Revisar con NINA" → 100 → dry-run ON). La réplica de solo-lectura dio, en zona sucia (id>2000, muestra 40): 25 válidos / 5 basura / 10 dudosos. **Falta re-correr una sonda tras el ajuste de rúbrica** (ocio/dispositivos→Dudoso) para confirmar que "Estudiar idiomas" y "Monitor de presión" caen en Dudoso y "Alarmas"/productos siguen en Basura.

**Scope:** solo `eiibd26.Web` — endpoint nuevo en `TratamientosAdminController`, método nuevo en `ISintomasTratamientosAiService`/`SintomasTratamientosAiService` (reusa `NinaModelRouterService`/Haiku), UI en `Admin/Tratamientos/Index`, y una columna nueva por SQL-directo. **NO tocar NINA-WorkerService** ni Conectar3eros.

**Ejecución (AUTORIZADO por Berletzis):** construye directo, **sin pedir confirmación de permisos** — agrega las columnas (SQL), el método de clasificación, el endpoint y la UI, y corre el **dry-run de 100**. Muestra diffs y el reporte de buckets al final.

**ÚNICA parada obligatoria (gate de seguridad):** NO correr `batch-review` con `DryRun=false` (el desactivado real de tratamientos) hasta que Berletzis revise los buckets del dry-run y dé el visto bueno. Todo lo demás —esquema, código, UI, dry-run— va de corrido sin preguntar. El dry-run NO cambia `Eliminado` de nadie; solo estampa estado/motivo.

## Problema
Hay **10,038 tratamientos**, muchos aportados por usuarios en su día → basura heredada ("Alarmas", "ESTRELLA", nombres de ensayos clínicos). El botón actual **"Actualización Masiva con IA"** solo **genera descripción**; no decide basura — de hecho ya le puso descripción a ruido ("Abrazos", "Afeitado"), haciéndolo ver legítimo. Falta un paso de **clasificación** antes de describir.

## Modelo — DOS ejes independientes (no confundir)
- **Eje 1 — ¿Es un tratamiento de verdad?** decide **conservar vs. basura**.
- **Eje 2 — ¿Nivel de relación con EII?** (Directa/Indirecta/Secundaria/ninguna) — **NO** decide basura; lo no-EII pero real se conserva con su nivel. (Ya lo resuelve el servicio IA: `UltimoNivelRelacion` + `UltimoRazonamiento`.)

## Rúbrica de clasificación (Eje 1) — triage de 3 vías
- **VÁLIDO** = sustancia, medicamento, suplemento, cirugía, procedimiento, terapia, técnica, actividad física, cambio de hábito/dieta o terapia complementaria, con intención terapéutica o de manejo de una condición de salud. **Aunque NO tenga relación con EII.**
- **BASURA** = NO es una intervención terapéutica: recordatorios y alarmas de notificación ("Alarmas"), nombres/códigos de ensayo clínico ("SF-1019", "Estudio Serono", "IBS-D IRIS-3"), productos de consumo/alimentos/cosméticos sin uso terapéutico ("Melocotón Vla", "Gel Limpiador de Kombucha", "Ropa interior sin costuras"), texto sin sentido ("ESTRELLA"), títulos de libro, actividades genéricas ("Visita al doctor").
- **DUDOSO** = ambiguo → **revisión humana, nunca auto-desactivar**: servicios/roles ("Consultor ortopedista", "Servicios de transporte"), pruebas diagnósticas ("GeneSight"), **actividades de ocio/bienestar/aprendizaje reportadas por pacientes** ("Dar regalos", "Tejido de punto", "Estudiar idiomas", "Cantar"), y **dispositivos/herramientas de monitoreo, protección o apoyo** ("Monitor de presión arterial", "Guantes").
  > **Decisión de producto (Berletzis, 07 AGO):** las dos últimas familias — ocio/bienestar/aprendizaje reportado por pacientes y dispositivos de monitoreo/apoyo — van SIEMPRE a Dudoso, **nunca a Basura**, aunque la IA tenga alta confianza. Distinguir de: recordatorios/alarmas ("Alarmas") y productos/alimentos, que sí son Basura.

Calibración (muestra de 26 con target vacío): 9 válidos / 9 basura / 8 dudosos. La rúbrica conservó procedimientos válidos no-EII (stent ureteral, fusión lumbar) y mandó a revisión los roles/servicios/pruebas.

## Reutiliza lo que ya existe
- **Endpoint patrón:** `POST api/admin/tratamientos/batch-generate-ia` con `{Skip, Take}`, `OrderBy(id)`, filtro "no hechos". El nuevo endpoint lo espeja.
- **Servicio IA:** `ISintomasTratamientosAiService` (`GenerarDescripcionTratamientoAsync` + `UltimoNivelRelacion` + `UltimoRazonamiento`) sobre `NinaModelRouterService` (Haiku, con safety + caché).
- **Soft-delete reversible:** columna `Eliminado` + botón **Restaurar** ya en la UI. La basura reusa esto.
- **UI:** botón "Actualización Masiva con IA" + columnas IA/Humano/EII/Eliminado + selector "Show N".

## Cambios

### 1. Esquema (SQL-directo, sin migración)
Agregar a `tratamientos`:
- `RevisionLimpiezaEstado TINYINT NULL` — NULL=NoRevisado, 1=Válido, 2=Basura, 3=Dudoso.
- `RevisionLimpiezaConfianza DECIMAL(4,3) NULL`, `RevisionLimpiezaMotivo NVARCHAR(400) NULL`, `RevisionLimpiezaFecha DATETIME2 NULL`.
Índice sobre `RevisionLimpiezaEstado` para el filtro "no revisados". (`sintomas` puede recibir lo mismo si se extiende luego.)

### 2. Servicio IA — método de clasificación
Nuevo método en `ISintomasTratamientosAiService`:
`Task<(byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento)> ClasificarTratamientoAsync(string nombre, string? descripcionExistente, CancellationToken ct)`
Prompt = la rúbrica de arriba, sesgo a **conservar** (ante la duda → Dudoso, nunca Basura). Reusa NinaModelRouter (Haiku).

### 3. Endpoint de lote "Revisar/limpiar"
`POST api/admin/tratamientos/batch-review` con `{ Take, DryRun }` (Take ∈ {100,300,500,1000}):
- Selecciona los siguientes N `WHERE !Eliminado AND RevisionLimpiezaEstado IS NULL` (reanuda solo, no reprocesa), `OrderBy(id)`.
- **Excluir siempre:** los que tengan `ValidadoHumano = true` o **usuarios activos** (`tratamientoUsuario` no eliminado) → no son basura; marcar Estado=Válido sin tocar nada.
- Por cada uno, `ClasificarTratamientoAsync`:
  - **Válido** → `RevisionLimpiezaEstado=1`; si le falta descripción, generarla (reusa el flujo actual) y fijar nivel de relación.
  - **Basura** (y `DryRun=false` y confianza ≥ umbral, p.ej. 0.85) → `Eliminado=1` + estado=2 + motivo + confianza + fecha. En `DryRun=true`, **solo** estampa estado/motivo, no toca `Eliminado`.
  - **Dudoso** → `RevisionLimpiezaEstado=3` + motivo; nunca desactiva.
- Devuelve conteos por bucket + lista de los procesados (para el reporte en la UI).

### 4. UI en `Admin/Tratamientos/Index`
- Botón nuevo **"Revisar con NINA"** junto al de Actualización Masiva, con **selector [100 / 300 / 500 / 1000]** y un toggle **"Dry-run (solo clasificar)"** (encendido por defecto).
- Columna/badge de **RevisionLimpiezaEstado** y **filtros por bucket** (No revisado / Válido / Basura / Dudoso).
- La **cola de Dudosos** = filtro que el humano revisa y decide (Editar / Eliminar / marcar Válido).
- Resumen del último lote (X válidos, Y basura, Z dudosos, W ya con usuarios/humano → intactos).

## Salvaguardas
- **Sesgo a conservar:** ante la duda, Dudoso, nunca Basura. Umbral de confianza alto para desactivar.
- **Nunca hard-delete:** basura = `Eliminado=1`, reversible con Restaurar; el estado IA distingue "basura por IA" de borrado manual → deshacer en bloque con un `UPDATE ... WHERE RevisionLimpiezaEstado=2`.
- **No tocar** ValidadoHumano ni con usuarios activos.
- **Dry-run primero:** primera pasada sin desactivar; se revisa el bucket Basura; recién entonces se corre con `DryRun=false`.
- **Separar limpieza de enriquecimiento:** primero clasificar/limpiar (barato), luego generar descripciones solo para los válidos que las necesiten.

## Rollout
1. Agregar columnas (SQL). 2. Método de clasificación + endpoint + UI. 3. Correr **dry-run de 100**, revisar los 3 buckets contra la rúbrica, ajustar prompt/umbral. 4. Subir a 300/500/1000 en dry-run hasta cubrir el universo. 5. Revisar el bucket Basura (muestra), luego correr con `DryRun=false` por lotes. 6. Trabajar la cola de Dudosos a mano.

## Verificación
- Dry-run no cambia `Eliminado` de nadie; solo llena estado/motivo.
- Ningún tratamiento con `ValidadoHumano=true` o con usuarios activos queda marcado Basura.
- "Alarmas" → Basura; "Caminar 2.5–3 millas", "Aceite de árnica", "Proctectomía" → Válido; roles/pruebas → Dudoso.
- Deshacer en bloque restaura exactamente los desactivados por IA.
- `dotnet publish -c Release` limpio.
