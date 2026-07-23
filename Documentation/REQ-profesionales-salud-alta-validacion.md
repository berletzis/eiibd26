# REQ — Profesionales de la salud: alta de validador desde admin, título en perfil y display limpio

**Fecha:** 22 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar `NINA-WorkerService` ni `Conectar3eros`.
**Modo:** analizar primero, **diff antes de aplicar**, y si algo se ve raro, avisar antes de seguir. No cambiar rutas públicas existentes (solo agregar).
**Origen:** necesitamos dar de alta profesionales (arrancando por nutriólogos) que validen contenido de platillos/ingredientes, sin depender del flujo de recomendación de pacientes. Y abrir el encuadre de "médico" a "profesional de la salud".

## Contexto verificado del código (no re-descubrir)
- **El permiso para validar es solo el rol de Identity `"Medico"` o `"Administrador"`**, chequeado en los page handlers (ej. `Pages/Platillos/Ingrediente.cshtml.cs`, `Pages/Glosario/Termino.cshtml.cs`). NO depende del directorio ni de recomendaciones.
- **El nombre público** en `ValidacionContenidoService` sale como `"Dr. {nombre}"` **solo si** el médico tiene ficha vinculada (`MedicoPerfilExtendido.MedicoId`) + badge `perfil_reclamado` **o** `verificado`; si no, `"Médico verificado"` (anónimo). Lógica repetida (glosario/artículos/notas): líneas ~147, 181, 421, 454, 535, 576 de `Services/Validacion/ValidacionContenidoService.cs`.
- **`RegisterM`** (`Areas/Identity/Pages/Account/RegisterM.cshtml.cs`) hoy otorga el rol `Medico` al instante (línea ~153) con `MedicoId = null`. Registro **abierto**.
- **`Admin/DirectorioMedicos`** gestiona fichas y otorga badges, pero NO crea cuentas, NO asigna rol `Medico`, NO vincula ficha↔cuenta fuera del claim.
- Roles sembrados: `Paciente, Medico, Admin, Administrador` (`Program.cs` ~1051).

## A — Copia: "médico" → "profesional de la salud" (solo texto visible)
- En `RegisterM.cshtml`: título, subtítulo y botón hablan de **"profesional de la salud"** (no "médico"). Ej.: "Registro de profesional de la salud".
- **El rol interno se queda como `Medico`** — NO renombrar (es interno, refactor innecesario). Solo cambia lo que el usuario lee.
- Banner/rótulo distintivo (ver parte E).

## B — Gating: registrarse ≠ poder validar (opción 1, cierra el hueco abierto)
- `RegisterM` deja de otorgar poder de validar al instante. El profesional se registra y queda como **pendiente** (sin capacidad de validar) hasta que el admin lo apruebe.
  - Implementación a criterio de Claude Code, la más simple y segura: p. ej. NO asignar el rol `Medico` en el registro y asignarlo al aprobar en admin; o un flag `ValidadorAprobado` que el gating de validación exija además del rol. **Proponer la opción y confirmar antes de aplicar.**
- **Regla:** sin aprobación de admin, la cuenta no valida y no muestra nombre. Con aprobación → valida.
- Mantener intacto el flujo B (token/claim por recomendación de paciente) como vía alterna.

## C — Alta/aprobación desde admin (desacoplado de pacientes)
En el panel de admin (extender `Admin/DirectorioMedicos` o crear pieza nueva, lo que sea más limpio):
- Poder **aprobar** un profesional registrado → le da capacidad de validar.
- Poder **crear/vincular una ficha con nombre + especialidad + título + cédula** a la cuenta, **sin** requerir propuesta ni claim de paciente. (Verificar si ya existe una acción de vínculo directo ficha↔cuenta; si no, agregarla.)
- Poder otorgar el badge **`verificado`** → habilita mostrar el nombre. (El otorgar badges ya existe; reusarlo.)
- **NO cambiar la lógica de display** — ya hace el OR `perfil_reclamado || verificado`.

## D — Campo "Título" en el perfil del profesional
- Agregar campo **`Titulo`** a la ficha que alimenta el display público (`MedicoDirectorio`, donde vive `Nombre`; confirmar).
- En el perfil del profesional: **combo/lista curada** de títulos comunes + opción **"Otro"** (texto libre corto). Lista inicial (curar): `Dr.`, `Dra.`, `Nut.`, `Lic. en Nutrición`, `Psic.`, `Enf.`, `Mtro.`, `Mtra.`, `Q.F.B.`, `Otro`.
- **Display:** el nombre se arma como **`{Titulo} {Nombre}`** → "Nut. Ana López". **Eliminar el `"Dr. "` hardcodeado** de `ValidacionContenidoService`; si no hay título, mostrar solo el nombre (nunca asumir "Dr.").
- **Salvaguarda:** el título lo elige el usuario, pero **solo aparece en público cuando el admin aprueba el nombre** (badge `verificado`). Al aprobar, el admin avala también el título. Sin aprobación → "Profesional verificado" (genérico, sin título ni nombre).

## E — URL bonita de invitación
- Agregar la ruta amigable **`/profesionaldelasalud/invitacion`** como **alias** de la página de registro de profesional (`RegisterM`, área Identity), vía convención de ruta en `Program.cs` (`AddAreaPageRoute` o equivalente — confirmar según su `AddRazorPages`).
- **Solo AGREGAR, no cambiar** la ruta original (regla: rutas públicas no cambian, SEO intacto).
- Poner **`noindex`** a esa página (registro/invitación, no debe indexarse).
- (Opcional) mantener también `/medico/invitacion` como segundo alias.
- Banner/rótulo distintivo en la página para que no se confunda con el registro de paciente. En `Register.cshtml` (paciente) agregar rótulo discreto "Registro de paciente" para cerrar la confusión por simetría.

## F — Display "Validado por" + lista (sin repetir etiqueta)
En la vista pública donde se listan las validaciones:
- **Un solo encabezado** "Validado por", y debajo la **lista** de validadores, cada uno como `{Titulo} {Nombre}` + check pequeño y su especialidad en texto chico.
- La etiqueta genérica **"Profesional verificado"** aparece **solo** en las filas sin nombre aprobado (no en cada una).
- No repetir "Profesional verificado…" por fila cuando hay varios. (Ver mockups aprobados en la conversación.)

## Fuera de alcance / no romper
- No renombrar el rol interno `Medico`.
- No cambiar el candado de validación de la IA ni el de contenido (roles) más allá del gating de la parte B.
- No cambiar rutas públicas existentes (solo agregar el alias).
- No tocar NINA ni Conectar3eros.

## Verificación
1. Un profesional se registra por `/profesionaldelasalud/invitacion` → NO puede validar aún; la URL en la barra queda bonita; la página se ve distinta a la de paciente.
2. Admin lo aprueba → ya valida.
3. Admin le vincula ficha + título "Nut." + nombre + badge `verificado` → en una nota validada aparece "Nut. Ana López" bajo "Validado por".
4. Un profesional aprobado pero sin nombre/badge → aparece "Profesional verificado" (genérico), sin "Dr.".
5. Nota validada por 3 → encabezado "Validado por" una sola vez, 3 filas, sin repetir la etiqueta.
6. Rutas viejas siguen funcionando; la de invitación tiene `noindex`.
