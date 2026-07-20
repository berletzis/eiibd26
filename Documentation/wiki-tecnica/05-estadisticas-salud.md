# Estadísticas de salud (Mood, Síntomas, Tendencias e Insights)

> Wiki técnica interna — no publicar. Incluye la regresión OLS exacta y los umbrales de tendencia.

## Qué problema resuelve

El paciente registra a diario su estado de ánimo (mood) y sus síntomas. La plataforma necesita convertir esos registros crudos en **resúmenes legibles** para el dashboard y el PDF de resumen médico: promedio de ánimo, tendencia de cada síntoma (mejorando / estable / empeorando), síntoma más frecuente y perfil de dolor. Estos servicios calculan todo eso **en memoria**, a partir de listas ya cargadas, sin tocar la base de datos (para poder reutilizarse en dashboard y PDF).

## Cómo funciona por dentro

### 1. Escala de severidad de síntomas

El campo `Estado` (string) se mapea a un valor numérico ordinal:

| Estado | Valor |
|---|---|
| Ninguno | 0 |
| Leve | 1 |
| Moderado | 2 |
| Severo | 3 |
| Extremo | 4 |

Un estado desconocido devuelve −1 y se descarta del cálculo.

### 2. Promedio de estado de ánimo (mood)

El mood se registra en una escala 1–5 (`EstadoAnimoEnum`). El promedio numérico se mapea a texto por bandas:

```
promedio ≤ 1.5 → "Muy malo"
promedio ≤ 2.5 → "Malo"
promedio ≤ 3.5 → "Regular"
promedio ≤ 4.5 → "Bueno"
promedio  > 4.5 → "Muy bueno"
```

### 3. Tendencia de un síntoma — regresión lineal por mínimos cuadrados (OLS)

Para cada síntoma se ordenan sus registros por fecha, se convierten a valores 0–4 y se ajusta una recta por **mínimos cuadrados ordinarios** usando el **índice de posición** `i = 0,1,2,…` como eje X (no la fecha real). Con `n` puntos, se usan las fórmulas cerradas de las sumas de una secuencia `0..n−1`:

```
sumX  = n(n−1)/2
sumX2 = (n−1)·n·(2n−1)/6
sumY  = Σ yᵢ
sumXY = Σ i·yᵢ

pendiente (β) = (n·sumXY − sumX·sumY) / (n·sumX2 − sumX²)
```

La **pendiente** se interpreta con un umbral de ±0.05:

```
β >  0.05 → "Empeorando"   (la severidad sube en el tiempo)
β < −0.05 → "Mejorando"    (baja)
en otro caso → "Estable"
```

Con menos de 2 puntos reconocidos → "Sin datos suficientes".

**Tendencia global (peor caso):** si algún síntoma está "Empeorando" → global "Empeorando"; si no, si alguno está "Mejorando" → "Mejorando"; si todos "Sin datos" → "Sin datos suficientes"; en otro caso "Estable". Las series de síntomas distintos **no se mezclan** — cada uno tiene su propia regresión.

### 4. Insights clínicos (`HealthInsightService`)

A partir de la lista de síntomas con sus trackings calcula:

- **Síntoma más frecuente:** el de mayor conteo de registros.
- **Promedio global de dolor:** media de todos los trackings con `Dolor > 0`.
- **Registros de dolor alto:** cuántos trackings tienen `Dolor ≥ 7` (`UmbralDolorAlto = 7`; escala de dolor 0–10).
- **Síntoma con mayor dolor:** el de mayor promedio de dolor (redondeado a 1 decimal).
- **Síntoma con mayor frecuencia registrada:** el síntoma que más veces registró una frecuencia del catálogo, y cuál es su frecuencia más común (moda).

### 5. Persistencia del tracking (`TrackingSintomaService`)

Regla de consistencia al guardar: solo los síntomas **medibles** (`TipoSintoma = 1`) admiten frecuencia y sangrado; para los demás esos campos se fuerzan a null. El dolor se acepta para todos los tipos. Es **idempotente por día**: si ya existe un tracking del mismo síntoma/usuario/fecha (comparando por `Fecha.Date`), se actualiza en vez de duplicar.

## Parámetros y umbrales (valores reales)

| Parámetro | Valor | Dónde |
|---|---|---|
| Escala severidad | Ninguno 0 … Extremo 4 | `HealthStatsService:13` |
| Bandas de mood | 1.5 / 2.5 / 3.5 / 4.5 | `:50` |
| Umbral de pendiente | ±0.05 | `HealthStatsService:120` |
| Mínimo de puntos para tendencia | 2 | `:108` |
| Umbral de dolor alto | ≥ 7 (escala 0–10) | `HealthInsightService:12` |
| Tipo de síntoma medible | 1 (admite frecuencia/sangrado) | `TrackingSintomaService:26` |

## Dónde vive

- Estadísticas base: `eiibd26/Services/Analytics/HealthStatsService.cs` — mood `:35`, regresión OLS `CalcularTendenciaIndividual` en `:100`, escala `:13`.
- Insights: `eiibd26/Services/Analytics/HealthInsightService.cs:14` (umbral dolor `:12`).
- Persistencia idempotente: `eiibd26/Services/Tracking/TrackingSintomaService.cs:16`.

## Cómo explicarlo en una presentación

Convertimos el diario del paciente en tres cosas: un ánimo promedio ("¿cómo estuvo esta semana?"), una tendencia por síntoma ("¿el dolor va mejorando o empeorando?") y un par de datos destacados ("tu síntoma más frecuente fue X; tu dolor promedio fue Y"). Para la tendencia no basta con comparar el primero y el último día: trazamos la **recta que mejor pasa por todos los puntos** y miramos si sube o baja. Si sube más de un pelito, decimos "empeorando"; si baja, "mejorando"; si es casi plana, "estable".

Analogía: es la misma línea de tendencia que Excel dibuja sobre una nube de puntos. Nosotros calculamos su inclinación (la pendiente) y la traducimos a una palabra que el paciente y su médico entienden de un vistazo. Para el resumen global usamos la regla del "peor caso": si cualquier síntoma empeora, encendemos la alerta, porque en salud conviene pecar de precavido.

## Limitaciones y supuestos

- El eje X es el **índice de posición**, no el tiempo real: registros con espaciado irregular pesan igual. Una recaída tras meses cuenta como "el siguiente punto", no como un salto temporal.
- La escala de severidad es **ordinal tratada como numérica**: se asume que la distancia Leve→Moderado equivale a Moderado→Severo, lo cual es una aproximación.
- El umbral ±0.05 es una heurística sin validación clínica; con pocos puntos, una sola variación mueve la etiqueta.
- No hay pruebas de significancia estadística (ni p-valores ni intervalos): la pendiente se usa cruda.
- Los servicios trabajan sobre listas ya filtradas por el llamador; la calidad del insight depende de qué registros se pasen.
- Tema sensible: estos números son orientativos y educativos, no un diagnóstico ni una medición clínica validada.
