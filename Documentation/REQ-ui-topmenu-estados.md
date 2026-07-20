# REQ — Top menu: estandarizar estados active vs clic (gris vs morado)

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Shared/_TopMenuDesktop.cshtml` (CSS inline, `<style>` ~144-160) + `Pages/Shared/_TopMenuMobile.cshtml` (para que coincida). Es `.cshtml`/CSS, **sin rebuild**. Reusar tokens `eii-*`. Diff antes de aplicar.
**Motivo (owner):** los estados `active` y clic tienen estilos distintos entre items con submenú (Ayuda, General) y los planos (Buscador, Glosario, Comunidad). Debe ser consistente.

## Estado actual (el desajuste)
En el `<style>` inline de `_TopMenuDesktop.cshtml`:
- `.topbar .nav-link.active` = **morado + bold** (`--eii-primary`, línea 156-158).
- **No hay** regla de `:focus`/`:focus-visible`/`:active` (el "contorno morado" de clic), **ni** de dropdown abierto (`.dropdown.show > .nav-link`).
- Consecuencia: los `.dropdown-toggle` (Ayuda, General) heredan el foco/outline por defecto de Bootstrap → se ven "más o menos" como el owner quiere; los planos no → inconsistentes.

## Estándar deseado
**Gris = estado (active), morado = interacción (clic/foco).**
- **Active (página actual):** el **gris clásico** — usar el mismo tratamiento de "active" gris que ya existe en el sidebar/otros menús (texto `--eii-text` o fondo gris sutil, según la convención; **quitar el morado + bold** actual).
- **Clic / foco:** **contorno morado** — `outline` o `box-shadow` ring en `--eii-primary` (ej. `box-shadow: 0 0 0 2px var(--eii-primary-soft)` + `outline: 1px solid var(--eii-primary)`), en `:focus-visible` y `:active`.
- **Consistencia:** las dos reglas deben cubrir **TODOS** los items — `.nav-link` planos Y `.nav-link.dropdown-toggle`. Agregar `.dropdown.show > .nav-link` con el mismo tratamiento (que el dropdown abierto no muestre un estilo divergente de Bootstrap).
- Neutralizar cualquier default de Bootstrap que haga que el toggle se vea distinto al plano.

## Alcance / verificación
- El `_Layout` principal usa `Pages/Shared/_TopMenuDesktop.cshtml` + `_TopMenuMobile.cshtml` — arreglar esos dos y que coincidan entre sí.
- **Verificar en TODOS los items:** con submenú (Ayuda, General) y planos (Buscador, Preguntas, Glosario, Comunidad, Panel, Iniciar sesión, Registro): active se ve gris consistente; al clic/foco, contorno morado consistente.
- Sin rebuild; diff antes de aplicar.

## Nota de fondo (deuda, no de este fix)
El top menu está **duplicado** en varios `_TopMenu*.cshtml` (Areas/Identity/…, Areas/Pages/…) — mismo problema que los sidebars. Si el área admin usa otro layout con otro `_TopMenu`, necesitará el mismo arreglo. Consolidar los `_TopMenu` a uno es deuda consciente aparte (ver la tarea de separar/unificar navegación).
