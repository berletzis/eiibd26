# REQ — Ficha de ingrediente: conectar las tarjetas de tolerancia a datos reales

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web` — `Pages/Platillos/Ingrediente.cshtml(.cs)`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build con vistas Razor (`dotnet publish`) antes de pushear.
**Objetivo:** las dos tarjetas de la ficha ("Lo que reporta la comunidad" y "Tu experiencia") hoy son **texto estático hardcodeado** (`Ingrediente.cshtml:184-192`) — muestran "nadie ha registrado" y "función llegará pronto" siempre, aunque haya votos. Conectarlas a **datos reales de solo-lectura**, con enlace a `/tolero/{slug}` para votar. NO se embebe la votación (sigue viviendo en `/tolero`).

## Regla de oro: una sola fuente del resultado comunitario
El cálculo del resultado ("Todos": conteos + `ToleranciaBayes.Estimar` + gate n≥10 y ancho IC ≤ 40) **ya vive en `Tolero/Encuesta.cshtml.cs` (`CargarResultadosAsync`)**. Para que la ficha y `/tolero` **nunca muestren cifras distintas**, extraer esa lógica a un **helper/servicio compartido** (ej. `ToleranciaResultadoService` o método reutilizable) y consumirlo desde ambas páginas. NO duplicar el cálculo inline. (El cálculo en sí, `ToleranciaBayes`, no se toca.)

## Tarjeta "Lo que reporta la comunidad" (pública, solo-lectura)
- Cargar conteos de `PlatTolerVoto` para el ingrediente + estimar (segmento "Todos").
- Si **pasa el gate** → mostrar el % + "lo más probable es que esté entre A % y B %" + n (mismo formato que `/tolero`).
- Si **no pasa** → "Aún no hay suficientes respuestas" (honesto; reemplaza el texto fijo "nadie ha registrado", que hoy miente si hay votos).
- Botón/enlace **"Responder"** → `/tolero/{slug}` (slug con el mismo `SlugHelper`).
- Mantener el disclaimer "experiencia de la comunidad, no consejo médico".

## Tarjeta "Tu experiencia"
- **Logueado con voto** → "Registraste: {No toleras / A veces / Sí toleras}" + botón **"Cambiar respuesta"** → `/tolero/{slug}`.
- **Logueado sin voto** → "Aún no registras tu tolerancia" + botón **"Responder"** → `/tolero/{slug}`.
- **Anónimo** → CTA **"Responder"** → `/tolero/{slug}` (sin leer la cookie anónima aquí; MVP).
- Eliminar el texto estático "Esta función llegará pronto".

## Fuera de alcance
- **NO embeber** el formulario de voto en la ficha — solo lectura + enlace a `/tolero`.
- Sin leer el voto anónimo (cookie) en la ficha.
- **Sin tabla nueva, sin deploy-gate SQL, sin tocar el cálculo bayesiano** ni la encuesta.
- Los segmentos por EII (Crohn/CUCI) NO se muestran en la ficha pública — solo "Todos", igual que `/tolero`.

## Verificación
1. Ingrediente **con votos suficientes** → la ficha muestra el mismo % + IC + n que `/tolero/{slug}` (idénticos, por el helper compartido).
2. Ingrediente **sin votos o insuficientes** → "Aún no hay suficientes respuestas" (ya no "nadie ha registrado").
3. Logueado que ya votó → "Tu experiencia" muestra su voto + "Cambiar respuesta".
4. Anónimo → ve el resultado comunitario + CTA "Responder".
5. El enlace "Responder" abre la encuesta correcta (slug idéntico, no 404).
6. `dotnet publish -c Release` limpio antes del push.
