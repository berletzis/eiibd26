# REQ — Pulido UI de la encuesta `/tolero/{slug}`

**Fecha:** 17 JUL 2026
**Archivo:** `eiibd26/Pages/Tolero/Encuesta.cshtml` (+ su CSS). **Solo presentación** — NO tocar la lógica del PageModel ni el comportamiento (voto, dedup, Laplace, anti-prefetch, CTA). Es `.cshtml` + CSS → sin rebuild, iteración por refresh.
**Mobile-first:** el tráfico viene de Facebook/PWA, casi todo móvil. Probar primero en ancho de teléfono.
**Regla del repo:** reusar los tokens `eii-*` existentes, NO inventar un sistema nuevo (la fragmentación de CSS es deuda conocida).

## Problema
Hoy el contenido va de borde a borde (se ve "suelto" en desktop) y los votos son texto plano. Se pidió: columna centrada y angosta + más grande, bonito y llamativo, responsive, con márgenes.

## Cambio 1 — Columna centrada con ancho máximo (estructural)
- Envolver TODO el contenido de la página en un contenedor centrado: `max-width` ~560px, `margin: 0 auto`, padding lateral (~16px) para que respire en móvil.
- Márgenes verticales consistentes entre secciones (~1rem–1.5rem).
- Responsive: en móvil el contenedor toma el ancho completo menos el padding; en desktop se queda centrado a ~560px (deja de irse a los bordes). Aplica a los DOS estados (votación y resultado).

## Cambio 2 — Más grande, bonito y llamativo (visual)
- **Título** "¿Toleras el {ingrediente}?": más grande y con peso (es el hook). Subtítulo chico y muted debajo.
- **Los 3 botones de voto** (estado de votación): botones grandes, de ancho completo dentro de la columna, con color por semántica y buen área de toque (alto ≥48px, radius 12px, icono + texto):
  - Sí → verde (`--bg-success` / `--text-success` / `--border-success`)
  - A veces → ámbar (`--bg-warning` / `--text-warning` / `--border-warning`)
  - No → rojo (`--bg-danger` / `--text-danger` / `--border-danger`)
  - (Si hay tokens `eii-*` equivalentes, usar esos; los de arriba son la intención de color.)
- **El resultado de la comunidad** (estado post-voto): convertirlo en el "premio" visual:
  - Número grande del %: "**64%** lo tolera bien" (el número ~32–36px).
  - Una **barra de proporción horizontal** Sí/A veces/No en los mismos colores (verde/ámbar/rojo), con el desglose (Sí X% · A veces Y% · No Z%) y la `n` debajo.
  - Mantener el guard: si `n < 10`, en vez de la barra va "Aún no hay suficientes respuestas — sé de los primeros".
- **"Tu respuesta / ¿Cambiar?"**: los mini-botones de cambio con los mismos colores, en versión chica.
- **CTA** ("Crear mi perfil" / "Lo que no tolero"): botón prominente, ancho completo.
- **Disclaimer "Esto no es una dieta"**: se mantiene, dentro de la columna centrada, tinte morado.
- Subir el tamaño del texto de cuerpo un punto para legibilidad móvil.

## Cambio 3 — Botón "Iniciar sesión" junto a "Crear mi perfil"
En la tarjeta "Lleva tu propio registro" (la que ve el **anónimo**), hoy solo está "Crear mi perfil". Agregar **al lado** un botón "Iniciar sesión" — para el usuario que YA tiene cuenta y no debería tener que registrarse de nuevo.
- **"Crear mi perfil"** = botón **primario** (relleno morado, como está), con su ícono actual.
- **"Iniciar sesión"** = botón **secundario/ghost**, con el MISMO estándar de colores, estilo e ícono que el "Iniciar sesión" del menú superior (`_TopMenu` / `_SidebarMenu`). **Reusar la convención existente, no inventar** un estilo nuevo.
- Links: Crear mi perfil → registro (como está); Iniciar sesión → `/Identity/Account/Login?returnUrl=/tolero/{slug}` (con el slug actual) para que vuelva a la misma encuesta tras entrar.
- Layout: los dos botones **lado a lado en desktop, apilados en móvil** (responsive), misma altura y familia de estilo. Primario destacado, secundario más discreto.
- Solo aparece para el anónimo (el logueado no ve esta tarjeta).

> Nota (fuera de alcance, futura): el voto anónimo queda por cookie; al iniciar sesión NO se migra automáticamente al usuario. Migrar el voto anón→usuario al loguear es un plus para después, no de este cambio.

## Fix chico de paso
- Pluralización: hoy dice "n = 1 **respuestas**". Singular cuando `n == 1` → "1 respuesta". (`respuesta`/`respuestas` según el conteo.)

## Referencia visual
La dirección aprobada: columna angosta centrada, botones de voto grandes a color (verde/ámbar/rojo con icono), y el resultado como número grande + barra de proporción. (Mockup revisado con el owner el 17 JUL.)

## Aceptación
- En móvil: columna a ancho completo menos padding, botones grandes tappables, todo legible sin zoom.
- En desktop: contenido centrado a ~560px, no de borde a borde.
- Votación y resultado, ambos con el tratamiento.
- Comportamiento idéntico (nada de lógica tocada); solo markup/CSS con tokens `eii-*`.
- Sin rebuild (es `.cshtml`+CSS); diff antes de aplicar.
