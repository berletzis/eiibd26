namespace eiibd26.Models.Campanas
{
    /// <summary>
    /// Audiencias disponibles para el panel de envío de campañas.
    /// Cada valor define el criterio de elegibilidad y el FaseLog que se guarda en EmailCampanaLog.
    /// </summary>
    public enum AudienciaCampana
    {
        /// <summary>
        /// Usuarios viejos (PasswordHash len=68) que NO recibieron Toque 1 (FaseLog=1).
        /// Base SQL: LEN(PasswordHash)=68 AND EmailConfirmed=1, excluyendo quienes ya tienen registro
        /// exitoso en EmailCampanaLog con Fase=1.
        /// </summary>
        ViejosSinToque1 = 1,

        /// <summary>
        /// Usuarios viejos que recibieron Toque 1 (FaseLog=1) y NO recibieron Toque 2 (FaseLog=2).
        /// El descarte de reactivados es automático: quienes cambiaron su contraseña ya no tienen
        /// hash len=68 (pasan a AQAAAA…) y quedan excluidos por el filtro UsuariosViejos sin lógica extra.
        /// </summary>
        Toque2 = 2,

        /// <summary>
        /// Usuarios viejos que recibieron Toque 2 (FaseLog=2) y NO recibieron Toque 3 (FaseLog=3).
        /// El descarte de reactivados funciona igual que en Toque2 vía el filtro UsuariosViejos.
        /// </summary>
        Toque3 = 3,

        /// <summary>
        /// Todos los usuarios con EmailConfirmed=1, sin distinción de antigüedad.
        /// Tracking por FaseLog=10 + TemplateId — cada template es una campaña independiente.
        /// </summary>
        TodosConfirmados = 4,

        /// <summary>
        /// Usuarios con EmailConfirmed=1 que NO tienen ninguna condición registrada (condicionUsuario.Eliminado=0).
        /// Tarea pendiente: invitarlos a registrar su diagnóstico.
        /// FaseLog=20. Sin exclusión por envíos previos — re-envíable manualmente.
        /// </summary>
        SinCondicion = 5,

        /// <summary>
        /// Usuarios con EmailConfirmed=1 que NUNCA registraron estado de ánimo (EstadoAnimoUsuario.Eliminado=0).
        /// Tarea pendiente: invitarlos a usar el tracker de mood.
        /// FaseLog=21. Sin exclusión por envíos previos — re-envíable manualmente.
        /// </summary>
        SinMood = 6,

        /// <summary>
        /// Usuarios con EmailConfirmed=1 cuyas preguntas recibieron al menos una respuesta de OTRO usuario
        /// en los últimos 7 días. Notificación de actividad reciente.
        /// FaseLog=22. Sin exclusión por envíos previos — re-envíable semanalmente.
        /// </summary>
        ConRespuestasSemana = 7,

        /// <summary>
        /// Usuarios con EmailConfirmed=1 que tienen al menos una condición cuya fechaInicio coincide
        /// con la fecha de registro del perfil (señal de que nunca actualizaron la fecha real de diagnóstico).
        /// Mismo criterio que VM.NeedsDiagnosisDateUpdate del dashboard.
        /// FaseLog=23. Sin exclusión por envíos previos — re-envíable manualmente.
        /// </summary>
        DiagnosticoPendiente = 8,

        /// <summary>
        /// Usuarios con EmailConfirmed=1 sin foto de perfil propia.
        /// Criterio IDÉNTICO al scoring de Admin/Usuarios/Index:
        /// Perfil.Avatar es null/vacío, o contiene "ui-avatars.com", o contiene "default".
        /// FaseLog=24. Sin exclusión por envíos previos — re-envíable manualmente.
        /// </summary>
        SinAvatar = 9
    }
}
