# REQ — Completar los sidebars (menú izquierdo) por perfil

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web` (dos partials de sidebar). NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Objetivo:** que cada perfil (Médico, Admin, Paciente) vea **siempre** las opciones correctas en su menú izquierdo. Se mantienen las 2 versiones (decisión del usuario: "así está bien ahorita"); solo se **sincronizan y completan**.

## Arquitectura (verificada — no rehacer)
- Layout único `Pages/Shared/_Layout.cshtml` llama `Html.PartialAsync("Shared/_SidebarMenu")` sin ruta absoluta → Razor resuelve por área:
  - **Versión A:** `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` → **la de los paneles reales** (`/Identity/Admin/*`, `/Identity/Medico/*`, `/Identity/Usuario/*`). **La crítica.**
  - **Versión B:** `Pages/Shared/_SidebarMenu.cshtml` → páginas raíz autenticadas (ej. `/DirectorioMedicos`).
- Dentro de cada archivo, bloques `@if (User.IsInRole(...))` por perfil. Las dos copias divergieron.

## PRIORIDAD 1 — Bugs reales (paneles reales, Versión A)
Un perfil no llega a una página que **sí existe**, desde su propio menú:

1. **MÉDICO — falta "Mis P&R" en Versión A** (bloque Medico, ~L404-433).
   - Agregar el `<li>` que ya existe en Versión B (L309-317) → ruta `/Identity/Medico/MedicoPreguntasRespuestas`.
   - Ajustar el estado activo del bloque Medico en A a **coincidencia exacta** (`IsActive`) por ítem, en vez de `IsStartingWith("/Identity/Medico")`, para que el resaltado no marque todo activo al agregar el segundo ítem.

2. **ADMIN — falta submenú "Laboratorios" en Versión A** (bloque Admin).
   - Portar de Versión B (L131-152) los dos ítems: **Laboratorios → Catálogo** (`/Identity/Admin/Laboratorios/Index`) y **Resultados** (`/Identity/Admin/Laboratorios/Resultados`). Las páginas existen.

## PRIORIDAD 2 — Sincronizar para que sea correcto "siempre" (Versión B ← A)
Para que ambas versiones muestren el menú completo por perfil (el usuario pidió "siempre"):
- **Admin en Versión B: agregar lo que solo está en A** — Calidad de Contenido, Oportunidades de contenido, API Keys, la sección **Motor de Cobertura** (6 ítems: Firmas, Firmas de Externos, Embeddings, Similitud, Similitud por Embeddings, Cobertura de Temas) y la sección **Platillos** (8 ítems: Platillos, Nuevo platillo, Ingredientes, Grupos, Atributos, Categorías, Unidades, Estadísticas de tolerancia).
- **Paciente:** ya está sincronizado en ambas — no tocar.
- Resultado: Admin y Médico completos en las dos versiones; ningún perfil pierde opciones según en qué página esté.

## Decisión a confirmar (no adivinar)
- **Enlace "Contenidos" (Admin) apunta distinto** entre versiones: A → `/Identity/Admin/Contenidos/Contenidos`; B → `/Identity/Admin/Contenidos/Index`. Ambas páginas existen. **Confirmar cuál es la canónica** (cuál usa hoy el admin en vivo) y unificar las dos versiones a esa. NO cambiar la ruta sin confirmar (regla: rutas públicas / navegación en prod).

## Fuera de alcance
- **NO unificar los dos archivos en uno** ahora — el usuario mantiene las 2 versiones a propósito. (Anotar como mejora futura: unificar en un solo partial/view component por ruta absoluta para que no vuelvan a divergir — esa divergencia es la causa raíz de estas omisiones.)
- No tocar el bloque Paciente (ya correcto).
- No cambiar el filtrado por rol (funciona; el bloque paciente ya se oculta al admin).

## Verificación
1. **Médico** en `/Identity/Medico/Dashboard` → ve "Mi Dashboard", **"Mis P&R"** y "Ver mi perfil público". El resaltado activo es correcto por ítem.
2. **Admin** en su panel → ve el submenú **Laboratorios** (Catálogo + Resultados) y llega a ambas.
3. **Admin** en una página raíz con sidebar (Versión B) → ve el mismo menú completo que en su panel (Motor de Cobertura, Platillos, Calidad, Oportunidades, API Keys, Laboratorios).
4. **Paciente** → sin cambios, menú igual que antes.
5. Ningún ítem de un perfil aparece en otro (admin no ve bloque paciente y viceversa).
6. `dotnet publish -c Release` limpio antes del push.
