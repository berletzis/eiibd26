# NINA — Router de modelo IA, filtros de seguridad y caché

> Wiki técnica interna — no publicar. Incluye modelos exactos, palabras de alto riesgo, patrones de bloqueo y las fórmulas de similitud del caché.

## Qué problema resuelve

NINA es la asistente de IA que responde preguntas de la comunidad sobre EII. El router decide **qué modelo usar para cada pregunta** con dos objetivos en tensión: minimizar costo (no gastar el modelo premium en preguntas triviales) y no comprometer la seguridad clínica (nunca ahorrar en preguntas de riesgo). Alrededor del router hay una capa de seguridad que valida cada respuesta y un caché que reutiliza respuestas de preguntas casi idénticas.

## Cómo funciona por dentro

### 1. El router (`NinaModelRouterService.AskAsync`)

Flujo de decisión en 5 pasos:

**Paso 1 — Detección de alto riesgo (local, sin costo).** Se busca en el texto (título + cuerpo, en minúsculas) cualquier coincidencia de subcadena contra un conjunto de **palabras de alto riesgo médico**. Si hay match → se salta la clasificación y se usa **Claude Sonnet obligatoriamente**, con disclaimer o respuesta de respaldo.

Las palabras de alto riesgo (`HighRiskKeywords`): `sangre, fiebre, dolor fuerte, urgencias, urgencia, hospital, efecto secundario, efectos secundarios, empeoró, empeoro, empeorando, grave, mortal, muerte, suicidio, emergencia, intoxicación, intoxicacion, sobredosis, convulsión, convulsion, desmayo, inconsciente, no responde, pecho, corazón, corazon, respirar, ahogo, asfixia, mareo severo, vomito sangre, vómito sangre, tos sangre`.

**Paso 2 — Clasificación de complejidad.** Si no es alto riesgo, se pide a **Claude Haiku** (modelo económico, `max_tokens = 10`, `temperature = 0.0` determinista) que clasifique la pregunta en `SIMPLE`, `MEDIA` o `COMPLEJA`. Ante cualquier error de clasificación, se asume `COMPLEJA` por seguridad.

**Paso 3 — Selección de modelo según nivel:**

| Nivel | Modelo | Detalle |
|---|---|---|
| SIMPLE | Plantilla local EII (`Modelo Base EIIBD`) | Si no hay plantilla local para el tema, escala a Haiku |
| MEDIA | Claude Haiku | `max_tokens = 500`, `temperature = 0.3` |
| COMPLEJA | Claude Sonnet | vía `IAiAnswerService.GenerarRespuestaAsync` |
| Alto riesgo (paso 1) | Claude Sonnet | forzado, sin pasar por clasificación |

Las preguntas SIMPLE se intentan resolver primero con conocimiento local (`IBDKnowledgeTemplates.TryResolve`) — respuestas pre-programadas, costo cero. Solo si no hay plantilla se paga un modelo.

**Paso 4 — Validación de seguridad** (ver sección 2). **Paso 5 — Enriquecer con autoría** NINA (firma "Autor: NINA / Fuente: <modelo amigable>").

Modelos configurados (constantes):
- Sonnet: `claude-sonnet-4.5-20250514`
- Haiku: `claude-3-5-haiku-20241022`
- Base local: `"Modelo Base EIIBD"` (plantillas).

### 2. Capa de seguridad (`AiSafetyService`)

`ValidarContenido` devuelve `false` (y se sustituye por la respuesta de respaldo) si el contenido:

1. Contiene alguna **frase prohibida** configurada (`_config.ForbiddenPhrases`).
2. Coincide con alguno de **5 patrones regex peligrosos**, diseñados para permitir contenido educativo pero bloquear prescripción/imperativos directos:
   - Consejo de dosis imperativo: `(debes|debe|tienes que)\s+(aumenta|reduce|modifica|…)\s+…(dosis|mg|cantidad|medicamento)`.
   - Cese de medicación imperativo: `(debes|debe|tienes que)\s+(suspende|deja de|para|detén)\s+…(tomar|medicamento|tratamiento)`.
   - Diagnóstico definitivo: `(definitivamente tienes|con certeza padeces|claramente sufres)\s+…(cáncer|tumor|enfermedad terminal)`.
   - Instrucción de dosis específica: `(toma|consume)\s+\d+\s*(mg|tableta|pastilla|cápsula)\s+(de|cada|al día)`.
   - Modificación de tratamiento imperativa: `(suspende|cambia|modifica|aumenta|reduce)\s+(tu|el|la)\s+(medicamento|tratamiento|dosis)\s+(inmediatamente|ahora|sin consultar)`.

Los regex corren con timeout de **100 ms**; si expira, **falla-seguro** (bloquea). Toda respuesta válida recibe un **disclaimer** ("Esta respuesta es informativa y educativa. No reemplaza la consulta con un profesional médico…"), sin duplicarlo si ya está presente. Si algo falla, se devuelve una **respuesta de respaldo** genérica sobre EII + disclaimer.

### 3. Caché / detección de preguntas similares

Antes de pagar una respuesta nueva, se busca una pregunta previa con respuesta de IA lo bastante parecida para reutilizarla. Hay **dos implementaciones**:

**a) `SimilarQuestionDetector` — híbrido keywords + Levenshtein.** Considera las últimas 100 preguntas con IA de los últimos 90 días. Similitud final:

```
similitud = 0.70 · Jaccard(keywords) + 0.30 · similitudLevenshtein
```

- **Jaccard** sobre conjuntos de palabras (sin stopwords españolas, palabras ≥ 3 letras): `|A ∩ B| / |A ∪ B|`.
- **Levenshtein normalizada** sobre los primeros 300 caracteres: `1 − distancia / max(len₁, len₂)`.
- Umbral por defecto `0.80`; corta la búsqueda temprano si encuentra ≥ 0.95.

**b) `QuestionCacheService` — solo Levenshtein.** Normaliza (minúsculas, sin signos, quita stopwords) y compara con `1 − distancia / max(len)`. Umbral por defecto `0.85`, sobre las 100 preguntas con IA más recientes.

Distancia de Levenshtein = número mínimo de inserciones, borrados o sustituciones de caracteres para transformar un texto en otro (programación dinámica clásica, matriz `(len₁+1) × (len₂+1)`).

## Parámetros y umbrales (valores reales)

| Parámetro | Valor | Dónde |
|---|---|---|
| Modelo Sonnet | `claude-sonnet-4.5-20250514` | `NinaModelRouterService:37` |
| Modelo Haiku | `claude-3-5-haiku-20241022` | `:38` |
| Clasificación | max_tokens 10, temp 0.0 | `:211`–`:213` |
| Respuesta Haiku | max_tokens 500, temp 0.3 | `:301`–`:302` |
| Timeout regex safety | 100 ms, falla-seguro | `AiSafetyService:98`,`:116` |
| Ponderación similitud híbrida | 0.70 keywords / 0.30 Levenshtein | `SimilarQuestionDetector:158` |
| Umbral similar (híbrido) | 0.80 (corte anticipado 0.95) | `:41`,`:92` |
| Ventana de búsqueda | 100 preguntas / 90 días | `:51`,`:61` |
| Umbral caché (Levenshtein) | 0.85 | `QuestionCacheService:28` |

## Dónde vive

- Router: `eiibd26/Services/AI/NinaModelRouterService.cs` — `AskAsync` en `:55`, alto riesgo `DetectHighRisk` en `:167`, clasificación `ClassifyQuestionAsync` en `:188`, respuesta simple/local `:256`, Haiku `:274`.
- Seguridad: `eiibd26/Services/AI/AiSafetyService.cs` — `ValidarContenido` en `:51`, patrones en `:76`, disclaimer en `:17`, fallback en `:19`.
- Detector híbrido: `eiibd26/Services/AI/SimilarQuestionDetector.cs` — `CalcularSimilitud` en `:136`, Jaccard `:198`, Levenshtein `:212`/`:223`.
- Caché Levenshtein: `eiibd26/Services/AI/QuestionCacheService.cs:26`/`:87`.

> Nota: el modelo real que atiende la generación premium/Haiku puede depender de la configuración `AiAnswerConfiguration`; las constantes de arriba son los identificadores por defecto en el router. El Worker externo (NINA-WorkerService) queda fuera del alcance de esta wiki.

## Cómo explicarlo en una presentación

NINA es como un triage de urgencias para preguntas. Primero, un filtro instantáneo y gratis busca señales de alarma ("sangre", "fiebre", "emergencia"): si aparecen, va directo al médico más experto (el modelo premium), sin escatimar. Si no hay alarma, un modelo barato clasifica la pregunta en fácil / media / difícil y la deriva al recurso adecuado: las fáciles se responden con plantillas ya escritas (gratis), las medias con un modelo económico, las difíciles con el premium. Antes de responder, un revisor de seguridad lee la respuesta y bloquea cualquier cosa que suene a "recetar" o "diagnosticar", reemplazándola por una respuesta segura, y siempre agrega el aviso de "consultá a tu médico". Y si alguien ya hizo una pregunta casi igual, reutilizamos esa respuesta en vez de pagarla de nuevo.

Analogía del caché: es un empleado con buena memoria que, antes de escribir una respuesta nueva, revisa si ya contestó algo casi idéntico en los últimos meses; si el parecido supera el 80-85%, copia la respuesta anterior.

## Limitaciones y supuestos

- La detección de riesgo es por **subcadena literal**: puede tener falsos positivos ("corazón" en un contexto inocuo) y no capta riesgos expresados con otras palabras.
- La similitud del caché es **léxica** (keywords + edición de caracteres), no semántica: dos preguntas con el mismo sentido pero distinta redacción pueden no cruzar el umbral.
- Los umbrales (0.80 / 0.85 / ponderación 70-30) son heurísticos.
- Coexisten dos servicios de similitud (`SimilarQuestionDetector` y `QuestionCacheService`) con fórmulas distintas — deuda de duplicación.
- El caché escanea solo las 100 preguntas más recientes: coincidencias más viejas se pierden.
- Este documento es un tema de salud sensible; la capa de seguridad reduce pero no elimina el riesgo de contenido clínico inadecuado.
