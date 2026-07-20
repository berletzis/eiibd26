# REQ — Detalle de ingrediente: espaciado del card de nota

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Platillos/Ingrediente.cshtml` + `wwwroot/css/detalle.css`. CSS + `.cshtml`, **sin rebuild**. Diff antes de aplicar. Solo espaciado — no tocar contenido ni lógica.

## Síntoma (owner)
En el detalle del ingrediente (`/Platillos/Ingrediente/{slug}`): (1) el card de la nota tiene **mucho blanco arriba** (antes del "En el grupo: …"), y (2) el card que le sigue ("Lo que reporta la comunidad") queda **muy pegado** debajo.

## Causa raíz (verificada)
1. **Blanco arriba:** `detalle.css:126` resetea el margen del primer heading **solo para `h1`**:
   ```css
   .contenido-html > h1:first-child { ... }
   ```
   Pero el bloque del grupo (`Ingrediente.cshtml:109-112`) empieza con un **`<h2>`** ("En el grupo: {grupo}"), que conserva su `margin-top` grande de artículo (`.contenido-html h2`, detalle.css:130) → el hueco arriba del card.
2. **Card pegado abajo:** los `<div class="contenido-html">` de la nota (`Ingrediente.cshtml:101` y `:109`) **no tienen margen inferior**; los cards siguientes usan `mb-4`, así que el de la comunidad queda glued.

## Fix
### 1. Blanco arriba (CSS, detalle.css)
Generalizar el reset del primer hijo para que cubra cualquier heading, no solo h1:
```css
.contenido-html > :first-child { margin-top: 0; }
```
(o extender la regla existente de `:126` a `h1, h2, h3`). Con esto el "En el grupo:" arranca pegado al borde superior del card, sin el blancote.

### 2. Card pegado abajo (markup)
Dar margen inferior al bloque de la nota, consistente con los cards siguientes. En `Ingrediente.cshtml`, agregar `mb-4` a los dos `<div class="contenido-html">` (líneas ~101 y ~109). Verificar que el `_NotaSello` que va debajo (líneas 105/113) tampoco quede pegado; si hace falta, un `mb-3` en su contenedor.

## Verificación
- El card de la nota ya no tiene el blanco grande arriba; el título "En el grupo:" respira normal.
- Espacio consistente entre el card de la nota y "Lo que reporta la comunidad" (igual que entre los demás cards).
- Probar en un ingrediente con nota de grupo (ej. `/tolero`… no — `/Platillos/Ingrediente/papa`) y en uno con nota de ingrediente propia (que empieza distinto), para confirmar que el reset del primer hijo no rompe otros casos.
- Sin rebuild; diff antes de aplicar.
