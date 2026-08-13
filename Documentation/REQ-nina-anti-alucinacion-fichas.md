# REQ — NINA no debe inventar fichas: gate de reconocimiento + guardrail + re-generación segura

**Scope:** solo `eiibd26.Web` — `Services/AI/SintomasTratamientosAiService.cs` (prompts + triage), un servicio nuevo `IReconocimientoEntidadService`, los dos admin controllers (`TratamientosAdminController`, `SintomasAdminController`), `GlossaryService` (propagación de `Activo`), y el gate de publicación. **Sin cambios de esquema obligatorios** (opcional: una columna-flag de rescate; ver §6). NO tocar NINA-WorkerService ni Conectar3eros. Reversible.

**Ejecución:** por fases (§7), detrás de un feature flag, con rollback. Build + `publish -c Release` + correr las sondas de aceptación (§8), sin escribir en BD hasta que pasen.

**Validación previa:** este REQ pasó **revisión adversarial contra el código real (Opus + Fable)**. Los hallazgos ya están integrados. Norma del proyecto: ningún REQ que toque contenido publicado o datos a escala se implementa sin esta pasada.

---

## 1. Motivo (caso real)
En `https://eiibd.com/Termino/aangamik`, NINA generó una ficha **falsa**: describió "Aangamik" como *"medicamento inyectable indicado para colitis ulcerosa moderada a grave"* y lo marcó **RELACIÓN EII = SÍ** con nivel. En realidad **"Aangamik" es una marca de suplemento de DMG (dimetilglicina), de venta libre** — ni inyectable, ni de receta, ni para colitis. Info médica falsa publicada a pacientes.

## 2. Causa raíz (dos capas que se retroalimentan)
1. **El generador alucina.** `GenerarDescripcion{Tratamiento,Sintoma}Async` piden describir **cualquier** término como tratamiento de EII, en el contexto de la plataforma. El único guardrail está **truncado a media frase** (`SintomasTratamientosAiService.cs:86`) y el de síntomas (`BuildSintomaSystemPrompt`, `:837`) **no tiene ninguno**. Además solo se prohíbe inventar **FUENTES** (`:99`), nunca la descripción, la vía (inyectable) ni la indicación (colitis).
2. **La descripción confabulada contamina el triage.** Aunque el prompt de triage YA dice *"clasifica por el NOMBRE; la descripción es solo contexto"* (`:150-152` tratamientos, `:180-182` síntomas), el caller `batch-review` **sí le pasa** `DescripcionIA` autogenerada (`TratamientosAdminController.cs:512`), así que el contexto envenenado igual influye. Por eso el catálogo "100% revisado" no lo cazó.

---

## 3. Solución central — Gate de reconocimiento de entidad
**No se construye un clasificador nuevo ni se usan embeddings como gate.** El reconocedor casi existe: el triage por nombre (`ClasificarTratamientoAsync`/`ClasificarSintomaAsync`, Haiku, temp 0.0, JSON, reintentos, parse-fail→Dudoso) ya está calibrado (su rúbrica marca "suplementos de marca sin DCI" → basura, con **sesgo obligatorio a conservar**). Se **envuelve, se cablea ANTES de generar, y se le da contrato fail-safe.**

**Componente nuevo:** `IReconocimientoEntidadService` (en `Services/AI/`), inyectado en ambos admin controllers. Se invoca **antes** de `GenerarDescripcion*Async` en los tres puntos de entrada: generación individual (`TratamientosAdminController.cs:54`, `SintomasAdminController.cs:54`), `batch-generate-ia` (`:319`) y el flujo de síntomas (`:312`).

**Contrato:**
```csharp
enum ResultadoReconocimiento { Reconocido, NoReconocido, RevisionHumana, GroundingNoDisponible }
record DecisionReconocimiento(ResultadoReconocimiento Resultado, string Fuente, double Confianza, string Motivo);
```

**Cascada de decisión:**
1. **Tier 0 — Allowlist en BD (determinista, sin red).** Si el registro tiene `ValidadoHumano == true` **o** su `GlossaryTerm` vinculado tiene `GlossaryValidations.Approved` → `Reconocido` (Fuente="allowlist"). Cero costo, inmune a outages. **Esta capa solo deja pasar: el gate tiene PROHIBIDO desactivar o revertir un registro Tier 0** (protege el trabajo médico, ver §6/C-5).
2. **Tier 1 — Triage LLM por NOMBRE.** Llamar `ClasificarTratamientoAsync(nombre, descripcionExistente: null)` (o el de síntomas). **Nunca pasar `DescripcionIA` autogenerada** (si `ValidadoIA && !ValidadoHumano` ⇒ descripción = `null`). Mapeo:
   - `Valido`  con confianza ≥ `ReconocimientoEntidad:ConfianzaMinima` (default 0.80) → `Reconocido`
   - `Basura`  con confianza ≥ umbral → `NoReconocido`
   - `Dudoso`, o cualquier estado con confianza < umbral → `RevisionHumana`
   - Excepción/timeout/parse-fail tras agotar reintentos → `GroundingNoDisponible`
   - Umbral en `appsettings` para calibrar sin redeploy.
3. **Embeddings FUERA del camino de decisión.** `ReferenciaRecuperacionService` (Voyage, umbral 0.55) **queda intacto**, solo para sugerir referencias candidatas al editor. NO es un gate: es recuperación temática de artículos (dejaría pasar Aangamik y suprimiría fármacos oscuros).

**Efectos por resultado (fail-safe ASIMÉTRICO — un outage nunca despublica):**
- `Reconocido` → generar la ficha (el guardrail del generador, §4, queda como 2ª red).
- `NoReconocido` → NO llamar al generador; NO escribir `DescripcionIA`; `RelacionEII=false`; `RevisionLimpiezaEstado=3` (Dudoso) con el `Motivo` del gate; `SincronizarActivoPorTratamientosAsync(id, activo:false)`. (Desactivación automática **solo** para términos autogenerados/no publicados; **nunca** Tier 0.)
- `RevisionHumana` → NO generar; sellar Dudoso; queda en la cola existente. **No despublicar** lo que ya estaba público solo por duda (conserva su `Activo` actual).
- `GroundingNoDisponible` → NO generar contenido nuevo; **NO cambiar `RevisionLimpiezaEstado` ni `Activo` de nada**; log + contador; reintento en la siguiente corrida. Un outage produce **cero** despublicaciones — solo pausa la producción de fichas nuevas.

## 4. Guardrail en los generadores (segunda red, ambos)
En `BuildTratamientoSystemPrompt`/`GenerarDescripcionTratamientoAsync` **y** `BuildSintomaSystemPrompt`/`GenerarDescripcionSintomaAsync` (hoy sin guardrail), regla dura arriba de todo:
> **Si NO reconoces el término como un medicamento / tratamiento / síntoma real y específico, NO inventes.** No afirmes vía (inyectable, oral…), forma, mecanismo ni indicación (colitis, Crohn…) que no puedas verificar. En ese caso devuelve estado **NoReconocido** (ver contrato abajo), NO una descripción plausible.

**Contrato NO_RECONOCIDO end-to-end (obligatorio):** el generador debe **devolver un estado**, no un marcador dentro del texto. Cambiar la firma para incluir `bool Reconocido` (o un enum), y en el controller: si NoReconocido ⇒ **no persistir `DescripcionIA`**, aplicar los efectos de §3. *(Sin esto, "NO_RECONOCIDO" se guardaría como descripción y se mostraría literal al paciente — peor que Aangamik.)*

## 5. Modelo
- **Triage / gate (`CallClaudeJsonAsync`)**: **Haiku fijo** (`:129`). Barato, alto volumen, decisión de 3 vías con rúbrica + cola humana. Se mantiene.
- **Generación de descripción (`CallClaudeApiAsync`, usa `_config.Model`, configurable)**: es contenido publicado y donde alucinó. **Subir a Sonnet** (punto dulce) u **Opus** (máx. seguridad). Es cambio de config, bajo riesgo. El modelo NO sustituye el gate ni el guardrail — los complementa.

## 6. Re-generación masiva SEGURA (preservando el trabajo humano)
Hay que rehacer lo ya generado (potencialmente confabulado). Orden estricto:
1. **Primero, el pipeline** (§3 gate + §4 guardrail + §5 modelo). Nada masivo antes.
2. **Re-triage por NOMBRE** (`batch-review` sin pasar `DescripcionIA` autogenerada). **Preservar** — NO resetear:
   - registros con `ValidadoHumano == true`, **y**
   - registros cuyo `GlossaryTerm` vinculado tiene `GlossaryValidations.Approved` *(las validaciones médicas viven aquí, NO en `tratamientos.ValidadoHumano` — el médico valida en `/Termino/{slug}`, `Termino.cshtml.cs:74,137`; ignorarlo sobrescribiría fichas ya validadas)*, **y**
   - rescates manuales marcados con un **flag/columna explícito** (NO el `RevisionLimpiezaMotivo LIKE '%Rescate manual (Berletzis)%'` — ese string es convención manual que ningún código escribe; frágil). Si no hay columna, agregar `RevisionLimpiezaRescateManual BIT` por SQL y sellar los rescates existentes una vez.
   - Resetear `RevisionLimpieza* = NULL` **solo en el resto** y re-correr.
3. **Re-generar descripciones** (`batch-generate-ia`, modelo nuevo + guardrail + gate) **solo sobre Válido/Dudoso** (no Basura). **OJO:** el filtro actual `IsNullOrEmpty(DescripcionIA) || !ValidadoIA` (`:293`) **no re-generará** los que ya tienen descripción confabulada + `ValidadoIA=true`; ajustar el filtro para el re-proceso.
4. **Gate de publicación:** nada se re-publica (`Activo=1`) sin pasar el pipeline.

**Costo:** triage/gate en Haiku (barato); descripciones en Sonnet/Opus (presupuestar; se topó con límites en Haiku). Correr por ramas con pausa.

## 7. Restricciones de ejecución
- **Regeneración estrictamente SECUENCIAL.** `_lastNivelRelacion/_lastRazonamiento` son campos de instancia (`:827-835`); solo funcionan por servicio Scoped + batch secuencial. Paralelizar corrompería nivel/razonamiento entre registros (se propaga al `GlossaryTerm` equivocado, `:819`). Prohibir concurrencia o eliminar el patrón `_last*`.
- **El gate y la generación son llamadas SEPARADAS** (no "describe X y de paso dime si existe": el modelo que ya empezó a describir tiene sesgo a completar).
- **Feature flag + rollback** para prompt+modelo+gate; **log/contador de NoReconocido y GroundingNoDisponible**; **auditoría** de qué se regeneró.
- **Verificar `NINA-WorkerService`** (fuera de scope): si tiene su PROPIO prompt de generación, el guardrail/gate de este servicio NO lo cubre y reintroduce el fallo. Confirmar antes de dar por cerrado.
- **Gate de publicación:** listados/búsqueda ya filtran `Activo` (`GlossaryService.cs:87,374,835`) y la página da `NotFound` para inactivo (bien). Agregar `noindex` para estado **Dudoso** (queda `Activo` pero no debe indexarse). Confirmar que ningún sitemap exponga inactivos.

## 8. Verificación — pruebas de aceptación (sonda read-only, sin escribir en BD)
Harness admin que corre el gate y devuelve la `DecisionReconocimiento` sin persistir:
1. **Positivos:** `Mesalazina`, `Infliximab`, `Prednisona`, `Dieta baja en FODMAP` → `Reconocido`.
2. **Negativos:** `Aangamik`, `Xyzqwe 500`, `Gel Limpiador de Kombucha` → `NoReconocido` (si alguno cae en `RevisionHumana`, aceptable: nunca se publica solo).
3. **Regla anti-supresión (crítica):** fármacos reales oscuros/no-EII — `Ciclopentolato`, `Hipurato de sodio`, `Vedolizumab`, `Ácido obeticólico` — **ninguno** puede salir `NoReconocido` (`Reconocido` o `RevisionHumana` OK). Un solo fármaco real en `NoReconocido` = prueba fallida → recalibrar umbral/rúbrica antes de activar.
4. **Fail-safe:** con API key inválida / red cortada → todos `GroundingNoDisponible` y **0 filas modificadas** en `tratamientos`/`GlossaryTerm`.
5. **Allowlist:** un registro con `ValidadoHumano=true` (o `GlossaryValidations.Approved`) y nombre basura deliberado → `Reconocido` vía Tier 0 (demuestra que el gate no pisa trabajo humano).
6. **Estabilidad:** repetir 1-3 dos veces; con temp 0.0 el resultado debe ser estable.
7. **Generador con guardrail:** `Aangamik` → estado NoReconocido, sin inventar vía/indicación; `Mesalazina` → descripción correcta. La ficha pública **nunca** muestra el string "NO_RECONOCIDO".
8. `/Termino/aangamik` sigue en 404 (ya contenido). `dotnet publish -c Release` limpio.

## Qué NO hacer (trampas confirmadas en revisión)
- NO usar `ReferenciaRecuperacionService`/0.55 como gate (similitud temática: deja pasar Aangamik, suprime fármacos oscuros).
- NO construir/mantener un corpus de vocabulario clínico embebido en v1 (mantenimiento sin dueño; si algún día hace falta, es match exacto de strings, no embeddings).
- NO fail-closed: "sin respuesta" ≠ "no reconocido". Vacío/timeout/outage jamás mapea a `NoReconocido`.
- NO pasar `DescripcionIA` autogenerada al gate ni al triage.
- NO paralelizar la corrida (hazard `_last*`).
- NO permitir que el gate despublique registros con `ValidadoHumano` o `GlossaryValidations.Approved` (solo puede bloquear entrada, nunca revertir validación humana).

## Reglas del proyecto (recordatorio)
Nunca reescribir lógica/queries/cálculos existentes — solo agregar. No cambiar rutas públicas (SEO). Esquema por SQL directo, sin `dotnet ef database update`. Analizar y preguntar antes de mover archivos. Trabajar solo en `eiibd26.Web` y proyectos nuevos.
