namespace eiibd26.Models
{
    public sealed record SendGridTemplateInfo
    {
        public string Id { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        /// <summary>0 = uso general, 1-3 = fase de campaña</summary>
        public int Fase { get; init; }
    }
}
