using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class sintomas
{
    [Key]
    public int id { get; set; }
    [Required]
    [StringLength(250)]
    public string nombre { get; set; }
    public int? idPadre { get; set; }
    public int idIdioma { get; set; }
    [StringLength(50)]
    public string icono { get; set; }
    public DateTime fechaEliminado { get; set; }
    public DateTime fechaModificado { get; set; }
    public DateTime fechaCreado { get; set; }
    public bool Eliminado { get; set; }

    public virtual ICollection<sintomasUsuario> SintomasUsuario { get; set; }
}