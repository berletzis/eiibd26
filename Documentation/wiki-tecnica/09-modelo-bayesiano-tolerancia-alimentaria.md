# Modelo bayesiano de tolerancia alimentaria (DISEÑO — no construido)

> Wiki técnica interna — no publicar.
> **Estado: NO IMPLEMENTADO.** Este artículo documenta un módulo *previsto*, no código existente. La fórmula bayesiana concreta (priors, actualización) **no está en el repositorio**; lo que sigue distingue explícitamente lo confirmado en los documentos de diseño de lo que sería la formulación estándar prevista.

## Qué problema resolvería

El objetivo es un consenso comunitario del tipo:

> "**X % de los pacientes con [tipo de EII] toleran [alimento]**"

aprendido de los registros reales de la comunidad (tolerancia **inferida**), en contraste con el módulo de Platillos ya existente, donde la tolerancia es **declarada** por el propio paciente (sus exclusiones) y por el editor (atributos del platillo), sin inferencia estadística.

## Qué está confirmado en los documentos (y qué NO)

Lo único que el repositorio dice sobre este modelo, de forma explícita, es que es un **módulo aparte e independiente, aún no construido**:

- `Documentation/REQ-modulo-platillos.md:238` — *"Tolerancia aprendida / modelo bayesiano → módulo aparte e independiente (tarea #16). Aquí la tolerancia es declarada, no inferida."* (sección "Fuera de alcance").
- `Documentation/REQ-platillos-F4-vista-ingrediente.md:83` — *"El modelo bayesiano y el registro de tolerancia → módulo #16, independiente."*
- `Documentation/RESUMEN-sesion-para-articulos.md:56` — *"Módulo de Tolerancia alimentaria (bayesiano) — diseñado, no construido (tarea aparte)."*

**No existe** en el repositorio un documento de diseño con priors numéricos, la regla de actualización, ni código (`Services/`, `Models/`) que implemente este modelo. Todo lo de la sección siguiente es la **formulación estándar prevista** para un consenso de proporción como este, señalada como tal — no debe citarse como "lo que hace el sistema", porque el sistema todavía no lo hace.

## Cómo funcionaría por dentro (formulación estándar prevista — Beta-Binomial)

El problema —estimar la proporción de pacientes que toleran un alimento— es el caso canónico de un **modelo Beta-Binomial**, que es casi con certeza lo que "modelo bayesiano" designa aquí.

### Planteo

Para cada par **(tipo de EII, alimento)** se estima una probabilidad de tolerancia `θ ∈ [0,1]`. Cada registro de un paciente es un ensayo Bernoulli: tolera (éxito) o no tolera (fracaso).

**Prior** (creencia previa, antes de ver datos): una distribución Beta

```
θ ~ Beta(α₀, β₀)
```

Un prior neutro (sin información) típico es `Beta(1, 1)` (uniforme). Un prior levemente optimista/pesimista se codifica con otros `α₀, β₀` (por ejemplo `Beta(2, 2)`, que "ancla" hacia 0.5 y regulariza cuando hay pocos datos).

**Actualización bayesiana** (conjugación Beta-Binomial): con `s` pacientes que toleran y `f` que no, el **posterior** es cerrado:

```
θ | datos ~ Beta(α₀ + s, β₀ + f)
```

**Estimador puntual** (el "X %" que se muestra) = media del posterior:

```
              α₀ + s
X % = E[θ] = ───────────────
             α₀ + β₀ + s + f
```

Esto es equivalente a un porcentaje de "éxitos" **suavizado** por el prior: con `Beta(1,1)` es la regla de sucesión de Laplace `(s+1)/(s+f+2)`.

**Incertidumbre:** el intervalo creíble al 95 % se obtiene de los cuantiles 0.025 y 0.975 de `Beta(α₀+s, β₀+f)`. Sirve para **no mostrar un porcentaje** cuando hay muy pocos datos (intervalo demasiado ancho).

### Por qué bayesiano y no un simple `s/(s+f)`

Con 2 pacientes que toleran y 0 que no, un porcentaje crudo diría "100 % toleran", lo cual es engañoso. El prior evita esos extremos: `Beta(1,1)` daría `3/4 = 75 %`, y el intervalo creíble seguiría siendo ancho, señalando "aún no sabemos". A medida que llegan datos, el prior se diluye y el estimador converge al porcentaje real.

## Parámetros y umbrales

**No definidos en el repositorio.** Al construir el módulo habría que fijar (y documentar aquí con su `archivo:línea`):

- Los hiperparámetros del prior `α₀, β₀` (p. ej. `Beta(1,1)` o `Beta(2,2)`).
- El **mínimo de registros** para mostrar un porcentaje (o el ancho máximo de intervalo creíble tolerado).
- La definición operativa de "tolera" a partir de los registros del paciente (¿ausencia de síntoma tras el alimento? ¿declaración explícita?).
- La segmentación por tipo de EII (CUCI / Crohn) y posiblemente por subfenotipo.

## Dónde vive

- **No hay código.** Referencias de diseño (solo mencionan su existencia como módulo futuro): `Documentation/REQ-modulo-platillos.md:238`, `Documentation/REQ-platillos-F4-vista-ingrediente.md:83`, `Documentation/RESUMEN-sesion-para-articulos.md:56`.
- Módulo relacionado ya construido (tolerancia **declarada**, no inferida): `eiibd26/Services/Platillos/` y `eiibd26/Models/Platillos/PlatPerfilExclusion.cs`.

## Cómo explicarlo en una presentación

Queremos poder decir "el 70 % de los pacientes con Crohn toleran el arroz", aprendido de la experiencia real de la comunidad. El reto: al principio hay pocos datos, y si tres personas dicen que lo toleran, "100 %" sería mentira. La estadística bayesiana resuelve justo eso: arrancamos con una creencia prudente ("no sabemos, quizás 50-50") y la vamos corrigiendo con cada nuevo testimonio. Con pocos datos, el número se mantiene cauto y decimos "todavía no hay suficiente evidencia"; con muchos, se vuelve preciso y confiable.

Analogía: es como pronosticar si a un jugador le irá bien la temporada. Tras un solo partido bueno no proclamás "es el mejor de la liga"; combinás ese dato con lo que sabías de antemano y esperás más partidos antes de confiar en la cifra. Nuestro "prior" es esa cautela inicial, y cada paciente que reporta es un partido más.

**Importante para la presentación:** aclarar que este módulo está **diseñado pero aún no construido**. Hoy la plataforma maneja tolerancia *declarada* (el paciente marca lo que evita); el modelo bayesiano que *infiere* el consenso comunitario es trabajo futuro (tarea #16).

## Limitaciones y supuestos

- **No implementado:** cualquier cifra concreta de este artículo (α₀, β₀, fórmulas) es la propuesta estándar, no el comportamiento del sistema.
- El Beta-Binomial asume registros **independientes e idénticamente distribuidos** dentro de un (EII, alimento); en la práctica un mismo paciente puede aportar varios registros correlacionados, lo que habría que manejar (p. ej. un voto por paciente).
- Definir "tolerar" a partir de síntomas es no trivial y sesga el resultado.
- Sesgo de autoselección: quien reporta puede no representar a la población de pacientes.
- Requiere **volumen** de datos para ser útil, igual que el catálogo de platillos necesita volumen antes de salir al paciente.
- Es un dominio de salud sensible: un consenso comunitario es orientativo y no sustituye indicación médica ni nutricional individual.
