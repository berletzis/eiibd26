# Requerimiento — Platillos F4: vista de ingrediente ("¿Puedo comer queso?")

**Fecha:** 13 JUL 2026
**Módulo:** Platillos (autocontenido). Misma regla de aislamiento del §0 del requerimiento base.

---

## 1. Contexto

La pregunta más común del paciente **no es** "¿qué platillos puedo comer?" — es **"¿puedo comer queso?"**.

Diseñamos platillo-primero, pero el paciente entra **ingrediente-primero**. Esta vista cierra ese hueco, y probablemente se convierta en la **puerta de entrada principal** al módulo: "¿puedo comer queso con colitis?" es una búsqueda de altísimo volumen en Google.

## 2. Principio rector — evidencia, no veredicto

**La plataforma no responde "sí" ni "no".** No existe el dato médico "el queso es seguro en colitis ulcerosa". Lo que varía es la **tolerancia individual**.

Lo que sí hacemos: **darle la evidencia y el contexto** para que él y su médico decidan.

**Y hay un riesgo real que esta pantalla debe contrarrestar activamente:** los pacientes con EII restringen alimentos "por si acaso", y eso tiene un costo — la desnutrición y la osteoporosis (sobre todo con corticoides) son riesgos documentados. Una herramienta que empuje a evitar alimentos sin motivo **hace daño**. Esta vista debe decir explícitamente cuándo **no hay razón para evitar** algo.

## 3. Qué muestra

En orden, y el orden importa:

1. **Qué es** — nombre, grupo y atributos (del catálogo).
2. **El hecho honesto** — que el alimento no está prohibido; lo que varía es la tolerancia.
3. **Notas del ingrediente** (`PlatIngrediente.NotasEII`) — el matiz que de verdad ayuda. Ej. en queso: *"los curados (manchego, parmesano) casi no tienen lactosa; los frescos (panela, requesón) tienen más. Si te cae mal el fresco, prueba el curado antes de descartar el queso entero."*
4. **Contexto clínico del grupo** (campo nuevo, ver §4) — ej. en lácteos: *"son tu principal fuente de calcio; restringirlos sin motivo aumenta el riesgo de osteoporosis, más aún con corticoides. No lo evites 'por si acaso'."*
5. **Tu perfil** — si el usuario autenticado lo tiene excluido, decírselo y enlazar a *Lo que no tolero*.
6. **Platillos que lo contienen** — enlace al listado filtrado.
7. **Bloques de comunidad y experiencia personal** — presentes pero **apagados** en v1 (ver §7).
8. **Cierre** — *"Esto no es consejo médico. Son datos para que los lleves a tu médico o nutriólogo y decidan juntos."*

## 4. Contenido clínico — en base de datos, no en el código

**Campo nuevo:** `PlatGrupo.NotasEII` (nvarchar null) — SQL directo, sin migración EF. Editable desde el CRUD de Grupos que ya existe.

**Por qué en BD y no quemado en la vista:** es **contenido médico**. Tiene que poder corregirse sin recompilar, y tiene que poder **revisarlo un médico**.

⚠️ **Los textos clínicos iniciales deben ser revisados por un médico antes de publicarse.** No los inventamos nosotros ni los dejamos en el código. Se cargan como datos, se revisan, y se corrigen ahí.

## 5. Ruta y SEO — el punto que más valor tiene

Página **pública, sin `[Authorize]`**, **renderizada en servidor** (indexable).

- **Ruta amigable:** `/Platillos/Ingrediente/{slug}` — mapear con `AddPageRoute` en `Program.cs`, igual que ya se hace con `/Preguntas/{slug}` y `/medicos/{slug}`.
- **Slug** derivado del nombre del ingrediente (minúscula, sin acentos, con guiones).
- **Meta title / description** propios y descriptivos. El title debe hablar el idioma del paciente, no el del catálogo.
- **Incluir en el sitemap.**

Esta es la única parte del módulo con valor SEO real. No la dejes sin meta tags.

## 6. Puntos de entrada

- Desde la tarjeta de un platillo: clic en un ingrediente → su vista.
- Desde el listado `/Platillos`: buscador tipo *"¿Puedo comer…?"*.
- Desde el perfil del paciente: sus ingredientes excluidos enlazan a su vista.

## 7. Preparado para el módulo de Tolerancia (#16)

Los bloques de **"lo que reporta la comunidad"** y **"tu experiencia"** se maquetan ya, pero en v1 muestran el estado honesto:

> *Nadie ha registrado su tolerancia a este alimento todavía. Sé el primero.*

Cuando aterrice #16 (partial pooling / Beta-Binomial, estratificado por diagnóstico), esos bloques **se encienden solos**. La vista no cambia de forma.

**Y cuando se enciendan, la regla no se negocia:** el porcentaje **siempre** va con su `n` y su incertidumbre, y con la frase *"es un promedio de otros, no una predicción sobre ti"*. Sin esa línea, un "68%" se lee como permiso o como prohibición, y el producto se vuelve peligroso.

## 8. Criterios de aceptación

1. La vista carga pública, server-rendered, con ruta amigable y meta tags.
2. Muestra grupo, atributos, notas del ingrediente y contexto clínico del grupo.
3. Si el usuario lo tiene excluido, se lo dice y enlaza a su perfil.
4. Lista los platillos activos que lo contienen.
5. Los bloques de comunidad/experiencia muestran el estado honesto de "aún sin datos".
6. El cierre con el aviso de que no es consejo médico está siempre visible.
7. Aislamiento intacto: solo tablas `Plat*` + `idUsuario`.
8. Cero CSS nuevo fuera de `eii-*`.

## 9. Fuera de alcance

- El modelo bayesiano y el registro de tolerancia → módulo #16, independiente.
- Imágenes.
- Cualquier veredicto sobre si un alimento es "seguro" o "dañino".
