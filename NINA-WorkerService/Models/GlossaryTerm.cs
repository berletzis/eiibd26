namespace NINA_WorkerService.Models;

/// <summary>
/// Vista READ-ONLY del término del glosario (tabla GlossaryTerm, BD compartida del Web).
/// Solo las columnas que el Worker necesita para armar el vocabulario de la firma.
/// El Worker NO escribe esta tabla — es dueña del Web.
/// </summary>
public class GlossaryTerm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }

    /// <summary>Relación EII sugerida por NINA. 1 = Directa (filtro del vocabulario).</summary>
    public int? MedicalRelationSuggestedId { get; set; }
}
