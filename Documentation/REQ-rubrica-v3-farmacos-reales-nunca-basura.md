# REQ — Rúbrica v3: medicamentos / diagnósticos / dispositivos reales NUNCA son Basura

**Scope:** solo `eiibd26.Web` — `Services/AI/SintomasTratamientosAiService.cs` (el prompt de `BuildTriageSystemPrompt`). NADA más. NO tocar NINA-WorkerService.
**Ejecución (AUTORIZADO por Berletzis, sin pedir permisos):** aplica el cambio de prompt, build + `publish -c Release`, re-corre la sonda de SOLO LECTURA de verificación, muestra diff al final. NO desactiva nada.

## Motivo
En "Medicamentos con Receta" hubo **14 falsos positivos de 27** en Basura: la IA mandó a Basura fármacos, agentes diagnósticos y dispositivos médicos **reales** por ser cosméticos, de otra condición, o no-EII (Botox, DIU Paragard, Hidroquinona, Ciclopentolato/Cyclogyl, Hipurato de Sodio PAH, Metilfenidato, Ibudilast, Ibuprofeno-paracetamol-codeína, etc.). Venta Libre y Cirugías traen contenido igual de "médico real" → se repetiría. Una línea al prompt lo cierra.

## Cambio — agregar a la rúbrica (regla firme, arriba del sesgo de conservación)
> **Medicamentos, diagnósticos y dispositivos reales NUNCA son Basura.** Si el registro es (o su nombre principal es) un **medicamento reconocido** (nombre genérico/DCI, de prescripción o de venta libre), un **agente diagnóstico** (medios de contraste, midriáticos como ciclopentolato, reactivos clínicos como el hipurato de sodio) o un **dispositivo/producto médico** (DIU, órtesis, toxina botulínica, etc.), **nunca lo clasifiques como Basura** — aunque su uso sea cosmético (p. ej. hidroquinona, Botox), de otra condición, o sin relación con EII. → **Válido** si es claramente una intervención terapéutica; **Dudoso** si su rol es diagnóstico, estético, o no-EII y quieres que un humano lo confirme.

**Deslinde (para NO sobre-corregir):** esta regla aplica a **sustancias/fármacos/dispositivos reconocidos**, NO a **productos comerciales de consumo, cosméticos o alimentos** que solo *contienen* ingredientes. Siguen siendo **Basura** si no son una intervención terapéutica:
- Cosméticos de marca sin fármaco activo dermatológico reconocido (enjuagues bucales comerciales, mascarillas/hidratantes faciales, queratina para cabello/uñas).
- Alimentos y bebidas.
- Pseudoterapias/homeopatía y "protocolos" sin base científica (GcMAF, Protocolo Marshall, Protocolo CAP de Wheldon, quelación EDTA-DMPS-DMSA).
- Ruido / nombres sin sentido / no verificables, y **códigos o nombres de ensayo clínico** que NO nombran un fármaco concreto (p. ej. "CALGB 9251").

## Alcance del efecto
- Solo afecta clasificaciones **nuevas** (Venta Libre, Cirugías, y cualquier registro aún NULL). **NO** re-clasifica lo ya sellado (las 3 ramas cerradas se rescataron a mano; se dejan como están).

## Verificación (sonda read-only, sin escribir en BD)
Confirmar que ahora caen bien:
- **A Válido/Dudoso (ya NO Basura):** Toxina Botulínica Tipo A, Hidroquinona, Metilfenidato, Ciclopentolato oftálmico, Hipurato de Sodio (PAH), DIU de Cobre (Paragard), Ibuprofeno-paracetamol-codeína, Ibudilast.
- **Siguen en Basura (sin cambio):** GcMAF, Protocolo Marshall, EDTA-DMPS-DMSA-Clorela, CALGB 9251, Enjuague Bucal comercial, Mascarilla de Ácido Hialurónico, Queratina (Keragel), "No especificado".
- Un fármaco de EII normal (p. ej. Mesalazina, Prednisona) → sigue Válido.
- `dotnet publish -c Release` limpio.

---

## CERRADO — 10 AGO 2026

**La verificación de v3 se da por cerrada con evidencia de producción** (decisión del owner).
No se corren más sondas read-only sobre este REQ: el comportamiento del prompt ya se observó
en la clasificación real, no en un ensayo.

Estado al cierre:
- El cambio de rúbrica vive en `eiibd26/Services/AI/SintomasTratamientosAiService.cs`
  (`BuildTriageSystemPrompt`): bloque "MEDICAMENTOS, DIAGNÓSTICOS Y DISPOSITIVOS REALES NUNCA
  SON 'BASURA'" + su DESLINDE, colocados **arriba** del sesgo obligatorio a conservar.
- Alcance sin cambios: solo afecta clasificaciones **nuevas**. Las 3 ramas ya selladas se
  quedan como están (se rescataron a mano); v3 **no** re-clasifica nada retroactivamente.
- Nada se desactivó como parte de este REQ.

No re-abrir para "re-verificar". Si aparece un falso positivo nuevo, es un REQ nuevo (v4) con
su propio caso concreto, no una revisión de éste.

**Continuado por v4 (10 AGO 2026):** `REQ-rubrica-v4-psicoterapias-procedimientos-nunca-basura.md`.
La rama Psicoterapia mostró el mismo patrón en intervenciones **no farmacológicas**, así que v4
**generaliza** esta regla (el bloque del prompt se reescribió, no se duplicó): ya no habla de
"medicamentos, diagnósticos y dispositivos" sino de **cualquier intervención terapéutica
reconocida** (+ procedimientos, psicoterapia/salud mental, terapia física/ocupacional/del habla).
Este REQ queda como el registro histórico del primer caso; la redacción viva del prompt es la de v4.
