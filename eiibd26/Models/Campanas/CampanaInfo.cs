namespace eiibd26.Models.Campanas
{
    /// <summary>
    /// Define una campaña de email en configuración (sección SendGrid:Campanas).
    /// Cada campaña es independiente — no encadena con otras.
    /// Agregar una nueva campaña = una entrada aquí + (si el público es nuevo) un valor en PublicoCampana.
    /// </summary>
    public sealed record CampanaInfo
    {
        /// <summary>Código único de la campaña (ej. "reactivacion", "bienvenida"). Inmutable.</summary>
        public string Codigo { get; init; } = string.Empty;

        public string Nombre { get; init; } = string.Empty;

        public string Descripcion { get; init; } = string.Empty;

        /// <summary>ID del template dinámico en SendGrid.</summary>
        public string TemplateId { get; init; } = string.Empty;

        /// <summary>Criterio de público. Determina el filtro SQL que aplica CampanaTargetingService.</summary>
        public PublicoCampana Publico { get; init; } = PublicoCampana.TodosConfirmados;

        /// <summary>
        /// Entero estable que se guarda en EmailCampanaLog.Fase.
        /// Decisión Fase 3: mapear Codigo→int para no tocar el esquema de BD.
        /// Valores reservados: 1=reactivacion, 2=bienvenida, 3=recordatorio. Nuevas campañas: 4+.
        /// </summary>
        public int FaseLog { get; init; }
    }
}
