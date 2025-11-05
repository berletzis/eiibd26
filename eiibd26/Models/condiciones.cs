using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class condiciones
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

    // Navegación hijos
    public virtual ICollection<condiciones> Hijos { get; set; }
    public virtual condiciones Padre { get; set; }
    public virtual ICollection<condicionUsuario> CondicionesUsuario { get; set; }
    public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
}