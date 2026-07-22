# REQ — Referencias por recuperación (real links), en cascada

**Fecha:** 21 JUL 2026
**Objetivo:** que las referencias de las notas de ingrediente/grupo sean **links reales recuperados**, no citas de la memoria del modelo. En cascada: índice del crawler → búsqueda en dominios de confianza → recurso general → nada.
**Cruza dos proyectos:** el crawler vive en `NINA-WorkerService` (fuera del scope de `eiibd26.Web`); la recuperación al generar la nota va en el web. Es una tanda que toca ambos.

## LA REGLA QUE NO SE ROMPE (en todos los niveles)
1. **Link siempre REAL** — recuperado de un índice o de una búsqueda que devuelve URLs de verdad. **El modelo NUNCA escribe una URL de memoria.** Si el modelo participa, solo **rankea/elige** entre candidatos recuperados y explica por qué encaja — no inventa el link.
2. **Solo dominios de confianza** (la whitelist: Mayo, CCF, ESPEN…). Nunca web abierta.
3. **Siempre candidato, no verdad.** "Parecido por significado" ≠ "respalda la afirmación". Cada referencia recuperada la **valida el humano** antes de publicar.
4. **Si no hay nada real y relevante → sin referencia + leyenda honesta** (la que ya existe). Nunca rellenar.

## Cascada
### Nivel 1 — Índice del crawler (el corazón, MVP)
Reusa el crawler (NINA) + embeddings (Motor de Cobertura). Al generar la nota:
- Embed del tema (nombre + grupo + atributos del ingrediente).
- Buscar en el índice crawleado las páginas **reales** de fuentes de confianza más similares (umbral del paciente).
- Ofrecerlas como **referencias candidatas** (título + URL real) en el editor, para que el humano confirme.
- **Depende de:** que las fuentes estén en el índice del crawler (ver REQ del crawler para Mayo, etc.).

### Nivel 2 — Búsqueda en vivo restringida a dominios de confianza (fase 2)
Si el Nivel 1 no trae nada:
- Búsqueda `site:mayoclinic.org OR site:crohnscolitisfoundation.org …` con una API de búsqueda → devuelve **URLs reales** de esos dominios → candidatas.
- **Decisión pendiente:** ¿qué capacidad de búsqueda? (API tipo Bing/Brave, o el buscador propio del sitio). El crawler hace sitemaps, no búsqueda arbitraria — esto es una dependencia nueva (costo + latencia al generar).

### Nivel 3 — Recurso general o nada
Si el Nivel 2 tampoco:
- Ofrecer un **recurso general** de una fuente de confianza (ej. la página de "dieta en EII" de Mayo/CCF), **etiquetado explícitamente**: *"Recurso general del tema, no una cita puntual — verifica que respalde lo que dice la nota."* NUNCA presentarlo como que sostiene la afirmación específica.
- Si ni eso: **sin referencia + leyenda honesta según estado de validación** (ya implementado).

## Fases sugeridas
- **Fase 1 (MVP, el valor real):** Nivel 1 — recuperación desde el índice del crawler. Ya entrega lo importante: links reales, imposibles de alucinar. Depende de tener las fuentes crawleadas.
- **Fase 2:** Nivel 2 (búsqueda en vivo) — decidir la API de búsqueda primero.
- **Fase 3:** Nivel 3 (recurso general) — el más débil; solo si aporta.

## Dependencias / orden
1. Crawler indexa las fuentes de confianza (Mayo español, CCF, ESPEN si crawlable) — con su chequeo de robots.txt como gate. **Va primero.**
2. Recuperación al generar la nota (Nivel 1) en `PlatillosAiService`, consultando el Motor de Cobertura.
3. (Fase 2) API de búsqueda para el Nivel 2.

## Fuera de alcance / no romper
- El candado de whitelist actual (para citas que el modelo igual proponga) se queda.
- La validación médica y el "nace borrador" se quedan — la recuperación da candidatos, no publica.
