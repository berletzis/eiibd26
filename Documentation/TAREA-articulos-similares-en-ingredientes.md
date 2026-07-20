# TAREA (backlog) — "Contenido relacionado" por embeddings entre platillos, ingredientes y artículos

**Fecha anotada:** 17 JUL 2026 · **Ampliada:** 17 JUL 2026 (sumar platillos)
**Idea del owner:** usar el **algoritmo de similitud semántica que ya existe** (Motor de Cobertura / embeddings / "Artículos Similares") para relacionar por significado **tres tipos de nodo**: **artículos**, **ingredientes** y **platillos**. Así el paciente descubre contenido relacionado sin importar por dónde entre.

## Qué se quiere — grafo semántico de 3 nodos, bidireccional
- **Artículo ↔ ingrediente:** el artículo "qué alimentos llevar de vacaciones" encuentra el ingrediente **papa**, y la papa encuentra el artículo. (Match por significado, no por palabras.)
- **Artículo ↔ platillo:** ese mismo artículo puede sugerir un **platillo** relacionado (ej. "salpicón"), y el platillo sugiere artículos.
- **Ingrediente ↔ platillo:** ya están unidos por estructura (un platillo contiene ingredientes); aquí el embedding **suma la capa semántica** (platillos "parecidos" aunque no compartan ingredientes exactos).

## Reuso (no reinventar)
- El motor de embeddings ya calcula similitud coseno entre nodos de contenido (ver `Documentation/wiki-tecnica/` de similitud/cobertura y los artículos divulgativos 01/04).
- La tarea es **sumar ingredientes y platillos como nodos** al grafo:
  - **Ingrediente:** embedding de su texto = nombre + **nota clínica**.
  - **Platillo:** embedding de nombre + **lista de ingredientes** + categoría. (Ojo: es texto más pobre que prosa; el match es más "de qué está hecho / de qué trata" que semántico fino. El mayor valor está en los enlaces **comida↔artículo**, que hoy no tienen ninguna otra conexión.)
- Renderizar un bloque "Contenido relacionado" en `Pages/Platillos/Ingrediente.cshtml` y en el detalle de platillo, con el mismo estilo del "Artículos Similares" de los artículos.

## Consideraciones (para cuando se construya)
- **Candado:** solo mostrar notas clínicas **publicadas** y contenido/platillos publicados (respetar el filtro único de visibilidad).
- **Encuadre médico:** bloque informativo, no consejo médico — mismo tono "esto no es una dieta".
- **Umbral:** reusar el del paciente (no el del editor).
- **Recálculo:** regenerar embeddings al crear/editar la nota del ingrediente, o al editar el platillo (como ya se hace con el contenido).
- **Mezcla de tipos en resultados:** decidir si el bloque mezcla artículos + platillos + ingredientes o los separa por sección (probablemente separados, con su etiqueta de tipo).

## Estado
Backlog — diseñada, no construida. Depende del motor de embeddings ya existente; es principalmente "sumar ingredientes y platillos al grafo" + bloques de UI en los dos detalles.
