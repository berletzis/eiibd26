using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>Categoría del platillo (Entrada, Plato fuerte, Ensalada…). Catálogo con baja lógica.</summary>
    public class PlatCategoria
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        /// <summary>Definición taxonómica de la categoría (qué agrupa). NO clínico: no afirma tolerancia
        /// ni cita fuentes. Editable en el CRUD; se puede autogenerar con IA. Nullable, va con SQL directo.</summary>
        public string? Descripcion { get; set; }

        public virtual ICollection<PlatPlatillo> Platillos { get; set; } = new List<PlatPlatillo>();
    }
}
