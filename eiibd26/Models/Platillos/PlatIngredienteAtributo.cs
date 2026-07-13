namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Atributo INTRÍNSECO de un ingrediente (cómo es siempre: gluten, lactosa, picante…).
    /// Clave compuesta (IngredienteId, AtributoId) — configurada en OnModelCreating.
    /// </summary>
    public class PlatIngredienteAtributo
    {
        public int IngredienteId { get; set; }
        public int AtributoId { get; set; }

        public virtual PlatIngrediente? Ingrediente { get; set; }
        public virtual PlatAtributo? Atributo { get; set; }
    }
}
