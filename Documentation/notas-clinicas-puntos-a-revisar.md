# Notas clínicas — puntos a revisar por el médico

**Fecha de extracción:** 14 JUL 2026
**Origen:** `Documentation/BORRADOR-notas-clinicas-v2.md`
**Para:** el médico que aprueba las notas del módulo de Platillos.

Estas son las anotaciones `*Revisor:*` que venían en el borrador y **no se cargaron a la
base de datos** — son instrucciones para ti, no contenido para el paciente. Cada nota está
en la BD con `RevisadaPorMedico = 0` (invisible para el paciente) hasta que la apruebes desde
el CRUD admin (F2). Usa esta lista para saber en qué poner atención antes de aprobar cada una.

---

## Corrección de evidencia aplicada (leer primero)

Un metaanálisis de 2025 (18 estudios, 1.38M personas) obligó a reescribir **carne roja** y
**embutidos** respecto al primer borrador:

- La carne roja se asocia con **desarrollar** colitis ulcerosa (+65% por 100 g/día), **pero NO
  con recaídas** en quien ya la tiene.
- Los embutidos muestran una **tendencia no significativa** y **tampoco** se asocian con recaídas.
- Traducción para el paciente que ya tiene EII: **no hay evidencia sólida de que la carne roja o
  los embutidos le provoquen brotes.** Restringirlos sería empujarlo a una restricción sin base.

---

## Anotaciones por nota

| Nota | Punto a revisar |
|---|---|
| **Lácteos** (grupo) | ⚠️ El grupo donde más daño hace la restricción innecesaria. |
| **Mariscos** (grupo) | ⚠️ **La nota más consecuente.** ¿Falta huevo crudo, carne cruda, quesos sin pasteurizar, germinados? |
| **Cereales** (grupo) | ⚠️ ¿Respaldas el punto del gluten con esa firmeza? |
| **Carne roja** (grupo) | ⚠️ Corregido respecto al borrador anterior. La asociación es con **desarrollar** la enfermedad, **no** con recaídas. |
| **Embutidos** (grupo) | ⚠️ **Cambió por completo respecto al borrador anterior.** Antes decía que aquí sí conviene limitar con respaldo. La evidencia no lo sostiene para quien ya tiene EII. |
| **Pescado** (grupo) | Deliberadamente **no** se afirmaron beneficios del omega-3 en EII: la evidencia es débil. ¿Coincides? |
| **Frutos secos** (grupo) | ⚠️ La fuente los recomienda explícitamente en remisión. ¿Suficiente para desmontar el mito? |
| **Grasas y aceites** (grupo) | ⚠️ La frase de la cirugía alude a malabsorción de sales biliares. ¿La nombramos o la dejamos así de indirecta? |
| **Bebidas** (grupo) | ¿Vale la pena nombrar los sueros de rehidratación oral? |
| **Yogur** (ingrediente) | Confirmar. |

---

## Pendientes generales para el revisor médico

1. ¿Falta alguna advertencia de **seguridad por inmunosupresión** además de mariscos, pescado y
   huevo crudos? (carne cruda, quesos sin pasteurizar, germinados)
2. ¿Conviene una nota sobre **suplementación de calcio y vitamina D** para quienes sí deben
   restringir lácteos?
3. ¿Hay algún alimento que consideres que **sí** debería llevar una advertencia clara y que aquí
   no esté?
4. Los textos de **carne** y **embutidos** cambiaron por completo respecto al primer borrador, al
   leer el metaanálisis. Revísalos con especial atención.
