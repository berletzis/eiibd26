using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public class ApplicationUser : IdentityUser<Guid>
{
    public bool RequiresPasswordReset { get; set; }

    // Relaciones de navegación (opcional, útil para incluir datos relacionados)
    public virtual ICollection<condicionUsuario> CondicionesUsuario { get; set; }
    public virtual ICollection<estudiosLabUsuario> EstudiosLabUsuario { get; set; }
    public virtual ICollection<sintomasUsuario> SintomasUsuario { get; set; }
    public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
    public virtual ICollection<TratamientoCondicionUsuario> TratamientoCondicionUsuario { get; set; }
    public virtual ICollection<TratamientoSintomaUsuario> TratamientoSintomaUsuario { get; set; }
}
}