namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Atributo de USO de un ingrediente en ESE platillo (crudo/frito/en jugo).
    /// Para alguien con EII la cebolla cruda y la cocida no son lo mismo.
    /// Clave compuesta (PlatilloIngredienteId, AtributoId) — configurada en OnModelCreating.
    /// </summary>
    public class PlatPlatilloIngredienteAtributo
    {
        public int PlatilloIngredienteId { get; set; }
        public int AtributoId { get; set; }

        public virtual PlatPlatilloIngrediente? PlatilloIngrediente { get; set; }
        public virtual PlatAtributo? Atributo { get; set; }
    }
}
