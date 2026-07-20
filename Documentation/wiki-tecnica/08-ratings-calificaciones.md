# Ratings y calificaciones (artículos, glosario, platillos)

> Wiki técnica interna — no publicar. Documenta cómo se agregan los votos.

## Qué problema resuelve

Los usuarios pueden calificar tres tipos de contenido — artículos, términos del glosario y platillos — para indicar si les resultó útil. El sistema necesita registrar un voto por usuario (idempotente, cambiable) y agregar los votos en contadores simples de "me gusta / no me gusta".

## Cómo funciona por dentro

Los tres sistemas usan el **mismo patrón: un voto binario like/dislike por usuario**, no una escala de estrellas ni un promedio ponderado.

### Artículos y glosario

Usan el enum `RatingType` (`Like` / `Dislike`). La agregación es un conteo directo:

```
likes    = ratings.Count(r => r.RatingType == Like)
dislikes = ratings.Count(r => r.RatingType == Dislike)
```

Cada usuario tiene a lo sumo un voto por contenido; volver a votar actualiza el registro existente. No se calcula un promedio ni un score compuesto: la vista muestra los dos contadores.

### Platillos (`PlatCalificacionesApiController`)

Mismo modelo, pero el voto se guarda como valor numérico corto:

- `ValorUtil = 1` (like)
- `ValorNoUtil = -1` (dislike)

La agregación cuenta cuántos votos hay de cada valor:

```
likes    = votos.Count(v => v.Valor == 1)
dislikes = votos.Count(v => v.Valor == -1)
```

El voto es idempotente por usuario: si ya existe una calificación del usuario para ese platillo, se sobrescribe su `Valor`; si no, se inserta.

No existe (en el código revisado) una fórmula de "puntuación neta", ranking bayesiano de ratings ni ponderación por antigüedad. La agregación es puramente `count(like)` y `count(dislike)`.

## Parámetros y valores reales

| Sistema | Modelo de voto | Agregación | Dónde |
|---|---|---|---|
| Artículos | enum `RatingType` Like/Dislike | count por tipo | `ArticleRatingsApiController.cs:47` |
| Glosario | enum `RatingType` Like/Dislike | count por tipo | `GlossaryRatingsApiController.cs:43` |
| Platillos | short `Valor` +1 / −1 | count por valor | `PlatCalificacionesApiController.cs:30`,`:66` |

## Dónde vive

- Artículos: `eiibd26/Controllers/ArticleRatingsApiController.cs:47` (conteo like/dislike; también `:193`).
- Glosario: `eiibd26/Controllers/GlossaryRatingsApiController.cs:43` (y `:158`).
- Platillos: `eiibd26/Controllers/PlatCalificacionesApiController.cs` — constantes `:30`, upsert de voto `:108`–`:135`, conteo `:66`/`:146`.

## Cómo explicarlo en una presentación

La calificación es un pulgar arriba o pulgar abajo, no un sistema de estrellas. Cada usuario deja un solo voto por artículo, término o platillo, y puede cambiarlo cuando quiera. Para mostrar el resultado simplemente contamos cuántos pulgares arriba y cuántos abajo hay. Es deliberadamente simple: sin promedios, sin fórmulas ocultas — un contador honesto de "a cuánta gente le sirvió".

## Limitaciones y supuestos

- No hay **normalización por volumen**: un contenido con 3 likes y 0 dislikes se ve "mejor" que uno con 200 likes y 5 dislikes, aunque el segundo tenga más respaldo. No se usa un promedio bayesiano ni intervalos de confianza (p. ej. Wilson).
- Al ser binario, no captura intensidad ("me encantó" vs "estuvo bien").
- Un solo voto por usuario asume identidad estable; no hay defensa explícita contra votos coordinados más allá de la unicidad por usuario.
- Los tres subsistemas replican la misma lógica en controladores separados (deuda de duplicación); no hay un servicio de rating unificado.
