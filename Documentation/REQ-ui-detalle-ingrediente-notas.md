# REQ — Detalle de ingrediente: distinguir nota propia vs nota de grupo

**Fecha:** 17 JUL 2026
**Archivo:** `Pages/Platillos/Ingrediente.cshtml` (+ CSS; posiblemente el partial `_NotaClinica.cshtml` para las referencias). `.cshtml`/CSS, **sin rebuild**. Reusar tokens `eii-*`. Diff antes de aplicar.
**Motivo (owner):** la página muestra hasta dos notas (la propia del ingrediente + la del grupo) como cards blancos idénticos → parece artículo de blog. Hay que **distinguirlas sin colorido** y sacar las referencias como bloque aparte.
**Reemplaza:** `REQ-ui-detalle-ingrediente-espaciado.md` — al reestructurar estos cards, el hueco de arriba y el card pegado se resuelven de paso.

## Contexto (modelo)
En `Ingrediente.cshtml` se renderizan dos notas posibles (líneas ~99-114): la **nota propia del ingrediente** (`Model.IngredienteNota`) y la **nota del grupo** (`Model.GrupoNota`, hoy con un `<h2>En el grupo: {grupo}</h2>`). Pueden aparecer 0, 1 o 2. El `_NotaClinica` partial pinta secciones + referencias.

## Diseño v4 (mockup v4 aprobado por el owner — REEMPLAZA v1, v2 y v3)
El anidado ya falló **dos veces**. v4 cambia el enfoque para que sea **estructuralmente imposible**: la nota es UN card cuyos hijos son **filas**, no cajas.

### 0. LO CRÍTICO — la causa del anidado y su regla
El anidado se repite porque el markup **envuelve la nota en un card nuevo mientras `.contenido-html` conserva su propio estilo de caja** (background, border, radius, padding) → dos superficies.
**Regla tajante:** la nota se renderiza como **UN solo card**; sus hijos son **filas separadas por hairline**. En este contexto, `.contenido-html` (y cualquier wrapper interno) **NO debe llevar estilo de caja** — sin fondo, sin borde, sin radius, sin padding propio. Verificar en el render que solo hay **un** borde por nota.

### 1. Fila de datos rápidos (arriba) — YA IMPLEMENTADA ✅
Grupo + Atributos intrínsecos como mini-cards suaves, estilo stat cards del detalle de platillo. **Se queda tal cual.** Dejar hueco para un tercero: el **% de tolerancia de la comunidad** cuando `/tolero` esté vivo.

### 2. Estructura de cada nota — filas tipo FAQ
Las secciones de la nota **son preguntas** ("¿Qué son?", "¿Qué suele pasar?"), así que van como **lista pregunta/respuesta**, no como prosa con títulos. Eso es lo que mata el "post de blog": se escanea en vez de leerse.

UN card, con filas separadas por `border-top: 0.5px solid var(--eii-border)`:
- **Fila 1 = encabezado** (parte del card, NO un wrapper por fuera):
  - Nota propia → etiqueta chica "Sobre el {ingrediente}" (uppercase suave, muted).
  - Nota de grupo → **chip** "Aplica a todo el grupo · {grupo}" con ícono de jerarquía, sobre un fondo de fila levemente distinto (`surface-1`) para distinguirla.
- **Una fila por sección:** pregunta (**15px, peso 500, `--eii-text`**) + respuesta debajo (**15px, `line-height: 1.7`, `--eii-text-soft`**). Padding de fila ~14px 18px.
- **Última fila = Referencias**, compacta y en una línea: "Referencias · ESPEN 2023 · CCF". Sin sección con viñetas.

### 3. Jerarquía
Por **peso y color**, no por cajas: pregunta fuerte, respuesta en secundario. Radius 16px en el card, `overflow: hidden` para que las filas respeten la esquina.

### 3b. Ajustes v5 — encabezados (APROBADO por el owner; REEMPLAZA el tratamiento de encabezado de v4)
Sobre la estructura de filas de v4, aplicar estos 4 cambios:
1. **Sin íconos** en ninguno de los dos encabezados (fuera el de "Sobre el arroz" y el de jerarquía del grupo).
2. **Sin cápsula/chip** en el encabezado del grupo → **texto plano**.
3. **Ambos encabezados como título de card estándar**, igual que los demás cards del sitio ("Calificar", "Compartir", "Platillos que lo incluyen"): caja normal (**no uppercase**), tamaño y peso de título de card, color de heading, con el padding/divisor que ya usan. **Reusar la clase existente** (`.eii-card__title` o equivalente) — no inventar un estilo nuevo.
4. **Sin fondo gris** en el encabezado ni en el card de la nota de grupo → **mismo blanco** que la nota propia.

**Consecuencia asumida:** la nota de grupo ya no se distingue por chip, fondo ni ícono, sino **por el texto de su título** ("Aplica a todo el grupo: cereal" vs "Sobre el arroz"). Es suficiente y más limpio — **no** agregar otro distintivo para compensar.

**Se mantiene de v4:** una sola superficie por nota (sin caja interna), filas de sección separadas por hairline, y las referencias en una línea compacta al pie.

### 6. Realce de "Importante" — DECISIÓN DEL OWNER (leer antes de implementar)
En el mockup la sección "Importante" va destacada en un bloque ámbar suave, y se ve bien. **PERO** el propio `_NotaClinica.cshtml` documenta una decisión contraria: *"el realce por callout de secciones 'Importante'/'seguridad' se hará cuando el CRUD marque un TIPO de sección; hoy los títulos son inconsistentes y adivinar por texto sería frágil en contenido clínico."*
- **Opción A (recomendada):** **no** realzar por coincidencia de texto. Las secciones van uniformes; la mejora de tipografía y aire ya resuelve lo denso. Respeta la decisión documentada y evita destacar la sección equivocada en contenido médico.
- **Opción B:** agregar un campo **"tipo de sección"** al editor de notas (tarea chica aparte) y realzar **por dato**, no por texto. Es el camino limpio.
Implementar **Opción A** salvo que el owner diga lo contrario.

### General
- El callout azul "No es un alimento prohibido…" se queda como está (arriba, suelto).
- Plano, sin sombras fuertes. La jerarquía es por superficie + chip + los datos rápidos, no por saturar de color.

## Cuidado de scope
`_NotaClinica.cshtml` puede estar **compartido** (el comentario menciona "misma escala que Contenidos/Detalle"). Antes de restilar las referencias en el partial, **verificar dónde más se usa**; si se comparte, scopear los estilos nuevos al contenedor del ingrediente (una clase propia) para no afectar otras vistas. La banda de "Aplica al grupo" va en `Ingrediente.cshtml` (no en el partial).

## Verificación
- Con 0, 1 y 2 notas: la propia y la de grupo se ven **distintas** (la de grupo con su banda + borde), sin colorido.
- Referencias de cada nota, en bloque aparte secundario, sin card.
- Sin el hueco grande arriba ni el card siguiente pegado.
- Sin rebuild; diff antes de aplicar.
