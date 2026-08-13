# REQ — Fix: la tarjeta del header de P&R no llena el ancho

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web` (CSS). NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Bug:** en las páginas de "Mis P&R" (médico y paciente) la tarjeta del header (`Mis Preguntas y Respuestas` + botón "Volver al Dashboard") se ve **angosta** y deja media pantalla vacía, en vez de llenar el ancho como el estándar (dashboard).

## Causa raíz (verificada)
- La tarjeta es `.eii-card.pqr-header-card` dentro de `.qp-wrapper-flex`.
- `.qp-wrapper-flex` es `display: flex` (`wwwroot/css/site.css:442`).
- `.pqr-header-card` (`wwwroot/css/preguntas.css` ~L1175) **no tiene `flex-grow`/`width`**, así que en un contenedor flex **se encoge al ancho de su contenido** en vez de llenar el contenedor.
- El propio repo documenta este mismo tropiezo en `wwwroot/css/eiibd-components.css:70-74`, con la clase `.eii-card--buscador { flex: 1 1 auto }` como solución al mismo problema.

## Fix
En `wwwroot/css/preguntas.css`, dentro de la regla **`.pqr-header-card`** (~L1175), agregar:
```css
flex: 1 1 auto;
```
(equivalente a `width: 100%;` en este contexto). Una línea.

## Alcance del efecto (a propósito)
- Arregla **las DOS** páginas de P&R a la vez — médico (`Areas/Identity/Pages/Medico/MedicoPreguntasRespuestas.cshtml`) y paciente (`Areas/Identity/Pages/Usuario/usuarioPreguntasRespuestas.cshtml`) — porque **comparten** la clase y la estructura (`container > qp-wrapper-flex > pqr-header-card`). Ambas quedan full-width y consistentes. Es el comportamiento deseado.

## Verificación
1. "Mis P&R" del médico: la tarjeta del header llena el ancho del contenedor (como el dashboard), sin espacio muerto a la derecha.
2. "Mis P&R" del paciente: igual, full-width.
3. En móvil (`max-width:1200px`, donde `.qp-wrapper-flex` pasa a `flex-direction: column`) la tarjeta sigue bien, sin desbordes.
4. No se rompió ninguna otra vista que use `.pqr-header-card` ni el layout con aside (`.qp-aside`) — la tarjeta del header vive sola en su `qp-wrapper-flex`, así que `flex:1 1 auto` solo la hace llenar, sin conflicto.
5. `dotnet publish -c Release` limpio antes del push.

## Opcional (si quieres de una)
- Revisar si hay otras tarjetas dentro de `.qp-wrapper-flex` con el mismo encogido (buscar `qp-wrapper-flex` en las vistas) y aplicarles el mismo `flex:1 1 auto` / `eii-card--buscador`. Reportar antes de tocar.
