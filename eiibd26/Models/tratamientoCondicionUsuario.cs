using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class TratamientoCondicionUsuario
{
    [Key]
    public int Id { get; set; }
    public Guid IdUsuario { get; set; }
    public int? IdCondicionUsuario { get; set; }
    public int? IdTratamientoUsuario { get; set; }
    [StringLength(512)]
    public string? Notas { get; set; }
    public DateTime FechaCreado { get; set; }

    [ForeignKey(nameof(IdUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
    [ForeignKey(nameof(IdCondicionUsuario))]
    public virtual condicionUsuario CondicionUsuario { get; set; }
    [ForeignKey(nameof(IdTratamientoUsuario))]
    public virtual tratamientoUsuario TratamientoUsuario { get; set; }
}
}