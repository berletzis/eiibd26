# Firma por conteo de vocabulario (método de 1ª generación)

> Wiki técnica interna — no publicar. Documenta el método **anterior** a los embeddings, que **sigue en el código** como fallback.

## Qué problema resuelve

Fue la primera manera de medir cuánto se parecen dos textos EII antes de migrar a embeddings (artículo 01). La firma resume un texto como un **vector disperso de conteos** de términos de un vocabulario EII curado, y compara vectores con similitud coseno. Sigue disponible como motor de respaldo (`?motor=firma`) y para páginas que solo tienen firma calculada.

## Cómo funciona por dentro

### 1. Construcción de la firma

Entrada: título + cuerpo (HTML o texto) + un vocabulario EII ya compilado. Pasos:

1. **Limpieza:** `StripHtml` (regex `<.*?>`) + `HtmlDecode`.
2. **Normalización** (`Normalizar`): minúsculas → descomposición Unicode `FormD` → se eliminan las marcas diacríticas (acentos) → recomposición `FormC` → se reemplaza todo lo que no sea `[a-z0-9\s]` por espacio → se colapsan espacios. Resultado: texto plano sin acentos ni signos.
3. **Conteo:** para cada término del vocabulario se cuenta cuántas veces aparece como **frase completa** (regex con límites de palabra `\b término \b`, precompilado). Términos multi-palabra como "colitis ulcerosa" se cuentan como frase. Solo se guardan los términos con conteo > 0 (representación **dispersa**).
4. **Serialización:** JSON `{ v, totalTokens, counts }` (`FirmaDto`), versión de formato `FirmaVersion = 1`. **El texto original se descarta**; solo se guarda la firma numérica (columna `Contenidos.Firma`, `NVARCHAR(MAX)`).

El vocabulario se compila una vez (`CompilarVocabulario`): normaliza cada nombre, elimina duplicados y precompila su regex de frase, preservando el orden.

### 2. Similitud coseno sobre vectores dispersos

La similitud entre dos firmas **a** y **b** es el coseno de sus vectores de conteos:

```
              Σ_t (count_a[t] · count_b[t])
cos(a, b) = ──────────────────────────────────
                    ‖a‖ · ‖b‖
```

La magnitud `‖a‖ = √(Σ count²)` se precalcula al parsear. El producto punto solo itera el diccionario más pequeño y busca coincidencias en el grande (eficiente para vectores dispersos):

```csharp
foreach (var kv in small)
    if (large.TryGetValue(kv.Key, out var v2)) dot += (double)kv.Value * v2;
return dot / (a.Mag * b.Mag);
```

### 3. Compuertas de calidad (afinadas con datos reales)

A diferencia del coseno denso de embeddings, la firma añade **pre-filtros** para evitar falsos 1.0 de firmas pobres (dos textos que comparten un solo término):

- **Riqueza mínima** (`RiquezaMin = 4`): una firma con menos de 4 términos distintos se descarta como "no comparable".
- **Términos compartidos mínimos** (`TerminosCompartidosMin = 3`): un par que comparte menos de 3 términos no se calcula.
- **Umbral de guardado** (`CosenoMin = 0.50`): solo se guardan pares con coseno > 0.50.

El coseno en sí no se altera; las compuertas solo deciden qué pares entran al cálculo y cuáles se persisten. La misma estructura de corrida (incremental, dedup por título normalizado, triángulo superior propio-propio, flush cada 1000) que la versión de embeddings.

## Parámetros y umbrales (valores reales)

| Constante | Valor | Uso |
|---|---|---|
| `FirmaVersion` | 1 | versión del formato JSON |
| `RiquezaMin` | 4 | mín. términos distintos por firma |
| `TerminosCompartidosMin` | 3 | mín. términos compartidos por par |
| `CosenoMin` | 0.50 | umbral de guardado |
| Precisión guardada | 4 decimales | `Math.Round(cos, 4)` |
| `FlushSize` | 1000 | filas por flush a BD |

## Dónde vive

- Lógica pura de firma (compartida Web + Worker): `eiibd26.Firma/FirmaCalculator.cs` — `CompilarVocabulario` en `:38`, `Calcular` en `:60`, `Normalizar` en `:90`, `StripHtml` en `:83`.
- DTO de la firma: `eiibd26.Firma/FirmaDto.cs`; término compilado: `eiibd26.Firma/VocabularioTermino.cs`.
- Cálculo de similitud, compuertas y corrida: `eiibd26/Services/Cobertura/SimilitudService.cs` — constantes en `:47`–`:49`; `Coseno` en `:314`; `Compartidos` en `:306`; compuerta de riqueza en `:277`; pre-filtro de compartidos en `:144`.

## Cómo explicarlo en una presentación

Antes de la IA de embeddings, medíamos parecido contando palabras clave. Teníamos una lista fija de términos médicos de EII ("brote", "colitis ulcerosa", "biológico"…) y por cada artículo anotábamos cuántas veces aparecía cada uno. Dos artículos se parecen si repiten los mismos términos con frecuencias parecidas.

Analogía: es como describir un plato solo por su lista de ingredientes y cuánto lleva de cada uno. Funciona para comparar recetas parecidas, pero no entiende que "berenjena" y "aubergine" son lo mismo, ni capta el sabor: solo cuenta ingredientes de una despensa cerrada. Por eso migramos a embeddings, que sí entienden significado. Para no dar falsos positivos, la firma exige que dos textos compartan al menos 3 términos y tengan un vocabulario mínimo antes de declararlos parecidos.

## Limitaciones y supuestos

- **Solo léxico, vocabulario cerrado:** ignora sinónimos, paráfrasis e idiomas distintos. Un texto que no usa los términos exactos del vocabulario EII produce una firma pobre o vacía.
- Sensible a la calidad del vocabulario curado: términos faltantes = temas invisibles.
- Las compuertas (4 / 3 / 0.50) fueron "afinadas con datos reales" pero son heurísticas, no un umbral estadístico formal.
- La normalización quita acentos, lo que puede colapsar términos que deberían diferir.
- Superado por embeddings como motor principal; se mantiene como fallback y para páginas ya firmadas.
