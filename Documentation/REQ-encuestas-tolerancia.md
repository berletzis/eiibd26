# REQ — Encuestas de tolerancia (`/tolero/{slug}`) · engagement + viralidad

**Fecha:** 17 JUL 2026
**Objetivo:** un micro-cuestionario de una pregunta ("¿toleras el queso?") lanzable desde push, correo y un link para Facebook, que (a) retiene pacientes con un loop de "responde → ve a la comunidad → vuelve", y (b) recoge los votos que alimentan el modelo bayesiano de tolerancia (#16, hoy diseñado y sin construir — ver `Documentation/wiki-tecnica/09-modelo-bayesiano-tolerancia-alimentaria.md`).

**Regla de oro del producto:** el resultado se enmarca como **experiencia de la comunidad, no consejo médico** ("tu cuerpo es único, esto no es una dieta"). Nunca "deberías poder comerlo".

## El truco de distribución: UNA URL por ingrediente
`/tolero/{slug}` es la unidad que se empuja por push, se manda por correo y se pega en Facebook. Se construye **una** página; los tres canales la reusan. Slug = el mismo que ya usa `/Platillos/Ingrediente/{slug}`.

## Qué reusa (no reinventar)
- Ingrediente + slug: catálogo `PlatIngrediente` existente.
- Push: `PushNotificationService` + VAPID + el panel admin de Notificaciones Push.
- Correo: SendGrid + el panel de Campañas.
- Rate-limiting: la infra de rate-limit ya usada en catálogos/short-urls.
- El disclaimer y el tono "esto no es una dieta" de las vistas de Platillos.

## Nuevo — modelo de datos (SQL directo, SIN migración EF, como el resto del repo)
Tabla `PlatTolerVoto`:
- `Id` (PK)
- `IngredienteId` (FK lógica a PlatIngrediente; sin FK física si sigue el patrón de aislamiento del módulo)
- `UserId` (Guid?, null si anónimo)
- `AnonId` (Guid?, cookie de dedup para anónimos)
- `Tolera` TINYINT — 1=Sí, 2=AVeces, 3=No (con CHECK IN (1,2,3), como los otros enums TINYINT del repo; y `HasConversion<byte>()` en el mapeo)
- `TipoEII` TINYINT? — capturado del perfil si está logueado (CUCI/Crohn); null si anónimo o desconocido. Se guarda ahora aunque el MVP no segmente, para que la data esté lista para el bayesiano.
- `FechaVoto` datetime
- Únicos: `(IngredienteId, UserId)` y `(IngredienteId, AnonId)` → un voto por paciente/cookie (upsert: puede cambiar su voto). Esto respeta la limitación "un voto por paciente" del doc bayesiano.

## La página `/tolero/{slug}` (pública, anónimo permitido)
1. Muestra "¿Toleras el {ingrediente}?" + 3 botones: **Sí · A veces · No**.
2. Al votar: registra (upsert por UserId o AnonId), y muestra el resultado de la comunidad:
   - "**El X% de la comunidad lo tolera**" + el desglose (Sí / A veces / No) + `(n=Y respuestas)`.
   - **Cálculo MVP:** proporción suavizada por Laplace `(s+1)/(s+f+2)` (s=Sí, f=No; "A veces" se muestra aparte en el desglose, no entra al binario). NO el bayesiano completo aún — eso es #16.
   - **Guard (del doc bayesiano):** si `n < 10` (parametrizable), NO muestres porcentaje; muestra "Aún no hay suficientes respuestas — sé de los primeros 🙌". Evita el "100% con 2 votos".
3. **Disclaimer** fijo: experiencia de la comunidad, no consejo médico; tu cuerpo es único.
4. **CTA según sesión:**
   - Anónimo → "Crea tu perfil para llevar tu propio registro de lo que toleras" → registro. (El embudo de Facebook.)
   - Logueado → además su voto puede ofrecer "¿agregarlo a *Lo que no tolero*?" si votó No (link a UsuarioAlimentacion). Opcional en MVP.

## Los 3 ganchos
- **Push (PWA instalada):** desde el panel de Notificaciones, "pregunta de la semana" con deep-link a `/tolero/{slug}`. MVP: envío manual eligiendo el ingrediente. Fase 2: rotación semanal automática (tarea programada).
- **Correo (Campañas):** correo con los 3 votos como links. **OJO anti-prefetch:** los escáneres de correo pre-cargan los links y dispararían votos falsos si el GET vota directo. Por eso el link **NO vota solo**: `/tolero/{slug}?intent=si` abre la página con la opción resaltada y el usuario **confirma con un tap**. Un solo tap, pero no automático.
- **Facebook:** postear `/tolero/{slug}` con un gancho. Anónimo entra, vota, ve el resultado, se le empuja a registrarse.

## Dedup y anónimos (nota de coherencia con M-4)
Los ratings (M-4) exigen sesión — pero **la encuesta SÍ permite anónimo a propósito**, porque su objetivo es alcance viral, no un rating de contenido médico. Mitigación: dedup por cookie `AnonId` + rate-limit por IP. Distinción de confianza para el futuro bayesiano: el consenso "% de pacientes con [tipo EII]" debería calcularse **solo con usuarios registrados** (que tienen tipo de EII conocido); los votos anónimos alimentan el número general de "comunidad" y el embudo, no el consenso clínico por tipo. En MVP se guardan todos con su origen; el split fino es de #16.

## Fases
- **MVP (ahora):** tabla + página `/tolero/{slug}` + % suavizado con guard de n mínimo + los 3 ganchos + dedup. Ingredientes (grupos se pueden sumar después con el patrón polimórfico tipo/destino de `PlatNotaClinica`).
- **Fase 2 (#16):** el bayesiano completo (prior Beta, posterior, intervalo creíble, segmentación por tipo de EII) reemplaza el cálculo suavizado. La data ya viene lista porque se guardó `TipoEII` desde el MVP.

## Restricciones
- SQL directo para el esquema (sin `dotnet ef`). Handlers Razor con `string?`/`int?`. Solo tocar el proyecto web. Diff antes de aplicar. Es `.cshtml` + un `.cs` nuevo + SQL → rebuild en VS para el `.cs`.
