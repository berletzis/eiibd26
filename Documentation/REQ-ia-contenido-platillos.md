# REQ — Servicio de IA para contenido del módulo Platillos (+ botones en el admin)

**Fecha:** 21 JUL 2026
**Objetivo:** generar con IA el contenido del módulo Platillos (notas clínicas de grupo/ingrediente, `NotasEII`, descripciones de atributos), con un **botón "Generar con IA"** en cada editor del admin. Hermano del servicio de síntomas ya existente — **mismo patrón, no una copia**.
**Reglas del repo:** Razor Pages (`string?`/`int?`), sin migraciones EF (SQL directo si hay esquema), solo proyecto web, diff antes de aplicar, rebuild en VS por los `.cs`.

---

## 📋 ESTADO E INSTRUCCIÓN — LEER PRIMERO

### Ya implementado (NO rehacer — solo verificar que lo nuevo encaja)
El servicio `PlatillosAiService` + interfaz, los 5 métodos (nota grupo, nota ingrediente, `NotasEII`, descripción atributo, descripción categoría), el controller `[Authorize(Administrador)]` que **solo genera, nunca guarda ni publica**, la lista blanca de fuentes (`FuentesClinicasPermitidas` en appsettings + `FiltrarPorListaBlanca` en backend), los botones `.btn-generar-ia` en los 5 editores (rellenan sin guardar, confirman antes de sobrescribir), y la columna `PlatCategoria.Descripcion`. Base + Anexo 1.

### Lo EXTRA a implementar en esta pasada — los 4 anexos nuevos
- **Anexo 2** — leyenda honesta al paciente cuando una nota NO tiene referencias, que **cambia según el estado** (validada por médico → tranquilizadora; sin validar → cauta). Nunca fija. + regla editorial de cuándo la referencia es obligatoria.
- **Anexo 3** — DOS EJES en el prompt clínico (tolerancia digestiva **+** seguridad por infección en inmunosuprimidos) + regla dura de coherencia cita-cuerpo (prohibido citar lo que el cuerpo no menciona) + chequeo backend de cita huérfana.
- **Anexo 4** — que cada nota deje un dato accionable (FACTOR + PATRÓN o cómo PROBARLO), no cerrar en "depende de tu tolerancia". Solo prompt.
- **Anexo 5** — nota de **precaución de seguridad** como una NOTA IA más (reusa `PlatNotaClinica` + el candado, **sin texto fijo**), atada al grupo de riesgo por un flag, con **validación médica obligatoria** antes de publicar.

### Instrucción para Claude Code
1. **Lee el REQ completo primero** — incluida la base ya hecha — y confirma que los 4 anexos **encajan** con lo que existe (prompt actual, modelo de notas, candado, flujo de validación).
2. Implementa **SOLO los Anexos 2-5.** No toques lo ya hecho salvo para integrarlo.
3. **Si algo te parece raro, riesgoso o inconsistente** —clínico, técnico o de diseño— **PÁRATE y avísanos ANTES de aplicar.** No lo resuelvas por tu cuenta. En especial el **Anexo 5** (contenido de seguridad), que es lo más delicado del módulo.
4. **Propón, no asumas:** el mecanismo del **flag de riesgo** del Anexo 5 (¿reusar el catálogo de Atributos con un ámbito nuevo, o campo/tabla aparte?) y la **marca de tipo** para renderizar la precaución distinta — descríbelos en el diff antes de escribir.
5. Diff antes de aplicar; build limpio entre fases; rebuild en VS por los `.cs`. Nada se publica solo (candado `Publicado=0` intacto).

---

## Base a reusar (ya existe y funciona)
- `Services/AI/SintomasTratamientosAiService.cs` — el patrón de prompt: rol, nivel de lectura, prohibiciones, estructura exacta, salida parseable, y el manejo anti-alucinación de fuentes (*"si NO estás seguro, NO inventes fuentes"* + el código que **nunca** rellena fuentes falsas).
- El botón: `.btn-generar-ia` / `#btnGenerarIA` en `Admin/Sintomas/Index.cshtml` (botón línea ~360, JS ~869) contra `Controllers/SintomasAdminController.cs`. **Reusar ese estilo y comportamiento.**

## Campos destino (verificados en los modelos)
| Entidad | Campo |
|---|---|
| `PlatGrupo` | `NotasEII` + nota clínica (`PlatNotaClinica`, TipoDestino='Grupo') |
| `PlatIngrediente` | `NotasEII` + nota clínica (TipoDestino='Ingrediente') |
| `PlatAtributo` | `Descripcion` (ya trae `Ambito`: 'Ingrediente' \| 'Uso') |
| `PlatCategoria` | **no tiene campo de descripción** → ver "Decisión pendiente" |

## 1. Servicio nuevo — `Services/AI/PlatillosAiService.cs` (+ interfaz)
Registrar en `Program.cs` junto al de síntomas (~línea 344). Métodos:

1. `GenerarNotaClinicaGrupoAsync(nombreGrupo, ingredientesDelGrupo)` → nota estructurada + referencias.
2. `GenerarNotaClinicaIngredienteAsync(nombreIngrediente, grupo, atributos)` → ídem.
3. `GenerarNotasEIIAsync(tipo, nombre, contexto)` → el texto corto de `NotasEII`.
4. `GenerarDescripcionAtributoAsync(nombreAtributo, ambito)` → la `Descripcion`.

## 2. DOS niveles de riesgo — no mezclar en un mismo prompt
**Esto es lo más importante del REQ.**

### A. Contenido CLÍNICO (notas de grupo/ingrediente, `NotasEII`) — reglas estrictas
Hereda todo del prompt de síntomas, más tres candados:
- **Lista blanca de fuentes.** Se le entregan al modelo las fuentes válidas (ESPEN 2023, Crohn's & Colitis Foundation, y las que el owner apruebe). **Prohibido citar cualquier cosa fuera de esa lista.** Si ninguna aplica → sin referencias y marcar la nota para revisión prioritaria. *(Precedente real: al citar la fuente sobre carne roja, la evidencia contradijo lo que se iba a escribir. Sin este candado, el modelo produce citas plausibles que no existen.)*
- **Nunca prescribir.** Prohibido "deberías evitar X", "es bueno para ti", "esta dieta funciona". Siempre observacional: *"algunas personas reportan"*, *"suele tolerarse"*. Sin *"causa / provoca / debido a"*.
- **Siempre borrador.** Lo generado entra con `Publicado = 0`. El botón **nunca** publica. El candado ya lo garantiza; que el flujo tampoco lo intente.

Estructura exacta de la nota (la que ya usan las notas existentes):
```
¿Qué es? / ¿Qué son?      → 1-2 frases
¿Qué suele pasar?          → observacional; en brote vs remisión si aplica
Importante                 → el matiz / el costo de excluir
Referencias                → SOLO de la lista blanca, o ninguna
```

### B. Contenido TAXONÓMICO (descripción de atributo) — reglas ligeras
Explica qué significa una etiqueta ("picante", "crudo"), **no** hace afirmaciones clínicas. Riesgo bajo:
- Sin bibliografía, sin validación médica, 1-2 frases.
- Debe respetar el `Ambito`: si es 'Uso', se describe como **cómo se prepara** (no como propiedad del alimento).

## 3. Botones en el admin (reusar `.btn-generar-ia`)
Ubicaciones:
- `Admin/Platillos/GrupoDetalle` → junto a `NotasEII`.
- `Admin/Platillos/IngredienteDetalle` → junto a `NotasEII`.
- `Admin/Platillos/NotaClinicaDetalle` → "Generar borrador con IA" (rellena título + secciones + referencias).
- `Admin/Platillos/Atributos` (o su detalle) → junto a `Descripcion`.

**Comportamiento obligatorio del botón:**
- **Rellena el formulario, NO guarda.** El humano revisa y guarda — igual que en síntomas.
- Si el campo **ya tiene contenido**, pedir confirmación antes de sobrescribir.
- Deshabilitar durante la llamada y mostrar estado (el CSS `.btn-generar-ia:disabled` ya existe).
- Errores de la API: mensaje claro, nunca dejar el form a medias sin avisar.

## 4. Decisión pendiente — Categoría
`PlatCategoria` **no tiene campo de descripción**. Dos caminos:
- **(a) No generar nada para categoría** — no hay dónde ponerlo. *(Recomendado si la descripción no se va a mostrar al paciente.)*
- **(b) Agregar `Descripcion` por SQL directo** y su botón. Solo si de verdad se va a usar en la UI; si no, es campo muerto.
**Preguntar al owner antes de implementar.**

## Verificación
- Botón en cada editor: genera, **rellena sin guardar**, pide confirmación si había contenido.
- Nota generada entra como **borrador**, nunca publicada.
- **Ninguna referencia fuera de la lista blanca.** Probar con un ingrediente sin fuente aplicable → debe quedar sin referencias, no inventarlas.
- Descripción de atributo respeta el `Ambito`.
- Diff antes de aplicar; rebuild en VS.

---

# ANEXO — Las solicitudes concretas (prompts, mismo estilo que síntomas)

> Dos placeholders los llena el owner antes de usar: **`{LISTA_BLANCA}`** (fuentes válidas) y el **set de atributos de uso** (crudo / cocido / asado / frito — con capeado dentro de frito).

## A) SYSTEM PROMPT — contenido CLÍNICO (grupo e ingrediente)
```
Actúa como redactor de contenido de salud para pacientes con Enfermedad Inflamatoria
Intestinal (EII: Crohn y Colitis Ulcerosa). NO como médico ni como enciclopedia clínica.

Objetivo: ayudar a un paciente a entender qué suele pasar con un alimento en EII, en
lenguaje sencillo, SIN recomendar ni prohibir.

Reglas obligatorias:
• Lenguaje claro y cotidiano (nivel lectura 6-8 grado).
• NUNCA prescribas. Prohibido "deberías evitar", "es bueno/malo para ti", "esta dieta
  funciona". Un alimento no es "bueno" ni "malo".
• Lenguaje observacional: "suele tolerarse", "algunas personas reportan", "en brote puede
  sentirse más pesado". NUNCA "causa", "provoca", "debido a".
• Distingue brote vs remisión cuando aplique.
• Recuerda que esto NO es una dieta: la plataforma no conoce la enfermedad, el tratamiento
  ni el momento del paciente. Su dieta la diseña con su médico o nutriólogo.
• NO expliques mecanismos biológicos. NO menciones tratamientos ni fármacos. Sin porcentajes.
• Cuerpo máximo ~140 palabras. Responde en TEXTO PLANO, sin markdown (sin **, *, #).

FUENTES — regla crítica:
Solo puedes citar fuentes de esta lista blanca:
{LISTA_BLANCA}
PROHIBIDO citar cualquier fuente fuera de esa lista. Si ninguna aplica, NO cites nada.
NUNCA inventes una fuente ni cites de memoria.
```

## B) USER PROMPT — nota de GRUPO
```
Genera el contenido para el GRUPO de alimentos: {nombreGrupo}
Ingredientes de este grupo: {listaIngredientes}

Al final, en líneas separadas, texto plano:
TITULO: [ej. ¿Puedo comer {nombreGrupo}?]
QUE_ES: [1-2 frases]
QUE_SUELE_PASAR: [observacional; brote vs remisión si aplica]
IMPORTANTE: [el matiz honesto: el costo de excluir el grupo entero]
FUENTES: [solo de la lista blanca, separadas por ';', o 'Ninguna']
REVISION_PRIORITARIA: [SÍ si el tema es sensible y no hubo fuente aplicable; si no, NO]
```

## C) USER PROMPT — nota de INGREDIENTE
```
Genera el contenido para el INGREDIENTE: {nombreIngrediente}
Grupo al que pertenece: {nombreGrupo}
Atributos intrínsecos: {listaAtributos}   (ej. gluten, cítrico, picante)

Escribe SOLO lo específico de este ingrediente que NO esté ya cubierto por la nota de su
grupo (para no repetir). Si no hay nada específico que agregar, dilo en QUE_SUELE_PASAR.

Misma salida que el grupo: TITULO / QUE_ES / QUE_SUELE_PASAR / IMPORTANTE / FUENTES /
REVISION_PRIORITARIA.
```

## D) USER PROMPT — `NotasEII` (texto corto, no la nota completa)
```
En UNA sola frase de máximo 120 caracteres, observacional y sin prescribir, resume qué
conviene tener en cuenta de {tipo} "{nombre}" en EII. Texto plano. Sin fuentes.
Solo la frase, nada más.
```

## E) Contenido TAXONÓMICO — descripción de ATRIBUTO (prompt aparte, ligero)
SYSTEM:
```
Explica en 1-2 frases, lenguaje simple, qué significa una etiqueta de alimento. NO es
contenido clínico: no afirmas si algo se tolera o no, no citas fuentes. Solo defines el
término. Texto plano.
```
USER:
```
Define la etiqueta: {nombreAtributo}
Ámbito: {ambito}   (Ingrediente = propiedad del alimento; Uso = cómo se prepara)
Si el ámbito es 'Uso', descríbelo como una FORMA DE PREPARACIÓN (ej. "asado: cocido al
calor seco, con poca o nada de grasa añadida"), no como propiedad del alimento.
Solo la definición.
```

---

# ANEXO 2 — Cuándo una nota exige referencia + leyenda honesta al paciente

## Regla editorial (para el admin/becario, y para el prompt)
**Entre más fuerte o contraintuitivo sea lo que afirma la nota, más obligatoria es la referencia.**
- **Nota que desafía una creencia común** (ej. "dejar el gluten sin necesidad", "la carne roja no se asocia a recaídas") → **referencia OBLIGATORIA** de la lista blanca. Sin cita, no se publica; se queda en borrador para revisión.
- **Nota suave / de sentido común** (ej. "el aceite depende de la cantidad, no del tipo") → **referencia opcional**. Se sostiene con la **validación médica** (el sello de un profesional), que en este módulo es una señal *aparte* de las referencias.

Nunca forzar una cita para justificar algo que ya es sensato — eso reintroduce el problema que el candado de lista blanca evita.

## Leyenda al paciente cuando una nota NO tiene referencias
En la página pública del ingrediente/grupo, si la nota no trae bibliografía, mostrar una leyenda breve y **honesta según el estado real** — NO una leyenda fija. Tres estados:

| Estado de la nota | Qué mostrar |
|---|---|
| **Sin referencias + validada por ≥1 médico** | "Sobre este punto no hay una cita puntual, pero un profesional de la salud lo respalda. En EII no todo tiene un estudio específico; parte es criterio clínico." *(tono: tranquilizador)* |
| **Sin referencias + sin validación médica** | "Este contenido es orientativo y aún no tiene respaldo con fuente ni validación de un profesional. Tómalo como punto de partida para hablar con tu médico o nutriólogo." *(tono: cauto, honesto — NO afirmar que es válido)* |
| **Con referencias** | (no aplica esta leyenda — se muestran las referencias normales) |

**Crítico:** la leyenda **depende del estado** (`tiene referencias` × `tiene validación médica`). Está PROHIBIDO mostrar "esto es válido aunque no tenga referencia" cuando la nota no está validada — eso es la falsa autoridad que el módulo entero evita. El dato de validación ya existe (el conteo de validadores por nota); reusarlo para elegir la leyenda.

**Encuadre común (siempre, en cualquier estado):** sigue aplicando el "esto no es una dieta / tu dieta la diseñas con tu médico".

---

# ANEXO 3 — Dos ejes (tolerancia + seguridad) y coherencia cita-cuerpo

Añadir al **SYSTEM PROMPT clínico** (Anexo A). Origen: la nota del camarón describía solo digestión pero citaba "Listeria en inmunocomprometidos" — el eje de seguridad faltaba y la cita quedó huérfana.

**Bloque a agregar al system prompt:**
```
DOS EJES DE PREOCUPACIÓN — considéralos ambos, no solo la digestión:
1. TOLERANCIA digestiva: si suele sentar pesado, la fibra, la grasa, brote vs remisión.
2. SEGURIDAD por infección: muchas personas con EII están en tratamiento que baja las
   defensas (inmunosupresores, esteroides, biológicos). Para ellas, algunos alimentos
   —mariscos y pescado crudos o poco cocidos, huevo crudo, lácteos sin pasteurizar,
   embutidos, germinados— pueden traer bacterias (Listeria, Salmonella) que ahí importan
   más que la digestión.

Si el alimento tiene un riesgo de SEGURIDAD relevante, MENCIÓNALO — observacional, ligado a
la PREPARACIÓN (crudo/poco cocido vs bien cocido) y a las defensas bajas. Ejemplo del tono:
"bien cocido suele estar bien; crudo o poco cocido puede traer bacterias, y si estás con
medicamentos que bajan tus defensas conviene comentarlo con tu médico". NUNCA como orden;
siempre deferir al médico. Si NO hay riesgo de seguridad, no lo inventes.

COHERENCIA CITA-CUERPO (regla dura): si citas una fuente, el CUERPO de la nota DEBE hablar de
ese punto. Prohibido citar algo que el texto no menciona (ej. citar 'Listeria en
inmunocomprometidos' sin decir nada de infección en el cuerpo). La referencia respalda lo que
escribiste, no un punto que omitiste.
```

**Validación de coherencia en el backend (además del prompt):** si la nota trae referencias pero el cuerpo no toca el eje de seguridad/infección, marcar `REVISION_PRIORITARIA` — es señal de cita huérfana. Barato de chequear y evita el caso del camarón.

**Consecuencia de riesgo:** las notas que activan el eje de SEGURIDAD son afirmaciones médicas más fuertes → entran en la regla del Anexo 2 (referencia de lista blanca **obligatoria** + validación médica antes de publicar). No se publican solas.

---

# ANEXO 4 — Dar algo útil, no solo "depende de tu tolerancia"

Añadir al **SYSTEM PROMPT clínico**. Problema: de tanto evitar prescribir, las notas caen todas en "depende de cada persona" — cierto, pero el paciente ya lo sabe y viene por más. **"No prescribir" ≠ "no ayudar".**

**Bloque a agregar:**
```
DA ALGO ÚTIL — no cierres siempre en "depende de tu tolerancia".
Es cierto, pero insuficiente: el paciente ya lo sabe. Cada nota DEBE dejar al menos UN dato
concreto y accionable, sin dejar de ser observacional. La diferencia es informar un PATRÓN que
el paciente decide, NO dictarle un veredicto:

• Nombra el FACTOR que más cambia, no "la tolerancia" en abstracto:
  ✗ "depende de tu tolerancia"
  ✓ "lo que más cambia aquí es la grasa: frito pesa más que asado"
  ✓ "lo que más cuesta es la fibra de la cáscara; pelado cae más suave"

• Da el AJUSTE de la mayoría como PATRÓN, no como orden:
  ✗ "debes comerlo pelado"            (orden → prohibido)
  ✓ "muchos lo toleran mejor pelado, bien cocido y en porción chica"   (patrón → el paciente decide)

• Brote vs remisión con concreto:
  ✓ "en brote muchos lo dejan bien cocido; en remisión suele volver sin problema"

• Cuando aplique, di CÓMO PROBARLO (empodera y es 100% seguro):
  ✓ "si quieres probarlo, empieza con poco y bien cocido, y fíjate cómo te sientes en las horas siguientes"

Regla: cada nota entrega al menos un FACTOR + un PATRÓN o una forma de PROBARLO. Sigue prohibido
prescribir, prometer resultados o decir "bueno/malo para ti" — pero eso NO te exime de ser útil.
```

**Enriquecer la sección "¿Qué suele pasar?"** para que cargue el factor + el patrón (no el hedge), y **"Importante"** para el cómo-probarlo / qué observar. (No agregar sección nueva; la estructura de 3 ya alcanza.)

---

# ANEXO 5 — "Advertencia de seguridad alimentaria" como bloque aparte

Contenido nuevo, **distinto** de la nota de tolerancia: para alimentos con riesgo de infección/parásito cuando el paciente está inmunosuprimido (marisco, cerdo/carne poco cocida, lácteo sin pasteurizar, huevo crudo, embutido, germinados).

## Cómo se muestra
Un bloque propio en la página del ingrediente/grupo, **visualmente marcado como precaución** (callout ámbar, ícono), separado de la nota de tolerancia. El modelo ya permite varias secciones/notas, así que cabe sin cambiar el esquema (una sección tipo "Precaución" o una nota aparte con su marca).

## Dos candados de precisión (evitan los dos errores)
1. **Siempre CONDICIONAL a inmunosupresión** — nunca asumir que el paciente lo está. La mesalazina NO baja las defensas; inmunosupresores/biológicos/esteroides sí. Redacción: *"si tu tratamiento baja tus defensas (no todos lo hacen)…"*.
2. **Atada a la PREPARACIÓN, no al alimento** — el riesgo es crudo/poco cocido; bien cocido reduce mucho. Nunca "evita el marisco"; sí "el marisco crudo o poco cocido…, bien cocido es más seguro".

## Fuente del texto: IA + candado, NO texto fijo (corrección del owner)
**No hay catálogo hardcodeado.** Es una **nota más**, generada por la IA con el eje de seguridad (Anexo 3), usando el sistema de notas que YA existe. La consistencia y el "validar una vez" salen del **mecanismo de nota-de-grupo**, no de fijar el texto:
- Se genera **una vez para el GRUPO de riesgo** (marisco, cerdo, lácteo-sin-pasteurizar…) → aplica a todos sus ingredientes.
- **Nace borrador. Un médico la valida antes de publicar.** Esa validación ES la seguridad — es el mismo candado que toda nota. La diferencia: en estas la validación es **obligatoria**, no opcional (afirmación de seguridad fuerte).

Las reglas de precisión (condicional a inmunosupresión, atada a preparación) viven en el **PROMPT**, así la IA las aplica a cualquier alimento de riesgo — sin listas fijas.

## Ejemplos de TONO que debería producir la IA (ilustrativos, no fijos, y a validar por médico)
- **Marisco:** "Si tu tratamiento baja tus defensas (algunos inmunosupresores, biológicos o esteroides — no la mesalazina), el marisco crudo o poco cocido puede traer bacterias o virus. Bien cocido reduce mucho ese riesgo. Coméntalo con tu médico."
- **Cerdo:** "Si tu tratamiento baja tus defensas, la carne de cerdo poco cocida puede traer parásitos o bacterias. Bien cocida es segura. Coméntalo con tu médico."

## Cómo se ata a los datos (reusar lo que hay)
Un flag por **grupo o atributo** ("riesgo-crudo", "riesgo-parasito-carne") marca qué alimentos disparan que la IA genere esta nota de precaución. Escrita en el grupo, aplica a todos sus ingredientes — igual que la nota de grupo. La IA la genera; el flag solo decide **dónde** aplica.

## Verificación
- El bloque de precaución solo aparece en alimentos flagueados, marcado como callout, separado de la tolerancia.
- Siempre condicional a inmunosupresión y atado a crudo/poco cocido — nunca "evita X".
- No se publica sin validación médica.

## Parsing (reusar el de síntomas)
El servicio ya sabe extraer líneas `ETIQUETA: valor` con regex tolerante a markdown. Reusar
esa lógica para `TITULO/QUE_ES/QUE_SUELE_PASAR/IMPORTANTE/FUENTES/REVISION_PRIORITARIA`.
Al mapear a la nota: cada sección → un `PlatNotaSeccion` en orden; `FUENTES` → `PlatNotaReferencia`
(vacío si 'Ninguna'); `REVISION_PRIORITARIA=SÍ` → marca/flag para que el admin la revise antes.
