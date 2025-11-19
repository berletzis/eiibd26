using System;

namespace eiibd26.DTOs
{
    public class ContenidoDto
    {
        public int? Id { get; set; }
        public int IdTipo { get; set; }
        public int? IdDetalleOfertaTrabajo { get; set; }
        public string? ContenidoTitulo { get; set; }
        public string? ContenidoTextoC { get; set; }
        public string? ContenidoTextoL { get; set; }
        public string? ContenidoTituloSlug { get; set; }
        public string? URLImagenPrincipal { get; set; }
        public int? EstadoPublicacion { get; set; }
        public DateTime? ContenidoFechaInicio { get; set; }
        public DateTime? ContenidoFechaFin { get; set; }
        public Guid? IdAutor { get; set; }
        public string? Autor { get; set; }
        public int? IdEmpresa { get; set; }
        public string? PaisClave { get; set; }
        public Guid? IdUser { get; set; }
    }
}