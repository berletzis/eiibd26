# REQ — Limpieza / triage de Síntomas (YA CONSTRUIDO — esto es operar, no construir)

**Scope:** operacional + ajustes de código chicos. **NO construir la infraestructura de triage: ya existe.** NO tocar NINA-WorkerService ni Conectar3eros.

**Validación:** verificado contra el código y contra la BD de producción (norma: pasada adversarial antes de implementar). La primera versión de este REQ pedía construir todo desde cero — era incorrecta; el pipeline ya está hecho.

**Estado (2026-08-12):** triage **ejecutado en dry-run sobre los 195 síntomas vivos**. Resultado abajo. El catálogo resultó estar limpio; el valor de esta corrida fue la verificación, no la limpieza.

---

## ✅ Resultado de la operación (dry-run, 195 síntomas)

| Estado | Conteo | Nota |
|---|---|---|
| Válido | 176 | incluye 26 intactos (pacientes activos → sellados Válido sin gastar IA) |
| Basura | 2 | **1 es falso positivo** (ver abajo) → basura real efectiva: **1** |
| Dudoso | 17 | casi todos hallazgos de lab / diagnósticos vs. síntoma sentido |
| Sin revisar | 0 | catálogo completo |

**Lectura:** a diferencia de tratamientos (miles de basura), síntomas está curado. No hay un "aplicar" grande que hacer.

### Acciones puntuales que sí salieron de la corrida
1. **Rescatar "Soledad".** El triage la marcó Basura [0.95] leyéndola como nombre propio. Es un falso positivo: *soledad* = síntoma emocional real (aislamiento), pertinente en enfermedad crónica y EII. **NO desactivar.**
2. **Decidir "Niveles tóxicos del fármaco AINE"** [Basura 0.95]. Es un hallazgo de toxicología, no un síntoma sentido. Basura defendible, o moverlo a Dudoso si se prefiere conservarlo en cola.
3. **Renombrar el registro corrupto** "Dolor en las articulaciones **mal escritas o con error**" [Dudoso 0.70]. El nombre trae texto de QA embebido — es artefacto de captura, no ambigüedad clínica. Es *rename*, no triage. El síntoma real (artralgia) es válido.
4. **Decisión editorial (tuya, no de la IA) sobre los 17 dudosos:** ¿el catálogo lista **hallazgos de laboratorio y diagnósticos** (Anemia, hipocalemia, proteinuria, angina, abscesos, episodios bipolares, lesiones cerebrales…) como "síntoma"? El modelo duda con razón: no son algo que el paciente "siente", pero son frecuentes en EII. Van a la cola humana (paso final) + `noindex`.

---

## ⚠ Lo que YA existía (NO reconstruir)
- **Esquema (modelo C#):** `sintomas` tiene `RevisionLimpiezaEstado/Confianza/Motivo/Fecha` y `ValidadoIA/ValidadoHumano`.
- **IA (triage por nombre):** `ClasificarSintomaAsync` + rúbrica `BuildTriageSystemPromptSintomas`. Clasifica por el NOMBRE y trata la descripción existente como posiblemente envenenada.
- **Endpoints** (`Controllers/SintomasAdminController.cs`, `api/admin/sintomas`): `batch-review`, `ramas`, `basura-preview`, `batch-apply-basura`, `batch-generate-ia`. Guards incluidos.
- **Runner UI:** botón "Revisar con NINA" + modal + "Aplicar desactivación a Basura".
- **Consistencia de counts:** `batch-apply-basura` invoca `SincronizarActivoPorSintomasAsync` → lo desactivado se oculta también del glosario.

---

## Correcciones a versiones anteriores de este REQ
- **§1 (anti-alucinación en el generador de síntomas): YA HECHO.** `GenerarDescripcionSintomaAsync` devuelve el estado `Reconocido`, `BuildSintomaSystemPrompt` tiene la REGLA CERO, y `SintomasAdminController.PasaGateAsync` es espejo del de tratamientos con el gate cableado en los dos puntos de generación. Gate demostrado con sonda (`tipo:2`): Diarrea/Dolor abdominal → Reconocido; Xyzqwe → NoReconocido.
- **Esquema NO estaba en producción (esto sí era bloqueante).** Las 4 columnas `RevisionLimpieza*` vivían solo en el modelo C#, sin commitear. `SintomaGridItem` ya las proyecta → desplegar sin el SQL rompía la grilla admin con 500. **Corregido:** se corrió `SQL/add-sintomas-revision-limpieza.sql` en prod (columnas + índice, verificado).
- **§3 (bug de tabs "Sin clasificar"): no aplica.** `GetTermsByTypeAsync` usa `NivelRelacion = MedicalRelationTypeId ?? MedicalRelationSuggestedId` (sin `(MedicalRelationType)0`) y el método es compartido con tratamientos. Counts cuadran hoy: Home 195 = Glosario "Todos" 195.

---

## 🆕 Preservar y mostrar la justificación del Dudoso (agregado en esta sesión)
Pedido: que la leyenda de por qué un registro quedó Dudoso no se pierda y sea consultable.

- **Persistencia:** el motivo ya se guardaba en `RevisionLimpiezaMotivo` incluso en dry-run (el sello de clasificación se persiste antes del guard de dry-run). Los 17 dudosos ya tienen su justificación en BD.
- **Ancho:** subido de `NVARCHAR(400)` a **1000** por prevención: `[StringLength(1000)]` en `sintomas.cs` y `tratamientos.cs`, todos los `[..400]` de ambos controllers a `[..1000]`, y `SQL/widen-revisionlimpiezamotivo-1000.sql` (ALTER COLUMN idempotente, ambas tablas). **Correr antes del deploy.** NOTA: verificado en prod que los motivos reales miden 141–218 caracteres — **nada se truncó** con el límite viejo de 400. NO hace falta re-correr el triage de los dudosos para "recuperar" texto; ese trabajo no existe. El ancho de 1000 es solo colchón a futuro.
- **Ficha `/Termino/{slug}` — solo curadores:** `GlossaryTermDetailDto.TriageMotivo` poblado en `GetTermBySlugAsync` (síntomas y tratamientos); banner en `Termino.cshtml` visible solo si `TriageEstado==3` **y** `User.IsInRole("Administrador"|"Medico")`. El paciente NO lo ve — es razonamiento interno de curaduría, no contenido clínico.
- **Grilla admin de síntomas:** el motivo del Dudoso se muestra **visible** bajo la insignia (antes solo `title`/tooltip), con escape de HTML. Válido/Basura lo conservan en tooltip.
- **Pendiente/opcional:** replicar el motivo visible en la grilla admin de **tratamientos** (2,609 dudosos — ahí importa más).

---

## Lo que queda por decidir / hacer
1. **Aplicar (síntomas):** casi nada. Rescatar Soledad, decidir el AINE, renombrar el corrupto. No hay desactivación masiva.
2. **Regenerar las 195 descripciones** (los 195 vivos son `ValidadoIA=1`, generados con el prompt pre-guardrail — mismo agujero que Aangamik). El triage saca basura pero NO reescribe las descripciones posiblemente confabuladas de los síntomas reales. `batch-generate-ia { regenerar: true }` ya existe y ahora pasa por el gate. **Decisión tuya de alcance** antes de tocar: ~195 llamadas a Sonnet, reescribe todo el glosario de síntomas publicado. Hacerlo DESPUÉS del triage (no regenerar lo que se va a desactivar) y con spot-check.
3. **Contención `noindex`/404 para Dudoso (#7):** sigue sin construir; aplica a síntomas y tratamientos.

## Orden respecto a los otros REQs
1. `REQ-nina-anti-alucinacion-fichas.md` (guardrail + gate) — **hecho**, cubre el generador de síntomas.
2. **Este** — triage de síntomas ejecutado (dry-run); falta decidir regeneración (punto 2 de arriba).
3. `REQ-cantidad-ingrediente-decimales.md` — independiente.

## Reglas del proyecto
Esquema por SQL directo (aquí SÍ hizo falta: `add-sintomas-revision-limpieza.sql` + `widen-revisionlimpiezamotivo-1000.sql`, ambos antes del deploy). No cambiar rutas públicas. Analizar contra el código antes de implementar. Trabajar solo en `eiibd26.Web`.
