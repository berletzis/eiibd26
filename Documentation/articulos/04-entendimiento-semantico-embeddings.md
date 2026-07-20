---
título: "Contenido relacionado: búsqueda por significado"
tipo: artículo semitécnico
módulo: Motor de Cobertura · Entendimiento semántico
voz: habla-berletzis
---

# Contenido relacionado: búsqueda por significado

**"Fatiga" y "cansancio" son la misma cosa (menos para una computadora).**

Curiosamente, uno de los problemas más difíciles de la plataforma era también el más humano: entender que dos personas pueden hablar de lo mismo con palabras totalmente distintas.

Un paciente escribe "estoy agotado". Un artículo dice "astenia". Otro dice "fatiga crónica". Otro, "no tengo energía ni para levantarme". Tú y yo sabemos que es lo mismo. Una computadora que solo cuenta palabras, no. Para ella, si no se repite la palabra, no se parece. Y así se pierden las conexiones que más importan.

Ese era, durante mucho tiempo, nuestro techo.

## Contar palabras contra entender significado

La primera versión del motor comparaba textos como quien cuenta coincidencias: ¿cuántas palabras comparten estos dos artículos? Simple, rápido, y ciego a los sinónimos. "Fatiga" y "cansancio" no comparten letras suficientes, así que para el sistema viejo eran extraños.

La versión nueva trabaja distinto. En vez de contar palabras, convierte cada texto en una representación de su **significado**. Piénsalo como ubicar cada artículo en un mapa: no un mapa de ciudades, sino un mapa de ideas. Dos textos que hablan de lo mismo quedan **cerca** en ese mapa, aunque no compartan una sola palabra. Y dos textos que usan las mismas palabras para cosas distintas quedan lejos, como debe ser.

No te voy a explicar aquí cómo se construye ese mapa —esa parte es la cocina, y hay recetas que uno se guarda—. Lo que importa para entender la plataforma es la idea: **cercanía en significado, no coincidencia de palabras.**

## El día que supe que funcionaba

Las cosas técnicas se sienten abstractas hasta que una las ve pasar. El momento en que me convencí fue con un artículo nuestro sobre aspirina infantil.

El sistema viejo nunca le encontraba pareja: era un tema puntual, con vocabulario propio, y contar palabras no lo llevaba a ningún lado. El motor nuevo, en cambio, encontró casi de inmediato su gemelo en otro sitio: un artículo externo que hablaba de lo mismo, con otras palabras, y que era prácticamente su reflejo. Algo que la versión de contar palabras jamás habría relacionado.

Ahí dejó de ser teoría. La máquina no reconoció palabras iguales; reconoció que **trataban del mismo asunto**. Eso es lo que queríamos.

## Bonus que no esperaba: los idiomas dejaron de estorbar

Como el mapa es de significados y no de palabras, el idioma se vuelve casi un detalle. Un texto en español y uno en inglés que hablan de lo mismo quedan cerca, sin que nadie tenga que traducir nada primero. Para una comunidad como la nuestra, donde mucho del mejor material está en inglés, eso no es un lujo: es poder conectarte con cosas que antes se quedaban del otro lado de la barrera del idioma.

## Para qué usamos todo esto

Dos cosas, sobre todo.

**Artículos similares.** En lo que estás leyendo, te mostramos lo relacionado —dentro de EIIBD y fuera— por significado. No porque compartan un tag que alguien puso a mano, sino porque de verdad tratan de lo cercano.

**Comparar lo nuestro con lo de terceros.** Aquí se cierra el círculo con el Radar. Cuando el Radar trae material de fuentes confiables, este motor mide qué tan cerca está de lo que nosotros ya tenemos. Si algo de afuera no se parece a nada nuestro, es una señal: hay un tema que no estamos cubriendo. Si se parece mucho, ya lo tenemos dicho. Así el hueco de contenido deja de ser una corazonada y se vuelve algo que se puede medir.

## Lo que no verás aquí

A propósito no cuento qué herramienta hace el mapa, ni dónde ponemos el corte para decir "esto se parece lo suficiente", ni cómo está armada la tubería por dentro. No por misterioso, sino porque esas decisiones son las que hacen que funcione bien y no me parece sensato regalarlas. Lo que sí quiero que quede claro es la idea, porque la idea es honesta: la plataforma intenta entender de **qué** hablas, no solo **qué palabras** usaste.

---

*Este motor trabaja detrás de "Artículos similares" y del tablero de cobertura. No lo ves directo, pero es lo que hace que "fatiga" te encuentre lo que habla de "cansancio".*
