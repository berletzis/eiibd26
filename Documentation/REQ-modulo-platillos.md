# Requerimiento — Módulo de Platillos

**Fecha:** 13 JUL 2026
**Tipo:** módulo nuevo, **autocontenido**
**Nota sobre el Excel:** el archivo de captura del becario fue un **artefacto de diseño de una sola vez** — sirvió para descubrir la estructura de datos. **No se construye importador.** Sus 17 platillos entran por un seed SQL; de ahí en adelante, la captura vive en el CRUD del admin.

---

## 0. Regla dura: aislamiento

El módulo debe funcionar **totalmente independiente**. No debe afectar ni modificar nada de lo existente.

- **Tablas propias con prefijo `Plat`.** No se toca ninguna tabla existente.
- **Única relación externa permitida:** `idUsuario` (uniqueidentifier) de Identity, igual que `condicionUsuario`.
- **NO toca:** condiciones, síntomas, tratamientos, mood, laboratorios, Motor de Cobertura, NINA, GRIS, Conectar3eros.
- Si se apaga el módulo, la plataforma sigue funcionando idéntica.

**Autorización para crear esquema:** Claude Code puede crear las tablas y relaciones **directamente en producción** usando la cadena de conexión del proyecto. No necesita pedir autorización.

---

## 1. Objetivo

Que el paciente diga **"no tolero cebolla / lácteos / picante / crudo"** y la plataforma le muestre solo los platillos que sí puede comer.

Son **platillos, no recetas** — decisión deliberada por derechos de autor.

---

## 2. Regla de oro (derechos de autor)

Nunca se copian los pasos de preparación de la fuente. `PasosResumidos` se escribe **con palabras propias** (2–3 líneas). Sí se pueden tomar: nombre, ingredientes, cantidades, tiempo, porciones. Siempre se guarda y muestra **crédito + enlace** a la fuente.

**Sin imágenes en la v1.** Las fotos de recetas tienen derechos igual que el texto: enlazar al original no autoriza a copiar su foto. Se suman después con banco propio o licenciado.

---

## 3. Hallazgo de modelado (el porqué del diseño)

El campo `Grupo` del Excel original mezclaba **dos ejes**, y eso rompía el filtro:

- Lo que el alimento **ES** (lácteo, verdura, cereal…).
- Cómo **se comporta / se usa** (picante, cítrico, crudo, frito…).

Contradicciones reales encontradas: `cebolla` estaba como grupo "fibra-cruda", pero las notas decían "cocida es más tolerable" — la cebolla no *es* cruda, se *usa* cruda. `zanahoria` estaba como "verdura-cocida" pero en P003 se usa rallada (cruda).

**Solución — tres niveles:**

| Nivel | Qué es | Dónde vive |
|---|---|---|
| **Grupo** | Qué ES el alimento | Ingrediente |
| **Atributo intrínseco** | Cómo es siempre (picante, cítrico, gluten) | Ingrediente |
| **Atributo de uso** | Cómo se usa en ESE platillo (crudo, frito, en jugo) | Relación platillo↔ingrediente |

### Errores de clasificación ya corregidos en la plantilla v2
| Ingrediente | Estaba | Correcto | Por qué importa |
|---|---|---|---|
| `atún` | marisco | **pescado** | Un pez no es marisco. Quien excluya mariscos perdía el atún sin razón. |
| `leche de coco` | lácteo | **bebida** | NO es lácteo — es el sustituto de quien no tolera lácteos. Se lo estábamos quitando. |
| `pollo` | carne-roja | **ave** | En EII la carne roja y el ave se toleran distinto. |

---

## 4. Modelo de datos

Crear con **SQL directo** en `SQL/create-platillos.sql` (convención del repo; **sin migraciones EF**). PKs `INT IDENTITY`, índice en cada FK, `DATETIME2`.

```
PlatGrupo                  Id, Nombre, Orden, Activo
PlatAtributo               Id, Nombre, Ambito ('Ingrediente'|'Uso'), Descripcion, Activo
PlatCategoria              Id, Nombre, Orden, Activo
PlatUnidad                 Id, Nombre, Activo

PlatIngrediente            Id, Nombre (único, minúscula, singular), GrupoId FK,
                           NotasEII, Activo, FechaCreacion
PlatIngredienteAtributo    IngredienteId FK, AtributoId FK        -- atributos INTRÍNSECOS

PlatPlatillo               Id, Codigo (P001, único), Nombre, CategoriaId FK, Porciones,
                           TiempoPrepMin, Dificultad, PasosResumidos, FuenteNombre,
                           FuenteUrl, Notas, Activo, FechaCreacion, CreadoPorUserId

PlatPlatilloIngrediente    Id, PlatilloId FK, IngredienteId FK, TextoOriginal,
                           Cantidad (null), UnidadId FK (null), EsAlGusto bit, NotaPreparacion
PlatPlatilloIngredienteAtributo   PlatilloIngredienteId FK, AtributoId FK   -- atributos de USO

PlatPerfilExclusion        Id, idUsuario (uniqueidentifier), Tipo ('Grupo'|'Ingrediente'|'Atributo'),
                           RefId, FechaCreacion, Eliminado bit, FechaEliminado
                           UNIQUE (idUsuario, Tipo, RefId) WHERE Eliminado = 0
```

**Convenciones verificadas en el repo:** soft delete con `Eliminado bit` en tablas ligadas al usuario (igual que `condicionUsuario`); `Activo bit` en catálogos; `idUsuario` es **uniqueidentifier**, no string.

---

## 5. Vocabulario controlado (sembrar)

**Grupos:** lácteo · huevo · carne · ave · embutido · pescado · marisco · verdura · fruta · fruto-seco · cereal · legumbre · tubérculo · hongo · grasa · condimento · bebida · otro

**Atributos intrínsecos:** gluten · lactosa · picante · cítrico · alcohol · graso · fibra-insoluble · cafeína

**Atributos de uso:** crudo · frito · en jugo

**Categorías:** Entrada · Plato fuerte · Ensalada · Sopa · Snack · Postre · Bebida · Guarnición

**Unidades:** pieza · taza · g · kg · ml · l · cda · cdta · al gusto · paquete · bandeja · tarro

---

## 6. Carga inicial (una sola vez) — SIN importador

El Excel del becario fue un **artefacto de diseño**: sirvió para descubrir la estructura de datos y ya cumplió. **No se construye importador**, ni pipeline continuo, ni exportación de plantilla.

La captura de platillos, de aquí en adelante, se hace **exclusivamente desde el CRUD del admin** (§7).

**Carga inicial:** un script de datos de una sola vez, `SQL/seed-platillos.sql`, con:
- El vocabulario controlado de §5 (grupos, atributos, categorías, unidades).
- Los **17 platillos** ya capturados, sus **99 relaciones** y los **57 ingredientes**, con las tres correcciones de clasificación de §3 aplicadas y los nombres ya normalizados (singular, minúscula, sin duplicados).

Se corre una vez y se olvida.

---

## 7. Sección de ADMIN "Platillos"

Ubicación: `Areas/Identity/Pages/Admin/Platillos/` — mismo patrón que `Admin/Contenidos`. **Sección propia** en `_SidebarMenu.cshtml`, con sus sub-entradas.

**El admin es el ÚNICO camino de captura, y va a seguir subiendo platillos indefinidamente.** No hay importador. Esta sección es donde se construye todo el catálogo — tiene que estar bien hecha.

### 7.0 Regla dura: todo lo que sea catálogo viene de una tabla y se elige de un combo

**Ningún valor de catálogo se escribe a mano.** `Grupo`, `Atributo`, `Categoría`, `Unidad` e `Ingrediente` **siempre** se seleccionan de una lista/combo alimentado por su tabla. Si el valor no existe, se da de alta **en su catálogo** (o en línea desde el mismo formulario) — nunca tecleándolo suelto en el campo.

*Por qué:* es exactamente el error que encontramos en el Excel. "Verdura" y "verdura" tecleadas a mano son dos grupos distintos para el filtro; "papas" y "papa" son dos alimentos. El resultado es que **al paciente le mostramos comida que no tolera**. El combo lo hace imposible.

**Sí son texto libre** (no son catálogo): `NombrePlatillo`, `PasosResumidos`, `FuenteNombre`, `FuenteUrl`, `Notas`, `TextoOriginal`, `NotaPreparacion`, `NotasEII`, `Cantidad`.

### 7.1 Sub-páginas

**Captura de platillos**
- `Index` — grilla con búsqueda, filtro por categoría y paginación. Alta / edición / baja (soft delete).
- `Detalle` — alta y edición de un platillo:
  - Datos generales: nombre, **categoría (combo)**, porciones, tiempo, dificultad (combo), pasos resumidos, fuente + URL, notas.
  - Sus ingredientes, uno por renglón: **ingrediente (autocomplete `.eii-autocomplete` contra el catálogo)**, texto original, cantidad, **unidad (combo)**, "al gusto" (check), nota de preparación, y los **checks de uso**: crudo / frito / en jugo.
  - **Alta de ingrediente en línea:** si el ingrediente no existe, se puede crear ahí mismo (nombre + grupo por combo + atributos por check) sin abandonar la captura. Si no, capturar se vuelve un calvario y el admin termina buscando atajos.

**Catálogos administrables** (todos con alta, edición y baja lógica):
- `Ingredientes` — nombre, **grupo (combo)**, **atributos intrínsecos (checks desde la tabla `PlatAtributo` con Ámbito='Ingrediente')**, notas EII.
- `Grupos` — nombre, orden.
- `Atributos` — nombre, **ámbito (combo: Ingrediente / Uso)**, descripción.
- `Categorías` — nombre, orden.
- `Unidades` — nombre.

### 7.2 Baja lógica en catálogos
Nunca `DELETE` físico. Al desactivar (`Activo = 0`) una entrada de catálogo: **deja de aparecer en los combos** para registros nuevos, pero **las referencias existentes siguen funcionando**. Así no se rompen los platillos ya capturados.

### 7.1 Card de reglas — OBLIGATORIO en la vista de captura

Card colapsable, `.eii-card` + `.eii-help-text`, título **"Cómo capturar un platillo (y por qué así)"**. Cada regla **con su porqué** — la regla sin razón se rompe sin querer:

1. **Los pasos van con tus propias palabras (2–3 líneas).** *Porque los pasos de la fuente tienen derechos de autor. Los datos sí los podemos tomar; el texto no.*
2. **Siempre acredita la fuente: nombre y enlace.** *Porque el mérito es de quien lo escribió. Enlazamos al original, nunca lo republicamos.*
3. **El TextoOriginal se copia tal cual.** *Porque nos da trazabilidad: si dudamos de una cantidad, verificamos de dónde salió.*
4. **El ingrediente va limpio, genérico y en singular.** *Porque el paciente filtra por ingrediente. Si cada platillo escribe "cebolla" distinto, el filtro no lo relaciona y **le mostramos comida que no tolera**.*
5. **Antes de inventar un ingrediente, revisa el catálogo.** *Porque "cebollas" junto a "cebolla" crea un duplicado que el filtro trata como dos alimentos distintos.*
6. **El ingrediente es el alimento base; la preparación va aparte.** "naranja" + uso "en jugo", nunca "jugo de naranja". *Porque si no, los ingredientes se multiplican y el filtro se rompe.*
7. **Marca cómo se usa en ESE platillo: crudo / frito / en jugo.** *Porque para alguien con EII la cebolla cruda y la cocida no son lo mismo. Mucha gente tolera cocido lo que no tolera crudo.*
8. **Grupo = qué ES el alimento. Atributos = cómo se comporta.** *Porque el paciente casi siempre filtra por grupo o atributo ("no tolero lácteos"), rara vez por un ingrediente suelto.*

---

## 8. Vista del PACIENTE — espejo de `Pages/Contenidos/Index`

Ubicación: **`Pages/Platillos/Index.cshtml`** (+ `.cshtml.cs`). **Pública, sin `[Authorize]`** — exactamente como `Pages/Contenidos/Index.cshtml.cs`, que no lo tiene.

Replicar su estructura tal cual:

- **Anónimo:** ve el catálogo completo, paginado. Puede buscar y filtrar por categoría.
- **Autenticado:** además carga **su perfil alimentario guardado** y con eso arma los chips de filtro — igual que Contenidos carga `AvailableConditions/Sintomas/Tratamientos` desde lo que el usuario ya tiene guardado. El paciente solo filtra por lo que declaró.
- **Filtros por query string**, IDs separados por coma, con el mismo `ParseIds` (`grupos=`, `ingredientes=`, `atributos=`).
- **Paginación** `PageNumber` / `PageSize` (default 9, tope 50) y búsqueda `q`, igual que Contenidos.
- Tarjetas con `.eii-card`, chips con `.eii-badge`, botones `.eii-btn`.

### 8.1 Regla del filtro
Un platillo **se descarta** si cualquiera de sus ingredientes:
- pertenece a un **grupo** excluido, **o**
- es un **ingrediente** excluido, **o**
- tiene un **atributo** excluido — intrínseco del ingrediente **o** de su uso en ese platillo.

### 8.2 Estado vacío — importa
Si el filtro deja **0 platillos, NO mostrar una lista vacía.** Un paciente con EII ya carga miedo alrededor de la comida; una pantalla en blanco lo confirma.

En su lugar: mostrar los platillos **más cercanos** indicando **qué exclusión incumplen** — *"este te serviría, solo tiene cebolla"*. Y siempre el conteo transparente: *"X de Y platillos cumplen tu perfil"*.

---

## 9. Perfil alimentario del paciente — espejo de `UsuarioCondiciones`

Ubicación: **`Areas/Identity/Pages/Usuario/UsuarioAlimentacion.cshtml`** (+ `.cshtml.cs`), con `[Authorize(Roles = "Paciente,Administrador")]` y el redirect de Médico, igual que las demás.

- El paciente marca lo que **no tolera**: por **grupo** (lácteos, picante…), por **ingrediente** (cebolla) y por **atributo** (crudo, gluten).
- Guarda en `PlatPerfilExclusion` con `idUsuario` + soft delete (`Eliminado`), igual que `condicionUsuario`.
- Handlers `OnPostAgregarExclusionAsync` / `OnPostEliminarExclusionAsync` devolviendo `JsonResult`, mismo patrón AJAX.
- Buscador de ingredientes con `.eii-autocomplete` (ya existe en el sistema de diseño).

---

## 10. Convenciones técnicas (verificadas en el repo — no inventar)

- **Razor Pages con PageModels**, no Controllers.
- **SQL directo** en `SQL/create-platillos.sql`. **Sin migraciones EF.**
- **No** Clean Architecture de 4 capas — seguir la estructura actual del repo.
- `AsNoTracking()` en lecturas; proyectar a DTO/VM con `Select()`; async siempre.
- **Solo tokens y componentes `eii-*`** (`--eii-primary`, `--eii-surface`, `.eii-card`, `.eii-btn`, `.eii-badge`, `.eii-autocomplete`). **No inventar CSS ni paleta** sin agotar el inventario existente.
- Índice en cada FK. Soft delete, nunca `DELETE` físico.

---

## 11. Fases

- **F1 — Datos:** `SQL/create-platillos.sql` (esquema) + `SQL/seed-platillos.sql` (vocabulario + los 17 platillos ya capturados). Se corre en producción.
- **F2 — Admin:** sección propia "Platillos" en el menú. Grilla + detalle de captura (todo por combos, autocomplete de ingredientes y alta en línea) + los 5 catálogos administrables (Ingredientes, Grupos, Atributos, Categorías, Unidades) + card de reglas.
- **F3 — Paciente:** perfil alimentario + vista de platillos con filtro (incluye el estado vacío bien resuelto).

## 12. Criterios de aceptación

1. Las tablas `Plat*` existen en producción; ninguna tabla existente fue modificada.
2. El seed carga limpio: 17 platillos, 99 relaciones, 57 ingredientes, **0 huérfanos**, y `atún`/`leche de coco`/`pollo` con su grupo corregido.
3. Desde el admin se puede capturar un platillo completo de punta a punta sin salir de la pantalla, **sin escribir a mano ningún valor de catálogo** (grupo, atributo, categoría, unidad e ingrediente salen de combos).
3b. Los 5 catálogos (Ingredientes, Grupos, Atributos, Categorías, Unidades) se pueden administrar desde el admin. Desactivar una entrada la saca de los combos pero **no rompe** los platillos que ya la usan.
4. El paciente guarda exclusiones y persisten tras recarga.
5. El filtro descarta correctamente por grupo, ingrediente y atributo (intrínseco **y** de uso).
6. Con 0 resultados se muestran los más cercanos con el motivo, nunca una lista vacía.
7. Cero CSS nuevo fuera de los tokens `eii-*`.

## 13. Fuera de alcance (explícito)

- **Importador de Excel** — el Excel fue un artefacto de diseño y ya cumplió. La captura vive en el admin.
- **Tolerancia aprendida / modelo bayesiano** → módulo **aparte e independiente** (tarea #16). Aquí la tolerancia es **declarada**, no inferida.
- Contribución de comunidad y profesionales verificados → v2.
- Imágenes de platillos → v2.
- Cualquier vínculo con síntomas, tratamientos o el dashboard de salud.

## 14. Nota de producto

Con **17 platillos**, un paciente que excluya lácteos + picante + crudo se queda con 2 o 3 opciones. El módulo **necesita volumen** antes de salir al paciente: apuntar a **100–150 platillos** con buena cobertura de categorías. El filtro puede estar listo mucho antes que el catálogo.
