using eiibd26.Models.Directorio;

namespace eiibd26.Services.Directorio;

public interface IMedicoDirectorioService
{
    Task<DirectorioIndexVm> GetListadoAsync(
        string? busqueda,
        string? estado,
        string? especialidad,
        int? areaId,
        int pagina = 1,
        int porPagina = 18);

    Task<MedicoDetalleVm?> GetDetalleAsync(int medicoId, Guid? usuarioId);

    Task<ProponerMedicoVm> GetProponerVmAsync();

    Task<int> ProponerMedicoAsync(ProponerMedicoVm vm, Guid usuarioId);

    Task<bool> ConfirmarAtencionAsync(int medicoId, int tipoConfirmacionId, Guid usuarioId);

    Task RecalcularNivelConfianzaAsync(int medicoId);

    Task<List<TipoConfirmacion>> GetTiposConfirmacionActivosAsync();
}
