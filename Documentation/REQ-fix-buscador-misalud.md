# REQ — Fix del card buscador + autocomplete en Mi Salud

**Fecha:** 17 JUL 2026
**Síntoma (owner):** el card del buscador que va arriba de las páginas de Mi Salud quedó **angosto** (antes era del ancho del contenedor), y el **autocomplete muestra las sugerencias sin estilo**. Pasa en TODAS: Condiciones, Síntomas, Tratamientos, Estado de ánimo, Seguimiento de síntomas.

## Causa raíz (diagnosticada, no adivinada)
El commit `e7d4dae "refactor(css): cerrar #6 — eliminar miSalud.css"` migró estas páginas del sistema viejo `.crm-*` (que vivía en `miSalud.css`) al nuevo `.eii-*`, pero la migración del card del buscador quedó **incompleta**:
- El card viejo `.crm-card` era ancho (`max-width: 1200px`, llenaba el renglón).
- El markup nuevo usa `<div class="qp-wrapper-flex"><div class="eii-card">…`. `.qp-wrapper-flex` es `display:flex`, y `.eii-card` es un flex-item **sin `flex-grow`** → se encoge al ancho de su contenido. De ahí el card angosto.
- El dropdown `.eii-autocomplete__dropdown` SÍ tiene estilo (en `eiibd-components.css:644`), pero (a) sale angosto porque el card/input lo está, y (b) es probable que el JS que arma los `<li>` de sugerencias no les ponga la clase `.eii-autocomplete__item` (resto de la migración) → salen sin diseño.

## Fix

### 1. Ancho del card (CSS)
`.qp-wrapper-flex` está en `wwwroot/css/site.css` (~línea 433). Recuperar el ancho:
```css
.qp-wrapper-flex > .eii-card { flex: 1 1 auto; width: 100%; max-width: 1200px; }
```
**OJO de scope:** confirmar dónde más se usa `.qp-wrapper-flex` — está pensado para un layout de dos columnas (`.qp-main` + `.qp-aside`). Si se reusa en otras vistas con hijos distintos, el selector `> .eii-card` podría sobre-aplicar. Si es el caso, en vez del selector global, dar una **clase dedicada al card buscador** (ej. `eii-card--buscador`) en el markup de las 5 páginas y estilar esa. Elegir según cómo esté usado `.qp-wrapper-flex` (verificar primero).

### 2. Sugerencias del autocomplete (JS)
Revisar el JS que llena las listas de sugerencias (`#condicionSugerencias`, `#sintomaSugerencias`, `#tratamientoSugerencias`, etc.). Asegurar que **cada `<li>` se cree con `class="eii-autocomplete__item"`**. Si hoy los crea sin clase o con `.crm-*`, ahí está el "sin diseño elegante". El estilo del item ya existe en `eiibd-components.css:660` (`.eii-autocomplete__item` + `:hover`).

## Alcance / verificación
- Es `.cshtml` / CSS / JS → **sin rebuild** (RazorRuntimeCompilation + assets estáticos; refresh basta).
- Reusar tokens `eii-*`, no inventar.
- **Verificar en las 5 páginas de Mi Salud** (Condiciones, Síntomas, Tratamientos, Estado de ánimo, Seguimiento): el card llena el ancho, y al escribir en el buscador las sugerencias salen estilizadas (con hover morado).
- Diff antes de aplicar.

## Nota de fondo (no de este fix)
Esto es un síntoma de la fragmentación de CSS (B-2 de la auditoría: 21 archivos, y migraciones `crm-*`→`eii-*` a medio terminar). La consolidación es deuda consciente aparte; aquí solo se cierra la regresión visible.
