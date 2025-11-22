using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models
{
    public class ContenidoCondicion
    {
        [Key]
        public int Id { get; set; }
        public int ContenidoId { get; set; }
        public int CondicionId { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public Guid UsuarioCreacion { get; set; }
        public Guid UsuarioModificacion { get; set; }
        public bool Borrado { get; set; }

        public Contenido Contenido { get; set; }
        public condiciones Condicion { get; set; }
    }
}