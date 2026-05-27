# REGLAS PERMANENTES — DESIGN SYSTEM EIIBD

> **ARCHIVO PROTEGIDO — NO SOBRESCRIBIR.**
> Solo agregar nuevas reglas o decisiones al final.
> Aplica a TODAS las sesiones futuras sin excepción.

---

## 1. Fuente única de verdad

Todo desarrollo visual nuevo usa exclusivamente el Design System EIIBD:

- `wwwroot/css/eiibd-tokens.css`
- `wwwroot/css/eiibd-layout.css`
- `wwwroot/css/eiibd-components.css`
- `wwwroot/css/eiibd-utilities.css`
- `wwwroot/css/eiibd-overrides.css`

**Prohibido crear CSS paralelos o archivos de estilos ad-hoc.**

---

## 2. NO reutilizar estilos legacy

No crear nuevos botones, cards, inputs ni layouts con prefijos:

- `crm-` · `se-` · `custom-` · `legacy-`
- `btn-old` · `panel-old` · `widget` · `cardNew`

Siempre migrar al nuevo estándar. Si un componente legacy aún existe es porque está en coexistencia temporal — no tomarlo como referencia para código nuevo.

---

## 3. Namespace obligatorio

Todo componente nuevo lleva prefijo `eii-`. Ejemplos:

```
eii-btn        eii-card       eii-input      eii-grid
eii-select     eii-sidebar    eii-filter     eii-header
eii-textarea   eii-label      eii-badge      eii-modal
```

**Prohibido crear componentes sin prefijo `eii-`.**

---

## 4. NO HARDCODE

Prohibido en cualquier archivo CSS o `<style>`:

```css
/* ❌ PROHIBIDO */
padding: 13px;
margin: 17px;
color: #ccc;
border-radius: 11px;
rgb(106, 78, 122)        /* sin ser rgba de primary */
color: #6a4e7a            /* usar var(--eii-primary) */
```

Valores permitidos — solo tokens:

```css
/* ✅ CORRECTO */
padding: var(--eii-space-4);
border: 1px solid var(--eii-border);
color: var(--eii-primary);
border-radius: var(--eii-radius-md);
```

**Excepción documentada:** `rgba(106, 78, 122, x)` para transparencias del primary donde CSS vars no funcionan en contexto (ej. box-shadow con alpha).

---

## 5. NO REASIGNAR / NO DUPLICAR

Si existe un componente del sistema, no crear otro:

```
❌ eii-card-v2   ❌ new-card      ❌ dashboard-card
❌ special-card  ❌ panel-card    ❌ sidebar-widget
```

Si el componente existente no cubre el caso → **extender con un modificador**:

```css
/* ✅ CORRECTO: extender con modificador */
.eii-card--dashboard { ... }
.eii-btn--wide        { ... }
```

---

## 6. NO CSS INLINE

Prohibido `style="..."` en HTML Razor:

```html
<!-- ❌ PROHIBIDO -->
<div style="margin-top:16px; color:#6a4e7a;">
```

Mover a tokens, utilities o componentes. **Excepciones permitidas** (documentar en comentario):

- JS dinámico en runtime (`element.style.color = '#6a4e7a'`)
- Chart.js / Google Maps (APIs de terceros que requieren valores directos)
- Inline en SVG embebidos con valores calculados en JS

---

## 7. NO `!important`

Prohibido agregar `!important` sin justificación. Antes de usarlo:

1. Buscar la causa raíz (especificidad, orden de carga, herencia)
2. Resolver con estructura correcta de selectores
3. Si aún es necesario: documentar en comentario por qué

Los `!important` en `eiibd-utilities.css` son la única excepción estructural del sistema.

---

## 8. Botones

Todo botón usa exclusivamente:

```
eii-btn eii-btn--solid          ← CTA principal (1 por vista)
eii-btn eii-btn--primary        ← acción secundaria importante
eii-btn eii-btn--neutral        ← cancelar / volver
eii-btn eii-btn--ghost          ← acción terciaria / toolbar
eii-btn eii-btn--link           ← navegación inline en texto
eii-btn eii-btn--danger         ← acción destructiva
eii-btn eii-btn--danger-solid   ← confirmar eliminación en modal
eii-btn eii-btn--success        ← confirmación positiva
```

Tamaños: `eii-btn--sm` · (default) · `eii-btn--lg` · `eii-btn--icon` · `eii-btn--circle`

**Eliminar variantes locales en cada migración.**

---

## 9. Cards

Todo contenedor visual usa:

```
eii-card                ← card estándar con borde
eii-card--elevated      ← shadow, sin borde, radio mayor
eii-card--flat          ← fondo sutil, sin borde
eii-card--link          ← hoverable / clickeable
eii-card--compact       ← padding reducido
eii-card--ghost         ← transparente, solo estructura
```

Prohibido: `panel` · `widget` · `box` · `container-custom` · `dashboard-card`.

---

## 10. Inputs y formularios

Usar exclusivamente:

```
eii-input       eii-select      eii-textarea
eii-label       eii-form-group  eii-form-text
eii-form-error  eii-input-group eii-input-group-text
```

Modificadores de estado: `eii-input--error` · `eii-input--sm`

Eliminar estilos de formulario por pantalla — todo centralizado.

---

## 11. Espaciado

Usar únicamente la escala de 4px:

```
--eii-space-1:  4px    --eii-space-6:  24px
--eii-space-2:  8px    --eii-space-7:  32px
--eii-space-3:  12px   --eii-space-8:  48px
--eii-space-4:  16px   --eii-space-9:  64px
--eii-space-5:  20px   --eii-space-10: 80px
```

Prohibido: 13px · 17px · 21px · 27px o cualquier valor fuera de la escala.

---

## 12. Nuevas funcionalidades

Antes de crear cualquier componente nuevo:

1. Revisar si existe en `eiibd-components.css` o `eiibd-utilities.css`
2. Reutilizar directamente
3. Si falta una variante → extender con modificador BEM
4. **Nunca crear duplicados**

---

## 13. Coexistencia durante migración

Durante migración progresiva:

- **NO** eliminar CSS viejos hasta que su página esté 100% migrada
- Marcar bloques legacy con comentarios:

```css
/* LEGACY — en coexistencia — eliminar cuando /ruta/ migre */
/* OBSOLETO — no usar en código nuevo */
/* ELIMINADO en migración 2026-05 */
```

---

## 14. Estilo visual objetivo

Referencia visual:

- Apps de salud modernas (Apple Health, Levels, Whoop)
- Dashboards SaaS minimalistas (Linear, Notion, Vercel)
- Blogs médicos de credibilidad

**Objetivo:** limpio · moderno · minimalista · confiable

**Evitar:** apariencia Bootstrap clásica · colores saturados sin sistema · sombras excesivas.

---

## 15. Validación obligatoria antes de cerrar sesión

Ver checklist en `10-session-checklist.md`. No cerrar sin ejecutarlo.

---

## 16. Excepciones documentadas permanentes

Las siguientes son **excepciones aprobadas** — no "bugs pendientes":

| Elemento | Archivo | Motivo |
|---|---|---|
| `linear-gradient(135deg, #667eea 0%, #764ba2 100%)` | `Preguntas/Detalles.cshtml`, `UusuarioPreguntaDetalle.cshtml` | Identidad visual del asistente NINA — preservado intencionalmente |
| `linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)` con `-webkit-background-clip:text` | `Preguntas/Detalles.cshtml` | Badge de texto del toggle NINA |
| `.btn-generar-ia` gradient | `Admin/Sintomas/Index.cshtml`, `Admin/Tratamientos/Index.cshtml` | Botón de acción IA — identidad diferenciada |
| Hex `#6a4e7a` en JS | `pwa.js`, `UsersMapPartial.cshtml`, `Mapa/Index.cshtml`, `Admin/DirectorioMedicos` | CSS vars no funcionan en JS string literals |

---

*Última actualización: 2026-05-26 — Migración CSS Design System completada (Módulos 1–12)*
