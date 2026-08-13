# REQ — Notas de ingredientes/grupos: mostrar el comentario del "Profesional verificado" (igual que el glosario)

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`, un partial. NO tocar NINA ni Conectar3eros.
**Modo:** diff antes de aplicar; build `dotnet publish` (Razor) **Y** correr en Development y abrir la ficha de un ingrediente/grupo con validación (el publish no caza todos los errores de Razor — lección del 24 JUL).
**Objetivo:** en las notas de platillos/ingredientes, el bloque "Validado por Profesionales de la Salud" debe mostrar el **comentario** del validador anónimo ("Profesional verificado"), como YA lo hace el glosario. Hoy en ingredientes solo sale la etiqueta sin el comentario.

## Causa raíz (verificada)
- **Glosario** (`Pages/Glosario/Termino.cshtml`): valida anónimo → muestra "Profesional verificado" **+ el comentario** + avatar + fecha. ✅
- **Ingredientes** (`Pages/Platillos/_NotaSello.cshtml`): los validadores anónimos (`!v.TieneNombre`) se **resumen en UNA sola línea** "Profesional verificado" / "N profesionales verificados" (L63-80), **SIN el comentario**.
- El propio `_NotaSello` dice en su encabezado que debe ser *"espejo del glosario: Termino.cshtml"* — o sea la intención era que se vieran igual, pero divergió.

## Cambio
En `Pages/Platillos/_NotaSello.cshtml`, el bloque de anónimos (`anonimos`, ~L63-80):
- En vez de una sola línea de conteo sin comentario, **renderizar cada validador anónimo como una fila** (`validacion-item`), igual que las del bloque `conNombre` pero:
  - **Nombre → "Profesional verificado"** (texto fijo, sin link ni especialidad-como-nombre; el check `bi-patch-check-fill` se queda).
  - **Avatar por defecto** (el `validacion-avatar-placeholder` o `/img/default-avatar.png`).
  - **Su comentario** (`@v.Comentario`) — mismo render que L54-56 del bloque con nombre (solo si no está vacío).
  - **La fecha** (`@v.CreadoEn.ToString("MMMM yyyy")`).
- **Espejar la lógica del glosario** (`Termino.cshtml`, bloque "Validado por Profesionales de la Salud") para que se vean idénticos — revisar cómo Termino pinta el anónimo con comentario y replicarlo.
- **Opcional (limpieza):** los anónimos **sin** comentario pueden seguir resumiéndose en una línea de conteo ("+ N profesionales verificados") para no llenar de filas vacías; los que **sí** tienen comentario van como fila individual. Si es más simple pintar todos como fila, también sirve.

## Fuera de alcance
- No cambiar la **regla de identidad**: el **nombre** sigue apareciendo solo con badge `verificado`/`perfil_reclamado`; sin badge → "Profesional verificado". Solo se agrega el **comentario** al anónimo.
- No tocar el bloque `conNombre` (ya muestra comentario) ni el glosario.

## Verificación
1. En una nota de ingrediente/grupo validada por un profesional **sin badge**: aparece "Profesional verificado" **+ su comentario** + fecha (igual que en el glosario).
2. Con badge: sigue saliendo con nombre + comentario (sin cambios).
3. Se ve **consistente** entre glosario e ingredientes.
4. `dotnet publish -c Release` limpio **y** la ficha abre sin error de Razor en Development.
