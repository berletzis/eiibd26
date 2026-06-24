using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace eiibd26.Models
{
    public class ApplicationUser : IdentityUser<Guid>
{
    public bool RequiresPasswordReset { get; set; }

    /// <summary>
    /// Soft-delete reversible (interruptor maestro). Cuando es true, el usuario
    /// queda excluido de TODOS los conteos vía UsuarioValidez.SoloValidos() y se
    /// le corta el acceso (lockout). Sus datos clínicos NO se tocan. Restaurable.
    /// </summary>
    public bool Eliminado { get; set; } = false;

    // Relaciones de navegación (opcional, útil para incluir datos relacionados)
    public virtual ICollection<condicionUsuario> CondicionesUsuario { get; set; }
    public virtual ICollection<estudiosLabUsuario> EstudiosLabUsuario { get; set; }
    public virtual ICollection<sintomasUsuario> SintomasUsuario { get; set; }
    public virtual ICollection<tratamientoUsuario> TratamientosUsuario { get; set; }
    public virtual ICollection<TratamientoCondicionUsuario> TratamientoCondicionUsuario { get; set; }
    public virtual ICollection<TratamientoSintomaUsuario> TratamientoSintomaUsuario { get; set; }
}
}