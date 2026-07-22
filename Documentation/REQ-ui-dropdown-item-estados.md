# REQ — Matar el azul de Bootstrap en los items de menú desplegable

**Fecha:** 17 JUL 2026
**Archivo:** `wwwroot/css/eiibd-components.css` (regla compartida). CSS, **sin rebuild**. Reusar tokens `eii-*`. Diff antes de aplicar.
**Motivo (owner):** al abrir un desplegable del menú superior (ej. General → "Familia y relaciones"), el item seleccionado/clicado sale en **azul** — el default de Bootstrap. Debe seguir el estándar de la plataforma.

## Causa raíz (verificada)
**Ningún archivo CSS define estados de `.dropdown-item`** (`grep -rn "\.dropdown-item" wwwroot/css/` → vacío). Las únicas menciones son cosméticas: `_menuContenidoPartial.cshtml:72` (`.fw-bold`) y `_TopMenuDesktop.cshtml:179` (solo `font-size`). Sin overrides, `:hover`/`:focus`/`:active` caen en el azul de Bootstrap.

El commit anterior de estados del top-menu cubrió los `nav-link`, **no** los items del desplegable — este es ese hueco.

## Fix — UNA regla compartida (no tocar los 10 partials)
Existen **10 partials** de menú (`_TopMenu` ×4, `_CategoryMenu` ×2, `_menuContenido` ×2, desktop/mobile). **No hay que editarlos**: como nadie sobrescribe `.dropdown-item`, basta definir los estados **una vez** en `eiibd-components.css` y todos lo heredan.

Aplicar el mismo estándar que ya fijamos para el top menu — **gris = estado, morado = interacción**:
```css
.dropdown-item:hover        { background: var(--eii-surface-subtle); color: var(--eii-text); }
.dropdown-item:focus,
.dropdown-item:focus-visible{ background: var(--eii-surface-subtle); color: var(--eii-text);
                              outline: 2px solid var(--eii-primary); outline-offset: -2px; }
.dropdown-item:active,
.dropdown-item.active       { background: var(--eii-primary-soft); color: var(--eii-primary); }
```
(Valores orientativos — ajustar a los tokens reales.) **Clave: neutralizar explícitamente** el `background-color`/`color` azul que Bootstrap pone en `:focus`/`:active`/`.active`.

## Alcance / verificación
- Como la regla es global, revisa que no rompa algún desplegable que a propósito use otro estilo (perfil, admin). Si alguno necesita excepción, scopearla.
- **Probar en:** Ayuda, General, el dropdown de perfil, los menús de admin, y las variantes mobile/categorías. En ninguno debe quedar azul.
- Sin rebuild; diff antes de aplicar.

## Nota de deuda (no de este fix)
Los **10 partials de menú duplicados** siguen siendo deuda (misma familia que los dos `_SidebarMenu`). Aquí se esquiva porque el arreglo va en CSS compartido, pero consolidarlos sigue pendiente.
