# REQ — #16 Modelo bayesiano de tolerancia alimentaria (Beta-Binomial)

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**. Esto **cambia un cálculo que ve el paciente** — autorizado explícitamente por el usuario. Rollout seguro (ver §7): admin primero, público detrás de decisión.
**Autorización:** el usuario pidió "terminar #16" — esto habilita modificar el cálculo de tolerancia (regla del proyecto: no tocar cálculos sin autorización explícita → concedida).

## Punto de partida (ya existe, NO rehacer)
- Tabla `PlatTolerVoto`: `Tolera` (1=Sí, 2=A veces, 3=No), `UserId`/`AnonId` (un voto por identidad, dedup), `CondicionIdPrincipal` (cruda, fuente de verdad) + `TipoEII` (1=CUCI, 2=Crohn; null si anónimo/desconocido).
- Estimador actual en `Pages/Tolero/Encuesta.cshtml.cs:194`: Laplace `(s+1)/((s+f)+2)`, con `MinVotos=10`. **"A veces" fuera del binario.**
- **Clave conceptual:** ese Laplace YA es la media posterior de un `Beta(1,1)`. O sea no reescribimos, **sofisticamos**: agregamos intervalo creíble + segmentación por EII, centralizados en un servicio.

## §1 — Formulación (la matemática exacta)
Para cada par (segmento, ingrediente), con `s` = votos "Sí" y `f` = votos "No":

- **Prior:** `θ ~ Beta(α₀, β₀)`. **Default `Beta(1,1)`** (neutro, = el MVP; continuidad). Configurable.
- **Posterior (conjugado):** `θ | datos ~ Beta(α₀ + s, β₀ + f)`.
- **Estimador puntual (el "X %"):** media posterior `E[θ] = (α₀+s) / (α₀+β₀+s+f)`.
- **Incertidumbre:** intervalo creíble al 95% = cuantiles 0.025 y 0.975 de `Beta(α₀+s, β₀+f)`.

**"A veces":** se mantiene FUERA del binario (`s`=Sí, `f`=No), consistente con el MVP. Se muestra aparte como contexto. (Crédito parcial / modelo ordinal de 3 niveles = futuro, NO ahora.)

## §2 — Centralizar en un servicio (fuente única de la matemática)
Extraer un servicio puro `ToleranciaBayesService` (o similar) que NO toque BD:
- `Estimar(int s, int f, double a0=1, double b0=1)` → `{ MediaPct, CiBajoPct, CiAltoPct, N }`.
- Reusarlo en la encuesta pública Y en el panel admin (hoy el Laplace está inline en `Encuesta.cshtml.cs` — moverlo aquí).

## §3 — Cálculo del cuantil Beta (decisión de implementación)
C# no tiene `Beta.InvCDF` en el BCL. Dos caminos, **confirmar cuál**:
- **(a) Hand-roll (recomendado, sin dependencia):** función regularizada incompleta beta `I_x(a,b)` (fracción continua de Lentz) + bisección para el cuantil inverso. ~50 líneas, acotada, testeable. Evita nueva dependencia (ustedes auditan deps).
- **(b) MathNet.Numerics:** `Beta(a,b).InverseCumulativeDistribution(p)`. Limpio y probado, pero **es una dependencia nueva** — decisión consciente por la auditoría de deps.

## §4 — Segmentación por tipo de EII
Calcular el posterior por segmento: **Todos**, **Crohn** (TipoEII=2), **CUCI** (TipoEII=1).
- **Sutileza crítica:** los votos **anónimos no tienen TipoEII**. Entonces "Todos" usa todos los votos; los segmentos Crohn/CUCI **solo usan votos logueados con EII conocida** → n de segmento ≪ n total. Esto hace que el gate de §5 golpee más seguido en segmentos. Documentarlo.
- Recompute: `TipoEII` es denormalización; `CondicionIdPrincipal` es la fuente. (Recalcular masivo = fuera de alcance aquí, pero la data ya está.)

## §5 — Gate de confiabilidad (honestidad con pocos datos)
Reemplazar/complementar el corte fijo `n≥10`:
- Mostrar el porcentaje **solo si** `n ≥ MinVotos` (mantener 10 como piso) **y** el ancho del intervalo creíble ≤ umbral (p. ej. 40 puntos). Configurable.
- Si no pasa: **"Aún no hay suficientes respuestas"** (como hoy), NUNCA un porcentaje.
- Cuando sí se muestra, **mostrar siempre el intervalo** ("entre A % y B %") y la **n** — la incertidumbre es parte del dato, no un adorno.

## §6 — Dónde se muestra
- **Admin `EstadisticasTolerancia`** (primero, interno): por ingrediente y por segmento → media posterior + IC 95% + n. Sin fricción de riesgo.
- **Público `/tolero/{slug}` + ficha de ingrediente:** reemplazar el % Laplace por la media posterior + IC + gate. Mantener el encuadre existente: **"experiencia de la comunidad, NO consejo médico"**, nunca "deberías poder comerlo".

## §7 — Rollout seguro (dominio de salud sensible)
- **El "todos" público** (sin segmentar) es evolución directa del MVP → OK mostrarlo con IC + gate + encuadre.
- **El segmentado público** ("X % de pacientes con Crohn toleran…") es lo sensible: puede influir qué come un enfermo. **DECISIÓN A CONFIRMAR:** ¿segmentado solo en admin por ahora, o también público? Recomendación: **admin primero**; público solo con n de segmento suficiente + caveats fuertes.
- Caveats obligatorios en cualquier vista: es orientativo, sesgo de autoselección, no sustituye indicación médica/nutricional. (Un voto por paciente ya está garantizado por el dedup.)

## Decisiones a confirmar antes de aplicar
1. **Prior:** `Beta(1,1)` (default, = MVP) vs `Beta(2,2)` (regulariza más los segmentos chicos). Recomiendo (1,1).
2. **Cuantil Beta:** hand-roll sin dep (a, recomendado) vs MathNet.Numerics (b).
3. **Segmentado público:** admin-only por ahora (recomendado) vs también público.

## Fuera de alcance
- Crédito parcial / modelo ordinal para "A veces".
- Prior jerárquico / empirical-Bayes (segmento que "pide prestado" del global) — mejora futura para segmentos chicos.
- Recompute masivo de `TipoEII` histórico.
- Nuevas tablas: NO hacen falta, la data ya está.

## Verificación
1. `Estimar(2,0)` con `Beta(1,1)` → media 75 % (no 100 %), IC ancho → el gate NO lo muestra si n<10.
2. Con muchos votos, media → `s/(s+f)` y el IC se angosta.
3. Segmento Crohn con pocos logueados → "aún no hay suficientes respuestas para Crohn", mientras "Todos" sí puede mostrarse.
4. La encuesta pública y el panel admin dan el MISMO número para el mismo (segmento, ingrediente) — un solo servicio.
5. El encuadre "experiencia comunitaria, no consejo médico" sigue presente; siempre se ve n + IC.
