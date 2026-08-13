# REQ — Rúbrica v4: psicoterapias y procedimientos reales NUNCA son Basura

Extiende `REQ-rubrica-v3-farmacos-reales-nunca-basura.md`. **Scope:** solo `eiibd26.Web` — `Services/AI/SintomasTratamientosAiService.cs` (el prompt de `BuildTriageSystemPrompt`). NADA más. NO tocar NINA-WorkerService.
**Ejecución (AUTORIZADO por Berletzis, sin pedir permisos):** aplica el cambio de prompt, build + `publish -c Release`, muestra diff al final. NO desactiva nada (sigue dry-run).

## Motivo
v3 cerró el caso de **medicamentos/diagnósticos/dispositivos** reales, pero dejó fuera las **intervenciones no-farmacológicas reales**. En la rama "Psicoterapia" reaparecieron ~5 falsos positivos del mismo tipo: psicoterapias e intervenciones reconocidas mandadas a **Basura por "no ser de EII"**, cuando deberían ser **Dudoso**:
- **Terapia de agresión sexual** (psicoterapia de trauma real) → Basura ✗
- **Integración de personalidades** (terapia real para trastorno disociativo) → Basura ✗
- **Hospitalización por intento de sobredosis** → Basura ✗, mientras su gemela **"Hospitalización por ideación suicida" quedó Válido** (inconsistencia)
- **Puntuación Cerebral (Brainspotting)** (psicoterapia de trauma, pariente de EMDR que quedó Válido) → Basura ✗
- **Doble problema en recuperación (DTR)** (grupo real de doble-diagnóstico; los demás grupos tipo AA/NA quedaron Dudoso) → Basura ✗

La rama **Cirugías** es 100% intervención real y va a soltar el mismo falso positivo en masa (procedimientos quirúrgicos reales de otra especialidad → "no es de EII" → Basura). Hay que cerrar el hueco antes.

## Cambio — generalizar la regla de v3 (arriba del sesgo de conservación)
Reescribir la regla dura de v3 para que cubra **cualquier intervención terapéutica reconocida**, no solo fármacos:
> **Las intervenciones terapéuticas reconocidas reales NUNCA son Basura.** Si el registro es (o su nombre principal es) una intervención reconocida — un **medicamento** (genérico/DCI, receta u OTC), un **agente diagnóstico**, un **dispositivo/producto médico**, un **procedimiento** (quirúrgico, endoscópico, de rehabilitación), una **psicoterapia o intervención de salud mental** reconocida (TCC, EMDR, DBT, psicoanálisis, hospitalización psiquiátrica, grupos de apoyo estructurados tipo 12 pasos, Brainspotting, terapia de trauma, etc.), o una **terapia física/ocupacional/del habla** reconocida — **nunca la clasifiques como Basura**, aunque su uso sea cosmético, de otra condición, o sin relación con EII. → **Válido** si es claramente una intervención terapéutica; **Dudoso** si su rol es diagnóstico, estético, no-EII, o es un rol/servicio profesional (el "quién", no el "qué") y quieres confirmación humana.

**Deslinde (para NO sobre-corregir) — esto SÍ sigue siendo Basura:**
- Pseudoterapias sin mecanismo plausible ni uso reconocido: homeopatía, sanación energética/pránica/chamánica, Reiki, biorresonancia, radiestesia, flores de Bach, osteopatía craneal, NAET, Hoxsey, y similares.
- Productos comerciales de consumo, cosméticos y alimentos que solo *contienen* ingredientes (aceites esenciales de marca, lociones, tés, suplementos-marca sin DCI).
- Ruido / nombres sin sentido / no verificables, protocolos-de-persona sin base ("Método de X", "Receta de Y"), y códigos/nombres de ensayo clínico que no nombran una intervención concreta.

**Regla de desempate reforzada:** ante la duda entre "intervención real no-EII" y "basura", responde **Dudoso**. Solo va a Basura lo que es pseudociencia, producto de consumo, o ruido — nunca una intervención clínica reconocida por el mero hecho de no ser de EII.

## Alcance del efecto
- Solo afecta clasificaciones **nuevas** (Cirugías y cualquier registro aún NULL). **NO** re-clasifica lo ya sellado; las ramas cerradas (incluida Psicoterapia) se rescataron a mano y se dejan como están.

## Verificación (sonda read-only, sin escribir en BD)
- **A Válido/Dudoso (ya NO Basura):** Terapia de agresión sexual, Integración de personalidades, Hospitalización por intento de sobredosis, Brainspotting, un procedimiento quirúrgico real de otra especialidad (p. ej. una cirugía ortopédica o cardiaca de muestra).
- **Siguen en Basura (sin cambio):** homeopatía, Reiki/biorresonancia/pseudoterapias, aceites de marca, alimentos, ruido, "Método de baja recuperación de Abraham".
- Una cirugía/procedimiento normal de EII (p. ej. resección intestinal, colonoscopía) → sigue Válido.
- `dotnet publish -c Release` limpio.

---

## Implementación — 10 AGO 2026

Un solo archivo tocado: `eiibd26/Services/AI/SintomasTratamientosAiService.cs`, dentro de
`BuildTriageSystemPrompt`. El bloque duro de v3 se **reemplazó** (no se agregó otro al lado,
para no dejar dos reglas compitiendo) por su versión generalizada, en el mismo lugar: después
de la REGLA FIRME sobre qué puede ser 'basura' y **arriba** del SESGO OBLIGATORIO A CONSERVAR.

Quedaron tres bloques encadenados:
1. **LAS INTERVENCIONES TERAPÉUTICAS RECONOCIDAS REALES NUNCA SON 'BASURA'** — lista de las 6
   familias que cuentan como intervención reconocida (medicamento, agente diagnóstico,
   dispositivo, procedimiento de cualquier especialidad, psicoterapia/salud mental, terapia
   física/ocupacional/del habla) + el ruteo a 'valido'/'dudoso'.
   Cierra con **COHERENCIA**: dos registros de la misma familia se clasifican igual, con los
   dos pares reales del motivo como ejemplo (ideación suicida ↔ intento de sobredosis;
   EMDR ↔ Brainspotting). Esto ataca la inconsistencia observada, no solo el umbral.
2. **DESLINDE** — ampliado a procedimientos y terapias, y con las pseudoterapias del REQ
   (Reiki, biorresonancia, radiestesia, flores de Bach, osteopatía craneal, NAET, Hoxsey…) y
   los protocolos-de-persona explícitos.
3. **DESEMPATE REFORZADO** — "que algo no tenga relación con la EII jamás es motivo suficiente
   para mandarlo a 'basura'". Es la frase que cierra el patrón que se repitió en v3 y v4.

Diff: +45 líneas, 0 borradas de otras secciones. `dotnet build -c Release` → **0 errores**
(751 warnings preexistentes, ninguno nuevo). `dotnet publish -c Release` → limpio.

Nada se desactivó: sigue siendo dry-run y no se re-clasificó ningún registro ya sellado.

**Pendiente:** la sonda read-only de verificación sobre Cirugías la corre Berletzis por el
panel (el REQ pide el cambio de prompt + build + publish, no la corrida).
