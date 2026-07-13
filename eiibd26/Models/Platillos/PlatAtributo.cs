using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Cómo se comporta / se usa un alimento. Ambito distingue el eje:
    /// 'Ingrediente' = atributo intrínseco (gluten, picante, cítrico…),
    /// 'Uso'         = atributo de uso en un platillo (crudo, frito, en jugo).
    /// </summary>
    public class PlatAtributo
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Ambito { get; set; } = "";   // 'Ingrediente' | 'Uso'
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }
}
