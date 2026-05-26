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
       
        
       public string? Titulo { get; set; }

        [Required]
        [StringLength(256)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Apellidos { get; set; }


        public DateTime? FechaDeNacimiento { get; set; }

        
        public int? EstoyAqui { get; set; }

        public int? idZone { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimaActividad { get; set; }

        [StringLength(80)]
        public string? slug { get; set; }
        [StringLength(100)]
        public string? Genero { get; set; }

        [StringLength(50)]
        public string? Latitud { get; set; }
        [StringLength(50)]
        public string? Longitud { get; set; }
        [StringLength(200)]
        public string? NombreCiudad { get; set; }
        [StringLength(200)]
        // ⚠️ NombrePais almacena el código ISO (ej: "MX"), NO el nombre del país.
        // Se usa para filtros y joins con la tabla Paises via PaisCodigo.
        // No cambiar a nombre completo sin migración de datos.
        public string? NombrePais { get; set; }
        public bool? AceptoPP { get; set; }

        public string? AcercaDe { get; set; }
        
        public Guid? UsuarioModificacion { get; set; }
        public Guid? UsuarioCreacion { get; set; }
        public DateTime? FechaModificado { get; set; }
        public DateTime? FechaCreado { get; set; }
     
        // NUEVOS CAMPOS para los 3 checkboxes solicitados
        public bool? PermitirTelefonoReal { get; set; } // Permitir utilizar teléfono para saber si soy real
        public bool? PermitirCorreoNoticias { get; set; } // Permitir enviarme correos de notificaciones o noticias
        public bool? PermitirMostrarPais { get; set; } // Permitir que otros usuarios vean de qué país soy
        public bool? PermitirCompartirDatosMedicos { get; set; } // Permitir que otros usuarios vean mis condiciones, síntomas, tratamientos y seguimiento

        [ForeignKey(nameof(idUser))]
        public virtual ApplicationUser Usuario { get; set; }
    }
}