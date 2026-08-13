# REQ — Precaución de seguridad: un solo mensaje (leyenda ámbar como voz única)

**Fecha:** 06 AGO 2026
**Scope:** solo `eiibd26.Web` — `Pages/Platillos/Ingrediente.cshtml` (bloque Precaución, ~L148–178). NO tocar servicios, DTOs, queries ni NINA/Conectar3eros. Cambio **solo de vista** (qué se pinta), no de datos.
**Decisión (Berletzis):** **Opción A — la leyenda ámbar de seguridad es la voz única en Precaución.** El sello (`_NotaSello`) NO se pinta en su estado vacío para Precaución; sí se pinta cuando hay validación (para mostrar al validador). Motivo: Precaución es eje **seguridad**, merece un tono más fuerte que el estado vacío neutro; el "sello como fuente única" aplica a las notas de tolerancia, no aquí.

**Ejecución (autorizado):** aplica directo, **sin pedir confirmación de permisos** — edita, siembra el dato temporal de prueba, corre build y verificación, y limpia. Muestra el diff al final. Scope acotado a `Ingrediente.cshtml`.

## Causa raíz (verificada en código)
En `Ingrediente.cshtml`, bloque `@if (Model.PrecaucionNota != null)` (~L148–178), la nota **sin validar** pinta **dos mensajes seguidos que dicen lo mismo**:
1. **Leyenda ámbar** (L166–175, `nota-legend--precaucion`): "Este aviso de seguridad todavía no lo ha validado un profesional… es orientativo; coméntalo con tu médico o nutriólogo."
2. **Sello** (L178, `_NotaSello` con `Model.ValidadoresPrecaucion`): que ahora **siempre** se pinta, incluido su estado vacío neutro ("Validación de profesionales de la salud → Este contenido aún no ha sido validado…").

En el lado **validado** hay un doble menor: la leyenda "respaldada" (L161–164) repite lo que el sello ya dice con nombre y foto.

Hoy es **hipotético en prod** (0 notas tipo Precaución; las 78 publicadas son Tolerancia), pero se activa en cuanto se publique una Precaución sin validar. Se resuelve ahora y se verifica con dato real temporal.

## Cambio (una sola voz por estado)
En el bloque Precaución de `Ingrediente.cshtml`:

1. **Quitar** la rama "respaldada" inline (L159–165, el `@if (Model.ValidadoresPrecaucion.Count >= 1) { nota-legend--respaldada }`). Cuando hay validación, el **sello** ya muestra al validador (nombre/foto/check); la frase inline sobra.

2. **La leyenda ámbar** (`nota-legend--precaucion`, exclamation-triangle) se pinta **solo cuando NO hay validación**:
   ```razor
   @if (Model.ValidadoresPrecaucion.Count == 0)
   {
       <div class="nota-row nota-legend nota-legend--precaucion">
           <i class="bi bi-exclamation-triangle nota-legend__ico" aria-hidden="true"></i>
           <p>
               Este aviso de seguridad <strong>todavía no lo ha validado un profesional de la salud</strong>.
               Es orientativo; coméntalo con tu médico o nutriólogo.
           </p>
       </div>
   }
   ```

3. **El sello** (`_NotaSello`) se pinta **solo cuando SÍ hay validación** (para no repetir la ámbar con el estado vacío):
   ```razor
   @if (Model.ValidadoresPrecaucion.Count >= 1)
   {
       <partial name="_NotaSello" model="Model.ValidadoresPrecaucion" />
   }
   ```
   Es decir: el `<partial name="_NotaSello" ... />` de L178 pasa de incondicional a gateado por `Count >= 1`.

Resultado:
- **Precaución sin validar** → **solo** la leyenda ámbar (un mensaje).
- **Precaución validada** → **solo** el sello con el validador (un mensaje, y dice quién).

## Alcance de la excepción (dejar claro)
- Esta es una **excepción deliberada y acotada a Precaución**: el sello aquí no muestra su estado vacío. En las notas de **ingrediente/grupo** el sello sigue igual (voz única de validación con su estado vacío). No generalizar.
- No tocar `_NotaSello.cshtml`, `_NotaSinReferencias.cshtml` ni `_NotaClinica.cshtml`. El cambio vive en el bloque de `Ingrediente.cshtml`.

## Verificación — NO hipotética (dato real temporal, luego limpieza)
Sembrar una nota de Precaución de prueba en la BD del app (misma convención que el caso 4: marcar **TEMPORAL-CLAUDE** en un campo de texto para poder localizarla y borrarla por Id), atada a un ingrediente de un grupo de riesgo para que `Ingrediente.cshtml` la renderice.

1. **Sin validar** → la ficha del ingrediente muestra el card ámbar "Precaución de seguridad" + **solo** la leyenda ámbar. **No** aparece el sello ni su estado vacío. (Antes salían los dos.)
2. **Validada** (insertar temporalmente ≥1 validador para esa nota) → card ámbar + **sello con el validador** (nombre/foto/check). **No** aparece la leyenda ámbar ni la frase "respaldada" inline.
3. **Caso mixto en una misma página** (ingrediente con Precaución sin validar + otra nota validada) → cada bloque con su voz, sin cruzarse.
4. **Limpieza:** borrar la nota y el validador temporales por Id; confirmar por query **0 residuos** (buscar TEMPORAL-CLAUDE → 0 filas). No tocar ningún otro dato de prod.
5. `dotnet publish -c Release` limpio **y** `Ingrediente.cshtml` abre sin error de Razor en Development.

## Nota de seguimiento (tarea #26)
Cierra el pendiente "Precaución · duplicado de leyenda": deja de ser hipotético una vez verificado con el dato temporal.
