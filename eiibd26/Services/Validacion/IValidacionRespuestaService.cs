using eiibd26.Models.Validacion;

namespace eiibd26.Services.Validacion
{
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
    }
}
