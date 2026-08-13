# REQ — Alerta visible al terminar "Aplicar desactivación a Basura"

**Scope:** solo `eiibd26.Web` — `Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml` (JS del handler `#btnAplicarBasura` + un elemento de alerta en la vista). **NO** requiere cambio de backend: el endpoint `POST /api/admin/tratamientos/batch-apply-basura` ya devuelve `{ ok, desactivados, bloqueados, bloqueadosDetalle }`. NO tocar NINA-WorkerService.
**Ejecución (AUTORIZADO por Berletzis):** aplica el cambio de UI, build + `publish -c Release`, muestra diff al final. No cambia ninguna lógica de negocio ni de datos.

## Motivo
Al terminar de aplicar la desactivación de Basura, **no aparece ninguna alerta visible**. El único feedback es una línea en el log de NINA:

```js
agregarLogNina('success', `🗑️ Aplicado: ${data.desactivados} desactivados, ${data.bloqueados} bloqueados por guard.`);
```

En lotes grandes (ej. Medicina Alternativa = 84) esa línea se pierde en el scroll del log y Berletzis no confirma si terminó, si falló, o cuántos se desactivaron. Necesitamos un aviso claro e imposible de perder.

## Cambio
En el `success` del `$.ajax` de `#btnAplicarBasura` (hoy solo llama `agregarLogNina`), además de mantener el log, **mostrar una alerta prominente** con el resumen:

- **Éxito:** banner/toast verde — `✅ Desactivación completada: {desactivados} desactivados · {bloqueados} bloqueados por guard · rama "{categoria}".`
- **Nada que aplicar:** banner amarillo — `Nada que aplicar: 0 desactivables ({bloqueados} bloqueados por guard).` (hoy solo va al log en la rama `prev.aDesactivar === 0`).
- **Error:** banner rojo — `❌ {data.error}` (o error de red).
- Si `data.desactivados !== prev.aDesactivar`, incluir el aviso de discrepancia en la alerta, no solo en el log.

### Implementación sugerida (mínima, sin librerías nuevas)
1. Agregar un contenedor de alerta reutilizable cerca del botón (junto a `#basuraRamaInfo` / `#avisoDryRunOff`), oculto por defecto:
   ```html
   <div class="alert d-none mt-3" id="avisoAplicarBasura" role="alert" aria-live="assertive"></div>
   ```
2. Helper JS que setea clase (`alert-success` / `alert-warning` / `alert-danger`), texto e ícono, quita `d-none`, y hace `scrollIntoView({ behavior: 'smooth', block: 'center' })` para traerla a la vista.
3. Llamarlo en los tres desenlaces del handler (éxito, nada-que-aplicar, error). Mantener también las líneas de `agregarLogNina` existentes (el log queda como historial).
4. Opcional: auto-ocultar el banner de éxito tras ~15 s; el de error y el de discrepancia se quedan hasta que el usuario navegue o dispare otra acción.

**No** cambiar el `confirm(...)` previo (la confirmación destructiva se queda igual) ni el `basura-preview` que lo alimenta.

## Alcance del efecto
- Puramente visual/UX en el panel admin de Tratamientos. No altera qué se desactiva, ni los guards, ni la propagación a `GlossaryTerm.Activo`.

## Verificación
1. Aplicar Basura en una rama con desactivables → aparece banner **verde** con los conteos y hace scroll a la vista; la línea del log sigue estando.
2. Aplicar en una rama con 0 desactivables (todo bloqueado por guard) → banner **amarillo**.
3. Forzar un error (ej. red caída) → banner **rojo**.
4. `dotnet publish -c Release` limpio y la página abre sin error de Razor/JS.
