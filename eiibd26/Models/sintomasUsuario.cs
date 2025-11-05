using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class sintomasUsuario
{
    [Key]
    public int id { get; set; }
    public int idSintoma { get; set; }
    public Guid idUsuario { get; set; }
    public DateTime fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    [ForeignKey(nameof(idSintoma))]
    public virtual sintomas Sintoma { get; set; }
    [ForeignKey(nameof(idUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
    public virtual ICollection<TratamientoSintomaUsuario> TratamientoSintomaUsuarios { get; set; }
}