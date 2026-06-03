using eiibd26.Models.Validacion;

namespace eiibd26.Services.Validacion
{
    public class ValidacionRespuestaAdminDto
    {
        public int Id { get; set; }
        public Guid RespuestaId { get; set; }
        public string PreguntaTitulo { get; set; } = "";
        public string? PreguntaUrl { get; set; }
        public string RespuestaPreview { get; set; } = "";
        public string? Comentario { get; set; }
        public EstadoValidacion Estado { get; set; }
        public DateTime CreadoEn { get; set; }
        public string? NotaModeracion { get; set; }
    }

    public interface IValidacionRespuestaService
    {
        Task<UpsertResult> GuardarValidacionAsync(
            Guid respuestaId,
            string usuarioMedicoId,
            string? comentario);

        Task<ValidacionExistenteDto?> ObtenerMiValidacionAsync(
            Guid respuestaId,
            string usuarioMedicoId);

        Task<List<ValidacionPublicaDto>> ObtenerValidacionesPublicasAsync(
            Guid respuestaId);

        Task<Dictionary<Guid, List<ValidacionPublicaDto>>> ObtenerValidadoresPorRespuestasAsync(
            List<Guid> respuestaIds);

        Task<bool> CambiarEstadoAsync(
            int validacionId,
            EstadoValidacion nuevoEstado,
            string adminUserId,
            string? nota = null);

        Task<List<ValidacionRespuestaAdminDto>> ObtenerValidacionesRespuestaMedicoAsync(
            string usuarioMedicoId);
    }
}
