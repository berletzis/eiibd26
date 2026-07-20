# Wiki Técnica Interna — Modelos y Algoritmos de EIIBD

> Documentación **interna** (no publicar). Detalla las fórmulas reales, umbrales y la matemática exacta de cada modelo y cálculo de la plataforma, con referencias `archivo:línea` al código fuente. Objetivo: que el fundador pueda explicar cada algoritmo en presentaciones técnicas.
>
> Generada leyendo el código en `eiibd26/Services/`, `eiibd26.Voyage/`, `eiibd26.Firma/`, `eiibd26/Models/` y los documentos de diseño en `Documentation/`. No incluye NINA-WorkerService ni Conectar3eros.

## Artículos

1. [Embeddings y similitud semántica (Voyage AI)](01-embeddings-voyage-similitud-semantica.md) — proveedor Voyage, `voyage-4-large`, vectores de 1024 dims, similitud coseno densa, umbral de guardado 0.40, migración desde la firma de vocabulario.
2. [Motor de Cobertura / Radar / Artículos Similares](02-motor-cobertura-radar-articulos-similares.md) — cómo la similitud se traduce a estados (hueco / débil / cubierto) y las tres bandas del paciente (0.55 / 0.78), con umbrales editoriales desacoplados.
3. [Firma por conteo de vocabulario (método de 1ª generación)](03-firma-conteo-vocabulario.md) — vector disperso de conteos, coseno, compuertas de riqueza (4) y compartidos (3), umbral 0.50. Fallback vigente.
4. [NINA — Router de modelo IA, seguridad y caché](04-nina-router-ia-safety-cache.md) — enrutado Sonnet/Haiku/plantillas, palabras de alto riesgo, patrones de bloqueo, caché por similitud (Jaccard 0.70 + Levenshtein 0.30).
5. [Estadísticas de salud (mood, síntomas, tendencias, insights)](05-estadisticas-salud.md) — promedio de ánimo, regresión OLS de tendencia (umbral de pendiente ±0.05), umbral de dolor alto 7.
6. [Consenso médico del glosario y badges](06-consenso-medico-glosario-badges.md) — endoso binario vs consenso graduado (Directa/Indirecta/Secundaria + voto NINA), score de ranking 3-2-1, badges y permisos por nivel.
7. [Targeting de campañas y audiencias](07-targeting-campanas-audiencias.md) — segmentación por forma del hash de contraseña, exclusiones globales (rebotes, sistema), secuencias de toque y anti-reenvío por fase.
8. [Ratings y calificaciones](08-ratings-calificaciones.md) — voto binario like/dislike por usuario en artículos, glosario y platillos; agregación por conteo.
9. [Modelo bayesiano de tolerancia alimentaria (DISEÑO — no construido)](09-modelo-bayesiano-tolerancia-alimentaria.md) — consenso "X % de pacientes con [EII] toleran [alimento]"; formulación Beta-Binomial prevista. **No implementado en el repo.**
10. [Slugs, URLs cortas, detección de bots y evaluador editorial GRIS](10-slugs-shorturl-bots-gris.md) — normalización de slugs, códigos aleatorios de 6 chars, lista de bots, rúbrica GRIS de 7 aspectos (LLM).

## Notas transversales

- **Umbrales provisionales:** varios umbrales del Motor de Cobertura (0.55 / 0.78) y de NINA (0.80 / 0.85) están marcados en el código como heurísticos "a calibrar con datos reales". Se documentan como tales, no como valores validados estadísticamente.
- **Honestidad sobre lo no construido:** el modelo bayesiano de tolerancia (artículo 09) está diseñado como módulo futuro (tarea #16); el repositorio no contiene su fórmula ni su código. El artículo lo señala explícitamente.
- **Contenido de salud sensible:** los modelos de NINA, estadísticas de salud y tolerancia alimentaria producen contenido orientativo/educativo, nunca diagnóstico. Las capas de seguridad reducen pero no eliminan el riesgo clínico.
- **Deuda de duplicación conocida:** existen dos servicios de similitud de preguntas (NINA) y tres controladores de rating con lógica repetida.
