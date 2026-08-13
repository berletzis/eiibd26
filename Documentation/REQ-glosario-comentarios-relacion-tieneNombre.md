# REQ — Glosario: cerrar la incoherencia en los comentarios de relación (flag TieneNombre)

**Fecha:** 06 AGO 2026
**Scope:** solo `eiibd26.Web` — `Services/Glossary/DTOs/GlossaryValidationCountsDto.cs`, `Services/Glossary/GlossaryService.cs`, `Pages/Glosario/Termino.cshtml`. NO tocar NINA ni Conectar3eros.
**Modo:** diff antes de aplicar; `dotnet publish` limpio **Y** abrir en Development un término con validación de relación (partial → solo compila cuando el padre lo pinta).
**Objetivo:** cerrar la MISMA incoherencia que ya arreglamos en el bloque "Validado por" y en `_NotaSello`, pero en los **dos bloques de comentarios de relación** del glosario (los que salen de `counts.ComentariosMedicos`, ~L570 y ~L697 de `Termino.cshtml`). Hoy el anónimo ahí sale con **foto real** + etiqueta **"Médico verificado"** — debe salir con **avatar por defecto** + **"Profesional verificado"**.

## Causa raíz
- Esos dos bloques renderizan `ValidationCommentDto`, que **NO tiene un flag `TieneNombre`**, así que la vista no puede distinguir anónimo de con-nombre → pinta la foto real y la etiqueta vieja para todos.
- Hoy no filtra nada porque `MedicoPerfilExtendido` está vacía y esos bloques salen vacíos, **pero se activa en cuanto entre el primer profesional.** Por eso conviene cerrarlo antes.

## Cambio
1. **`GlossaryValidationCountsDto.cs`:** agregar `public bool TieneNombre { get; set; }` a `ValidationCommentDto`.
2. **`GlossaryService.cs` (`GetValidationCountsAsync`):** al poblar `ComentariosMedicos`, calcular `TieneNombre` con la **misma lógica de identidad** que ya se usa en `ObtenerValidacionesPublicasAsync` / el resto del servicio (médico con badge `verificado`/`perfil_reclamado` **y** ficha con nombre → `true`; si no → `false`). Reusar el helper existente, no reimplementar.
3. **`Termino.cshtml` (bloques ~L570 y ~L697):**
   - `!c.TieneNombre` → **avatar por defecto** (`/img/default-avatar.png` / placeholder) + etiqueta **"Profesional verificado"** (NO "Médico verificado") + su comentario (se queda).
   - `c.TieneNombre` → nombre + foto real, sin cambios.

## Regla (no romper)
- Nombre + foto reales → solo con badge. Sin badge → "Profesional verificado" + avatar por defecto.
- El comentario del anónimo se sigue mostrando.
- Consistente con el bloque "Validado por" (ya arreglado), `_NotaSello`, y el copy "profesional" (no "médico").

## Verificación
1. Validador de relación **sin badge** → "Profesional verificado" + avatar por defecto + comentario (no la foto real, no "Médico verificado").
2. Validador de relación **con badge** → nombre + foto real, sin cambios.
3. Consistente entre los dos bloques de relación, el "Validado por", y las notas de ingredientes.
4. `dotnet publish -c Release` limpio **y** el término abre sin error de Razor en Development.
