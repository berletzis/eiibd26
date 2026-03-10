namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// DTO para transferir definiciones médicas desde el dominio médico al glosario.
    /// NO expone todo el modelo, solo lo necesario (bajo acoplamiento).
    /// </summary>
    public class MedicalDefinitionDto
    {
        /// <summary>
        /// Nombre médico oficial
        /// </summary>
        public string Nombre { get; set; } = "";

        /// <summary>
        /// Descripción generada por IA (si existe)
        /// </summary>
        public string? DescripcionIA { get; set; }

        /// <summary>
        /// Indica si tiene relación con EII
        /// </summary>
        public bool RelacionEII { get; set; }

        /// <summary>
        /// Explicación de la relación con EII
        /// </summary>
        public string? RelacionEIIDescripcion { get; set; }

        /// <summary>
        /// Fuentes verificadas (si existen)
        /// </summary>
        public string? Fuentes { get; set; }
    }
}
