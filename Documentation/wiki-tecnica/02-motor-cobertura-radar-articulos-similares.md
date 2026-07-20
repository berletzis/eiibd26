# Motor de Cobertura / Radar de Contenido / Artículos Similares

> Wiki técnica interna — no publicar. Incluye los umbrales exactos y su desacople editor vs paciente.

## Qué problema resuelve

El Motor de Cobertura traduce un número crudo de similitud (el coseno de embeddings del artículo 01, o el coseno de firma del artículo 03) en **estados de negocio** que dos audiencias distintas consumen:

1. **El paciente**, en la vista del artículo, ve bloques de "Artículos Similares" (subtema fino), "Explora el tema" (mismo tema) y "En otros sitios" (externos relacionados).
2. **El editor/admin**, en el panel de cobertura y la vista de Oportunidades, ve una "matriz de huecos": qué temas externos ya están cubiertos por un artículo propio, cuáles están débilmente cubiertos y cuáles son huecos.

La pregunta editorial que responde: *"¿la plataforma ya cubre este tema, y qué tan bien frente a lo que hay afuera?"* — para decidir qué crear, detectar huecos y evitar duplicados.

## Cómo funciona por dentro

El motor **no recalcula** similitud en tiempo de vista: lee la tabla de pares ya calculada (`CoberturaSimilitudesEmbedding` para embeddings, `CoberturaSimilitudes` para firma) y aplica **umbrales de banda** sobre el `Score` guardado.

### Vista paciente — tres bandas sobre el mismo motor de embeddings

Dado un `contenidoId`, la consulta filtra los pares propio→externo por banda de score según la pestaña:

| Pestaña (`tab`) | Banda de score | Significado |
|---|---|---|
| `similares` | `Score ≥ 0.78` | subtema fino (casi el mismo artículo) |
| `area` | `0.55 ≤ Score < 0.78` | mismo tema / área |
| `externos` | `Score ≥ 0.55` | todos los externos relacionados (lista única del sidebar) |

Para artículos propios similares (`ObtenerSimilaresPropiosAsync`), se toman los pares propio↔propio que tocan el artículo, se ordena por score descendente y se devuelven los top N (default 6). Solo se consideran artículos "reales" (los que cuelgan del árbol de la categoría raíz "General", `Sequence = 1`, recorrido por BFS).

Paginación eficiente: se pide `take + 1` filas para saber si hay más, sin un `COUNT` aparte.

### Vista admin — matriz de huecos con umbrales editoriales

`ObtenerCoberturaTemasAsync` recorre el universo de temas externos indexados por el motor activo y, para cada externo, busca su **mejor artículo propio** (el par de mayor score por encima del piso). El estado se decide así:

```
Estado =
    "Hueco"     si no hay ningún artículo propio por encima del piso
    "Cubierto"  si el mejor score ≥ umbral_cubierto
    "Débil"     si piso ≤ mejor score < umbral_cubierto
```

Los umbrales dependen del motor elegido (embeddings por defecto; `?motor=firma` como fallback en vivo), y **no son comparables 1:1 entre escalas**:

| Motor | Piso (hueco por debajo) | Cubierto (≥) |
|---|---|---|
| Embeddings (editorial) | 0.55 | 0.78 |
| Firma (editorial) | 0.50 | 0.60 |

### Desacople editor vs paciente (clave)

Los umbrales del paciente (`UmbralArea = 0.55`, `UmbralSimilares = 0.78`) y los umbrales editoriales (`CobEmbFloorEditorial = 0.55`, `CobEmbCubiertoEditorial = 0.78`) **arrancan con los mismos valores pero son constantes independientes**. El comentario en el código lo dice explícitamente: los editoriales "ya NO aliasan `UmbralArea`/`UmbralSimilares`, así se pueden tunear sin afectar lo que ve el paciente". Es una decisión de diseño para poder calibrar el panel de admin sin mover la experiencia pública.

## Parámetros y umbrales (valores reales)

| Constante | Valor | Uso |
|---|---|---|
| `UmbralSimilares` | 0.78m | paciente: "Artículos Similares" |
| `UmbralArea` | 0.55m | paciente: "Explora el tema" / externos |
| `CobEmbCubiertoEditorial` | 0.78m | admin embeddings: cubierto |
| `CobEmbFloorEditorial` | 0.55m | admin embeddings: piso/hueco |
| `CobFirCubierto` | 0.60m | admin firma: cubierto |
| `CobFirFloor` | 0.50m | admin firma: piso (= CosenoMin de firma) |
| `CategoriaGeneral` | 1 | raíz del árbol "artículo real" |
| TTL caché de categorías-artículo | 10 min | `CacheTtl` |

## Dónde vive

- Vista y umbrales: `eiibd26/Services/Cobertura/CoberturaVistaService.cs` — constantes en `:20`–`:33`; vista paciente `ObtenerSimilaresAsync` en `:97` (bandas por pestaña `:108`); similares propios `:146`; matriz de huecos admin `ObtenerCoberturaTemasAsync` en `:174` (regla de estado `:222`).
- Árbol de categorías "artículo real" (BFS desde General): `:48`–`:93`.
- Datos de pares: producidos por `SimilitudEmbeddingService` (art. 01) y `SimilitudService` (art. 03).

## Cómo explicarlo en una presentación

El motor toma el puntaje de parecido entre dos textos (un número de 0 a 1) y lo convierte en un semáforo. Para el lector: si el parecido es muy alto (≥ 0.78) le mostramos "artículos casi idénticos"; si es medio (0.55–0.78) le mostramos "más sobre este tema". Para el editor: le decimos si cada tema que existe allá afuera ya está *cubierto* por un artículo nuestro, está *flojo*, o es un *hueco* que deberíamos escribir.

Analogía: es como un mapa de calor de nuestra biblioteca contra todo lo que se publica en internet sobre EII. Verde = ya lo tenemos bien; amarillo = lo tocamos de refilón; rojo = nadie lo cubre, oportunidad de contenido. Detalle importante: el termostato del lector y el del editor son perillas separadas — podemos ajustar el panel interno sin cambiarle la experiencia al paciente.

## Limitaciones y supuestos

- Los umbrales son **provisionales** y están marcados "a calibrar con datos reales". El 0.78 / 0.55 no salen de una validación formal.
- Las escalas de firma y embeddings **no son intercambiables**: 0.60 en firma no equivale a 0.60 en embeddings. Comparar motores requiere leer cada umbral en su propia escala.
- "Artículo real" depende de que la taxonomía cuelgue correctamente de la categoría General; una categoría mal enraizada excluye o incluye artículos por error.
- La vista depende de que las corridas de similitud estén frescas; si no se recalcularon, los estados reflejan datos viejos.
- El título del externo se **deriva del slug de la URL** (no hay título real scrapeado), así que puede ser impreciso.
