using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class condicionUsuario
{
    [Key]
    public int id { get; set; }
    public int? idCondicion { get; set; }
    public Guid idUsuario { get; set; }
    public DateTime? fechaInicio { get; set; }
    public DateTime? fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }
    

        [ForeignKey(nameof(idCondicion))]
    public virtual condiciones Condicion { get; set; }
    [ForeignKey(nameof(idUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
    public virtual ICollection<TratamientoCondicionUsuario> TratamientoCondicionUsuarios { get; set; }
}
}