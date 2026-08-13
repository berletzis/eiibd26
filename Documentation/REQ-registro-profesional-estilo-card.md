# REQ — Registro de profesional: usar el card estándar (mismo estilo que el login)

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`, una vista. NO tocar NINA ni Conectar3eros.
**Modo:** diff antes de aplicar. Es cambio de vista Razor → si se prueba local, correr en Development y abrir la página (recordar: `dotnet publish` limpio NO garantiza que el runtime compilation esté limpio — usan Razor distinto).
**Objetivo:** que el **Registro de profesional de la salud** (`RegisterM`) se vea con el **mismo card estándar** que el login, en vez del card actual que se ve plano/ancho. Conservar la leyenda de "profesional de la salud".

## Causa raíz (verificada)
- Login (`Areas/Identity/Pages/Account/Login.cshtml:26`) usa el card estándar: `<div class="eii-card">`.
- Registro (`Areas/Identity/Pages/Account/RegisterM.cshtml:17`) usa otra clase: `<div class="account-card">` → por eso se ve distinto.

## Cambio
En `Areas/Identity/Pages/Account/RegisterM.cshtml`:
1. **Línea 17:** cambiar `<div class="account-card">` → **`<div class="eii-card">`**.
2. **Conservar la leyenda** (NO tocar): el badge "Registro de profesional de la salud" (L18-20) y el subtítulo explicativo (L22-25) se quedan.
3. **Opcional (para igualar 100% al login):** cambiar `<h2 class="perfil-title">Registro de profesional de la salud</h2>` (L21) por un `<h2>` simple como el del login, **manteniendo el mismo texto**. Si `perfil-title` ya se ve bien dentro del `eii-card`, dejarlo.
4. Verificar que no haya otro `account-card` anidado en la misma vista que también deba cambiar (el wrapper principal es el de L17).

## Fuera de alcance
- No cambiar la lógica del registro, los campos, ni el dropdown de tipo de profesional.
- No tocar el login ni otras vistas de cuenta.
- Mantener el `noindex` (L7-10) y el resto de la vista igual.

## Verificación
1. `/profesionaldelasalud/invitacion` se ve con el card estándar (limpio y centrado, igual que el login), no plano/ancho.
2. La leyenda/badge "Registro de profesional de la salud" sigue visible.
3. El formulario funciona igual (registro + tipo de profesional).
4. `dotnet publish -c Release` limpio; y si se prueba local, correr en Development y abrir la página (por el runtime compilation).
