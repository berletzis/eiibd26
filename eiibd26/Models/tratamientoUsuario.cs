using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class tratamientoUsuario
{
    [Key]
    public int id { get; set; }
    public int? IdCondicion { get; set; }
    public int? idTratamiento { get; set; }
    public Guid idUsuario { get; set; }
        public DateTime fechaInicio { get; set; }
        public DateTime? fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    [ForeignKey(nameof(IdCondicion))]
    public virtual condiciones Condicion { get; set; }
    [ForeignKey(nameof(idTratamiento))]
    public virtual tratamientos Tratamiento { get; set; }
    [ForeignKey(nameof(idUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
    public virtual ICollection<TratamientoCondicionUsuario> TratamientoCondicionUsuarios { get; set; }
    public virtual ICollection<TratamientoSintomaUsuario> TratamientoSintomaUsuarios { get; set; }
}
}