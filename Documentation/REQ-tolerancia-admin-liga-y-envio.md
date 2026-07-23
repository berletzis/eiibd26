# REQ — Admin tolerancia: generar liga de encuesta + control de envíos (simple)

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web`, panel `Admin/Platillos/EstadisticasTolerancia`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build con vistas Razor (`dotnet publish`) antes de pushear.
**Objetivo:** desde el mismo grid de estadísticas, poder (1) generar/copiar la liga de la encuesta por ingrediente, y (2) llevar control simple de cuáles ya se enviaron.

## §1 — Copiar liga por fila
- Botón **"Copiar liga"** en cada fila del grid → copia al portapapeles la URL pública de la encuesta: `{base}/tolero/{slug}`.
- **El slug se genera con el MISMO `SlugHelper.GenerateSlug(nombreIngrediente)`** que usa `Pages/Tolero/Encuesta.cshtml.cs` para resolver — si difiere, la liga no resuelve (404). Fuente única.
- **La base (`https://eiibd.com`) NO se hardcodea** — sale de config o del host del request.
- Copia con JS al portapapeles + feedback visual ("¡Copiada!"). Opcional: mostrar el slug/URL en un tooltip.

## §2 — Control de envío (simple: última vez enviada)
- **Columna "Envío"** en el grid: muestra **"Enviada · {fecha}"** o **"Pendiente"**.
- Botón **"Marcar enviada"** por fila (manual, tú decides cuándo cuenta) → guarda la fecha actual.
- Botón/acción **"Deshacer"** para volver a "Pendiente".
- **Filtro "Pendientes de enviar"** (junto a los filtros existentes) para trabajar la lista sin abrir una por una.

## §3 — Almacenamiento (SQL directo, como todo Platillos)
- Tabla nueva chica **`PlatToleroEnvio`** (aislada, sin FK física, patrón del módulo):
  - `IngredienteId` (int, UNIQUE — una fila por ingrediente).
  - `EnviadaEn` (datetime2 NULL — `NULL` = pendiente).
  - `MarcadaPorUserId` (uniqueidentifier NULL — quién la marcó).
- "Marcar enviada" = upsert `EnviadaEn = GETUTCDATE()`, `MarcadaPorUserId = admin`. "Deshacer" = `EnviadaEn = NULL` (o borrar la fila).
- Script deploy-gate `SQL/create-plat-tolero-envio.sql` (idempotente, correr antes de desplegar).
- (Alternativa aceptable si Claude Code lo ve más simple: columna `ToleroEnviadaEn` en `PlatIngrediente`. Preferible la tabla aparte para no mezclar estado de campaña con el catálogo.)

## Fuera de alcance
- **NO tocar** el cálculo bayesiano (`ToleranciaBayes`) ni la encuesta pública `/tolero`.
- Sin historial de envíos (varios envíos, canal, nota) — eso es la versión "con historial", futura. Aquí solo "última vez enviada".
- Sin envío automático de correos — la liga se copia y se manda por fuera; esto solo registra que ya se envió.

## Verificación
1. "Copiar liga" en un ingrediente → la URL copiada abre la encuesta correcta (`/tolero/{slug}` resuelve, no 404). Slug idéntico al que genera la encuesta.
2. "Marcar enviada" → la fila pasa a "Enviada · {fecha}"; persiste al recargar.
3. "Deshacer" → vuelve a "Pendiente".
4. Filtro "Pendientes" → muestra solo los `EnviadaEn IS NULL`.
5. No cambió ningún número del cálculo bayesiano ni la vista pública.
6. `dotnet publish -c Release` limpio (vistas Razor incluidas) antes del push.
