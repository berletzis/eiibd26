using eiibd26.Models.Medico;

namespace eiibd26.Services.Medico;

public interface IMedicoBadgeService
{
    Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId);
    Task<List<MedicoBadgeDto>> GetTodosLosBadgesAsync(int medicoId);
    Task<int> GetNivelActualAsync(int medicoId);
    Task<bool> OtorgarBadgeAsync(int medicoId, string codigo, string otorgadoPor);
    Task EvaluarBadgesAutomaticosAsync(int medicoId);
    Task<bool> TienePermisoAsync(int medicoId, string permiso);
}
