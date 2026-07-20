---
título: "Radar de NINA: contenido útil de otros sitios"
tipo: artículo semitécnico
módulo: Servicio de NINA · Radar de contenido
voz: habla-berletzis
---

# Radar de NINA: contenido útil de otros sitios

**El radar que mira sin tomar.**

Algo que aprendí manteniendo EIIBD: por más que escribamos, nunca vamos a tener todo. Hay material buenísimo sobre EII regado por internet, en fundaciones, en sitios de pacientes, en lugares que llevan años haciéndolo bien. Fingir que solo existe lo nuestro sería mentira, y además una tontería.

Pero tampoco quería caer en lo fácil: copiar y pegar lo ajeno para llenar la plataforma. Eso no es ayudar, es apropiarse.

De esa tensión salió lo que llamamos el Radar: una parte de la plataforma que todos los días revisa sitios de confianza sobre EII y, cuando encuentra algo útil, te conecta con él. Siempre mandándote al original. Nunca quedándose con el texto.

## Cómo mira sin tomar

Por dentro es un Worker —un proceso que corre solo, en segundo plano, sin que nadie tenga que apretar un botón—. Lo que hace, en simple:

Recorre los **sitemaps** de las fuentes que confiamos. Un sitemap es como el índice que un sitio publica de sus propias páginas; es la forma educada de preguntar "¿qué tienes?" sin andar tocando puertas al azar. De ahí saca qué hay de nuevo.

Y respeta el **robots.txt** de cada sitio. Ese archivo es donde cada quien escribe qué permite mirar y qué no. Nosotros lo obedecemos. Si un sitio dice "esto no", es "esto no", punto.

Lo importante: de cada página **no nos guardamos el texto**. Nos quedamos con el significado —una representación de lo que trata— y con el enlace al original. Cuando te lo mostramos, te mandamos allá, a la fuente, con su crédito. El artículo se lee en la casa de quien lo escribió, no en la nuestra.

## Por qué cuento la parte legal

Normalmente uno esconde estos detalles. Yo prefiero contarlos, porque son justo lo que hace confiable a una herramienta así.

Robots.txt te deja **mirar**, no copiar. Son dos permisos distintos y no los confundimos. Nunca saltamos un login ni un muro de pago para sacar contenido —si algo está detrás de una barrera, esa barrera se respeta—. Y siempre acreditamos y enlazamos a la fuente.

Esa es la línea entre un radar que respeta el ecosistema y un raspador que se lo come. Del lado correcto de esa línea es donde quiero que esté EIIBD.

## Sumar una fuente nueva sin sumar basura

Hace poco agregamos **Educa Inflamatoria** como fuente. Y ahí aparece un problema que nadie te cuenta: un sitio no es solo sus artículos. También tiene páginas de autores, de etiquetas, de patrocinadores, avisos, listados vacíos. Si dejas al Radar tragarse todo, terminas "descubriendo" la página de un tag o el perfil de un autor como si fuera contenido útil. Ruido con cara de señal.

Así que cada fuente nueva entra con una **lista de exclusión**: patrones de URL que se quedan fuera desde el inicio. No es glamoroso. Es de esas cosas que decides una vez, con paciencia, para que después el Radar solo te traiga lo que de verdad vale.

## Y del otro lado, para quien escribe

El mismo Radar tiene una segunda cara, para nuestros editores. Si sabemos qué temas están cubriendo bien los sitios de confianza, también sabemos **qué nos falta a nosotros**. Eso se volvió un pequeño tablero de "oportunidades de contenido": qué escribir después, no por corazonada, sino porque hay un hueco real.

Lo bonito es que el corte del editor y el del paciente están separados. El editor puede subir su exigencia para encontrar huecos finos, sin que eso cambie en nada lo que ve un paciente. Cada quien con su lente.

## Lo que el Radar no sabe hacer solo

Hay una pieza que todavía no conté, y sin ella el Radar sería tonto. ¿Cómo sabe que un artículo de otro sitio "habla de lo mismo" que uno nuestro, si usan palabras distintas? ¿Cómo mide que algo está *cubierto* o *sin cubrir*? Eso no es cosa del Worker que recorre sitemaps. Es entendimiento de significado, y merece su propio texto.

---

*El Radar corre solo, a diario. Cuando te sugiere algo de otro sitio, el enlace te lleva a la fuente original — ahí es donde debe leerse.*
