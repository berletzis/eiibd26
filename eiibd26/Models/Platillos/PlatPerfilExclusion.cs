using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Lo que el paciente declaró que NO tolera. Dato de usuario → soft delete con Eliminado,
    /// igual que condicionUsuario. Polimórfico: Tipo indica a qué apunta RefId.
    /// Único filtrado en SQL: (idUsuario, Tipo, RefId) WHERE Eliminado = 0.
    /// </summary>
    public class PlatPerfilExclusion
    {
        [Key]
        public int Id { get; set; }
        public Guid idUsuario { get; set; }
        public string Tipo { get; set; } = "";           // 'Grupo' | 'Ingrediente' | 'Atributo'
        public int RefId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Eliminado { get; set; }
        public DateTime? FechaEliminado { get; set; }
    }
}
