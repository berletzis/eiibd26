using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace eiibd26.Models
{
    public class tratamientos
{
    [Key]
    public int id { get; set; }
    [Required]
    [StringLength(250)]
    public string? nombre { get; set; }
    public int? idPadre { get; set; }
    public int? idIdioma { get; set; }
    [StringLength(50)]
    public string? icono { get; set; }
    public DateTime fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    public virtual ICollection<tratamientos> Hijos { get; set; }
    public virtual tratamientos Padre { get; set; }
    public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
}
}