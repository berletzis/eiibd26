using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace eiibd26.Models
{
    public class estudiosLabUsuario
{
    [Key]
    public int id { get; set; }
    public int idestudiosLab { get; set; }
    public Guid idUsuario { get; set; }
    public DateTime fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    [ForeignKey(nameof(idestudiosLab))]
    public virtual estudiosLab Estudio { get; set; }
    [ForeignKey(nameof(idUsuario))]
    public virtual ApplicationUser Usuario { get; set; }
}
}