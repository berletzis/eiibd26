namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Niveles de relación médica de un término con la EII.
    /// </summary>
    public enum MedicalRelationType
    {
        /// <summary>Relación directa con la EII 🟢</summary>
        Directa = 1,

        /// <summary>Relación indirecta con la EII 🟡</summary>
        Indirecta = 2,

        /// <summary>Relación secundaria con la EII 🔵</summary>
        Secundaria = 3
    }
}
