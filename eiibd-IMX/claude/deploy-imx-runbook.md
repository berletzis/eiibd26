# Runbook de publicación — eiibd-IMX

Sitio de Fundación IMX (`https://www.imx.org.mx`). Proyecto de **solo lectura**
sobre la base de EIIBD, publicado por FTP a un IIS detrás de Cloudflare.

---

## 0. Regla que no se toca

`ImxDbContext.SaveChanges` y `SaveChangesAsync` lanzan `NotSupportedException` a
propósito. Este sitio **nunca escribe en la base**. Si alguna vez hace falta un
dato nuevo, va en `appsettings.json`, no en una tabla.

Verificación rápida antes de publicar:

```sh
grep -n "NotSupportedException" eiibd-IMX/Data/ImxDbContext.cs   # deben salir 2 líneas
```

---

## 1. Qué NO debe quedar en la carpeta de producción

Estos dos archivos estuvieron publicados en el servidor y no deben volver:

| Archivo | Por qué |
|---|---|
| `eiibd-IMX.pdb` | Entrega rutas de archivo y números de línea en cualquier stack trace que se escape. |
| `appsettings.Development.json` | No tiene por qué existir en producción; basta que alguien le meta una clave para que quede publicada. |

Ya está resuelto en el `.csproj`: `DebugType=none` para Release y
`CopyToPublishDirectory="Never"` en el appsettings de desarrollo. En Debug los
símbolos siguen intactos, así que depurar en local no cambia.

**Comprobación después de cada publicación** (en la carpeta del sitio en el
servidor, o sobre la salida de `dotnet publish -c Release`):

```sh
find . -name "*.pdb"                  # debe devolver 0 resultados
ls appsettings.Development.json       # debe decir "no such file"
```

Si el FTP publica con `DeleteExistingFiles=false` (que es como está el perfil),
los dos archivos **siguen ahí de publicaciones anteriores**: hay que borrarlos a
mano una vez.

---

## 2. Publicar

```sh
dotnet publish eiibd-IMX/eiibd-IMX.csproj -c Release -o <carpeta>
```

Después de publicar, correr la verificación contra el sitio en vivo:

```sh
sh eiibd-IMX/claude/verificar-seo.sh https://www.imx.org.mx
```

Comprueba canonical/og:url, las tres institucionales, el texto propio de las
categorías, robots.txt, y que cada URL del sitemap responda 200 con el host
canónico. Sale con código 1 si algo falla.

---

## 3. Configuración que vive en `appsettings.json`

| Sección | Para qué |
|---|---|
| `Sitio:HostCanonico` | Origen de `canonical`, `og:url`, `robots.txt` y `sitemap.xml`. **Ninguna vista escribe el host a mano.** |
| `Analytics:GA4Id` | Id de GA4. Vacío = no se emite el snippet. |
| `Organizacion` | Razón social, RFC, régimen, cuenta, CLABE, correo y teléfonos. Fuente única del footer, `/contacto`, `/transparencia` y el JSON-LD. |
| `Categorias:<slug>` | `H1`, `MetaDescripcion` e `IntroHtml` por categoría. Lo que no se declare cae al valor de la base. |
| `Filas` | Categorías de la home **y** las que entran al sitemap. |

Agregar una tercera categoría = agregarla a `Filas` (y opcionalmente a
`Categorias`). No hay que tocar código.

---

## 4. GA4 y conversiones (pendiente del owner)

El código está listo y el measurement ID **`G-QTD8HR2PJ3`** ya está en
`Analytics:GA4Id` en `appsettings.json`. El id **no es un secreto**: viaja en el
HTML de todas las páginas. Tratarlo como credencial solo complica el despliegue.

En Development el snippet **no se emite** aunque el id esté configurado, para que
las pruebas locales no ensucien los datos con los que Ad Grants va a medir.
Verificado en ambos sentidos.

Falta la parte de consola:

1. Confirmar en el **DebugView** de GA4 que llegan los cuatro eventos:
   `click_telefono`, `click_correo`, `click_donativo`, `click_registro`.
   Requiere el sitio publicado: en local el snippet no se renderiza.
2. En GA4 (Administrar → Eventos), marcar los cuatro como **eventos clave**.
3. Importarlos a Google Ads:
   - **Principales**: `click_telefono`, `click_correo`. Son contacto real con la
     Fundación, que es lo que el grant debe producir.
   - **Secundarias**: `click_registro`, `click_donativo`.

   `click_registro` va como secundaria a propósito: manda tráfico a `eiibd.com`,
   otro dominio, y una cuenta de grant cuya conversión principal es enviar
   tráfico fuera es exactamente el patrón que la política señala.

### Ganchos de los eventos — qué NO renombrar

Los cuatro eventos se cuelgan de marcado que ya existía; no hay atributos de
analítica en el HTML. Renombrar cualquiera de estas clases **rompe la medición
en silencio** (la página sigue funcionando, el evento deja de llegar):

| Evento | Gancho | Dónde vive |
|---|---|---|
| `click_telefono` | `a[href^="tel:"]` | Teléfonos del footer y de `/contacto` |
| `click_correo` | `a[href^="mailto:"]` | Correo del footer, panel 02, `/contacto`, `/transparencia` |
| `click_donativo` | `a.panel-cta` | Comprobante bancario (partial `_DatosFiscales`) |
| `click_registro` | `a.cta-registro` | Tarjeta de registro (partial `_CardRegistro`) |

Hoy `.panel-cta` se usa **solo** para el comprobante bancario. Si algún día se
reutiliza esa clase para otro botón, hay que separar el selector o se van a
contar donativos que no lo son.

---

## 5. Landing pages: términos de medicamentos con receta

La política de *restricted drug terms* de Google Ads aplica **también a la
página de destino**, no solo a las palabras clave y los anuncios.

Estas páginas nombran principios activos de receta y **no deben usarse como
página de destino de campaña** (pueden seguir indexadas y en el sitemap; el
límite es el destino del anuncio):

- `/info-y-ayuda/tratamientos`
- `/info-y-ayuda/crohn-y-colitis-y-otras-condiciones-inmunologicas-relacionadas`
- `/vivir-con-crohn-y-colitis/envejecer-con-crohn-o-colitis`
- `/vivir-con-crohn-y-colitis/viajar-con-crohn-o-colitis`
- `/vivir-con-crohn-y-colitis/fumar-o-vapear-con-crohn-o-colitis`

Destinos limpios y recomendados: `/`, `/info-y-ayuda`,
`/vivir-con-crohn-y-colitis`, `/quienes-somos`, `/contacto`.

El contenido de esos artículos vive en la base de EIIBD, que aquí es de solo
lectura: no se edita desde este proyecto. Volver a correr la revisión cuando se
publiquen artículos nuevos.

---

## 5 bis. Aviso de privacidad — PUBLICADO, pendiente de revisión legal

`/aviso-de-privacidad` existe, está enlazada desde el footer (columna "La
asociación") y es **indexable a propósito**: un aviso accesible y rastreable es
parte de lo que un revisor de Ad Grants espera encontrar. Entra al sitemap.

> **El texto es un borrador técnico redactado sobre los elementos que exige la
> LFPDPPP y NO lo ha revisado un abogado.** Esa revisión sigue pendiente y es lo
> único que falta de esta página.

Al editarla: cada bloque corresponde a un requisito legal concreto. Quitarle una
frase puede quitarle el requisito. No "mejorar" la redacción sin criterio legal.

La razón social, el RFC, el domicilio y el correo **no están escritos en la
vista**: salen de `Organizacion` en `appsettings.json`, la misma fuente que el
footer, `/transparencia`, `/contacto` y el JSON-LD.

Si cambia lo que el sitio recaba —un formulario, otra herramienta de analítica,
cualquier cookie nueva— hay que actualizar el aviso **y** la fecha de "Última
actualización" que aparece al inicio de la página.

El `Views/Home/Privacy.cshtml` heredado del scaffold ya se borró: no lo servía
ninguna ruta y, conviviendo con una página real, solo invitaba a que alguien
editara el archivo equivocado.

---

## 6. `/transparencia`: datos que faltan

La página está publicada con lo verificado (figura legal, RFC, régimen,
**domicilio**, destino de los donativos, cuenta, CLABE y procedimiento del CFDI)
y con un **marcador visible** —bloque `.pendiente`, borde punteado— para los dos
documentos que siguen sin confirmar:

- Informe anual de actividades
- Estados financieros dictaminados

Cuando lleguen, se agregan a la vista y **se borra el bloque `.pendiente`**.
No inventar ninguno de esos datos, ni sustituir el marcador por un
"próximamente" que sugiera que ya existen: o están, o se ve que faltan. Es
información legal de una donataria autorizada.

**Ojo:** la lista de pendientes decía antes "Domicilio fiscal y constancia de
autorización vigente del SAT". Al publicarse el domicilio se retiró el renglón
completo, así que **la constancia del SAT ya no aparece como pendiente**. Si se
quiere volver a declarar, hay que agregarla como su propio elemento.

Falta también un **logo raster** propio. Hoy el JSON-LD apunta a
`/img/logo-imx.svg`, generado a partir del logo del sitio. Google prefiere un
PNG de al menos 112×112 px para `Organization.logo`.

---

## 7. Pendientes de servidor (no son trabajo de código)

### 7.1 Cambiar el usuario `sa` por `imx_reader` — MÁS URGENTE QUE TODO LO DEMÁS

Hoy el sitio se conecta con `sa`. Un sitio público anónimo, de solo lectura, no
tiene por qué tener credenciales de administrador de la instancia.

Crear un login `imx_reader` con `GRANT SELECT` únicamente sobre las tres tablas
que el sitio lee (`contenidos`, `contenidosCategorias`,
`contenidosCategoriasRelacion`) y `DENY` sobre el resto de `dbo`, y cambiar
`ConnectionStrings:DefaultConnection` en el servidor.

El `ImxDbContext` ya está preparado para el día que existan vistas `pub`: solo
hay que cambiar cada `ToTable("x")` por `ToView("vw_x", "pub")`.

### 7.2 Cloudflare: certificado de origen y modo Full (strict)

Pasar Cloudflare a **Full (strict)** con un certificado de origen instalado en
IIS. Mientras no esté, el tramo Cloudflare→origen no valida el certificado.

Ojo con el orden en `Program.cs`: `UseForwardedHeaders` va **primero** en el
pipeline. Sin eso la app cree que toda petición llegó por HTTP,
`UseHttpsRedirection` redirige a HTTPS, Cloudflare vuelve a pedir al origen y se
arma un bucle infinito de redirecciones.

Además, `KnownNetworks` y `KnownProxies` están vacíos: se confía en el proxy sin
lista blanca. Eso **asume que el origen solo es alcanzable desde Cloudflare**.
Si el firewall no está restringido a los rangos de CF, cualquiera que llegue por
IP directa puede falsear `X-Forwarded-For`.

### 7.3 Search Console

Con el sitemap ya publicado: dar de alta la propiedad y enviar
`https://www.imx.org.mx/sitemap.xml`. Sin esto, un dominio nuevo tarda semanas
en ser descubierto.

---

## 8. Orden de rutas en `Program.cs` — no reordenar a la ligera

Las dos últimas rutas son catch-all y se tragan cualquier cosa:

```
{categoria}/{slug}   ← 2 segmentos
{categoria}          ← 1 segmento
```

Toda ruta con segmento literal (`robots.txt`, `sitemap.xml`, `quienes-somos`,
`transparencia`, `contacto`, `aviso-de-privacidad`) **va registrada antes**. Si
una queda después,
entra como slug de categoría, no la encuentra en la base y devuelve 404 —
justo el bug que estas páginas vinieron a arreglar.
