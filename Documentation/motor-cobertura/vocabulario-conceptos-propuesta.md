# Motor de Cobertura — Propuesta de vocabulario ampliado (TipoTermino=3)

> Estado: **PROPUESTA para revisión del usuario**. Nada cargado a la BD todavía.
> Objetivo: cubrir los ~20-25 artículos reales que hoy quedan con firma vacía
> (condiciones EII, dieta, embarazo, salud mental, experiencia del paciente).

Instrucciones de revisión: marca en la columna **OK** (✅ mantener / ❌ quitar) y
agrega en "Términos a añadir" los que falten. Cuando esté aprobada, se genera el
SQL idempotente (INSERT ... WHERE NOT EXISTS por Nombre) que **tú** ejecutas, con
`TipoTermino=3`, `Activo=1`, `MedicalRelationSuggestedId=1` (Directa).

Nota de matching: la firma normaliza (minúsculas, sin acentos) y cuenta cada término
como **frase completa**. Por eso conviene incluir tanto la forma larga
("enfermedad de crohn") como la forma corta/distintiva ("crohn") como términos
separados — cada uno es una dimensión de la firma.

---

## A. Condiciones EII

| Término | OK |
|---|---|
| Enfermedad Inflamatoria Intestinal | |
| EII | |
| Enfermedad de Crohn | |
| Crohn | |
| Colitis Ulcerosa | |
| Colitis Ulcerosa Crónica Idiopática | |
| CUCI | |
| Colitis indeterminada | |
| Proctitis | |
| Proctitis ulcerosa | |
| Ileítis | |
| Pancolitis | |
| Enfermedad perianal | |
| Fístula | |
| Estenosis | |
| Reservoritis | |
| Pouchitis | |

## B. Estados clínicos / curso de la enfermedad

| Término | OK |
|---|---|
| Brote | |
| Remisión | |
| Recaída | |
| Recidiva | |
| Actividad inflamatoria | |
| Cronicidad | |
| Enfermedad crónica | |
| Diagnóstico | |
| Comorbilidad | |

## C. Anatomía / fisiología / procesos

| Término | OK |
|---|---|
| Intestino | |
| Intestino delgado | |
| Intestino grueso | |
| Colon | |
| Íleon | |
| Recto | |
| Mucosa | |
| Mucosa intestinal | |
| Tracto digestivo | |
| Sistema inmune | |
| Sistema inmunológico | |
| Autoinmune | |
| Inflamación | |
| Microbiota | |
| Microbioma | |

## D. Dieta / alimentación / nutrición

| Término | OK |
|---|---|
| Dieta | |
| Alimentación | |
| Nutrición | |
| Fibra | |
| Dieta baja en residuos | |
| Dieta baja en FODMAP | |
| FODMAP | |
| Intolerancia | |
| Lactosa | |
| Gluten | |
| Probióticos | |
| Prebióticos | |
| Suplementos | |
| Hidratación | |
| Desnutrición | |

## E. Embarazo / reproducción

| Término | OK |
|---|---|
| Embarazo | |
| Gestación | |
| Lactancia | |
| Fertilidad | |
| Concepción | |
| Parto | |
| Nacimiento | |
| Anticoncepción | |

## F. Salud mental / emocional / calidad de vida

| Término | OK |
|---|---|
| Ansiedad | |
| Depresión | |
| Estado de ánimo | |
| Estrés | |
| Calidad de vida | |
| Bienestar | |
| Salud mental | |
| Fatiga emocional | |
| Aislamiento | |

## G. Experiencia del paciente / advocacy / autocuidado

| Término | OK |
|---|---|
| Autocuidado | |
| Adherencia | |
| Relación médico-paciente | |
| Comunidad | |
| Apoyo | |
| Empoderamiento | |
| Estigma | |
| Gaslighting médico | |
| Ostomía | |
| Bolsa de ostomía | |
| Colostomía | |
| Ileostomía | |
| Discapacidad | |
| Baño | |

## H. Diagnóstico / pruebas / seguimiento

| Término | OK |
|---|---|
| Colonoscopia | |
| Endoscopia | |
| Biopsia | |
| Calprotectina | |
| Calprotectina fecal | |
| Resonancia magnética | |
| Marcadores inflamatorios | |
| Seguimiento | |
| Cirugía | |
| Resección | |

## I. Manifestaciones extraintestinales (posible solape con síntomas ya cargados)

> ⚠️ Revisar solape: algunos ya podrían existir como TipoTermino=1 (Síntoma).
> Si ya existen con relación Directa, NO duplicar.

| Término | OK |
|---|---|
| Artritis | |
| Uveítis | |
| Eritema nodoso | |
| Manifestación extraintestinal | |
| Osteoporosis | |
| Anemia | |

---

## Términos a añadir (usuario)

- …

## Términos a quitar (usuario)

- …
