# Resumen de lo construido — material para artículos, video y publicación

**Fecha:** 14 JUL 2026
**Uso:** semilla para los artículos semi-técnicos del sitio, y de ahí los guiones de video y las publicaciones. Cada bloque trae *qué hace para el usuario* (para el artículo) y *cómo funciona por dentro* (para la parte semi-técnica), sin revelar la "fórmula secreta".

---

## 1. Motor de Cobertura con embeddings — "Radar de Contenido" y "Artículos Similares"

**Para el usuario:** ahora la plataforma relaciona contenido por **significado**, no por palabras. Busca "fatiga" y encuentra lo que habla de "cansancio". En cada artículo ves lo relacionado, dentro y fuera de EIIBD.

**Semi-técnico (sin receta):** migramos el motor de una firma por conteo de vocabulario a **embeddings** (representaciones numéricas del significado). Dos textos que hablan de lo mismo quedan "cerca" aunque no compartan palabras. El caso que lo validó: un artículo de aspirina infantil encontró su gemelo externo casi idéntico, algo que el conteo de palabras nunca lograba. Multilingüe (español/inglés sin traducir).

**No revelar:** proveedor, umbrales, arquitectura del pipeline.

## 2. "Servicio de NINA" — el rastreador de contenido externo

**Para el usuario:** una parte de la plataforma revisa a diario sitios de confianza sobre EII y te conecta con material útil de otros lados — siempre enlazando al original, nunca copiándolo.

**Semi-técnico:** el Worker recorre sitemaps de fuentes de confianza, respeta `robots.txt`, nunca republica (guarda solo el significado + enlace). Se sumó **Educa Inflamatoria** como fuente nueva, con una lista de exclusión de URLs para dejar fuera páginas basura (autores, tags, patrocinadores).

**Disciplina legal (sí contar, es confianza):** robots permite mirar, no copiar; nunca se saltan logins ni paywalls; se acredita y enlaza a la fuente.

## 3. Vista "Oportunidades de contenido" (para editores)

**Para el usuario (editor):** un tablero que muestra qué temas cubren sitios de confianza y a EIIBD le faltan — para saber qué escribir. Con % de similitud y estado "sin cubrir / ampliar / cubierto".

**Semi-técnico:** reusa la data de cobertura (embeddings) y la reenmarca como backlog accionable. Umbrales del editor **desacoplados** de los del paciente (subir el corte editorial no afecta lo que ve el paciente).

## 4. Módulo de Platillos (lo grande de la sesión)

**Para el usuario:** dile a la plataforma qué **no toleras** (lácteos, picante, crudo, cebolla…) y te muestra solo los platillos que sí puedes comer. Cada ingrediente tiene su ficha "¿Puedo comer queso?".

**Semi-técnico / decisiones de diseño (buen material de artículo):**
- **No son recetas, son "platillos"** — decisión legal: se toman datos (ingredientes, cantidades), no los pasos con derechos de autor.
- Modelo de datos en tres ejes: **Grupo** (qué ES el alimento) ≠ **Atributo intrínseco** (picante, cítrico, gluten) ≠ **Atributo de uso** (crudo/frito, por platillo). Esa separación es la que hace que el filtro funcione bien.
- Filtro por grupo, ingrediente y atributo; el estado vacío nunca deja pantalla en blanco (muestra los más cercanos con el motivo).
- Principio que se repite en toda la UI: **"esto no es una dieta"** — la plataforma no dice si algo te hace bien, solo filtra por lo que declaraste.

## 5. Notas clínicas con validación médica

**Para el usuario:** las fichas de ingredientes ("¿puedo comer queso?") son contenido médico, escrito con estructura de glosario y **con bibliografía real** (ESPEN, Crohn's & Colitis Foundation, estudios). Llevan un sello "Validado por profesionales de la salud".

**Semi-técnico / lo que da confianza:**
- **Contenido con fuente:** si le decimos a un paciente que no evite los lácteos, citamos en qué nos basamos. La disciplina de citar incluso nos hizo corregir afirmaciones (la carne roja NO se asocia con recaídas en quien ya tiene EII, según metaanálisis 2025 — íbamos a decir lo contrario).
- **Candado de seguridad:** una nota clínica **no se le muestra al paciente hasta que el admin la publica**. El sistema se niega a mostrar lo no publicado — seguro por construcción, no por disciplina.
- **Validación médica = señal, no interruptor:** el médico valida la nota (respaldo de confianza), pero **publicar lo decide el admin**. Dos ejes independientes. Calcado del flujo del glosario de síntomas.

---

## Pendientes (no urgentes)

- **Deploy-gate:** revertir la nota de queso a no-publicada antes de subir a producción (quedó publicada para pruebas locales).
- Prueba tranquila de corrido del módulo de Platillos completo.
- Seguridad: limpieza de git HEAD + decisión de purga de historia (repo público con secretos viejos ya rotados).
- Módulo de Tolerancia alimentaria (bayesiano) — diseñado, no construido (tarea aparte).
- Bibliografía en el glosario (F3) — requiere trazar dónde vive cada descripción.

## Plan para mañana

1. Escribir **artículos semi-técnicos** para el sitio, uno por bloque de arriba (voz `habla-berletzis` para los divulgativos; tono más explicativo para los técnicos, "informar sin dar la fórmula").
2. De cada artículo, derivar el **guion de video**.
3. Y de ahí, la **publicación** (redes / comunidad).
