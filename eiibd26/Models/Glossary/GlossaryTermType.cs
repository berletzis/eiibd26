namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Tipos de términos en el glosario médico
    /// </summary>
    public enum GlossaryTermType
    {
        /// <summary>
        /// Síntoma médico
        /// </summary>
        Sintoma = 1,

        /// <summary>
        /// Tratamiento médico
        /// </summary>
        Tratamiento = 2,

        /// <summary>
        /// Concepto general EII (condiciones, dieta, embarazo, salud mental,
        /// experiencia del paciente, anatomía/procesos). Usado por el Motor de
        /// Cobertura para enriquecer el vocabulario de la firma. Aún NO se muestra
        /// como categoría en el glosario público (mejora futura).
        /// </summary>
        ConceptoGeneralEII = 3
    }
}
