# REQ — Glosario: avatar por defecto para el validador anónimo (consistencia con notas)

**Fecha:** 06 AGO 2026
**Scope:** solo `eiibd26.Web`, `Pages/Glosario/Termino.cshtml`. NO tocar NINA ni Conectar3eros.
**Modo:** diff antes de aplicar; build `dotnet publish` **Y** abrir en Development un término del glosario con validación (el publish no caza todos los errores de Razor).
**Objetivo:** que el bloque "Validado por Profesionales de la Salud" del glosario use el **avatar por defecto** para los validadores **anónimos** (sin badge → "Profesional verificado"), en vez de la **foto real**. Hoy muestra la cara pero oculta el nombre — se contradice con la propia regla de identidad. `_NotaSello` (notas de ingredientes) ya lo hace bien; esto alinea el glosario.

## Causa raíz
- En `_NotaSello.cshtml` (ya corregido) el anónimo usa el **placeholder** (no la foto real).
- En `Termino.cshtml`, el bloque "Validado por Profesionales de la Salud" usa `v.AvatarUrl ?? default` también para el anónimo → **muestra la foto real de alguien cuyo nombre se oculta.** Incoherente.

## Cambio
En `Pages/Glosario/Termino.cshtml`, bloque "Validado por Profesionales de la Salud": para los validadores **sin nombre aprobado** (`!v.TieneNombre` / que salen como "Profesional verificado"), pintar el **avatar por defecto** (`/img/default-avatar.png` o el `validacion-avatar-placeholder`, lo que use el partial), **no** `v.AvatarUrl`. Los que **sí** tienen nombre aprobado (badge) siguen con su foto real, sin cambios.

## Regla (no romper)
- Nombre real → solo con badge `verificado`/`perfil_reclamado` (sin cambios).
- **Coherencia:** si se oculta el nombre, se oculta la cara. Nombre y foto van juntos, gateados por el badge.
- El **comentario** del anónimo sí se sigue mostrando (eso no cambia).

## Verificación
1. Validador **sin badge** en un término del glosario → "Profesional verificado" con **avatar por defecto** (no su foto) + su comentario.
2. Validador **con badge** → nombre + su foto real, sin cambios.
3. Consistente con las notas de ingredientes (`_NotaSello`).
4. `dotnet publish -c Release` limpio **y** el término abre sin error de Razor en Development.
