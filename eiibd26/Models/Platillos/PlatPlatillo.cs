using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Platillos
{
    /// <summary>
    /// Cabecera del platillo. Contenido de autoría (no dato de usuario): baja lógica con Activo.
    /// PasosResumidos SIEMPRE con palabras propias (derechos de autor); FuenteNombre/FuenteUrl obligatorio dar crédito.
    /// </summary>
    public class PlatPlatillo
    {
        [Key]
        public int Id { get; set; }
        public string Codigo { get; set; } = "";          // P001, único
        public string Nombre { get; set; } = "";
        public int CategoriaId { get; set; }
        public int? Porciones { get; set; }
        public int? TiempoPrepMin { get; set; }
        public string? Dificultad { get; set; }           // 'Fácil' | 'Media' | 'Difícil'
        public string? PasosResumidos { get; set; }
        public string? FuenteNombre { get; set; }
        public string? FuenteUrl { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public Guid? CreadoPorUserId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public virtual PlatCategoria? Categoria { get; set; }

        public virtual ICollection<PlatPlatilloIngrediente> Ingredientes { get; set; } = new List<PlatPlatilloIngrediente>();
    }
}
