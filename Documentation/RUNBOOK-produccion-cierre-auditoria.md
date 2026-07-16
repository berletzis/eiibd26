# Runbook — cerrar la auditoría de producción (lo que ya no es código)

> Estado: el código de la auditoría está cerrado y pusheado (`f49bd9d`…`f0b10fc`).
> Lo que falta para que el veredicto pase de **NO LISTO** a **LISTO** se ejecuta en el
> servidor y en las consolas de los proveedores. Nada de esto lo puede hacer un commit.
>
> Auditoría origen: `Documentation/AUDITORIA-PRODUCCION-fable-2026-07-15.md`

---

## Lo primero: por qué el orden importa

Hay dependencias reales entre las tareas. Ejecutadas en desorden, algunas se anulan
entre sí o cortan el acceso:

- **A-1 antes de rotar el password de `sa`.** Si rotas `sa` primero y la app todavía se
  conecta como `sa`, tumbas producción hasta que actualices la variable de entorno.
  Crea el login dedicado, mueve la app, y recién entonces rota `sa`.
- **Rotar antes de purgar el historial.** La purga es cosmética si los valores siguen
  vivos: lo que ya se clonó, clonado está. Rotar es lo que mata el secreto; purgar solo
  evita que se vuelva a leer desde el repo.
- **C-2 (keyring) desloguea a todos.** Agéndalo, no lo hagas a media tarde.

Orden sugerido: **1 → 2 → 3 → 4 → 5 → 6**.

---

## 1 · C-2 — Regenerar el keyring de Data Protection 🔴

**Es lo único que aún deja vivo el riesgo de forjado.** El XML salió del tracking en
`f49bd9d`, pero esa clave sigue publicada en el historial de un repo público: quien la
tenga puede **firmar cookies de sesión de cualquier usuario** y **tokens de reset de
contraseña**. Desrastrear no invalida una llave que ya está en la calle. Solo regenerarla
lo hace.

La app persiste el keyring en `ContentRoot/DataProtectionKeys` (`Program.cs:83`) y crea
claves nuevas sola si la carpeta está vacía.

```powershell
# En el servidor, con la app detenida (o inmediatamente antes de reciclar el app pool).
# Respaldo por si hay que volver atrás:
Copy-Item -Recurse "C:\ruta\a\eiibd26\DataProtectionKeys" "C:\backup\dpkeys-20260715"

Remove-Item "C:\ruta\a\eiibd26\DataProtectionKeys\*.xml"
# Arrancar la app → genera un key-<guid>.xml nuevo al primer request.
```

**Impacto esperado (es el precio, no un fallo):**
- Los ~1000 usuarios con sesión activa quedan deslogueados. Vuelven a entrar y ya.
- Los tokens de reset de contraseña y confirmación de email **pendientes** mueren. Quien
  esté a media recuperación tiene que pedirla de nuevo.

**Verificar:** que aparezca un `key-*.xml` con GUID nuevo; que un login funcione; que una
cookie vieja (otro navegador con sesión abierta) haya dejado de valer.

**Ventana:** baja actividad. No es reversible en la práctica — restaurar el backup
revive las llaves comprometidas, así que solo hazlo si algo se rompe de verdad.

---

## 2 · A-1 — Login SQL dedicado (dejar de correr como `sa`) 🔴

Hoy cualquier SQL injection o RCE escala a control total del servidor de BD, otras bases
incluidas. Y el password de `sa` estuvo committeado (C-1), así que asumirlo comprometido
no es paranoia.

```sql
-- 1) En el servidor SQL, crear el login y el usuario acotado a eiibd26:
CREATE LOGIN eiibd26_app WITH PASSWORD = '<password-nuevo-fuerte>';
USE eiibd26;
CREATE USER eiibd26_app FOR LOGIN eiibd26_app;
ALTER ROLE db_datareader ADD MEMBER eiibd26_app;
ALTER ROLE db_datawriter ADD MEMBER eiibd26_app;
GRANT EXECUTE TO eiibd26_app;
```

Ojo con Hangfire: usa su propio esquema y **crea/altera tablas**. Si arranca con el login
nuevo y falla por permisos, dale ownership de su esquema en vez de volver a `sa`:

```sql
GRANT CREATE TABLE TO eiibd26_app;
ALTER AUTHORIZATION ON SCHEMA::HangFire TO eiibd26_app;
```

**2)** Cambiar `ConnectionStrings__DefaultConnection` (variable de entorno del servidor,
ver `SECRETS.md`) al login nuevo. **3)** Reiniciar y verificar. **4)** Solo cuando todo
esté verde, rotar el password de `sa` (tarea 4).

**Verificar:** login de paciente, alta de un registro cualquiera, dashboard médico, y que
Hangfire procese un job (encolar una pregunta a NINA). Si algo truena, es permisos —
concede lo puntual, no vuelvas a `sa`.

**Extra del hallazgo:** `TrustServerCertificate=True` anula la validación TLS contra un
SQL remoto por IP pública. Certificado válido = tarea aparte, no bloqueante.

---

## 3 · A-2 — Clave pública del webhook de SendGrid 🔴

`SendGridWebhookController.cs:66-91`: si `SendGrid:WebhookPublicKey` viene vacía, procesa
el evento y solo loguea un warning. `web.config` no la define → **producción corre sin
validar firma**, en un endpoint `[AllowAnonymous]`.

El riesgo concreto: cualquiera postea rebotes falsos y `BounceClasificador` suprime el
correo de usuarios arbitrarios — incluido el de reset de contraseña. Es una denegación de
servicio dirigida y silenciosa.

1. SendGrid → Settings → Mail Settings → Event Webhook → activar *Signed Event Webhook* y
   copiar la **clave pública** (no es la API key).
2. En el servidor: `SendGrid__WebhookPublicKey=<clave>` (doble guion bajo).
3. Reiniciar.

El código de verificación ya existe (`:190`); solo falta la config.

**Verificar:** el botón *Test Your Integration* de SendGrid → evento aceptado; un POST a
mano sin firma → rechazado.

**Endurecimiento GATED (opcional):** hacer que en producción devuelva 503 si la clave
falta, en vez de procesar. **No lo apliques hasta confirmar que la clave ya está puesta**
— si no, rompes el webhook.

---

## 4 · C-1 — Rotar lo que estuvo committeado, y después purgar 🔴

El tracking ya está limpio (`f0b10fc`: `.claude/settings.local.json` y el doc de
`planes/`; `9ef50d8`: lighthouse y capturas). **Eso no rota nada.** Todo lo que estuvo en
un repo público debe tratarse como comprometido:

- [ ] **SendGrid API key** — rotar. Ya se había rotado una vez y volvió al repo dentro
      del allowlist de `settings.local.json`, que guarda comandos literales con el
      secreto pegado. Ese archivo ya está en `.gitignore`; la key nueva no debe volver
      a escribirse en ningún comando que se guarde.
- [ ] **Password de `sa`** — rotar (después de la tarea 2, no antes).
- [ ] **Google Maps API key** — no es rotable como secreto (es de cliente, siempre
      visible). Lo que aplica es **restricción por referrer HTTP** a tus dominios en la
      consola de Google Cloud + alerta de cuota. Sin restricción es facturable por
      terceros.
- [ ] **Purgar el historial** — `git filter-repo` (o BFG) sobre `.claude/settings.local.json`,
      el doc de `planes/`, el XML de Data Protection, `lighthouse-report.report.json` y
      `.playwright-mcp/`. **Reescribe la historia**: coordínalo si hay otro clon vivo, y
      hazlo con el repo respaldado. Corresponde a SEGURIDAD #9.
- [ ] **Regla operativa:** los docs y planes nunca llevan credenciales — placeholders y
      referencia a `SECRETS.md`. La violación de esta regla fue la fuga.

**Lo que iba dentro de `.playwright-mcp/`, para dimensionar la purga:** 2 PDF de
resúmenes médicos **reales** exportados y 173 `.yml` con el árbol de accesibilidad de
páginas autenticadas. Mientras no se purgue, esos datos de paciente están publicados.

---

## 5 · M-2 — Datos de prueba en la BD de producción 🟡

Ya no se muestran al público (el filtro por badge de médico verificado lo tapa,
`GlossaryService.cs:853-905`), pero contaminan métricas y son PII de prueba latente: si
un consumidor futuro no pasa por ese filtro, salen.

```sql
-- Verificar PRIMERO qué va a tocar:
SELECT Id, Comment, Approved FROM GlossaryValidations
WHERE Comment IN ('validar descripcion usuario prueba', 'xxxx')
   OR (Comment LIKE '%prueba%' AND LEN(Comment) < 50);

-- Ejecutar solo si el SELECT devuelve lo esperado:
UPDATE GlossaryValidations SET Approved = 0
WHERE Comment IN ('validar descripcion usuario prueba', 'xxxx')
   OR (Comment LIKE '%prueba%' AND LEN(Comment) < 50);
```

⚠️ El `LIKE '%prueba%'` es amplio: puede alcanzar una validación legítima de un paciente
que escribió "prueba" en una frase real. **Mira el SELECT fila por fila antes del UPDATE.**
Es `Approved = 0`, no `DELETE` — reversible.

---

## 6 · M-4 — Ratings anónimos (esto sí es código, su propio ciclo) 🟡

`ArticleRatingsApiController.cs:87` y `GlossaryRatingsApiController.cs:75` aceptan POST
anónimo (`UserId = null`) sin rate limiting: un loop de curl infla o hunde el rating de
contenido médico.

Dos caminos, y la decisión es de producto, no técnica:

- **Exigir sesión** (como ya hace `PlatCalificacionesApiController`). Mata el abuso de
  raíz; cuesta los ratings de lectores no registrados, que en un sitio que vive de
  tráfico orgánico es la mayoría.
- **Rate limit por IP + dedupe por cookie anónima.** Conserva la señal abierta y frena el
  volumen. No frena a un atacante decidido, pero sí el ruido.

Recomendación: la segunda. El valor de la calificación abierta es real y el daño es de
métricas, no de seguridad del paciente. Si prefieres la primera, es una línea.

---

## Fuera de este runbook

- **A-3** (registro sin confirmar email + login inmediato) — riesgo aceptado, decisión
  tomada.
- **M-3** (antiforgery inconsistente) — su propio ciclo tras el baseline. Hoy `SameSite=Lax`
  lo mitiga, pero es defensa única.
- **M-5b** (caché de slugs del middleware SEO) y **M-6** (CSP con `unsafe-inline`) —
  deuda consciente.
- **B-2** (CSS fragmentado) — diferido.
