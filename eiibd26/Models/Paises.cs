using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class Paises
{
    [Key]
    [StringLength(2)]
    public string PaisCodigo { get; set; }

    [Required]
    [StringLength(52)]
    public string PaisNombre { get; set; }

    [Required]
    [StringLength(50)]
    public string PaisContinente { get; set; }

    [Required]
    [StringLength(26)]
    public string PaisRegion { get; set; }

    [Required]
    [StringLength(45)]
    public string PaisNombreLocal { get; set; }

    public int? PaisCapital { get; set; }

    [Required]
    public bool VIsibleBuscador { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    [Required]
    public DateTime FechaModificacion { get; set; }

    public Guid? UsuarioCreacion { get; set; }

    public Guid? UsuarioModificacion { get; set; }

    [Required]
    public bool Borrado { get; set; }

    public decimal? PaisLat { get; set; }
    public decimal? PaisLong { get; set; }

    public double? LatitudDefault { get; set; }
    public double? LongitudDefault { get; set; }

    // Navegación para zonas horarias
    public virtual ICollection<ZonaHoraria> ZonasHorarias { get; set; }
}
}