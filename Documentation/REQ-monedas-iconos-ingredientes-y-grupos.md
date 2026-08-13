# Monedas ilustradas de ingredientes y grupos

Estado: **implementado y verificado en local (2026-08-11)**. Falta únicamente que lleguen los SVG
de Claude Design (REQ 1) — no hay que tocar código para darlos de alta.

---

## Corrección al REQ original

El REQ 2 que circula está escrito contra **otro proyecto** (oOrt: .NET 4.8, MVC5, EF6
database-first con `oOrtData.edmx`, `BussinessEntities.Utils.Slug`). EIIBD es ASP.NET Core 8 +
Razor Pages + EF Core 8 y **no tiene ningún `.edmx`**.

Su premisa central — "el módulo de platillos aún no existe, se crea desde cero" — es falsa aquí, y
su modelo de datos propuesto sería un **retroceso** sobre el que ya está en producción:

| REQ 2 propone | EIIBD ya tiene |
|---|---|
| `PlatIngrediente.Grupo NVARCHAR(60)` (texto suelto) | `PlatIngrediente.GrupoId` → FK a catálogo `PlatGrupo` (con `Orden`, `Activo`, `NotasEII`, `RiesgoTipo`) |
| N:M solo `(PlatilloId, PlatIngredienteId)` | `PlatPlatilloIngrediente` con `Cantidad`, `UnidadId`, `EsAlGusto`, `NotaPreparacion` + atributos de uso |
| `Slug.Create(txt, new SlugOptions { ToLower = true })` | `SlugHelper.GenerateSlug(txt)` — ya quita acentos y baja a minúsculas |
| Crear tablas + refrescar EDMX | Nada: **cero SQL, cero cambios de esquema** |

De los 8 criterios de aceptación del REQ, los 4 de datos ya estaban cumplidos. Lo implementado
aquí es solo la capa visual.

---

## Qué se implementó

Una **moneda** por alimento: círculo con el color de su grupo y el glifo tono-sobre-tono.

- `Services/Platillos/IconoAlimentoService.cs` — resuelve el SVG y lo inyecta **inline**.
  Un `<img>` no hereda `color`, así que el glifo nunca tomaría `currentColor`; por eso va inline.
- `Pages/Shared/_MonedaAlimento.cshtml` — el partial. Modelo `MonedaAlimentoVm { Nombre, Grupo, ExtraClass }`.
- `wwwroot/css/eiibd-tokens.css` — paleta: par `surface`/`ink` por grupo.
- `wwwroot/css/eiibd-components.css` — componente `.eii-moneda` + mapeo `.eii-grupo-*`.

Superficies en **tinte claro con tinta profunda**, no los HEX del REQ: esos son superficies
oscuras con glifo claro, pensadas para fondo oscuro, y el sitio es solo tema claro
(`--eii-surface: #ffffff`). Mismos matices, invertida la relación.

### Dónde aparece

| Vista | Moneda |
|---|---|
| `Pages/Platillos/Detalle.cshtml` | Una por ingrediente, arriba de su card. **Se conservan** cantidad, unidad, usos, nota de preparación y el pie "¿Puedo comerlo?" — la moneda se suma, no reemplaza |
| `Pages/Platillos/Ingrediente.cshtml` | `--lg` en el encabezado |
| `Areas/Identity/Pages/Usuario/UsuarioAlimentacion.cshtml` | `--sm` dentro de cada chip de grupo |

---

## Cómo dar de alta los íconos (REQ 1)

Soltar los archivos y ya. No hay build, ni sprite que regenerar, ni reinicio:

```
eiibd26/wwwroot/img/ingredientes/{slug-del-ingrediente}.svg   ← 67 archivos
eiibd26/wwwroot/img/grupos/{slug-del-grupo}.svg               ← 18 archivos
eiibd26/wwwroot/img/ingredientes/_fallback.svg                ← ya existe (plato genérico)
```

El slug sale de `SlugHelper.GenerateSlug(nombre)`: minúsculas, sin acentos, espacios a guiones.
`champiñón` → `champinon.svg`, `aceite de oliva` → `aceite-de-oliva.svg`.

**Cadena de fallback:** ingrediente → **su grupo** → genérico. Un ingrediente sin ícono propio cae
al de su grupo (que ya comunica algo), no a un genérico mudo. Hoy, sin ningún archivo de REQ 1,
todas las monedas muestran el plato genérico con el color correcto de su grupo.

### Requisitos de los SVG

- `fill="currentColor"` (o `stroke`) — **el color NO va horneado en el archivo**, lo pone el CSS
  desde el grupo. Un SVG con color fijo ignora la paleta.
- `viewBox` presente. `width`/`height` se ignoran: los quita el servicio y manda el CSS.
- Sin `<script>` (se strippea), sin `<style>` con colores fijos.

### Grupos del catálogo real (18, `SQL/seed-platillos.sql`)

`lácteo` · `huevo` · `carne` · `ave` · `embutido` · `pescado` · `marisco` · `verdura` · `fruta` ·
`fruto-seco` · `cereal` · `legumbre` · `tubérculo` · `hongo` · `grasa` · `condimento` · `bebida` · `otro`

Los slugs de archivo: `tubérculo` → `tuberculo.svg`, `lácteo` → `lacteo.svg`.

> El grupo 19 del REQ, **`Endulzante / Azucares`, NO existe en el catálogo**. Su paleta quedó
> definida por si entra después (cubre los slugs `endulzante` y `endulzante-azucares`), pero hoy
> ningún ingrediente lo usa. Si se da de alta, va por el CRUD de grupos, no por SQL.

### Grupo nuevo

Agregar el par de tokens en `eiibd-tokens.css` y su clase en `eiibd-components.css`. Sin eso, el
grupo cae solo a los colores de "otro" (gris legible) — nunca queda sin fondo.

---

## Verificación hecha (local, 2026-08-11)

- `dotnet build --no-restore` → **0 errores**.
- `/Platillos/arroz-con-champinones` → 8 monedas, cada una con la clase de su grupo
  (`eii-grupo-cereal`, `eii-grupo-hongo`, …) y el SVG inline con `class="eii-moneda__icono"`.
  Las cards conservan cantidad, unidad y "¿Puedo comerlo?".
- `/Platillos/Ingrediente/champinon` → moneda `--lg` en el encabezado, `eii-grupo-hongo`.
- **Cadena de fallback, con archivo temporal:** se puso un `grupos/hongo.svg` de prueba → solo la
  moneda de hongo lo tomó, cereal siguió en genérico; se borró → HTML byte a byte idéntico al
  inicial. Confirma también que **soltar un SVG no exige reiniciar la app** (la caché se invalida
  por fecha y tamaño del archivo).

Consola del navegador: 0 errores.

---

## Gotchas encontrados

- **Inicializador de objeto en atributo de tag helper**: `<partial model="new Vm { ... }" />` rompe
  el parser de Razor (`CS0747`, `CS1003`). El VM va en una variable antes de la etiqueta. Pariente
  del gotcha RZ1031 ya documentado.
- El partial se invoca por nombre desde `Pages/`, pero desde `Areas/` va con ruta explícita
  `~/Pages/Shared/_MonedaAlimento.cshtml`.
