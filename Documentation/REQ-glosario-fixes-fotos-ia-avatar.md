# REQ — Glosario: fotos de validadores, quitar "(IA)" y avatar por defecto

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Vista:** `Pages/Glosario/Termino.cshtml` (+ su PageModel y servicios de validación/comunidad).
**Patrón de avatar a reutilizar (ya existe):** imagen por defecto `/img/default-avatar.png` (en `wwwroot/img/`); helper canónico `ResolveAvatar` (`GlossaryService.cs` L517-522) que descarta vacío y el sentinela `"default.jpg"` y antepone `/`. Todo `<img>` de avatar lleva `onerror="this.onerror=null;this.src='/img/default-avatar.png'"`.

## Bug 1 — Falta la FOTO del médico en "Relación con EII"
- **Dónde:** `Termino.cshtml` L566-574. El loop pinta un icono fijo `bi bi-person-badge-fill` + el comentario como string.
- **Causa:** `RelationConsensusItemDto.Comments` es `List<string>` — descarta el `UserId`, así que no hay foto que pintar.
- **Fix (simple, sin cambiar DTO):** en la vista, cruzar cada nivel con `counts.ComentariosMedicos.Where(c => c.RelationType == level)` — ese DTO **ya trae `AvatarUrl`** (se resuelve en L539-552). Pintar `<img src="@(c.AvatarUrl ?? "/img/default-avatar.png")" onerror="…default-avatar.png">` en vez del `<i>`, junto al comentario.

## Bug 2 — En "Validado por Profesionales de la Salud" no aparece ni foto ni comentario
- **Dónde:** `Termino.cshtml` L629-683. Hoy cae a la rama de solo-conteo (L655-660: "1 médico validaron…").
- **Causa (doble):**
  1. El PageModel (`Termino.cshtml.cs`) **nunca llama** a `ValidacionContenidoService.ObtenerValidacionesPublicasAsync` (L114-220), que es el que devuelve `UserDisplay` + `AvatarUrl` + `Comentario` por validación. Ese dato existe pero no se pide ni se pinta.
  2. La rama con comentario (L640-654) usa `MeaningComments` (`List<string>`, sin avatar) filtrado por badge; si el médico no tiene badge o el comentario está vacío, queda vacía.
- **Fix:** cablear en el PageModel `ObtenerValidacionesPublicasAsync(TipoContenidoValidado.Termino, Term.Id)`, exponer la lista, y en el bloque iterar esos DTOs pintando `<img>` (avatar + fallback) + `@v.Comentario` + `@v.UserDisplay`. Sustituir el `validacion-avatar-placeholder` (L645-647) por `<img>` real.
- **REGLA DE IDENTIDAD (respetar, ya definida):** el **nombre + foto real** solo se muestran si el médico tiene badge `verificado`/`perfil_reclamado`; si no, sale como **"Profesional verificado" + avatar por defecto**. `ObtenerValidacionesPublicasAsync` ya calcula ese `UserDisplay`. El **comentario sí se muestra** en ambos casos (atribuido a "Profesional verificado" si es anónimo).
  - **Nota para probar:** si tu médico de prueba no tiene el badge `verificado`, aparecerá como "Profesional verificado" con avatar por defecto — no es bug, es la regla. Para verlo con nombre + foto, otórgale el badge.

## Bug 3 — Quitar el "(IA)"
- **Dónde:** `Termino.cshtml` — L560 y L599 (`NINA (IA)` en el bloque de relación), L531 (badge de nivel `🤖 IA`). También aparece en L475 (card de definición) y L1134 (sidebar "Sugerido por NINA (IA)").
- **Fix (confirmado por el usuario):** quitar el "(IA)" / el sufijo "IA" en **TODAS** las ocurrencias — L531 (badge de nivel `🤖 IA`), L560 y L599 (`NINA (IA)` del bloque de relación), L475 (card de definición) y L1134 (sidebar "Sugerido por NINA (IA)"). Debe quedar **"NINA"** en todos lados. Hacer un grep final de `(IA)` / `\bIA\b` en la vista para no dejar ninguna suelta.

## Bug 4 — Avatar por defecto para pacientes sin foto ("Comunidad")
- **Dónde:** `Termino.cshtml` L834-843 — cuando no hay `AvatarUrl` cae a un icono `bi-person`, no a la imagen por defecto. La tarjeta creada por JS al añadir mood (L934) usa el mismo icono.
- **Causa:** `CommunityExperienceService.cs` L293 pone `AvatarUrl = null` cuando no hay foto, y **no** filtra el sentinela `"default.jpg"` ni antepone `/` (a diferencia de `ResolveAvatar`). La `<img>` de L837 tampoco tiene `onerror`.
- **Fix:**
  - En la vista, el `else` (L841) → `<img src="/img/default-avatar.png" alt="@exp.AliasUsuario">` en vez del icono. Y a la `<img>` de L837 agregar `onerror="this.onerror=null;this.src='/img/default-avatar.png'"`. Igualar el JS de L934.
  - En `CommunityExperienceService.cs` L293, aplicar la misma lógica que `ResolveAvatar` (tratar `""`/`"default.jpg"` como sin foto y anteponer `/` a rutas relativas) para no mandar una ruta que 404ee.

## Fuera de alcance
- No cambiar la regla de identidad (badge → nombre+foto; sin badge → "Profesional verificado" + default). Solo se corrige el **render** (que hoy ni siquiera pinta la foto/comentario).
- No tocar el cálculo de consenso ni los conteos.

## Verificación
1. "Relación con EII": el comentario del médico aparece con su **foto** (o avatar por defecto si no tiene/no está badgeado).
2. "Validado por Profesionales de la Salud": aparece **foto + comentario + nombre/"Profesional verificado"** por cada validación de la descripción (no solo el conteo).
3. No aparece "(IA)" en ninguna de las ocurrencias corregidas; queda "NINA".
4. En "Comunidad", un paciente **sin foto** muestra `/img/default-avatar.png`, no un icono ni una imagen rota. Un `Avatar = "default.jpg"` en BD también cae al por defecto.
5. `dotnet publish -c Release` limpio antes del push.
