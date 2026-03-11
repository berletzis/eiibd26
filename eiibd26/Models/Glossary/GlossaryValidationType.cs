namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Tipos de validación humana sobre un término del glosario.
    /// </summary>
    public enum GlossaryValidationType
    {
        /// <summary>Validación de significado / descripción médica</summary>
        MeaningValidation = 1,

        /// <summary>Validación del nivel de relación con EII</summary>
        RelationValidation = 2
    }
}
