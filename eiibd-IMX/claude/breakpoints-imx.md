# Breakpoints de eiibd-IMX

Fuente de verdad de los anchos a los que responde el sitio. Si agregas o mueves
una media query, actualiza esta tabla **y** la cabecera de `wwwroot/css/site.css`.

Antes de este documento (R12, octubre 2026) el CSS tenía siete valores sueltos
(520, 760, 860, 899, 900 como mínimo y como máximo, 980) y nadie sabía cuáles eran.

## Escala

| Rango | Ancho | Media query |
|---|---|---|
| Móvil | hasta 599px | `max-width:599px` |
| Tablet | 600 – 899px | `min-width:600px` / `max-width:899px` |
| Escritorio | 900px y más | `min-width:900px` |
| **Excepción** | hasta 980px | `max-width:980px`: solo slider y footer |

No se agregan otros valores. Si un componente no encaja, se discute antes.

## Qué componente responde a cuál

| Componente | ≤599 (móvil) | ≤899 (móvil + tablet) | ≥900 (escritorio) | ≤980 (excepción) |
|---|---|---|---|---|
| Cabecera (`--header-h`) | 56px, sin bajada | — | 64px con bajada | — |
| Menú (`.nav` / `.nav-toggle`) | — | Panel desplegable con botón | Horizontal | — |
| `.cards` | 1 columna, cuerpo a 15px | 2 columnas; la última impar a ancho completo | 5 columnas | — |
| `.cards-programas` | 1 columna | 2 columnas | 3 columnas | — |
| `.cards-contacto` | 1 columna | 2 columnas desde 600 | 2 columnas | — |
| `.split` | — | 1 columna, figura arriba | 2 columnas | — |
| `.articulo-grid` / `.side` | — | 1 columna, `.side` después del artículo y no sticky | Artículo + sidebar 300px sticky | — |
| `.modal-body` | Padding reducido | — | — | — |
| `.bloque` | Padding reducido | — | — | — |
| Footer (`.foot-grid`) | 1 columna | Gap de 32px | 4 columnas | 2 columnas |
| Slider (`.slider`, `.slide`) | — | — | Rail + paneles animados | Apilado, sin rail ni flechas |
| Objetivos táctiles | — | Todo enlace o botón fuera de texto corrido ≥ 44×44px | Sin cambios | — |

## Lo que no depende de un breakpoint

- **Altura de la cabecera:** sale de `--header-h`, y de ahí se calculan el
  `scroll-margin-top` de los anclas (+16), `.side{top}` (+28) y
  `.slider-rail{top}` (+32). El `rootMargin` del menú activo en `site.js` mide
  `.site-header` directamente. No hardcodear esas alturas.
- **Gutter lateral:** `.slider`, `.split`, `.intro`, `.row-section` y
  `.articulo-grid` anulan el `padding-inline` de `.wrap` con su atajo
  `padding:X 0`. Una regla con `clamp()` (al final de `site.css`) les devuelve
  solo el padding que falta para quedar a 24px del borde, según
  `--wrap-max` (1140px, o 820px en `.wrap-doc`). En escritorio ancho da 0, así
  que no cambia nada. Si agregas una sección `.wrap` con atajo de padding,
  súmala a ese selector.
- **Contenido de la base de datos:** `.prosa table` es `display:block` con
  scroll propio, y `main` y `.modal-body` llevan `overflow-wrap:break-word`
  para URLs largas. `.articulo-grid` en ≤899 usa `minmax(0,1fr)` y no `1fr`:
  con `1fr` la columna se estira al ancho de la tabla y la página entera
  desborda. No volver a `1fr`.

## Nota para comparar capturas de escritorio

La home a 1440px tiene **dos renderizados de fuentes posibles** con el mismo CSS
(se comprobó: el mismo commit produce los dos en corridas distintas). Una
diferencia en las filas de texto de toda la página no prueba que el CSS cambió.
Para decidir, capturar el CSS anterior y el nuevo **alternando varias veces en la
misma sesión** y comparar los hashes: si el nuevo solo produce estados que el
anterior también produce, no hay cambio.

## Excepción 980: por qué existe

Se probó bajar el slider y el footer a 899 (R12, 2 OCT 2026), a 900, 940 y 980px:

- **Slider a 900px:** funciona y `click_donativo` sigue alcanzable, pero las
  columnas de texto quedan en ~300px, la razón social del panel 02 se parte en
  4 líneas y los regímenes se envuelven.
- **Footer a 4 columnas a 900px:** el correo se parte en dos líneas
  (`fundacion@industrialesm` / `x.org`).

Mover cualquiera de los dos cambiaría el escritorio (900–980), que está aprobado
y recibe tráfico de campañas.

**El 980 del slider vive en dos lugares que se mueven juntos:**

- `site.css`: `@media (max-width:980px)` del slider
- `site.js`: `apilado()` → `matchMedia("(max-width: 980px)")`

Si se mueve uno sin el otro, el JS anima un slider que el CSS ya apiló, o al
revés, y el panel 02 (donde vive `click_donativo`) puede quedar recortado.

## Incoherencia conocida y aceptada

Entre 900 y 980px el slider está apilado pero `.split` va a dos columnas. Antes
de R12 esta franja iba de 861 a 980. Cerrarla del todo exigiría cambiar el
escritorio.

## Cómo verificar un cambio de breakpoints

Comparar los estilos computados contra el commit anterior en estos anchos:
375, 390, 520, 521, 560, 599, 600, 700, 759, 760, 860, 861, 880, 899, 900,
940, 980, 981 y 1440. Las diferencias deben caer solo en los rangos que se
querían mover. A 1440 la captura debe salir idéntica píxel por píxel.
