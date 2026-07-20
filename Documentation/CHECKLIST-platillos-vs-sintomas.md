# Checklist comparativo — Platillos ↔ Síntomas (glosario)

**Fecha:** 14 JUL 2026
**Para:** entender cómo funciona el módulo de Platillos y su flujo médico, comparado con el de Síntomas (ej. "diarrea") que ya conocías.

---

## 1. Primero, las tres piezas de Platillos (aquí estaba la confusión)

| Pieza | Qué es | ¿Tiene contenido médico? |
|---|---|---|
| **Platillo** | Una receta (Lasaña de acelga y queso). Datos + lista de ingredientes. | **No.** Es solo una combinación. |
| **Ingrediente / Grupo** | Queso, cebolla… (o el grupo: lácteos, mariscos). | Sí — cada uno puede tener una nota. |
| **Nota clínica** | El "¿Puedo comer queso?" — el texto educativo (qué es, qué pasa, importante, bibliografía). | **Sí. Esto es lo que el médico valida.** |

La regla de oro: **el platillo NO se valida; el conocimiento (la nota del ingrediente) SÍ.** Un platillo no se juzga "bueno" o "malo" — eso depende del paciente, no de la receta.

---

## 2. Comparación lado a lado

| | **Síntomas (diarrea)** | **Platillos (nota de ingrediente)** |
|---|---|---|
| **El contenido** | Descripción del síntoma | Nota clínica del ingrediente/grupo |
| **Quién lo escribe** | Admin (`Admin/Sintomas`) | Admin (`Admin/Platillos/Ingredientes`, panel lateral) |
| **Qué lo publica al paciente** | Toggle admin "Registro activo" | Toggle admin **"Publicado"** ← el candado |
| **Dónde valida el médico** | En la página pública del término, card solo-médico en el sidebar | En la página pública del ingrediente, card solo-médico en el sidebar |
| **¿El médico publica?** | **No.** Es señal de confianza. Publica el admin. | **No.** Igual — publica el admin. |
| **Qué ve el paciente** | Descripción + sello "Validado por Profesionales de la Salud" + consenso de relación | Nota + sello "Validado por Profesionales de la Salud" |
| **Capa extra** | — | El **platillo** (receta), que NO se valida — con un card amarillo que lo explica |

**El patrón es el mismo en los dos: el admin decide qué se ve (publicar); el médico agrega confianza (validar). Son dos ejes independientes — validar nunca publica.**

---

## 3. Cómo hace el médico el consenso (tu pregunta puntual)

### En Síntomas (diarrea) — consenso GRADUADO
1. El médico abre la página pública de "diarrea".
2. En el sidebar, en el card solo-médico, hace **dos cosas**:
   - **Valida el contenido** (con comentario clínico opcional).
   - **Valida la relación con EII**: elige un **nivel** — Directa / Indirecta / Secundaria — y comenta.
3. Varios médicos hacen lo mismo. El sistema **cuenta los votos por nivel**: "Consenso médico: Directa (3), Indirecta (1)".
4. El nivel con más votos es el consenso que ve el paciente.

→ Es un consenso **con grados**: no solo "sí/no", sino *qué tan* relacionado está con la EII.

### En Platillos (nota de ingrediente) — consenso BINARIO (endoso)
1. El médico abre la página pública del ingrediente (ej. "queso").
2. En el sidebar, en el card solo-médico ("Validar nota de: queso"), agrega un **comentario clínico opcional** y da **"Validar contenido"**.
3. Varios médicos hacen lo mismo. El sello muestra "Validado por Profesionales de la Salud" con el conteo/avatares.
4. **No hay niveles** — es un respaldo directo: "yo, como profesional, respaldo esta nota".

→ Es un consenso **binario**: la nota está respaldada por N profesionales, sin gradación.

**Nota elegante del diseño:** la validación vive **sobre la nota**, no sobre la página. El médico valida la nota de "lácteos" una vez → cuenta en las páginas de queso, leche, yogur… No hay que validar lo mismo diez veces.

---

## 4. La única diferencia real de diseño

| | Síntomas | Platillos |
|---|---|---|
| Tipo de consenso | **Graduado** (nivel de relación: Directa/Indirecta/Secundaria) | **Binario** (respaldado por N médicos) |

Todo lo demás es idéntico. Si algún día quieres que las notas de platillos también tengan un nivel (ej. "evidencia fuerte / moderada / débil"), se puede sumar reusando el mismo mecanismo del glosario — pero binario es más simple y suficiente para arrancar.

---

## 5. Estado del módulo (qué está vivo)

- ✅ Paciente: listado de platillos filtrado por su perfil, detalle de platillo, detalle de ingrediente ("¿Puedo comer…?"), badge en ingredientes con nota, disclaimers.
- ✅ Admin: catálogo de platillos e ingredientes, editor de notas + toggle **Publicado**.
- ✅ Médico: valida notas en la página pública, sello de consenso.
- ✅ Candado: solo el admin publica; validar es señal, no interruptor. En un solo servicio.
- ⏳ Pendiente (deploy): revertir la nota de queso a no-publicada antes de subir a producción (quedó publicada para pruebas de diseño local).
