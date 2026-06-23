namespace eiibd26.Models
{
    /// <summary>
    /// DTO simple para un Dynamic Template leído EN VIVO desde la API de SendGrid
    /// (GET /v3/templates?generations=dynamic). Solo Id + Nombre; el etiquetado de
    /// "Fase" se resuelve cruzando contra SendGrid:Templates en el handler.
    /// </summary>
    public sealed record SendGridApiTemplate
    {
        public string Id { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
    }
}
