# INVENTARIO — Fase 7: jubilación en standby de firma-por-conteo + traducción Anthropic

> **Tipo:** documento de inventario (SOLO LECTURA). No se modificó código, config ni datos.
> **Fecha:** 2026-07-09
> **Objetivo:** catalogar todo lo que depende de (a) la firma-por-conteo y (b) la traducción
> Anthropic del Worker, para poder jubilarlas **en standby** sin romper nada.
> **Alcance de la tarea:** `eiibd26` (Web), `NINA-WorkerService` y la librería compartida
> `eiibd26.Firma`. Conectar3eros queda fuera de alcance.

---

## 0. Resumen ejecutivo (TL;DR)

- **El switch user-facing ya está hecho.** Toda la superficie de paciente (Contenido/Detalle:
  "Similares de EIIBD", externos, "ver más") entra por `CoberturaVistaService` y lee
  **`CoberturaSimilitudEmbedding`** (embeddings Voyage). **Ninguna vista de paciente depende
  de la firma de conteo.**
- La dependencia de la **firma de conteo** que resta es **exclusivamente admin**: paneles
  `Cobertura`, `Similitud`, `Firmas`, `FirmasExternas` y el método
  `CoberturaVistaService.ObtenerCoberturaTemasAsync`.
- La **traducción Anthropic del Worker** usa la clave `Anthropic:ApiKey` (sección `Anthropic`),
  que es **DISTINTA** de la de NINA/GRIS (`AiAnswer:AnthropicApiKey`, sección `AiAnswer`, en el
  Web). Jubilar la del Worker **no afecta** al router de IA. Solo alimenta la firma de externos
  en inglés; su resultado (la traducción) nunca se persiste.
- **No hay cron/RecurringJob** en toda la solución. Todos los jobs (firma y embeddings) se
  disparan **on-demand** desde botones del panel admin, en la cola Hangfire `default`.
- La **Voyage key** vive hoy solo declarada (vacía) en `eiibd26/appsettings.json` bajo
  `Voyage:ApiKey`; en runtime viene de user-secrets / env var. El panel admin de API keys
  **no la lista todavía**.

---

## 1. Librería compartida `eiibd26.Firma/`

Lógica **pura** de firma (sin EF ni acceso a datos), compartida entre Web (propios) y Worker
(externos) para que ambas firmas sean idénticas en método.

### `FirmaCalculator.cs` — `public static class FirmaCalculator`
- `FirmaVersion = 1` — `FirmaCalculator.cs:21` (versión del formato serializado).
- **`CompilarVocabulario(IEnumerable<string> nombres)`** — `:38`. Normaliza cada nombre,
  deduplica, precompila un regex de frase `\b{term}\b` por término. Cada app lee sus nombres
  de BD y llama aquí.
- **`Calcular(string? titulo, string? cuerpo, IReadOnlyList<VocabularioTermino> vocab)`** — `:60`.
  Concatena título+cuerpo, `StripHtml` + `HtmlDecode` + `Normalizar`, cuenta ocurrencias por
  término (vector **disperso**, solo counts > 0) y serializa `FirmaDto` a JSON.
- **`StripHtml(string?)`** — `:83`. Quita tags vía regex (no elimina contenido de `<script>`/`<style>`).
- **`Normalizar(string?)`** — `:90`. minúsculas + quita acentos (Unicode FormD/NonSpacingMark) +
  no-alfanumérico→espacio + colapsa espacios.

> Nota: NO hay método `CalcularFirma` ni coseno dentro de la librería. El cálculo se llama
> `Calcular`; el coseno vive en los servicios de similitud (§3).

### `VocabularioTermino.cs` — `public sealed class VocabularioTermino` (`:9`)
Término normalizado + su `Regex Patron` de frase precompilado.

### `FirmaDto.cs` — `public sealed class FirmaDto` (`:9`)
`int V`, `int TotalTokens`, `Dictionary<string,int> Counts`. Serializa como
`{ "v":1, "totalTokens":N, "counts":{term→conteo} }`.

### Consumidores de la librería (todos siguen vivos):
| Consumidor | Método usado | file:line |
|---|---|---|
| Web `FirmaService` | `Calcular`, `CompilarVocabulario` | `FirmaService.cs:69,121` |
| Web `SimilitudService` | `Normalizar` (dedup por título) | `SimilitudService.cs:282` |
| Web `SimilitudEmbeddingService` | `Normalizar` (dedup por título) | `SimilitudEmbeddingService.cs:280` |
| Web `EmbeddingService` | `StripHtml` | `EmbeddingService.cs:163` |
| Worker `Worker.cs` | `CompilarVocabulario`, `Calcular` | `Worker.cs:47,388,398` |

> ⚠️ **Implicación para la jubilación:** la librería `eiibd26.Firma` **NO se puede eliminar**
> aunque se apague la firma: `SimilitudEmbeddingService` y `EmbeddingService` (motor nuevo)
> siguen usando `Normalizar` y `StripHtml`. Standby = dejar de *calcular firmas*, no borrar la lib.

---

## 2. Pipeline de firma-por-conteo (propios + externos)

### 2a. Propios (Web)
```
Admin Firmas.cshtml.cs  ──Enqueue──▶  FirmaContenidoJob  ──▶  IFirmaService (FirmaService)
   :63 / :73                          :31-33                    └─ FirmaCalculator.Calcular
                                                                └─ escribe contenidos.Firma / FirmaCalculadaEn
```
- `FirmaService.cs:38` `FirmarPendientesAsync` — lotes de 25 con `Firma == null`, estados
  visibles `{1,2,3}`. Escribe `contenidos.Firma` (JSON) + `FirmaCalculadaEn`.
- Vocabulario: `CargarVocabularioAsync` `:113` — `GlossaryTerms` con
  `Activo == true && MedicalRelationSuggestedId == Directa` (**sin** filtrar `TipoTermino`).
- `FirmaContenidoJob.cs:27` `[AutomaticRetry(Attempts=2)]`, cola `default`.
- DI: `Program.cs:383-384` (`IFirmaService`, `FirmaContenidoJob`).

### 2b. Externos (Worker) — con traducción EN→ES
```
Worker.cs (ScrapingWorker)  ──▶  FirmaCalculator.CompilarVocabulario / Calcular
                                  └─ (si es inglés) AnthropicTranslationService.TraducirAEspanolAsync
                                  └─ escribe dbo.ScrapedPage.Firma / FirmaCalculadaEn
```
- `Worker.cs:226` decide firmar: `vocab.Count>0 && (esEspanol || (esIngles && translator.Habilitado))`.
- `Worker.cs:375-394` (punto EN→ES): si la página es inglés, traduce **título+cuerpo juntos**,
  calcula firma sobre la traducción y guarda `anchor.Firma` / `FirmaCalculadaEn`. La traducción
  **NUNCA se persiste** (comentario `:377-379`).
- `Worker.cs:398` firma directa para español.

### 2c. Similitud por conteo (Web)
```
Admin Similitud.cshtml.cs  ──Enqueue──▶  SimilitudJob  ──▶  ISimilitudService (SimilitudService)
   :71 / :80                             :29-31              └─ lee contenidos.Firma + ScrapedPageRef.Firma
                                                             └─ coseno sparse
                                                             └─ escribe dbo.CoberturaSimilitud
```
- `SimilitudService.cs`: compuertas `RiquezaMin=4` (`:46`), `TerminosCompartidosMin=3` (`:47`),
  `CosenoMin=0.50` (`:48`). Coseno sparse `:314`. Escribe `CoberturaSimilitud` (`:149`).
- `SimilitudJob.cs:25` `[AutomaticRetry(Attempts=2)]`, cola `default`.
- DI: `Program.cs:387-388`.

---

## 3. El "switch" conteo → embeddings (dónde está el corte exacto)

### Motor viejo vs nuevo (tablas y jobs paralelos)
| | Viejo (conteo) | Nuevo (embeddings Voyage) |
|---|---|---|
| Modelo | `CoberturaSimilitud.cs:19` (`AFirmaEn`/`BFirmaEn`) | `CoberturaSimilitudEmbedding.cs:14` (`AEmbEn`/`BEmbEn`) |
| Tabla | `CoberturaSimilitud` (`DbContext:130`) | `CoberturaSimilitudEmbedding` (`DbContext:138`) |
| Job | `SimilitudJob` → `SimilitudService` | `SimilitudEmbeddingJob` → `SimilitudEmbeddingService` |
| Fuente | columna `Firma` (JSON counts) | columna `Embedding` (JSON `float[]`) |
| Pre-filtros | Riqueza≥4, Compartidos≥3 | **ninguno** (coseno denso directo) |
| Umbral | `CosenoMin=0.50` | `CosenoMin=0.40` |

Ambos jobs corren en paralelo (standby); cada uno escribe su propia tabla.

### Punto EXACTO del switch (lo que consume el paciente)
`CoberturaVistaService.cs` — **lee embeddings** en las dos rutas user-facing:
- **`ObtenerSimilaresAsync`** (externos / tab): `CoberturaVistaService.cs:105` →
  `_db.CoberturaSimilitudesEmbedding`.
- **`ObtenerSimilaresPropiosAsync`** ("Similares de EIIBD" propio↔propio):
  `CoberturaVistaService.cs:144` → `_db.CoberturaSimilitudesEmbedding`.
- Umbrales: `UmbralSimilares=0.78` (`:20`), `UmbralArea=0.55` (`:21`).

Consumidores user-facing (todos vía embeddings):
- `Pages/Contenidos/Detalle.cshtml.cs:384` (`ObtenerSimilaresPropiosAsync`)
- `Pages/Contenidos/Detalle.cshtml.cs:510` (`ObtenerSimilaresAsync("externos", …)`)
- `Pages/Contenidos/Detalle.cshtml.cs:654` (paginación "ver más")

### ¿Queda algo user-facing en conteo? **NO.**
Las únicas lecturas en runtime de la tabla vieja `CoberturaSimilitud` / columna `Firma` son
**admin**:
| Ubicación | Rol | Motor |
|---|---|---|
| `CoberturaVistaService.cs:167,178` (`ObtenerCoberturaTemasAsync`) | Admin | conteo |
| `Areas/…/Admin/Contenidos/Cobertura.cshtml.cs:46` (matriz de huecos) | Admin | conteo |
| `Areas/…/Admin/Contenidos/Similitud.cshtml.cs` (dispara/monitorea job viejo) | Admin | conteo |
| `Areas/…/Admin/Contenidos/FirmasExternas.cshtml.cs:52-63` (`ScrapedPage.Firma`) | Admin | conteo |
| `Areas/…/Admin/Contenidos/Firmas.cshtml.cs` (progreso firma propios) | Admin | conteo |

> Salvedad: `Admin/Contenidos/Detalle.cshtml.cs:719-751` (`ObtenerSimilaresPorTextoAsync`) usa
> una **tercera vía** (detector de texto en memoria `_similarDetector.CalcularSimilitud`),
> independiente tanto de conteo como de embeddings. Es admin.

---

## 4. Hangfire — registro y cron

- `eiibd26/Program.cs:421-427`: `AddHangfire` + `UseSqlServerStorage(DefaultConnection)` +
  `AddHangfireServer(WorkerCount=2)`. **Sin colas explícitas** → todo en `default`.
  Dashboard `/hangfire` protegido (`Program.cs:977-980`).
- `NINA-WorkerService` **no usa Hangfire** (es `AddHostedService<ScrapingWorker>`, cola en memoria).
- **No existe ningún `RecurringJob` / cron en toda la solución.** Todos los jobs se encolan
  fire-and-forget desde el panel admin:

| Job | Encolado desde | Schedule |
|---|---|---|
| `FirmaContenidoJob` | `Firmas.cshtml.cs:63,73` | Manual |
| `SimilitudJob` (conteo) | `Similitud.cshtml.cs:71,80` | Manual |
| `EmbeddingContenidoJob` | `Embeddings.cshtml.cs:76,91` | Manual |
| `SimilitudEmbeddingJob` | `SimilitudEmbedding.cshtml.cs:72,81` | Manual |

> Implicación: para "apagar" la firma **no hay que desprogramar ningún cron** — basta con
> retirar/ocultar los botones de los paneles `Firmas` y `Similitud` (y opcionalmente dejar de
> registrar los jobs viejos en DI). Nada dispara la firma automáticamente hoy.

---

## 5. DB — conteos reales (producción, SELECT solo lectura, 2026-07-09)

### Columnas (confirmadas por `sys.columns`)
`contenidos` y `ScrapedPage` tienen **ambas familias**: `Firma`,`FirmaCalculadaEn` (conteo, viejo)
y `Embedding`,`EmbeddingModelo`,`EmbeddingCalculadoEn` (Voyage, nuevo). `CoberturaSimilitud`
tiene `AFirmaEn`/`BFirmaEn`; `CoberturaSimilitudEmbedding` tiene `AEmbEn`/`BEmbEn`.

### Conteos
| Objeto | Filas |
|---|---:|
| `contenidos` — total | 160 |
| `contenidos` — con Firma (conteo) | 106 |
| `contenidos` — con Embedding (Voyage) | 106 |
| `ScrapedPage` — total | 253 |
| `ScrapedPage` — con Firma (conteo) | 163 |
| `ScrapedPage` — con Embedding (Voyage) | 163 |
| **`CoberturaSimilitud`** (viejo/conteo) — total | **2 441** |
| &nbsp;&nbsp;· TipoPar=1 (propio↔propio) | 463 |
| &nbsp;&nbsp;· TipoPar=2 (propio↔externo) | 1 978 |
| **`CoberturaSimilitudEmbedding`** (Voyage) — total | **15 107** |
| &nbsp;&nbsp;· TipoPar=1 (propio↔propio) | 3 654 |
| &nbsp;&nbsp;· TipoPar=2 (propio↔externo) | 11 453 |

> Nota: propios y externos tienen exactamente la misma cantidad de firmas que de embeddings
> (106 y 163) → la migración de datos está completa; ambos motores tienen cobertura equivalente,
> pero el de embeddings genera ~6× más pares (coseno denso sin pre-filtros de riqueza/compartidos).

### Vocabulario de la firma (aclaración "207 vs 87")
| Métrica | Valor |
|---|---:|
| **Vocabulario EFECTIVO de la firma** (`Activo=1 AND MedicalRelationSuggestedId=Directa`, **todos los tipos**) | **207** |
| &nbsp;&nbsp;· TipoTermino=1 (Síntoma, Activo=1) | 195 (del total; solo los Directa entran) |
| &nbsp;&nbsp;· TipoTermino=2 (Tratamiento, Activo=1) | 10 033 (del total; solo los Directa entran) |
| &nbsp;&nbsp;· **TipoTermino=3 (ConceptoGeneralEII, Activo=1)** | **87** (todos con MedicalRelationSuggestedId=Directa) |

> **Aclaración:** el requerimiento hablaba de "207 términos TipoTermino=3 ConceptoGeneralEII".
> En realidad: **207** es el vocabulario efectivo COMPLETO de la firma (`Directa` + `Activo`,
> todos los tipos, tabla `GlossaryTerm`). Los **ConceptoGeneralEII** (TipoTermino=3) son **87**.
> Fuente del vocabulario de firma: `FirmaService.cs:113-121` filtra por `Activo` +
> `MedicalRelationSuggestedId==Directa` sin condicionar `TipoTermino`.

---

## 6. Config de keys + panel admin de API keys

### Claves de configuración (nombres EXACTOS)
| Clave de config | Proyecto | Uso | ¿Jubilar? |
|---|---|---|---|
| **`Anthropic:ApiKey`** (sección `Anthropic`) | **NINA-WorkerService** (`AnthropicTranslationService.cs:50`; valor en `appsettings.json:21`) | **Traducción EN→ES del Worker** | **SÍ (a standby)** |
| `AiAnswer:AnthropicApiKey` (sección `AiAnswer`) | eiibd26 Web (`appsettings.json:90`, `Prod:26`) | **NINA + GRIS** (comparten HttpClient `"AnthropicClient"`) | **NO TOCAR** |
| `Gris:Model` (`claude-sonnet-4-6`) | eiibd26 Web | Solo modelo de GRIS (sin key propia) | — |
| `AiAnswer:Model` (`claude-haiku-4-5-20251001`) | eiibd26 Web | Modelo de NINA | — |
| `Anthropic:Model` (`claude-haiku-4-5-20251001`) | Worker | Modelo de la traducción | (acompaña a la anterior) |
| **`Voyage:ApiKey`** (sección `Voyage`) | eiibd26 Web (`appsettings.json:145`, **vacía**) + Worker (`Program.cs:24` la lee, sección **ausente** en su appsettings) | **Embeddings Voyage** (motor nuevo) | **Añadir al panel** |

**Confirmado:** la key de traducción del Worker (`Anthropic:ApiKey`) y la de NINA/GRIS
(`AiAnswer:AnthropicApiKey`) son **literales de config distintos, en proyectos y appsettings
distintos**. NINA y GRIS **comparten** físicamente la misma key vía el HttpClient
`"AnthropicClient"` (`Program.cs:334-376`; consumidores: `AiAnswerService.cs:27`,
`NinaModelRouterService.cs:50`, `GrisEvaluadorService.cs:43`, `SintomasTratamientosAiService.cs:27`).

### Panel admin de API keys
- Vista/code-behind: `Areas/Identity/Pages/Admin/ApiKeys/Index.cshtml(.cs)`,
  `[Authorize(Roles="Administrador")]`.
- Lee la **config efectiva** (`IConfiguration[configPath]`, `Index.cshtml.cs:70`), solo lectura;
  enmascara a `prefijo + ••••••••••` (`:86-87`); estado booleano `Configurada` (`:71`).
- Filas actuales (`OnGet` `:38-62`): Anthropic (NINA) y (GRIS) → ambos `AiAnswer:AnthropicApiKey`;
  SendGrid, Twilio (SID/Token), Google Maps, VAPID (pub/priv).
- **No lista Voyage** ni la key del Worker.
- **Para Fase 7 (Voyage al panel):** añadir en `OnGet` una fila
  `Rows.Add(Build("Voyage (embeddings)", "Voyage:ApiKey", prefixLen: 6));` — no requiere tocar
  la vista `.cshtml` (itera `Model.Rows` genéricamente).

### Dónde vive HOY la Voyage key
| Ubicación | Estado |
|---|---|
| `eiibd26/appsettings.json:144-151` (`Voyage:ApiKey`) | declarada **vacía** |
| `eiibd26/appsettings.Production.json` | sección **ausente** |
| `NINA-WorkerService/appsettings.json` | sección **ausente** (aunque `Program.cs:24` la lee) |
→ En runtime la key efectiva proviene de **user-secrets / variable de entorno**.

---

## 7. Hallazgos / riesgos (no se corrige nada en esta fase)

1. **La librería `eiibd26.Firma` NO se puede borrar** al jubilar la firma: `EmbeddingService`
   (`StripHtml`) y ambos servicios de similitud (`Normalizar`) del motor nuevo dependen de ella.
   Standby ≠ eliminar la lib.
2. **Sin cron:** la firma no se dispara sola. "Apagar" = quitar/ocultar botones admin (`Firmas`,
   `Similitud`) y opcionalmente dejar de registrar `FirmaContenidoJob`/`SimilitudJob` en DI. Los
   datos viejos (`CoberturaSimilitud`, columnas `Firma`) se pueden **conservar** intactos.
3. **Panel admin de Cobertura sigue en conteo:** `ObtenerCoberturaTemasAsync` (matriz de huecos,
   `CoberturaVistaService.cs:167`) lee la tabla vieja. Si se apaga la firma sin migrar este panel,
   la matriz de cobertura admin quedaría congelada. Decidir en el diseño: migrar el panel a
   embeddings o mantener la firma **solo** para este panel admin.
4. **Traducción Worker:** apagar `Anthropic:ApiKey` del Worker deja de traducir externos EN→ES,
   pero el motor nuevo (embeddings) es **multilingüe y no traduce** → los externos en inglés
   siguen cubiertos por embeddings. La firma de externos en inglés dejaría de recalcularse (dato
   viejo intacto). Confirmado que NO afecta NINA/GRIS.
5. **⚠️ Seguridad (fuera de scope de Fase 7, reportado para acción aparte):**
   `eiibd26/appsettings.Production.json` y `NINA-WorkerService/appsettings.json` contienen
   **secretos reales en texto plano y commiteados** (Anthropic key `sk-ant-…`, SendGrid, Twilio,
   VAPID, credenciales SQL). No se tocó en esta fase.

---

## 8. Superficie de jubilación (mapa para el diseño de la Fase 7)

**A apagar (dejar de calcular / ocultar), datos conservados en standby:**
- Botones admin: `Firmas.cshtml(.cs)`, `Similitud.cshtml(.cs)`.
- Jobs viejos (DI `Program.cs:383-384,387-388`): `FirmaContenidoJob`, `SimilitudJob`,
  `IFirmaService`/`FirmaService`, `ISimilitudService`/`SimilitudService`.
- Worker: rama de traducción+firma (`Worker.cs:226,375-398`) y `AnthropicTranslationService` +
  clave `Anthropic:ApiKey`.

**A decidir en el diseño:**
- Panel admin `Cobertura` (matriz de huecos) → ¿migrar a embeddings o mantener firma solo aquí?
- `FirmasExternas.cshtml` → panel de estado de firma de externos (¿retirar o dejar informativo?).

**A conservar SIEMPRE (no tocar):**
- Librería `eiibd26.Firma` (la usa el motor nuevo).
- Datos: `contenidos.Firma`, `ScrapedPage.Firma`, tabla `CoberturaSimilitud`, `GlossaryTerm`.
- NINA/GRIS: `AiAnswer:AnthropicApiKey` y el HttpClient `"AnthropicClient"`.

**A añadir:**
- Fila `Voyage:ApiKey` en el panel admin de API keys.

---

*Fin del inventario. Cero cambios de código o datos en esta fase.*
