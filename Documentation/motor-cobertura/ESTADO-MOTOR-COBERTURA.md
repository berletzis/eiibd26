# Estado del Motor de Cobertura de Contenido — EIIBD

> **Propósito de este archivo:** dejar registrado el estado completo del Motor de
> Cobertura para retomar en cualquier sesión futura sin perder contexto: qué está
> hecho, qué falta, qué se decidió y por qué, y qué **NO** hacer.
>
> Última actualización: 2026-07-08 · Fases 1 y 2 completadas. Próximo: Fase 3 (similitud).
> Documento hermano: [`vocabulario-conceptos-propuesta.md`](./vocabulario-conceptos-propuesta.md).

---

## 1. Visión del motor

El Motor de Cobertura responde a una pregunta editorial: **"¿la plataforma ya cubre
este tema, y qué tan bien lo cubre frente a lo que hay afuera?"** Sirve para decidir
qué contenido crear, detectar huecos y evitar duplicados.

Tres entradas de uso previstas:

1. **Artículo externo del crawler** — el Worker (NINA) trae artículos de sitios de
   EII; el motor mide qué tan parecidos son a lo que EIIBD ya publicó.
2. **Búsqueda de tema en admin** — un editor busca un tema y ve qué cobertura propia
   y externa existe, con % de similitud.
3. **Anti-duplicado al crear** — al redactar un artículo nuevo, avisar si ya hay
   contenido propio muy similar (evitar duplicar).

---

## 2. Cómo funciona la firma

- Se toma el **texto completo** del contenido (título + cuerpo HTML), se limpia
  (StripHtml + normalización: minúsculas, sin acentos vía `Normalize(FormD)`, sin
  signos) **en memoria**.
- Se **cuenta cuántas veces aparece cada concepto** del vocabulario EII en ese texto.
  Términos multi-palabra ("colitis ulcerosa") se cuentan como frase completa.
- Se guarda **solo la firma numérica** (JSON) — **el texto se descarta**.
- Formato de la firma (columna `contenidos.Firma`, `NVARCHAR(MAX)`):

  ```json
  { "v": 1, "totalTokens": 812, "counts": { "inflamacion": 4, "diarrea": 2 } }
  ```

  - `v`: versión del formato (para migrar/comparar en el futuro).
  - `totalTokens`: total de palabras del texto (para **normalizar** en la comparación
    sin re-procesar el texto).
  - `counts`: diccionario **disperso** término→conteo (solo términos con conteo > 0).

- **Comparación (Fase 3):** similitud de **coseno** entre firmas, con **pre-filtro
  Jaccard** para descartar pares obviamente distintos antes del cálculo costoso
  (mismo patrón O(n²) con pre-filtro que ya usa `ContenidoCalidadService`).

---

## 3. Decisiones clave tomadas (con el porqué)

| Decisión | Por qué |
|---|---|
| **Contenido PROPIO se guarda completo** | Es de EIIBD, sin riesgo legal. Ya vive en `contenidos`. |
| **Contenido AJENO: solo firma, el texto se descarta** | Guardar el texto ajeno = copia no autorizada aunque no se muestre. La firma numérica **no necesita** el texto original. |
| **Traducción inglés→español con Anthropic, en memoria, antes de firmar; se descarta** | El vocabulario EII está en español; para firmar un artículo en inglés hay que traducirlo primero. La traducción es efímera (solo para contar términos). |
| **Vocabulario = `GlossaryTerm WHERE Activo=1 AND MedicalRelationSuggestedId=1` (Directa), SIN filtrar por `TipoTermino`** | Incluye tipo 1 (síntoma), 2 (tratamiento) y 3 (concepto general EII). Solo relación **Directa** para evitar ruido (Indirecta/Secundaria traen ~9.800 términos ajenos: medicamentos de otras condiciones). |
| **Se creó `TipoTermino=3` (ConceptoGeneralEII)** | El glosario solo tenía síntomas/tratamientos; faltaban condiciones (Crohn, colitis), dieta, embarazo, salud mental y experiencia del paciente. Sin ellos, artículos reales quedaban con firma vacía. |

---

## 4. Estado por fase

### ✅ Fase 1 — Firmar contenido propio (COMPLETADA)

Commits: **`f7cae47`** (motor base) + **`d300426`** (vocabulario ampliado).

- `Services/Cobertura/FirmaService.cs` (+ `IFirmaService`): carga vocabulario,
  normaliza, cuenta, serializa a JSON, guarda en `contenidos.Firma` + `FirmaCalculadaEn`.
- `Jobs/FirmaContenidoJob.cs`: Hangfire, `IServiceScopeFactory`, `[AutomaticRetry(2)]`,
  **reanudable** (procesa solo `Firma IS NULL`).
- Vista admin **`/Identity/Admin/Contenidos/Firmas`**: progreso (firmados/total con
  polling), botón "Recalcular firmas" (pendientes) y "Forzar recálculo total".
- **Esquema (YA ejecutado, sin migración EF):** `ALTER` a `contenidos` con
  `Firma NVARCHAR(MAX) NULL` + `FirmaCalculadaEn DATETIME NULL`.
- **Vocabulario ampliado de 120 → 207 términos**: 87 conceptos tipo-3 cargados vía
  [`SQL/insert-conceptos-eii-firma.sql`](../../SQL/insert-conceptos-eii-firma.sql)
  (idempotente, `WHERE NOT EXISTS` por Nombre o Slug; algunos de los ~103 propuestos
  no entraron por ya existir como síntoma/tratamiento — correcto).
- **Resultado:** **105 artículos firmados**, **10 firmas vacías** (páginas
  institucionales sin conceptos EII — correcto que estén vacías).
- Hallazgo resuelto: el artículo 141 ("16 Datos sobre la Enfermedad de Crohn") **no
  contaba "crohn"** porque las condiciones no estaban en el vocabulario. Resuelto con
  los conceptos tipo-3.

### ✅ Fase 2 — Firmar externos (COMPLETADA)

Commits: **`2879ff7`** (2A biblioteca) · **`8cce074`** + **`f19338a`** (2B español + panel) · **`72ebb7c`** (2C traducción).

- **2A — Biblioteca compartida `eiibd26.Firma`** (net8.0, sin EF): lógica pura
  (`FirmaCalculator`: `Normalizar`/`StripHtml`/`CompilarVocabulario`/`Calcular` +
  `FirmaDto` + `VocabularioTermino`). El Web `FirmaService` delega en ella.
  Verificado: 8/8 firmas byte-idénticas old-vs-new → las 105 propias no cambian.
- **2B — Worker firma español (funeiico):** entidad read-only `GlossaryTerm`, lee el
  vocabulario (mismo filtro que el Web), extrae texto principal con HtmlAgilityPack
  (excluye `script/style/nav/header/footer/aside`), firma con la biblioteca y guarda
  solo la firma en `ScrapedPage.Firma` + `FirmaCalculadaEn`. Optimización lastmod
  (no re-firma si `PublishedAt` no cambió). **Validado: 68 funeiico firmados**,
  formato correcto, `AnclasConHtml=0`. Panel admin read-only en el Web:
  `/Identity/Admin/Contenidos/FirmasExternas`.
- **2C — Traducción EN→ES (mycrohns):** `AnthropicTranslationService` (cliente HTTP
  propio, replica el patrón del Web; **NO** toca NINA/GRIS). Inglés: traduce en
  memoria → firma la traducción → guarda solo la firma; texto y traducción se
  descartan. Optimización lastmod aplica igual.

**Esquema (ejecutar):** `SQL/alter-scrapedpage-firma.sql` (Firma + FirmaCalculadaEn en ScrapedPage).

**Config del Worker (2C):** sección `Anthropic` en `NINA-WorkerService/appsettings.json`
(en disco, **no versionada** por sparse-checkout — contiene secretos). La `Anthropic:ApiKey`
va en **user-secrets/env** del Worker, nunca hardcodeada. Sin key, el inglés se deja sin firmar.

### ⏳ Fase 3 — Cálculo de similitud (PENDIENTE)

Similitud de coseno propio-vs-externo (pre-filtro Jaccard) → persistir en una tabla
**`ArticleSimilarity`** (propioId, externoId, score, calculadoEn).

### ⏳ Fase 4 — Vistas (PENDIENTE)

- **Paciente:** partial en `Contenidos/Detalle` — "sitios externos similares" + % de
  similitud.
- **Admin:** grid de temas escaneados con % de cobertura/similitud.

---

## 5. Decisiones de arquitectura de Fase 2 (RESUELTAS)

1. **¿Dónde se firman los externos?** → En el **Worker** (net10/EF10), en la misma
   pasada del crawl. El desajuste EF8/EF10 se evitó con una **biblioteca compartida
   net8.0 sin EF** (`eiibd26.Firma`) que ambos referencian.
2. **¿Cómo comparte el Worker el vocabulario?** → Entidad **read-only `GlossaryTerm`**
   en el `Eiibd26Context` del Worker (misma BD), mismo filtro que el Web.
3. **Traducción EN→ES** → cliente Anthropic **propio** del Worker (patrón del Web,
   key en config del Worker), sin tocar NINA/GRIS.
4. **Optimización lastmod** → implementada: no re-firma/re-traduce si `PublishedAt`
   no cambió.

**Pendiente para Fase 3:** el **umbral de similitud** para considerar "cubierto" /
"duplicado" (se define al implementar el cálculo coseno).

---

## 6. Qué NO hacer (aprendizajes)

- ❌ **NO guardar el texto completo de externos** — solo la firma numérica.
- ❌ **NO guardar la traducción de externos** — es efímera, solo para firmar.
- ❌ **NO meter términos genéricos al vocabulario** (ej. "leche", "correr" de relación
  Indirecta = ruido). **Solo Directa.**
- ❌ **NO firmar con un vocabulario sin las condiciones** (el hallazgo del artículo
  141: "Crohn" no se contaba). Ya resuelto con tipo-3.
- ❌ **NO tocar NINA / GRIS / `AiAnswerService`** al llevar Anthropic al Worker (si se
  decide esa vía). Son servicios estables en producción.

---

## 7. Pendientes menores de Fase 1

- **Depurar variantes duplicadas** en el vocabulario (ej. "la inflamacion" vs
  "inflamacion"; "sistema inmune" vs "sistema inmunologico"). Depuración con datos
  reales, no bloqueante.
- **3ª categoría pública del glosario:** el enum `GlossaryTermType.ConceptoGeneralEII = 3`
  ya existe, pero falta:
  - Página pública `Pages/Glosario/Conceptos.cshtml(.cs)` (clon de `Sintomas`, usa el
    type-agnóstico `GetTermsByTypeAsync`).
  - Link/tarjeta en `Pages/Glosario/Index`.
  - **Arreglar `Pages/Glosario/Termino.cshtml:433`**, que hoy asume síntoma/tratamiento
    (`tipoLabel = isSintoma ? "Síntoma" : "Tratamiento"`) y etiquetaría mal un
    concepto tipo-3.

---

## 8. Archivos clave

| Archivo | Rol |
|---|---|
| `Services/Cobertura/FirmaService.cs` · `IFirmaService.cs` | Cálculo y persistencia de la firma. |
| `Jobs/FirmaContenidoJob.cs` | Job Hangfire reanudable. |
| `Areas/Identity/Pages/Admin/Contenidos/Firmas.cshtml(.cs)` | Vista admin (progreso + disparo). |
| `Models/Contenido.cs` | Campos `Firma` + `FirmaCalculadaEn`. |
| `Models/Glossary/GlossaryTermType.cs` | Enum con `ConceptoGeneralEII = 3`. |
| `SQL/insert-conceptos-eii-firma.sql` | Carga idempotente de conceptos tipo-3. |
| `Documentation/motor-cobertura/vocabulario-conceptos-propuesta.md` | Lista revisable de conceptos por área. |
