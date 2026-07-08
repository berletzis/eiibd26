/* =====================================================================
   Motor de Cobertura — Carga de "Conceptos Generales EII" (TipoTermino=3)
   ---------------------------------------------------------------------
   Enriquece el vocabulario de la firma (Fase 1) con condiciones EII,
   dieta, embarazo, salud mental, experiencia del paciente, anatomía, etc.

   - TipoTermino = 3 (ConceptoGeneralEII)
   - Activo = 1                      -> requerido para que FirmaService lo tome
   - MedicalRelationSuggestedId = 1  -> Directa (filtro del vocabulario)
   - CreatedByAI = 0                 -> curado manualmente (no NINA)

   IDEMPOTENTE: cada concepto se inserta solo si NO existe ya un término
   con el mismo Nombre O el mismo Slug (evita duplicados y colisiones de
   slug con síntomas/tratamientos ya cargados). Re-ejecutable sin efecto.

   Ejecuta el USUARIO. Claude Code NO corre DML en producción.
   Tras ejecutarlo: correr "Forzar recálculo total" en
   /Identity/Admin/Contenidos/Firmas para regenerar las firmas.
   ===================================================================== */

SET NOCOUNT ON;

;WITH Conceptos (Nombre, Slug) AS (
    -- A. Condiciones EII
    SELECT N'Enfermedad Inflamatoria Intestinal', N'enfermedad-inflamatoria-intestinal' UNION ALL
    SELECT N'EII',                                 N'eii' UNION ALL
    SELECT N'Enfermedad de Crohn',                 N'enfermedad-de-crohn' UNION ALL
    SELECT N'Crohn',                               N'crohn' UNION ALL
    SELECT N'Colitis Ulcerosa',                    N'colitis-ulcerosa' UNION ALL
    SELECT N'Colitis Ulcerosa Crónica Idiopática', N'colitis-ulcerosa-cronica-idiopatica' UNION ALL
    SELECT N'CUCI',                                N'cuci' UNION ALL
    SELECT N'Colitis indeterminada',               N'colitis-indeterminada' UNION ALL
    SELECT N'Proctitis',                           N'proctitis' UNION ALL
    SELECT N'Proctitis ulcerosa',                  N'proctitis-ulcerosa' UNION ALL
    SELECT N'Ileítis',                             N'ileitis' UNION ALL
    SELECT N'Pancolitis',                          N'pancolitis' UNION ALL
    SELECT N'Enfermedad perianal',                 N'enfermedad-perianal' UNION ALL
    SELECT N'Fístula',                             N'fistula' UNION ALL
    SELECT N'Estenosis',                           N'estenosis' UNION ALL
    SELECT N'Reservoritis',                        N'reservoritis' UNION ALL
    SELECT N'Pouchitis',                           N'pouchitis' UNION ALL

    -- B. Estados clínicos / curso de la enfermedad
    SELECT N'Brote',                    N'brote' UNION ALL
    SELECT N'Remisión',                 N'remision' UNION ALL
    SELECT N'Recaída',                  N'recaida' UNION ALL
    SELECT N'Recidiva',                 N'recidiva' UNION ALL
    SELECT N'Actividad inflamatoria',   N'actividad-inflamatoria' UNION ALL
    SELECT N'Cronicidad',               N'cronicidad' UNION ALL
    SELECT N'Enfermedad crónica',       N'enfermedad-cronica' UNION ALL
    SELECT N'Diagnóstico',              N'diagnostico' UNION ALL
    SELECT N'Comorbilidad',             N'comorbilidad' UNION ALL

    -- C. Anatomía / fisiología / procesos
    SELECT N'Intestino',                N'intestino' UNION ALL
    SELECT N'Intestino delgado',        N'intestino-delgado' UNION ALL
    SELECT N'Intestino grueso',         N'intestino-grueso' UNION ALL
    SELECT N'Colon',                    N'colon' UNION ALL
    SELECT N'Íleon',                    N'ileon' UNION ALL
    SELECT N'Recto',                    N'recto' UNION ALL
    SELECT N'Mucosa',                   N'mucosa' UNION ALL
    SELECT N'Mucosa intestinal',        N'mucosa-intestinal' UNION ALL
    SELECT N'Tracto digestivo',         N'tracto-digestivo' UNION ALL
    SELECT N'Sistema inmune',           N'sistema-inmune' UNION ALL
    SELECT N'Sistema inmunológico',     N'sistema-inmunologico' UNION ALL
    SELECT N'Autoinmune',               N'autoinmune' UNION ALL
    SELECT N'Inflamación',              N'inflamacion' UNION ALL
    SELECT N'Microbiota',               N'microbiota' UNION ALL
    SELECT N'Microbioma',               N'microbioma' UNION ALL

    -- D. Dieta / alimentación / nutrición
    SELECT N'Dieta',                    N'dieta' UNION ALL
    SELECT N'Alimentación',             N'alimentacion' UNION ALL
    SELECT N'Nutrición',                N'nutricion' UNION ALL
    SELECT N'Fibra',                    N'fibra' UNION ALL
    SELECT N'Dieta baja en residuos',   N'dieta-baja-en-residuos' UNION ALL
    SELECT N'Dieta baja en FODMAP',     N'dieta-baja-en-fodmap' UNION ALL
    SELECT N'FODMAP',                   N'fodmap' UNION ALL
    SELECT N'Intolerancia',             N'intolerancia' UNION ALL
    SELECT N'Lactosa',                  N'lactosa' UNION ALL
    SELECT N'Gluten',                   N'gluten' UNION ALL
    SELECT N'Probióticos',              N'probioticos' UNION ALL
    SELECT N'Prebióticos',              N'prebioticos' UNION ALL
    SELECT N'Suplementos',              N'suplementos' UNION ALL
    SELECT N'Hidratación',              N'hidratacion' UNION ALL
    SELECT N'Desnutrición',             N'desnutricion' UNION ALL

    -- E. Embarazo / reproducción
    SELECT N'Embarazo',                 N'embarazo' UNION ALL
    SELECT N'Gestación',                N'gestacion' UNION ALL
    SELECT N'Lactancia',                N'lactancia' UNION ALL
    SELECT N'Fertilidad',               N'fertilidad' UNION ALL
    SELECT N'Concepción',               N'concepcion' UNION ALL
    SELECT N'Parto',                    N'parto' UNION ALL
    SELECT N'Nacimiento',               N'nacimiento' UNION ALL
    SELECT N'Anticoncepción',           N'anticoncepcion' UNION ALL

    -- F. Salud mental / emocional / calidad de vida
    SELECT N'Ansiedad',                 N'ansiedad' UNION ALL
    SELECT N'Depresión',                N'depresion' UNION ALL
    SELECT N'Estado de ánimo',          N'estado-de-animo' UNION ALL
    SELECT N'Estrés',                   N'estres' UNION ALL
    SELECT N'Calidad de vida',          N'calidad-de-vida' UNION ALL
    SELECT N'Bienestar',                N'bienestar' UNION ALL
    SELECT N'Salud mental',             N'salud-mental' UNION ALL
    SELECT N'Fatiga emocional',         N'fatiga-emocional' UNION ALL
    SELECT N'Aislamiento',              N'aislamiento' UNION ALL

    -- G. Experiencia del paciente / advocacy / autocuidado
    SELECT N'Autocuidado',              N'autocuidado' UNION ALL
    SELECT N'Adherencia',               N'adherencia' UNION ALL
    SELECT N'Relación médico-paciente', N'relacion-medico-paciente' UNION ALL
    SELECT N'Comunidad',                N'comunidad' UNION ALL
    SELECT N'Apoyo',                    N'apoyo' UNION ALL
    SELECT N'Empoderamiento',           N'empoderamiento' UNION ALL
    SELECT N'Estigma',                  N'estigma' UNION ALL
    SELECT N'Gaslighting médico',       N'gaslighting-medico' UNION ALL
    SELECT N'Ostomía',                  N'ostomia' UNION ALL
    SELECT N'Bolsa de ostomía',         N'bolsa-de-ostomia' UNION ALL
    SELECT N'Colostomía',               N'colostomia' UNION ALL
    SELECT N'Ileostomía',               N'ileostomia' UNION ALL
    SELECT N'Discapacidad',             N'discapacidad' UNION ALL
    SELECT N'Baño',                     N'bano' UNION ALL

    -- H. Diagnóstico / pruebas / seguimiento
    SELECT N'Colonoscopia',             N'colonoscopia' UNION ALL
    SELECT N'Endoscopia',               N'endoscopia' UNION ALL
    SELECT N'Biopsia',                  N'biopsia' UNION ALL
    SELECT N'Calprotectina',            N'calprotectina' UNION ALL
    SELECT N'Calprotectina fecal',      N'calprotectina-fecal' UNION ALL
    SELECT N'Resonancia magnética',     N'resonancia-magnetica' UNION ALL
    SELECT N'Marcadores inflamatorios', N'marcadores-inflamatorios' UNION ALL
    SELECT N'Seguimiento',              N'seguimiento' UNION ALL
    SELECT N'Cirugía',                  N'cirugia' UNION ALL
    SELECT N'Resección',                N'reseccion' UNION ALL

    -- I. Manifestaciones extraintestinales (posible solape con síntomas)
    SELECT N'Artritis',                     N'artritis' UNION ALL
    SELECT N'Uveítis',                      N'uveitis' UNION ALL
    SELECT N'Eritema nodoso',               N'eritema-nodoso' UNION ALL
    SELECT N'Manifestación extraintestinal', N'manifestacion-extraintestinal' UNION ALL
    SELECT N'Osteoporosis',                 N'osteoporosis' UNION ALL
    SELECT N'Anemia',                       N'anemia'
)
INSERT INTO dbo.GlossaryTerm
    (Nombre, Slug, TipoTermino, Activo, FechaCreacion, CreatedByAI, MedicalRelationSuggestedId)
SELECT
    c.Nombre, c.Slug, 3, 1, SYSDATETIME(), 0, 1
FROM Conceptos c
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.GlossaryTerm g
    WHERE g.Nombre = c.Nombre OR g.Slug = c.Slug
);

PRINT CONCAT('Conceptos EII insertados en esta corrida: ', @@ROWCOUNT);

-- Verificación
SELECT COUNT(*) AS ConceptosTipo3Activos
FROM dbo.GlossaryTerm
WHERE TipoTermino = 3 AND Activo = 1;

SELECT COUNT(*) AS VocabularioFirma_DirectaActivos
FROM dbo.GlossaryTerm
WHERE Activo = 1 AND MedicalRelationSuggestedId = 1;   -- debe subir ~120 -> ~220
