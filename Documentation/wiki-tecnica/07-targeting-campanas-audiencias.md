# Targeting de campañas y resolución de audiencias

> Wiki técnica interna — no publicar. Incluye los criterios exactos de segmentación, incluido el truco del hash de contraseña.

## Qué problema resuelve

Las campañas de email necesitan enviar el mensaje correcto a la audiencia correcta, y hacerlo de forma consistente entre el **conteo en vivo** (lo que el admin ve antes de enviar) y el **job de envío** (Hangfire). El sistema resuelve audiencias con una **única fuente de verdad** para las reglas de inclusión/exclusión, de modo que "elegibles" en la UI y "destinatarios" en el envío siempre coincidan.

Hay dos capas: `CampanaTargetingService` (criterio de "público" base) y `CampanaAudienciaService` (audiencias de negocio con exclusiones globales y anti-reenvío).

## Cómo funciona por dentro

### 1. Público base (`CampanaTargetingService`)

Sobre el universo de usuarios se exige siempre email confirmado y no vacío, y luego se segmenta por antigüedad usando un **detalle del hash de contraseña de ASP.NET Identity**:

| Público | Criterio | Traducción SQL |
|---|---|---|
| `UsuariosViejos` | `LEN(PasswordHash) == 68` | hash del formato anterior (no Identity v3) |
| `UsuariosNuevos` | `PasswordHash LIKE 'AQAAAA%'` | `AQAAAA` = base64 del byte marcador 0x01 de Identity v3 (PBKDF2/SHA-256) |
| `TodosConfirmados` | (sin filtro extra) | todos los confirmados |

Es un truco elegante: se distingue a los usuarios migrados de una versión vieja de la plataforma **por la forma de su hash**, sin necesidad de una columna de "versión de usuario". Los filtros se traducen a SQL (`LEN`, `LIKE`) y corren en servidor.

### 2. Resolución de audiencias (`CampanaAudienciaService`)

Toda audiencia parte de un universo base con **exclusiones globales** aplicadas de raíz:

1. Solo usuarios válidos (`SoloValidos()`, excluye suspendidos).
2. **Excluir rebotes:** direcciones con hard bounce o soft bounce reincidente, calculadas al vuelo desde `SendGridEventLog` (cruce por email en minúsculas). Solo afecta envíos.
3. **Excluir cuentas de sistema:** NINA y Comunidad (GUIDs desde config).

Sobre esa base, cada audiencia aplica su criterio de negocio **y excluye a quienes ya recibieron esa fase** (anti-reenvío, vía `EmailCampanaLogs` con `Exito == true`), identificada por un número de `Fase`:

| Audiencia | Fase | Criterio |
|---|---|---|
| ViejosSinToque1 | 1 | usuarios viejos que aún no recibieron fase 1 |
| Toque2 | 2 | viejos que recibieron fase 1 y no la 2 |
| Toque3 | 3 | viejos que recibieron fase 2 y no la 3 |
| TodosConfirmados | 10 | todos (dedup opcional por `templateId`) |
| SinCondicion | 20 | sin ninguna condición registrada |
| SinMood | 21 | nunca registraron mood |
| ConRespuestasSemana | 22 | recibieron respuesta de otro en los últimos **7 días** |
| DiagnosticoPendiente | 23 | fecha de inicio de condición == fecha de creación del perfil (placeholder) |
| SinAvatar | 24 | avatar nulo, o contiene `ui-avatars.com` o `default` |
| CompletarFechaDiagnostico | 25 | condición sin fecha real (null o 1-ene de cualquier año) |
| SinMoodReciente | 26 | último mood hace más de **14 días** (excluye a quienes nunca registraron) |

Las secuencias "Toque 1→2→3" implementan un **drip de reactivación**: cada toque solo va a quien recibió el anterior y no el actual. Los criterios como `DiagnosticoPendiente` replican exactamente la lógica del dashboard (p. ej. `NeedsDiagnosisDateUpdate`) para que UI y envío coincidan.

## Parámetros y umbrales (valores reales)

| Parámetro | Valor | Dónde |
|---|---|---|
| Hash usuario viejo | `LEN == 68` | `CampanaTargetingService:23` |
| Hash usuario nuevo | `StartsWith("AQAAAA")` | `:28` |
| Ventana "con respuestas" | 7 días | `CampanaAudienciaService:175` |
| Ventana "sin mood reciente" | 14 días | `:267` |
| Placeholder de diagnóstico | 1 de enero (mes==1 && día==1) | `:253` |
| Detección "sin avatar" | null / `ui-avatars.com` / `default` | `:232` |
| Códigos de fase | 1,2,3,10,20–26 | `:42`–`:50` |

## Dónde vive

- Público base y truco del hash: `eiibd26/Services/Campanas/CampanaTargetingService.cs:10` (viejos `:23`, nuevos `:28`).
- Audiencias, exclusiones globales y anti-reenvío: `eiibd26/Services/Campanas/CampanaAudienciaService.cs` — `BuildAudienciaQueryAsync` en `:69`; exclusión de rebotes `:81`; cuentas de sistema `:90`; mapeo de fases `FaseLogPara` en `:53`.

## Cómo explicarlo en una presentación

Para cada campaña armamos la lista de destinatarios con reglas precisas. Primero quitamos a todos los que nunca deberían recibir correo: cuentas suspendidas, direcciones que rebotan y cuentas internas del sistema. Después elegimos el segmento: por ejemplo, "usuarios que nunca registraron su estado de ánimo" o "usuarios que dejaron de registrar hace más de dos semanas". Y siempre excluimos a quienes ya recibieron ese mensaje, para no repetir.

El truco más ingenioso: distinguimos a los usuarios antiguos de los nuevos **mirando la forma de su contraseña cifrada**. Cuando migramos de sistema, el cifrado cambió de formato; así que la "huella" del hash nos dice, sin ninguna columna extra, quién viene de la versión vieja y quién se registró después. Lo mismo que la UI muestra como "elegibles" es exactamente lo que el envío usa — una sola fuente de verdad, sin sorpresas.

## Limitaciones y supuestos

- La segmentación viejo/nuevo depende de un **detalle de implementación** del hash de Identity: si Identity cambia el formato de hash (nuevo marcador), la regla `LEN == 68` / `AQAAAA` queda obsoleta.
- Varias audiencias materializan HashSets de IDs en memoria (rebotes, ya-enviados, criterios): con universos muy grandes esto consume memoria.
- El "placeholder de diagnóstico" (1 de enero) es una convención frágil: un diagnóstico real ocurrido un 1 de enero se clasificaría como pendiente.
- La exclusión por rebote se calcula al vuelo en cada resolución; depende de que `SendGridEventLog` esté actualizado.
- El anti-reenvío se basa en `EmailCampanaLogs.Exito`: si un log no se escribió, un usuario podría recibir dos veces.
