# Requerimiento — Notas clínicas en base de datos (Platillos + Glosario)

**Fecha:** 14 JUL 2026
**Tipo:** tablas nuevas + seed + candado de revisión médica.

---

## 1. El problema con lo que hay hoy

`PlatGrupo.NotasEII` y `PlatIngrediente.NotasEII` son **un solo campo de texto plano**. Pero las notas clínicas reales tienen **estructura**, igual que el glosario de síntomas:

> **¿Qué es?** … **¿Qué suele pasar?** … **Antes de eliminarlos** … **Importante** … **Bibliografía**

Y el título de la sección intermedia **cambia según el caso**: "Antes de eliminarlos", "Lo que dice la evidencia", "Ojo — esto es de seguridad, no de tolerancia". Un campo plano no puede con eso.

## 2. El requisito que manda sobre todos los demás

**Es contenido médico.** No puede llegar a un paciente sin que un médico lo haya revisado.

Y eso **no se resuelve con disciplina** ("acuérdate de revisarlo"). Se resuelve con el modelo de datos: **el sistema debe negarse a mostrar una nota que no esté marcada como revisada.** Seguro por construcción, no por memoria.

---

## 3. Modelo de datos

SQL directo, idempotente, sin migraciones EF. Prefijo `Plat*` (mismo módulo, mismo aislamiento).

```
PlatNotaClinica            -- una nota por grupo o por ingrediente
  Id                 INT IDENTITY PK
  TipoDestino        VARCHAR(12)      -- 'Grupo' | 'Ingrediente'
  DestinoId          INT              -- Id de PlatGrupo o PlatIngrediente
  Titulo             NVARCHAR(200)    -- "¿Puedo comer lácteos?"
  RevisadaPorMedico  BIT NOT NULL DEFAULT 0   -- ← EL CANDADO
  RevisadaPorUserId  UNIQUEIDENTIFIER NULL    -- quién la revisó
  FechaRevision      DATETIME2 NULL
  Activo             BIT NOT NULL DEFAULT 1
  FechaCreacion      DATETIME2
  UNIQUE (TipoDestino, DestinoId)

PlatNotaSeccion            -- las secciones, en orden
  Id                 INT IDENTITY PK
  NotaClinicaId      INT FK → PlatNotaClinica (ON DELETE CASCADE)
  Orden              INT
  Titulo             NVARCHAR(200)    -- "¿Qué suele pasar?", "Antes de eliminarlos"…
  Contenido          NVARCHAR(MAX)    -- texto; una viñeta por línea si empieza con "- "

PlatNotaReferencia         -- la bibliografía
  Id                 INT IDENTITY PK
  NotaClinicaId      INT FK → PlatNotaClinica (ON DELETE CASCADE)
  Orden              INT
  Titulo             NVARCHAR(500)
  Url                NVARCHAR(1000) NULL
```

**Índice en cada FK.** `ON DELETE CASCADE` en las dos aristas de pertenencia (secciones y referencias mueren con su nota) — igual que hicimos con los renglones de platillo.

**Retirar** `PlatGrupo.NotasEII` y `PlatIngrediente.NotasEII` del uso (dejar las columnas, no borrarlas; simplemente dejar de leerlas). El contenido vive ahora en las tablas nuevas.

### Glosario
Campo simple, el glosario ya tiene su contenido estructurado en su propio texto:
- `GlossaryTerm.Bibliografia` NVARCHAR(MAX) NULL.
- Se muestra como bloque "Referencias" al final del término, si no está vacío.

---

## 4. El candado — cómo se aplica

**En la vista del paciente** (`/Platillos/Ingrediente/{slug}`):
- Se muestran **únicamente** las notas con `RevisadaPorMedico = 1` **y** `Activo = 1`.
- Si la nota existe pero **no** está revisada, **no se muestra nada**. Ni el texto, ni un aviso, ni un placeholder. Simplemente no está.
- El bloque "Referencias" se arma desde `PlatNotaReferencia`, con `rel="noopener"`.

**En el admin:**
- CRUD de notas: crear/editar la nota, sus secciones (con orden) y sus referencias.
- Badge visible del estado: **"Pendiente de revisión médica"** (rojo) o **"Revisada por [nombre] el [fecha]"** (verde).
- El botón para marcar como revisada debe llevar una advertencia explícita:
  > **Solo marca esto si un médico revisó el texto.** Al marcarlo, la nota se vuelve visible para los pacientes. Quedará registrado quién la aprobó.
- Al marcar, se guarda `RevisadaPorUserId` y `FechaRevision`. **Hay responsable.**
- Cualquier **edición del contenido** de una nota ya revisada **la regresa a "no revisada"**. Si se cambia el texto, hay que revisarlo otra vez. Sin excepciones.

*(Esa última regla es la que evita que alguien edite un texto aprobado y le meta algo que nadie validó.)*

---

## 5. Seed — cargar las 24 notas

Fuente: **`Documentation/BORRADOR-notas-clinicas-v2.md`** (ya está en el repo).

Contiene **18 grupos** y **6 ingredientes**, cada uno con sus secciones y su bibliografía. Generar `SQL/seed-notas-clinicas.sql` a partir de ese documento.

**Reglas del seed:**
- Todas entran con **`RevisadaPorMedico = 0`**. Ninguna se muestra al paciente todavía. Es lo correcto: el contenido está cargado y listo para revisar, pero invisible hasta que un médico lo apruebe.
- Respetar el orden de las secciones tal como aparece en el documento.
- Las líneas que empiezan con `- ` son viñetas; conservarlas.
- Las notas marcadas *"(Dejar vacío)"* —el grupo `otro`— **no se cargan**.
- Idempotente (`NOT EXISTS`), como todos los seeds del proyecto.

**Ignorar las líneas de *"Revisor:"*** del documento — son instrucciones para el médico, no contenido para el paciente. (Pero conviene que Claude Code las extraiga a un archivo aparte, `Documentation/notas-clinicas-puntos-a-revisar.md`, para que el médico las tenga a la mano.)

---

## 6. Criterios de aceptación

1. Las tablas existen; índices en las FKs; cascade en las dos aristas de pertenencia.
2. Las 24 notas cargadas, con sus secciones en orden y sus referencias.
3. **Ninguna nota se muestra al paciente** — todas están en `RevisadaPorMedico = 0`.
4. Al marcar una como revisada desde el admin, **aparece** en la vista del ingrediente, con su bloque de Referencias.
5. Al **editar** una nota revisada, vuelve a "pendiente" y **desaparece** de la vista del paciente.
6. El glosario acepta y muestra su bibliografía.
7. Aislamiento intacto: solo `Plat*` + `idUsuario`. (El `GlossaryTerm.Bibliografia` es un cambio deliberado y aparte.)

## 7. Fases

- **F1:** tablas + seed + que el paciente no vea nada (candado cerrado).
- **F2:** CRUD admin de notas (secciones, referencias, marcar revisada).
- **F3:** bibliografía en el glosario.
