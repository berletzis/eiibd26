using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class Perfil
    {
        [Key]
        public Guid idUser { get; set; }

        [Required]
        [StringLength(200)]
        public string Avatar { get; set; }

        public int? imagenFondo { get; set; }
        [Required]
        [StringLength(256)]
        public string Titulo { get; set; }

        public bool Activo { get; set; }

        [Required]
        [StringLength(256)]
        public string Nombre { get; set; }

        [StringLength(50)]
        public string? Apellidos { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; }

        [Required]
        [StringLength(1024)]
        public string Email { get; set; }

        public DateTime? FechaDeNacimiento { get; set; }

        [Required]
        [StringLength(500)]
        public string UsoPlataforma { get; set; }

        public int? idZone { get; set; }

        // Quitar campo notas y descripcion (según nuevas instrucciones)
        // public string? notas { get; set; }
        // public string? descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime UltimaActividad { get; set; }

        [StringLength(80)]
        public string? slug { get; set; }
        [StringLength(100)]
        public string? Genero { get; set; }

        [Required]
        [StringLength(50)]
        public string Latitud { get; set; }
        [Required]
        [StringLength(50)]
        public string Longitud { get; set; }
        [StringLength(200)]
        public string? NombreCiudad { get; set; }
        [StringLength(200)]
        public string? NombrePais { get; set; }
        public bool? AceptoPP { get; set; }
        [StringLength(200)]
        public string? UltimosEstudios { get; set; }
        [StringLength(200)]
        public string? ExperienciaLaboral { get; set; }
        [StringLength(200)]
        public string? UltimaCertificacion { get; set; }
        public string? AcercaDe { get; set; }
        public string? Extras { get; set; }
        public Guid? UsuarioModificacion { get; set; }
        public Guid? UsuarioCreacion { get; set; }
        public DateTime? FechaModificado { get; set; }
        public DateTime? FechaCreado { get; set; }
        public bool? Eliminado { get; set; }

        // NUEVOS CAMPOS para los 3 checkboxes solicitados
        public bool PermitirTelefonoReal { get; set; } // Permitir utilizar teléfono para saber si soy real
        public bool PermitirCorreoNoticias { get; set; } // Permitir enviarme correos de notificaciones o noticias
        public bool PermitirMostrarPais { get; set; } // Permitir que otros usuarios vean de qué país soy

        [ForeignKey(nameof(idUser))]
        public virtual ApplicationUser Usuario { get; set; }
    }
}