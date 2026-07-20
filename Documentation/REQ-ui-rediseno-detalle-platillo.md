# REQ — Rediseño del detalle de platillo (ingredientes, preparación, "cómo encaja")

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Platillos/Detalle.cshtml` (+ CSS; el punto 3 necesita un campo en el ViewModel `Detalle.cshtml.cs`). Reusar tokens `eii-*`. Diff antes de aplicar.
**Motivo (owner):** hoy ingredientes y preparación fluyen "como nota de blog" dentro de un solo card. Darle estructura.
**Reemplaza:** el REQ previo "¿Puedo comerlo? como link" — ese elemento ahora vive como pie de las mini-cards de ingrediente (abajo).

## 1. Ingredientes en mini-cards (visual: ver mockup revisado con el owner)
- **Título "Ingredientes" FUERA del card**, como encabezado de sección.
- Los ingredientes en una **grilla de mini-cards** (no viñetas). Responsive: **2 columnas en móvil, 3-4 en desktop** (`repeat(auto-fit, minmax(~160px, 1fr))`).
- Cada mini-card (`.eii-card` chico o clase nueva `.ingrediente-card`):
  - **Título = nombre del ingrediente**, es el **link** a `/Platillos/Ingrediente/@r.Slug` (siempre; la ficha existe aunque no tenga nota).
  - **Segundo renglón = cantidad** (`@r.Cantidad @r.Unidad`, muted). Si `r.EsAlGusto`, mostrar "al gusto" en vez de/junto a la cantidad.
  - **Pie (footer), separado por hairline:**
    - Si `r.TieneNota` → link discreto **"ⓘ ¿Puedo comerlo?"** (a la misma ficha).
    - Si `!r.TieneNota` → leyenda **muted y chica** "Aún no hay una ficha para este alimento" (no link, gris suave — que se lea como nota al pie, no como "falta esto").

## 2. Preparación en card aparte
- Título "Preparación" (fuera o dentro, consistente con Ingredientes) y el texto en **su propio `.eii-card`**, separado del de ingredientes.
- Conservar la línea "Fuente: …" al pie.

## 3. "¿Cómo encaja contigo?" — lista sin card (callout)
Hoy es un card con "Este platillo cumple tu perfil." (afirmación pelada). Cambiarlo a:
- **Sin card → callout suave de éxito** (tinte verde `--eii-success-soft`, ícono check), más ligero (patrón "callout para señales").
- **Enlistar el porqué:** las intolerancias declaradas del usuario que este platillo respeta, como chips/checks:
  > "No contiene lo que dijiste que no toleras: ✓ sin lácteos · ✓ sin picante · ✓ sin crudo"
- **Encuadre honesto obligatorio:** "no contiene lo que TÚ dijiste que no toleras" — **nunca** "es bueno para ti" (respeta "esto no es una dieta").
- **Cap:** si hay muchas exclusiones, mostrar ~4-5 chips y "…y N más" para no hacer un muro.
- **Caso sin exclusiones:** si el usuario no ha declarado nada que no tolera, mostrar un mensaje suave (ej. "Aún no marcaste alimentos que no toleras — cuando lo hagas, filtramos por ti") en vez de una lista vacía.

### Backend (pequeño)
El ViewModel (`Detalle.cshtml.cs`) debe **exponer la lista de exclusiones del usuario** (nombres) para poder enlistarlas — los datos ya se conocen al calcular el match, solo falta pasarlos a la vista. Esto es lógica + vista → **rebuild en VS** para esta parte (los puntos 1 y 2 son solo `.cshtml`/CSS, sin rebuild).

## Verificación
- Ingredientes en mini-cards (2 col móvil / 3-4 desktop), con nombre-link, cantidad, y pie que varía (¿Puedo comerlo? / leyenda "aún no hay ficha").
- Preparación en card separado.
- "¿Cómo encaja?" como callout verde con la lista honesta de exclusiones respetadas; cap y caso-sin-exclusiones cubiertos.
- Probar con: platillo que cumple con varias exclusiones, ingrediente con y sin nota, usuario sin exclusiones.
- Diff antes de aplicar; rebuild solo por el punto 3.
