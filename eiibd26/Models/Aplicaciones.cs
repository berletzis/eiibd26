using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Aplicaciones
{
    [Key]
    public Guid idApp { get; set; }

    public Guid idPerfil { get; set; }

    [Required]
    [StringLength(50)]
    public string provider { get; set; }

    public bool active { get; set; }

    [Required]
    public DateTime fecha_actualizacion { get; set; }

    // Relación a Perfil (debes definir el modelo Perfil e idPerfil = idUser)
    // [ForeignKey("idPerfil")]
    // public virtual Perfil Perfil { get; set; }
}