# REQ — Detalle de platillo: "¿Puedo comerlo?" como link, no cápsula

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Platillos/Detalle.cshtml` (+ CSS compartido si se agrega clase). CSS + `.cshtml`, **sin rebuild**. Diff antes de aplicar. Solo presentación.

## Síntoma (owner)
En el detalle del platillo, junto a cada ingrediente, el "ⓘ ¿Puedo comerlo?" se ve como **cápsula grande** (`.eii-badge`) y compite con el nombre. El owner lo quiere como **link discreto**, sin el look de pastilla. (El resto de la vista —cards de stats coloridos— está bien, no tocar.)

## Estado actual
`Detalle.cshtml:119-121`: es un `<span class="eii-badge"><i class="bi bi-info-circle"></i> ¿Puedo comerlo?</span>` — pastilla, y **no es clickable** (el nombre del ingrediente de al lado, `:113`, es el `<a>` que lleva a la ficha). El comentario del código lo hizo span para no doblar el subrayado dentro de `.contenido-html`.

## Fix
Convertir el "¿Puedo comerlo?" en un **link inline discreto** (con el ícono), sin fondo ni borde de pastilla:
- Cambiar el `<span class="eii-badge">` por un `<a href="/Platillos/Ingrediente/@r.Slug">` con una clase propia, ej. `.eii-inline-link`:
  ```html
  <a href="/Platillos/Ingrediente/@r.Slug" class="eii-inline-link" title="Ver qué se sabe sobre @r.Nombre">
      <i class="bi bi-info-circle" aria-hidden="true"></i> ¿Puedo comerlo?
  </a>
  ```
- CSS de `.eii-inline-link` (en el CSS compartido, reusando tokens): color `--eii-primary`, `font-size: var(--eii-text-sm)`, **sin** background/border/padding de badge, `text-decoration: none` y **subrayado solo en hover**. Que se lea como "más info", ligero, sin competir con el nombre.
- Ahora sí es clickable (va a la misma ficha que el nombre) — mejor área de toque para "¿puedo comer esto?".

## De paso
- El ejemplo inline en la tarjeta amarilla "¿Por qué este platillo no tiene sello médico?" (`Detalle.cshtml:251-252`) usa el mismo `.eii-badge` — actualizarlo al nuevo estilo de link para que coincida, y ajustar el copy si dice "el badge" → "el enlace ¿Puedo comerlo?".

## Verificación
- El "¿Puedo comerlo?" se ve como link discreto (no cápsula), alineado con el nombre del ingrediente.
- Es clickable y lleva a la ficha del ingrediente.
- El ejemplo de la tarjeta amarilla coincide con el nuevo estilo.
- Sin rebuild; diff antes de aplicar.
