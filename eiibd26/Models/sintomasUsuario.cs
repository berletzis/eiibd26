using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class sintomasUsuario
{
    [Key]
    public int id { get; set; }
    public int? idSintoma { get; set; }
    public Guid idUsuario { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public DateTime? fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    /// <summary>Indica si este es el síntoma principal del usuario.</summary>
    public bool EsPrincipal { get; set; }

    [ForeignKey(nameof(idSintoma))]
    public virtual sintomas Sintoma { get; set; }
    [ForeignKey(nameof(idUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
    public virtual ICollection<TratamientoSintomaUsuario> TratamientoSintomaUsuarios { get; set; }
}
}