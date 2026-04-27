using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models;

/// <summary>Catálogo de frecuencias de síntoma (inmutable, seed en DbContext).</summary>
public sealed class FrecuenciaSintomaCatalog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Orden de presentación en UI.</summary>
    public int Orden { get; set; }
}
