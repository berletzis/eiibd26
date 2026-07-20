# Embeddings y similitud semántica (Voyage AI)

> Wiki técnica interna — no publicar. Contiene proveedor, modelo, umbrales y la matemática exacta.

## Qué problema resuelve

El Motor de Cobertura necesita medir **qué tan parecidos son dos textos por su significado**, no por las palabras que comparten. La primera generación del motor usaba una "firma por conteo de vocabulario" (ver artículo 03), que solo detectaba coincidencias léxicas dentro de un vocabulario EII cerrado. Dos artículos que hablan del mismo tema con palabras distintas quedaban invisibles entre sí.

La solución es representar cada texto (artículo propio o página externa scrapeada) como un **vector denso de embeddings** generado por un modelo multilingüe de Voyage AI, y comparar esos vectores con **similitud coseno**. Esto captura similitud semántica real y funciona entre español y otros idiomas.

## Cómo funciona por dentro

### 1. Generación del embedding (proveedor y modelo)

- **Proveedor:** Voyage AI, endpoint `POST https://api.voyageai.com/v1/embeddings`.
- **Modelo:** `voyage-4-large` (valor por defecto en `VoyageOptions.Model`).
- **Dimensiones de salida:** `output_dimension` = `null` → default del modelo = **1024 floats** por vector. El código solo envía `output_dimension` si se configura explícitamente.
- **input_type:** `"document"` para **todos** los textos (propios y externos). Esto produce una similitud **simétrica** artículo↔artículo. No se usa `"query"` para ninguno — decisión validada por un experimento donde un par "gemelo" alcanzó 0.90 de coseno.
- **Autenticación:** `Authorization: Bearer <ApiKey>`, con la key leída de user-secrets / variable de entorno. Si la key es vacía o el centinela `SET_IN_ENVIRONMENT_OR_SECRETS`, el cliente queda deshabilitado y no embebe.
- **Resiliencia:** `HttpClient` estático reutilizable; timeout de 60 s por request vía `CancellationTokenSource` enlazado; hasta **5 reintentos** en 429/5xx con backoff exponencial base 2 s (respeta `Retry-After` si el servidor lo envía). Otros 4xx fallan directo.

### 2. Qué texto se embebe

El texto de entrada es **título + cuerpo** (`"{titulo}. {cuerpo}"`), con:

- HTML removido (`FirmaCalculator.StripHtml`) y entidades decodificadas.
- Espacios colapsados a uno.
- **Sin normalizar** — se preservan acentos, mayúsculas y signos, porque el modelo multilingüe usa el significado natural del texto (a diferencia de la firma vieja, que sí normalizaba).
- Truncado a **32 000 caracteres** (`MaxChars`; `voyage-4-large` admite 32K tokens, así que es holgado).

Los contenidos sin texto embebible (páginas institucionales) se marcan con vector vacío `"[]"` y modelo `"(sin-texto)"` para que la corrida "complete" y la similitud los salte.

Procesamiento por lotes: **16 contenidos por request** (`BatchSize`), con pausa de **200 ms** entre lotes (`PaceMs`). El vector se serializa como JSON de floats en la columna `Contenidos.Embedding`, junto con `EmbeddingModelo` (para detectar vectores obsoletos si cambia el modelo) y `EmbeddingCalculadoEn`.

### 3. Similitud coseno

Dado dos vectores densos **a** y **b** de la misma dimensión, la similitud es el coseno del ángulo entre ellos:

```
                Σ (aₖ · bₖ)
cos(a, b) = ───────────────────
              ‖a‖ · ‖b‖
```

donde `‖a‖ = √(Σ aₖ²)` es la magnitud (norma euclidiana), **precalculada** al cargar cada vector en memoria. En el código:

```csharp
double dot = 0;
for (int k = 0; k < va.Length; k++) dot += (double)va[k] * vb[k];
return dot / (a.Mag * b.Mag);
```

Reglas de comparabilidad: si alguna magnitud es 0 (vector cero) o las dimensiones difieren (modelos distintos), el coseno es 0 (no comparable). **No hay pre-filtros** de solapamiento léxico — a diferencia de la firma vieja, el coseno denso se calcula directo.

### 4. Qué pares se calculan

En cada corrida se comparan:

- **propio → externo** (`TipoPar = PropioExterno = 2`): cada artículo propio contra cada página externa scrapeada.
- **propio → propio** (`TipoPar = PropioPropio`): triángulo superior `j > i`, con el Id menor como A (evita duplicar A-B/B-A y auto-pares).

Total estimado de pares = `propios × externos + propios × (propios − 1) / 2`.

Deduplicación propio-propio: si dos artículos tienen el **mismo título normalizado** (`FirmaCalculator.Normalizar`), el par se salta (son duplicados).

Corrida **incremental**: si un par ya existe y ninguno de los dos embeddings cambió (`AEmbEn`/`BEmbEn` iguales), no se recalcula. Si cambió, se borra la fila y se re-evalúa.

## Parámetros y umbrales (valores reales)

| Parámetro | Valor | Dónde |
|---|---|---|
| Modelo | `voyage-4-large` | `VoyageOptions.Model` |
| Dimensión | 1024 (default del modelo) | `VoyageOptions.OutputDimension = null` |
| input_type | `document` (ambos lados) | `VoyageOptions.InputType` |
| Timeout | 60 s | `VoyageOptions.TimeoutSeconds` |
| Reintentos | 5, backoff base 2 s | `VoyageOptions.MaxRetries` / `RetryBaseSeconds` |
| Lote de embedding | 16 contenidos/request | `EmbeddingService.BatchSize` |
| Pausa entre lotes | 200 ms | `EmbeddingService.PaceMs` |
| Máx. caracteres | 32 000 | `EmbeddingService.MaxChars` |
| **Umbral de guardado** | **coseno > 0.40** | `SimilitudEmbeddingService.CosenoMin` |
| Precisión guardada | 5 decimales | `Math.Round(cos, 5)` |
| Flush a BD | cada 1000 filas | `FlushSize` |

El umbral de guardado **0.40** es un piso deliberadamente bajo y provisional: se guardan pares por debajo de los umbrales de vista (0.55 / 0.78) para conservar datos de calibración. Bajarlo aún más requiere "Recalcular total" (el incremental no re-crea pares por debajo del piso). Los umbrales que decide qué ve el usuario están en el artículo 02.

## Dónde vive

- Cliente Voyage: `eiibd26.Voyage/VoyageEmbeddingClient.cs:38` (método `EmbedAsync`); backoff en `:129`.
- Configuración: `eiibd26.Voyage/VoyageOptions.cs:8` (modelo `:17`, input_type `:26`, dimensión `:29`).
- Generación de embeddings de contenidos: `eiibd26/Services/Cobertura/EmbeddingService.cs:54` (`EmbedPendientesAsync`); construcción del texto `:161`.
- Cálculo de similitud coseno y corrida de pares: `eiibd26/Services/Cobertura/SimilitudEmbeddingService.cs:83` (`CalcularAsync`); función `Coseno` en `:299`; umbral `CosenoMin` en `:48`.

## Cómo explicarlo en una presentación

Cada artículo se convierte en una lista de 1024 números — una especie de "huella de significado" — usando un modelo de inteligencia artificial de Voyage entrenado en muchos idiomas. Para saber si dos artículos hablan de lo mismo, medimos el ángulo entre sus dos huellas: si apuntan casi en la misma dirección, tratan el mismo tema, aunque usen palabras completamente distintas.

Analogía: imaginá que a cada texto le asignás una coordenada en un mapa gigante de "temas de salud". Artículos sobre brotes de Crohn caen en la misma zona del mapa; uno sobre nutrición cae en otra. La similitud coseno es, en esencia, medir qué tan cerca están dos puntos en ese mapa. Lo potente frente al método anterior: el modelo entiende sinónimos y paráfrasis, no solo palabras exactas de una lista.

## Limitaciones y supuestos

- Los umbrales de vista (0.55 / 0.78) son **provisionales**, marcados en el código como "a calibrar con datos reales". No provienen de una validación estadística formal, sino de un experimento inicial.
- La comparación exige **misma dimensión y mismo modelo**; si se cambia de modelo, todos los vectores viejos quedan no-comparables (por eso se persiste `EmbeddingModelo`).
- El texto se trunca a 32 000 caracteres: artículos muy largos pierden su cola.
- La corrida es O(propios × externos) — cuadrática en el peor caso; mitigada por el flush por lotes y el modo incremental, pero no escala indefinidamente sin particionado.
- Depende de un proveedor externo (Voyage) y de su disponibilidad; sin API key, no se embebe nada y el motor cae al método de firma.
