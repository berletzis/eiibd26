namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// Experiencia de comunidad: estado de ánimo de un paciente asociado a un síntoma o tratamiento.
    /// Solo campos públicos — sin UserId ni datos sensibles.
    /// </summary>
    public class CommunityExperienceDto
    {
        /// <summary>Estado de ánimo (1=MuyMal … 5=MuyBien)</summary>
        public int EstadoMood { get; set; }

        /// <summary>Nombre del estado: "MuyMal", "Mal", "Neutral", "Bien", "MuyBien"</summary>
        public string EstadoMoodNombre { get; set; } = "";

        /// <summary>Ruta al SVG del icono de estado (ej: /img/muybien.svg)</summary>
        public string MoodIconUrl { get; set; } = "";

        /// <summary>Texto del paciente truncado a 160 caracteres (puede ser null)</summary>
        public string? TextoPreview { get; set; }

        /// <summary>Fecha relativa calculada en el servidor (ej: "hace 2 días")</summary>
        public string FechaRelativa { get; set; } = "";

        /// <summary>Alias anonimizado: primer nombre + inicial apellido o "Paciente anónimo"</summary>
        public string AliasUsuario { get; set; } = "Paciente anónimo";

        /// <summary>Slug del usuario para enlace a /u/{slug}. Null si no tiene perfil público.</summary>
        public string? UserSlug { get; set; }

        /// <summary>URL del avatar del usuario. Null si no tiene foto de perfil.</summary>
        public string? AvatarUrl { get; set; }

        /// <summary>Nombre de la condición registrada en este estado (si aplica)</summary>
        public string? CondicionNombre { get; set; }

        /// <summary>Nombre del síntoma registrado en este estado (si aplica)</summary>
        public string? SintomaContextoNombre { get; set; }

        /// <summary>Nombre del tratamiento registrado en este estado (si aplica)</summary>
        public string? TratamientoContextoNombre { get; set; }
    }
}
