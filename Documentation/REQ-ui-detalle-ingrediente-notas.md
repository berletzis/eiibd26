# REQ — Detalle de ingrediente: distinguir nota propia vs nota de grupo

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Platillos/Ingrediente.cshtml` (+ CSS; posiblemente el partial `_NotaClinica.cshtml` para las referencias). `.cshtml`/CSS, **sin rebuild**. Reusar tokens `eii-*`. Diff antes de aplicar.
**Motivo (owner):** la página muestra hasta dos notas (la propia del ingrediente + la del grupo) como cards blancos idénticos → parece artículo de blog. Hay que **distinguirlas sin colorido** y sacar las referencias como bloque aparte.
**Reemplaza:** `REQ-ui-detalle-ingrediente-espaciado.md` — al reestructurar estos cards, el hueco de arriba y el card pegado se resuelven de paso.

## Contexto (modelo)
En `Ingrediente.cshtml` se renderizan dos notas posibles (líneas ~99-114): la **nota propia del ingrediente** (`Model.IngredienteNota`) y la **nota del grupo** (`Model.GrupoNota`, hoy con un `<h2>En el grupo: {grupo}</h2>`). Pueden aparecer 0, 1 o 2. El `_NotaClinica` partial pinta secciones + referencias.

## Diseño v2 (mockup v2 revisado con el owner — ITERA sobre lo ya aplicado en v1)
Plano, neutral, con aire. **Clave: UN solo card por nota — QUITAR el card interno.** (v1 quedó con card-dentro-de-card, doble borde; eso es lo primero que hay que corregir.)

### 1. Nota propia del ingrediente
**Un solo card**, sin caja interna. Arriba, una etiqueta chica: "Sobre el {ingrediente}" (ícono + muted, uppercase suave, letter-spacing). Las secciones fluyen **directo dentro del mismo card**. Solo si `IngredienteNota != null`.

### 2. Nota del grupo — distinguida, sutil (sin banda pesada)
**Un solo card**, distinguido así (reemplaza la banda gris ancha + el borde izquierdo de v1):
- Un **chip pill** arriba: "Aplica a todo el grupo · {grupo}" (fondo suave `--eii-surface`, borde hairline, `border-radius` full, ícono de jerarquía). Reemplaza el `<h2>En el grupo: {grupo}</h2>`.
- El card **sutilmente recesado**: fondo `--eii-surface-subtle` / `surface-1` — un punto más apagado que el blanco de la nota propia. Eso lo marca como "contexto de grupo", plano y sin color.

### 3. Referencias como bloque aparte (por nota)
Hoy fluyen dentro del contenido (`_NotaClinica`: `<h3>Referencias</h3> <ul>`). Sacarlas a un **bloque secundario al pie de cada card**: hairline divisor + título chico muted "Referencias" (uppercase suave) + la lista, en peso secundario. **Sin card propio.**

### General — toques "para esta época"
- **Un card por nota, sin caja interna** (lo más importante de esta iteración).
- Radius suave (16px), padding generoso (~1.25rem), `line-height: 1.7`, más espacio entre secciones.
- Títulos de sección en **peso medio (500)**, no negro pesado — se siente editorial, no denso (mata el "parece blog").
- Plano: sin sombras fuertes. La jerarquía es por **superficie** (recesado) + **chip**, no por color.
- El callout azul "No es un alimento prohibido…" se queda como está (arriba, suelto).

## Cuidado de scope
`_NotaClinica.cshtml` puede estar **compartido** (el comentario menciona "misma escala que Contenidos/Detalle"). Antes de restilar las referencias en el partial, **verificar dónde más se usa**; si se comparte, scopear los estilos nuevos al contenedor del ingrediente (una clase propia) para no afectar otras vistas. La banda de "Aplica al grupo" va en `Ingrediente.cshtml` (no en el partial).

## Verificación
- Con 0, 1 y 2 notas: la propia y la de grupo se ven **distintas** (la de grupo con su banda + borde), sin colorido.
- Referencias de cada nota, en bloque aparte secundario, sin card.
- Sin el hueco grande arriba ni el card siguiente pegado.
- Sin rebuild; diff antes de aplicar.
