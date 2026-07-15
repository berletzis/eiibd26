using eiibd26.Models.Validacion;

namespace eiibd26.Services.Validacion
{
    public enum UpsertResult { Creada, Actualizada, SinCambios, Error }

    public class ValidacionExistenteDto
    {
        public int Id { get; set; }
        public string? Comentario { get; set; }
        public EstadoValidacion Estado { get; set; }
        public bool EnRevisionOOculta => Estado != EstadoValidacion.Validado;
    }

    public class ValidacionPublicaDto
    {
        public int Id { get; set; }
        public string UserDisplay { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public string? Slug { get; set; }
        public string? Comentario { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }

    public class ValidacionAdminDto
    {
        public int Id { get; set; }
        public TipoContenidoValidado TipoContenido { get; set; }
        public int ContenidoId { get; set; }
        public string ContenidoTitulo { get; set; } = "";
        public string? ContenidoUrl { get; set; }
        public string? Comentario { get; set; }
        public EstadoValidacion Estado { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
        public string? NotaModeracion { get; set; }
    }

    public class ValidacionContenidoPartialVm
    {
        public int ContenidoId { get; set; }
        public string Slug { get; set; } = "";
        public string PageHandler { get; set; } = "GuardarValidacion";
        public ValidacionExistenteDto? Existente { get; set; }
        // Título del card (header). Default = el del glosario; Platillos lo rotula por nota
        // ("Validar nota de: queso") para distinguir la tarjeta del ingrediente de la del grupo.
        public string Titulo { get; set; } = "Validar contenido";
    }

    public interface IValidacionContenidoService
    {
        Task<UpsertResult> GuardarValidacionAsync(
            TipoContenidoValidado tipo,
            int contenidoId,
            string usuarioMedicoId,
            string? comentario);

        Task<ValidacionExistenteDto?> ObtenerMiValidacionAsync(
            TipoContenidoValidado tipo,
            int contenidoId,
            string usuarioMedicoId);

        Task<List<ValidacionPublicaDto>> ObtenerValidacionesPublicasAsync(
            TipoContenidoValidado tipo,
            int contenidoId);

        Task<bool> CambiarEstadoAsync(
            int validacionId,
            EstadoValidacion nuevoEstado,
            string adminUserId,
            string? nota = null);

        Task<List<ValidacionAdminDto>> ObtenerValidacionesMedicoAsync(
            string usuarioMedicoId);

        Task<Dictionary<int, List<ValidacionPublicaDto>>> ObtenerValidadoresPorContenidosAsync(
            TipoContenidoValidado tipo,
            List<int> contenidoIds);

        Task<Dictionary<int, List<ValidacionPublicaDto>>> ObtenerValidadoresGlosarioPorTerminosAsync(
            List<int> terminoIds);
    }
}
