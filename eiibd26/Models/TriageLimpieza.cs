namespace eiibd26.Models
{
    /// <summary>
    /// Eje 1 del triage de catálogo: ¿el registro es una intervención terapéutica real?
    /// NO decide relación con EII (ese es el eje 2, <see cref="Glossary.MedicalRelationType"/>):
    /// un procedimiento real sin relación con EII es <see cref="Valido"/>, no basura.
    /// Se persiste en <c>tratamientos.RevisionLimpiezaEstado</c> (TINYINT NULL).
    /// </summary>
    public enum TriageLimpieza : byte
    {
        /// <summary>Sustancia, medicamento, suplemento, cirugía, procedimiento, terapia,
        /// técnica, actividad física, cambio de hábito/dieta o terapia complementaria.</summary>
        Valido = 1,

        /// <summary>No es una intervención terapéutica: recordatorios, códigos de ensayo
        /// clínico, objetos sin uso terapéutico, texto sin sentido, títulos de libro.</summary>
        Basura = 2,

        /// <summary>Ambiguo → cola de revisión humana. NUNCA se auto-desactiva.</summary>
        Dudoso = 3
    }
}
