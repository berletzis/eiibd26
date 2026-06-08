using System;
using System.ComponentModel.DataAnnotations;

namespace eiibd26.DTOs
{
    public class CrearPreguntaDto
    {
        [Required, MaxLength(300)]
        public string Titulo { get; set; }

        [Required]
        public string Cuerpo { get; set; }

        public int? CondicionId { get; set; }
    }

    public class CrearRespuestaDto
    {
        [Required, MinLength(10), MaxLength(8000)]
        public string Cuerpo { get; set; }

        // Optional parent id for nested replies
        public Guid? ParentId { get; set; }

        // Optional preguntaId: supported by RespuestasApiController via header/query/body; not required here
        public Guid? PreguntaId { get; set; }
    }

    public class VotarDto
    {
        [Required]
        public int Valor { get; set; } // 1 o -1
    }

    public class AñadirEtiquetaDto
    {
        [Required]
        public Guid EtiquetaId { get; set; }
    }
}