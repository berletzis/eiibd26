# Slugs, códigos cortos, detección de bots y evaluador editorial GRIS

> Wiki técnica interna — no publicar. Agrupa cálculos utilitarios y de calidad de contenido.

Este artículo cubre cuatro mecanismos menores pero con lógica algorítmica propia.

---

## 1. Generación y deduplicación de slugs

### Qué problema resuelve

Convertir un título ("¿Qué es la Colitis Ulcerosa?") en un slug URL-safe y estable (`que-es-la-colitis-ulcerosa`), garantizando unicidad porque las rutas públicas dependen de él (SEO).

### Cómo funciona (`Slugify`)

1. Minúsculas → descomposición Unicode `FormD`.
2. Se eliminan las **marcas diacríticas** (acentos): se filtran los caracteres de categoría `NonSpacingMark`.
3. Se borra todo lo que no sea `[a-z0-9\s-]`.
4. Espacios → guiones; guiones repetidos → un solo guión.

Es la misma técnica de normalización que la firma (artículo 03), pero conservando el guion como separador en vez de colapsar todo a espacios.

### Deduplicación / validación

- Al guardar contenido, si el slug viene vacío se genera desde el título; luego se verifica colisión contra otros contenidos no eliminados (`OnGetCheckSlugAsync` / consulta `AnyAsync` con exclusión del propio Id).
- La URL SEO final antepone el slug de la **categoría principal** del contenido (`BuildSeoUrlAsync`).

**Dónde vive:** `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Detalle.cshtml.cs` — `Slugify` en `:1527`, chequeo de unicidad `OnGetCheckSlugAsync` en `:255`, generación al guardar `:308`, URL SEO `BuildSeoUrlAsync` en `:1540`.

---

## 2. Códigos cortos de URL (`ShortUrlService`)

### Qué problema resuelve

Generar enlaces cortos únicos (para campañas y compartir) y contar clics reales.

### Cómo funciona

- **Código:** 6 caracteres (`CodeLength = 6`) de un alfabeto de 62 (`[a-zA-Z0-9]`), generados con `RandomNumberGenerator` (criptográficamente seguro) mediante `byte % 62`.
- **Unicidad por reintento:** hasta 5 intentos (`MaxRetries`); si el código ya existe, se regenera; tras 5 colisiones lanza excepción. El espacio es 62⁶ ≈ 5.68·10¹⁰, así que las colisiones son raras.
- **Conteo de clics:** incrementa `ClickCount` y registra una fila `ShortUrlClick` con timestamp por cada visita no-bot.

> Nota estadística: `byte % 62` introduce un **sesgo de módulo** leve (256 no es múltiplo de 62), así que los primeros valores del alfabeto son marginalmente más probables. Irrelevante para el propósito, pero real.

**Dónde vive:** `eiibd26/Services/ShortUrl/ShortUrlService.cs` — `GenerarCodigo` en `:69`, creación con reintentos `:19`, conteo de clic `:55`.

---

## 3. Detección de bots (`BotDetector`)

### Qué problema resuelve

No contar como "clic humano" los accesos de crawlers y generadores de vista previa (WhatsApp, Facebook, Slack, Googlebot…), que golpean los enlaces cortos al desplegar previews.

### Cómo funciona

Sobre el `User-Agent`:

1. Si está vacío → se trata como bot (`true`).
2. Coincidencia por subcadena contra una **lista de bots conocidos**: `facebookexternalhit, WhatsApp, Twitterbot, Slackbot, TelegramBot, LinkedInBot, Discordbot, Googlebot, bingbot, Applebot, redditbot, Pinterest, SkypeUriPreview, vkShare, W3C_Validator, Embedly`.
3. Patrón genérico regex: `\b(bot|crawler|spider|preview)\b` (case-insensitive).

**Dónde vive:** `eiibd26/Services/ShortUrl/BotDetector.cs:32` (lista `:8`, patrón genérico `:28`).

---

## 4. Evaluador editorial GRIS (`GrisEvaluadorService`)

### Qué problema resuelve

Puntuar la **calidad editorial** (no médica) de un artículo con una rúbrica consistente, y sugerir/alertar categorías. Es un evaluador basado en LLM, no una fórmula estadística.

### Cómo funciona

- Toma título + cuerpo (HTML removido, truncado a **3000 caracteres**) y el catálogo de categorías del sitio.
- Llama a Claude (`temperature = 0.3`, `max_tokens = 1500`) con un system prompt que fuerza **salida JSON pura** (sin markdown), robustecida al parsear (extrae JSON aunque venga con fences).
- Rúbrica de **7 aspectos**, cada uno puntuado **1–10**: (1) Claridad y estructura, (2) Relevancia y utilidad, (3) Precisión y credibilidad, (4) Lenguaje adecuado, (5) Originalidad y voz propia, (6) Engagement y experiencia, (7) Optimización técnica.
- Devuelve además un `puntajeGlobal` **0–100**, sugerencias, y categorías sugeridas/alerta.
- **Anti-alucinación:** las categorías sugeridas/alerta se **filtran contra los IDs reales** del catálogo; los IDs inventados se descartan. Los puntajes se recortan con `Math.Clamp` (aspectos 1–10, global 0–100).
- Persiste el resultado (upsert en `ContenidoCalidad`): puntaje global (byte), aspectos, sugerencias y categorías, con fecha.

El puntaje global lo **produce el modelo**, no una suma ponderada de los 7 aspectos en el código (el servicio solo lo recorta a 0–100). No hay una fórmula determinística que combine los aspectos: es un juicio del LLM.

**Dónde vive:** `eiibd26/Services/Calidad/GrisEvaluadorService.cs` — `EvaluarAsync` en `:52`, rúbrica en el prompt `:144`, construcción/validación del DTO `BuildDto` en `:232` (clamps `:241`/`:245`, filtro de categorías `:250`/`:258`), persistencia `:278`.

---

## Cómo explicarlo en una presentación

Cuatro piezas de plomería inteligente. **Slugs:** convertimos títulos en direcciones web limpias y únicas, sin acentos ni símbolos, porque de ellas depende que Google nos encuentre. **Enlaces cortos:** generamos códigos aleatorios de 6 caracteres, garantizando que no se repitan, y contamos los clics. **Detector de bots:** cuando WhatsApp o Google visitan un enlace para armar la vista previa, no lo contamos como una persona real. **GRIS:** un evaluador de IA que le pone nota del 1 al 10 a siete aspectos de cada artículo (claridad, utilidad, precisión…) y sugiere en qué categorías encaja, con un candado que descarta categorías que la IA se invente.

## Limitaciones y supuestos

- **Slugs:** quitar acentos puede colisionar títulos que solo difieren en tildes; la unicidad se resuelve por chequeo, no por sufijo automático.
- **ShortUrl:** sesgo de módulo leve en el alfabeto; el límite de 5 reintentos asume baja densidad de códigos usados.
- **BotDetector:** lista estática de user-agents; bots nuevos o que se disfrazan de navegador no se detectan (falsos negativos), y un UA vacío legítimo se marca como bot (falso positivo conservador).
- **GRIS:** el puntaje depende del criterio del LLM y de `temperature = 0.3` (no totalmente determinista); no es una métrica objetiva reproducible. Trunca el cuerpo a 3000 caracteres, así que artículos largos se evalúan parcialmente.
