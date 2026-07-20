# BORRADOR — Notas clínicas para el módulo de Platillos

**Fecha:** 13 JUL 2026
**Estado:** ⚠️ **BORRADOR. NO PUBLICAR SIN REVISIÓN MÉDICA.**

---

## Cómo usar este documento

Estos son los textos que verá el paciente en la vista de ingrediente (`/Platillos/Ingrediente/{slug}`). Se cargan en dos lugares del admin:

- **Contexto clínico del grupo** → `PlatGrupo.NotasEII`, en `/Identity/Admin/Platillos/Grupos`.
- **Nota del ingrediente** → `PlatIngrediente.NotasEII`, en `/Identity/Admin/Platillos/Ingredientes`.

**Para el médico revisor:** cada bloque trae una *nota para el revisor* que señala qué afirmación conviene escrutar. No necesitas leer todo con lupa — enfócate en esas.

## Principios con los que están escritos

1. **Nunca un veredicto.** No decimos que un alimento sea "seguro" o "dañino". La tolerancia es individual.
2. **Contrapeso a la restricción.** El paciente con EII tiende a eliminar alimentos "por si acaso". Eso tiene un costo real: desnutrición, pérdida de masa, osteoporosis. Cuando no hay razón para evitar algo, **lo decimos**.
3. **El matiz que sí sirve.** Preferimos "prueba el queso curado" antes que "evita los lácteos".
4. **Brote vs remisión.** Es la distinción que más importa y casi nadie se la explica al paciente.
5. **Lenguaje de paciente**, no de artículo médico.

---

# Contexto clínico por GRUPO

## lácteo

> Los lácteos no están prohibidos en EII. Lo que varía de persona a persona es la tolerancia a la lactosa, que es algo más frecuente en quienes tienen afectado el intestino delgado.
>
> Antes de eliminarlos, considera esto: son tu principal fuente de calcio, y en EII el riesgo de osteoporosis es real — más aún si tomas o has tomado corticoides. Restringirlos sin motivo también tiene un costo.
>
> Si sospechas que te caen mal, hay opciones antes de descartarlos: los quesos curados casi no tienen lactosa, el yogur suele tolerarse mejor que la leche, y existe leche sin lactosa. Coméntalo con tu médico o nutriólogo.

*Nota para el revisor:* revisar el énfasis en osteoporosis/corticoides y la afirmación sobre quesos curados y yogur. Es el grupo donde más daño hace la restricción innecesaria.

## huevo

> El huevo suele tolerarse bien y es una fuente de proteína de buena calidad — importante si has perdido peso o masa muscular.
>
> No hay razón para evitarlo salvo alergia o una intolerancia personal comprobada.

*Nota para el revisor:* ¿algo que agregar sobre el huevo crudo o poco cocido en pacientes inmunosuprimidos?

## carne

> La carne roja aporta proteína y hierro, y ambos importan en EII, sobre todo si hay anemia.
>
> Algunos estudios asocian el consumo alto de carne roja con más recaídas, aunque la evidencia no es concluyente. En brote, los cortes grasos suelen digerirse peor.
>
> Antes que eliminarla: cortes magros, bien cocida, porciones moderadas. Quitar la proteína de tu dieta tiene su propio costo.

*Nota para el revisor:* ⚠️ la asociación con recaídas — ¿con qué fuerza quieres afirmarla? La dejé deliberadamente suave ("algunos estudios", "no concluyente").

## ave

> El pollo y el pavo suelen tolerarse bien y son proteína magra. Son de las carnes que mejor caen, incluso en brote, si se preparan sin grasa añadida.
>
> No hay motivo para restringirlos.

*Nota para el revisor:* sin puntos conflictivos.

## embutido

> Los embutidos —tocino, salchicha, jamón— son de los pocos alimentos donde sí conviene moderar: son altos en grasa, sal y conservadores, y se asocian con peor evolución.
>
> No están "prohibidos", pero es de los pocos casos donde limitar tiene respaldo.

*Nota para el revisor:* ⚠️ ¿es correcto decir que **aquí sí** la evidencia apoya limitar? Es la única nota del documento que empuja hacia la restricción, y quiero que sea sólida.

## pescado

> El pescado suele tolerarse bien y es una buena fuente de proteína. Cocido y sin empanizar, es de las opciones más amables.
>
> No hay razón para restringirlo.

*Nota para el revisor:* deliberadamente **no** afirmé beneficios del omega-3 en EII, porque la evidencia es débil. ¿Coincides?

## marisco

> Los mariscos no tienen ninguna contraindicación específica en EII, salvo alergia.
>
> Pero hay algo importante que sí debes saber: si tomas inmunosupresores o biológicos, tu riesgo de infección es mayor. Evita mariscos **crudos o poco cocidos** (ceviche, aguachile, ostión crudo). Bien cocidos, no hay problema.
>
> Esto no es sobre tolerancia. Es sobre seguridad.

*Nota para el revisor:* ⚠️ **la nota más importante del documento.** No es sobre digestión, es sobre riesgo de infección en inmunosupresión. ¿Está bien planteada? ¿Falta algo (huevo crudo, carne cruda, quesos sin pasteurizar)?

## verdura

> Las verduras no son el enemigo, aunque mucha gente las elimina después de un brote y ya no las vuelve a comer.
>
> Lo que cambia es la preparación. En brote, la fibra cruda —cáscaras, tallos, hojas duras, semillas— puede irritar. En remisión, no hay razón para evitarlas.
>
> El truco casi siempre es **cocinarlas, pelarlas y quitarles las semillas**, no eliminarlas. Y volver a introducirlas poco a poco después de un brote es parte de recuperar tu dieta.

*Nota para el revisor:* el mensaje central es **brote ≠ remisión**. Es la distinción que más se le escapa al paciente.

## fruta

> Igual que con las verduras: el problema no suele ser la fruta, sino su fibra.
>
> En brote: pelada, sin semillas, o cocida (compota, plátano, manzana cocida). En remisión: sin restricción.
>
> Eliminar la fruta de forma permanente después de un brote es un error común, y te cuesta vitaminas.

*Nota para el revisor:* sin puntos conflictivos.

## fruto-seco

> Las nueces, almendras y semillas tienen fibra dura y pueden sentirse mal en brote.
>
> Pero la vieja recomendación de evitarlas para siempre no tiene respaldo: no hay evidencia de que provoquen brotes. En remisión suelen tolerarse, y las cremas (de cacahuate, de almendra) son una alternativa más suave.

*Nota para el revisor:* ⚠️ la afirmación "no hay evidencia de que provoquen brotes" — ¿la respaldas tal cual?

## cereal

> Los cereales aportan energía y suelen tolerarse bien. En brote, los refinados (arroz blanco, pan blanco, pasta) caen más suave que los integrales, por la fibra.
>
> Y un aviso importante: mucha gente con EII deja el gluten sin necesidad. Salvo que tengas celiaquía o una sensibilidad comprobada, no hay razón para hacerlo — y una dieta sin gluten innecesaria es cara, restrictiva, y no te va a ayudar.

*Nota para el revisor:* ⚠️ el punto del gluten innecesario me parece de los más útiles que puede leer un paciente. ¿Lo respaldas con esa firmeza?

## legumbre

> Frijoles, lentejas y garbanzos suelen dar gas e hinchazón, y en brote muchos no los toleran.
>
> Pero son una fuente excelente de proteína y fibra, y eliminarlos para siempre es perder mucho. Prueba en remisión: bien cocidos, sin cáscara, en porciones chicas, o en puré (como el hummus).

*Nota para el revisor:* sin puntos conflictivos.

## tubérculo

> La papa y el camote suelen tolerarse muy bien, sobre todo cocidos y pelados. Son de los alimentos más amables en brote y una buena fuente de energía.
>
> Salvo que te caigan mal, no hay motivo para evitarlos.

*Nota para el revisor:* sin puntos conflictivos.

## hongo

> Los hongos tienen una fibra que a algunas personas les cuesta digerir, pero no hay contraindicación específica en EII.
>
> Si te caen bien, no hay razón para evitarlos.

*Nota para el revisor:* sin puntos conflictivos.

## grasa

> La grasa no es el enemigo: es esencial y aporta calorías, algo que importa si estás perdiendo peso.
>
> Lo que suele caer mal no es la grasa en sí, sino **la fritura** y las cantidades grandes de golpe. El aceite de oliva se tolera bien.
>
> Si tuviste cirugía de intestino delgado, o notas diarrea después de comer grasa, coméntalo con tu médico: ahí sí puede haber una razón concreta detrás.

*Nota para el revisor:* ⚠️ la última frase alude a malabsorción de sales biliares / resección ileal. ¿La dejo así de indirecta, o la nombramos?

## condimento

> Las hierbas y especias suaves suelen ir bien en cantidades normales. El picante es otro tema y está marcado aparte.
>
> No hay razón para comer sin sabor.

*Nota para el revisor:* sin puntos conflictivos.

## bebida

> Mantenerte hidratado es especialmente importante en EII: la diarrea te deshidrata más rápido de lo que crees.
>
> El agua es lo mejor. El café y el alcohol están marcados aparte. Las bebidas con gas pueden darte más inflamación y molestia.

*Nota para el revisor:* ¿vale la pena mencionar sueros de rehidratación oral en brote?

## otro

*(Dejar vacío. No forzar una nota donde no hay nada útil que decir.)*

---

# Notas por INGREDIENTE (las que dan el matiz accionable)

## queso

> No todos los quesos son iguales. Los curados —manchego, parmesano, añejo— casi no tienen lactosa. Los frescos —panela, requesón, cottage— tienen más.
>
> Si te cae mal el fresco, prueba el curado antes de descartar el queso entero.

*Nota para el revisor:* ⚠️ el ejemplo bandera de toda la función. Confirmar la afirmación sobre lactosa en curados vs frescos.

## leche

> Si sospechas intolerancia a la lactosa, existe leche sin lactosa. No tienes que dejar los lácteos por completo.

## yogur

> El yogur suele tolerarse mejor que la leche: sus cultivos ya digirieron parte de la lactosa.

*Nota para el revisor:* confirmar.

## cebolla

> Cruda irrita a mucha gente. Cocida suele tolerarse bien. Antes de eliminarla, prueba cocida.

## leche de coco

> No es un lácteo, aunque el nombre confunda. Es una alternativa para quien no tolera la lactosa.

## camarón

> Bien cocido no hay problema. Crudo (ceviche, aguachile) sí: si tomas inmunosupresores o biológicos, el riesgo de infección es mayor.

*Nota para el revisor:* ⚠️ mismo punto de seguridad que el grupo marisco.

---

## Pendiente de decidir con el médico

1. ¿Falta alguna advertencia de **seguridad por inmunosupresión** además de mariscos crudos? (huevo crudo, carne cruda, quesos sin pasteurizar, germinados)
2. ¿Conviene una nota sobre **suplementación de calcio y vitamina D** en quienes sí deben restringir lácteos?
3. ¿Hay algún alimento que el médico considere que **sí** debería llevar una advertencia clara y que aquí no esté?
