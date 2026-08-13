# REQ — Ocultar acciones de perfil paciente a médico y admin

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web` (vistas). NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Objetivo:** los botones de "agregar a mi perfil" (síntomas, tratamientos, no tolerados — seguimiento personal del paciente) NO deben aparecer para usuarios con perfil **Médico** o **Administrador**. Hoy solo se protegen por "estar logueado", así que médico/admin los ven.

## Condición de visibilidad (consistente en todos lados)
Mostrar el botón **solo si es paciente puro**:
```
@if (User.IsInRole("Paciente") && !User.IsInRole("Administrador") && !User.IsInRole("Medico"))
```
(Mismo espíritu que el filtrado del sidebar de paciente, más la exclusión de Medico.)

## Puntos a corregir (confirmados)
1. **Glosario — `Pages/Glosario/Termino.cshtml:795`.** El botón `btnAgregarMiMood` ("Agregar" síntoma/tratamiento al seguimiento) está protegido solo por `@if (User.Identity?.IsAuthenticated == true)`. Cambiar esa condición por la de "paciente puro" de arriba.
   - Mantener intacto el CTA de "Inicia sesión…" para no autenticados (L805+).
2. **Encuesta de tolerancia — `Pages/Tolero/Encuesta.cshtml`.** El botón **"Agregar a mis no tolerados"** (feature reciente) hoy aparece para cualquier logueado que votó "No". Gatearlo con la misma condición "paciente puro". (Votar en sí puede seguir abierto — es comunitario; lo que se oculta a médico/admin es **agregar a la lista personal**.)
3. **Ficha de ingrediente — `Pages/Platillos/Ingrediente.cshtml:225` (tarjeta "Tu experiencia").** Es la **tolerancia personal del paciente** a ese alimento. Ocultar la tarjeta "Tu experiencia" completa a médico/admin (con la condición "paciente puro"). **IMPORTANTE:** la tarjeta **"Lo que reporta la comunidad"** (el dato comunitario) se **queda visible para todos** — es info que médico/admin sí querrían ver. Solo se oculta la experiencia personal.

## Decisiones resueltas (por el usuario)
- **Rating de ingrediente/contenido** (`Ingrediente.cshtml:262`, `ingredienteRating`, like/dislike) → **se deja ABIERTO a todos.** Es feedback de calidad del contenido, no una acción de perfil paciente. Sin cambio.
- **"Tu experiencia"** → **se OCULTA a médico/admin** (ver punto 3). Regla de fondo: lo comunitario y los datos los ven todos; la experiencia/seguimiento personal del paciente, solo pacientes.

## Sweep recomendado (para no dejar otros sueltos)
Buscar en las vistas otros botones/acciones de **agregar al perfil del paciente** (síntomas, condiciones, tratamientos, laboratorios, alimentación/exclusiones, mood) que estén protegidos **solo** por `IsAuthenticated` y aplicarles la misma condición. Patrones a rastrear: `btnAgregar*`, `data-tipo="sintoma|tratamiento"`, enlaces a `/Identity/Usuario/Usuario*`. Reportar lo que se encuentre antes de tocarlo.

## Fuera de alcance
- No cambiar la lógica de las acciones ni sus handlers — solo la **visibilidad** en la vista.
- No tocar el filtrado de rol del sidebar (va en su propio REQ).
- Votar en `/tolero` sigue abierto a todos (es comunitario); solo se oculta el "agregar a lista personal".

## Verificación
1. Logueado como **Paciente** → ve "Agregar" en glosario, "Agregar a mis no tolerados" en `/tolero`, y la tarjeta "Tu experiencia" en la ficha de ingrediente.
2. Logueado como **Médico** → NO ve ninguno de esos tres. **SÍ ve** "Lo que reporta la comunidad" y el rating.
3. Logueado como **Administrador** → igual que médico: NO ve los tres de paciente; SÍ ve comunidad y rating.
4. No autenticado → sigue viendo el CTA de "Inicia sesión…", sin cambios.
5. El sweep no dejó otros botones de perfil paciente visibles a médico/admin.
6. `dotnet publish -c Release` limpio antes del push.
