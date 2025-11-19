using System;

namespace eiibd26.DTOs
{
    public class ContenidoListItemDto
    {
        public int Id { get; set; }
        public string ContenidoTitulo { get; set; }
        public string Excerpt { get; set; }
        public DateTime FechaCreado { get; set; }
        public int EstadoPublicacion { get; set; }
        public Guid IdAutor { get; set; }
        public string Autor { get; set; }
        public bool Eliminado { get; set; }
    }
}