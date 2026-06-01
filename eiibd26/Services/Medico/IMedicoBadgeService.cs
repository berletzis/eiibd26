using eiibd26.Models.Medico;

namespace eiibd26.Services.Medico;

public interface IMedicoBadgeService
{
    Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId);
    Task<List<MedicoBadgeDto>> GetTodosLosBadgesAsync(int medicoId);
    Task<int> GetNivelActualAsync(int medicoId);
    Task<bool> OtorgarBadgeAsync(int medicoId, string codigo, string otorgadoPor);
    /// <param name="revocadoPor">Actor que revoca: "admin", "sistema", nombre de usuario, etc.</param>
    /// <param name="motivo">Motivo opcional de revocación para trazabilidad.</param>
    Task<bool> RevocarBadgeAsync(int medicoId, string codigo, string revocadoPor = "admin", string? motivo = null);
    /// <param name="actor">Actor que marca: "admin", "nina", etc. Nunca hardcodear.</param>
    Task<bool> MarcarEnRevisionAsync(int medicoId, string codigo, bool enRevision, string actor, string? motivo = null);
    Task EvaluarBadgesAutomaticosAsync(int medicoId);
    Task<bool> TienePermisoAsync(int medicoId, string permiso);
    Task<List<MedicoBadgeHistorialDto>> GetHistorialAsync(int medicoId);
}

