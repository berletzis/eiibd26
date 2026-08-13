# REQ — URL de invitación para pacientes

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Objetivo:** crear una URL bonita de invitación para **pacientes**, paralela a la de profesionales (`/profesionaldelasalud/invitacion`), que apunte a la página de registro de paciente.

## Contexto (verificado, patrón a espejar)
- La de profesionales es un **alias de ruta** a `Areas/Identity/Pages/Account/RegisterM` vía `AddAreaPageRoute("Identity", "/Account/RegisterM", "profesionaldelasalud/invitacion")` en `Program.cs`, con `noindex` en la vista.
- La página de registro de paciente es `Areas/Identity/Pages/Account/Register` (ya tiene su rótulo "Registro de paciente" de la simetría previa).
- **Diferencia clave con la de profesionales:** el paciente NO lleva candado/gating (nada de `MedicoPendiente` ni aprobación). Se registra y usa la plataforma. Esto es SOLO la URL + noindex.

## Cambio
- Agregar la ruta amigable **`/paciente/invitacion`** como **alias** de `Register` (área Identity):
  `options.Conventions.AddAreaPageRoute("Identity", "/Account/Register", "paciente/invitacion");`
  (Confirmar la forma exacta según cómo está configurado `AddRazorPages`, igual que se hizo para la de profesionales.)
- **Solo AGREGAR, no cambiar** la ruta original de `Register` (regla: rutas públicas no cambian, SEO intacto).
- **`noindex`** en la vista de registro cuando se llega por esta ruta de invitación (o en la página, mismo criterio que la de profesionales).
- **Slug:** el usuario puede preferir `comunidad/invitacion` en vez de `paciente/invitacion` — usar el que confirme; el default es `paciente/invitacion` (paralelo a profesionales).

## Opcional (confirmar con el usuario)
- Copy de bienvenida más cálida en el registro de paciente cuando se llega por la invitación (tono de "únete a la comunidad"), en vez del rótulo seco "Registro de paciente". Si se quiere, es cambio de vista aparte.

## Fuera de alcance
- Sin gating ni roles nuevos (el paciente ya se registra normal).
- No tocar `RegisterM` ni la ruta de profesionales.

## Verificación
1. `eiibd.com/paciente/invitacion` (o el slug elegido) abre la página de registro de paciente, con la URL bonita en la barra (alias, no redirect a la fea).
2. La ruta original de `Register` sigue funcionando.
3. La página de invitación tiene `noindex`.
4. `dotnet publish -c Release` limpio antes del push.
