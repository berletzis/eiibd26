using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services;

/// <summary>
/// Valida que registros clínicos del paciente (condicion, síntoma, tratamiento, etc.)
/// pertenezcan al usuario autenticado antes de persistir o leer datos sensibles.
/// Aplica el principio: el cliente NO decide el ownership (SEC-010/011).
/// </summary>
public class ClinicalOwnershipValidator
{
    private readonly ApplicationDbContext _db;

    public ClinicalOwnershipValidator(ApplicationDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>Verifica que una condicionUsuario pertenece al usuario dado.</summary>
    public Task<bool> OwnsCondicionAsync(int condicionUsuarioId, Guid userId, CancellationToken ct = default)
        => _db.condicionUsuario
              .AsNoTracking()
              .AnyAsync(c => c.id == condicionUsuarioId && c.idUsuario == userId && !c.Eliminado, ct);

    /// <summary>Verifica que un sintomaUsuario pertenece al usuario dado.</summary>
    public Task<bool> OwnsSintomaAsync(int sintomaUsuarioId, Guid userId, CancellationToken ct = default)
        => _db.sintomasUsuario
              .AsNoTracking()
              .AnyAsync(s => s.id == sintomaUsuarioId && s.idUsuario == userId && !s.Eliminado, ct);

    /// <summary>Verifica que un tratamientoUsuario pertenece al usuario dado.</summary>
    public Task<bool> OwnsTratamientoAsync(int tratamientoUsuarioId, Guid userId, CancellationToken ct = default)
        => _db.tratamientoUsuario
              .AsNoTracking()
              .AnyAsync(t => t.id == tratamientoUsuarioId && t.idUsuario == userId && !t.Eliminado, ct);

    /// <summary>Verifica que un EstadoAnimoUsuario pertenece al usuario dado.</summary>
    public Task<bool> OwnsEstadoAnimoAsync(int estadoAnimoId, Guid userId, CancellationToken ct = default)
        => _db.EstadoAnimoUsuario
              .AsNoTracking()
              .AnyAsync(e => e.Id == estadoAnimoId && e.IdUsuario == userId && !e.Eliminado, ct);

    /// <summary>
    /// Valida en paralelo los FK opcionales de un registro de estado de ánimo.
    /// Devuelve el nombre del primer campo inválido, o null si todos son válidos.
    /// </summary>
    public async Task<string?> ValidateEstadoAnimoRelationsAsync(
        int? condicionUsuarioId,
        int? sintomaUsuarioId,
        int? tratamientoUsuarioId,
        Guid userId,
        CancellationToken ct = default)
    {
        if (condicionUsuarioId.HasValue)
        {
            if (!await OwnsCondicionAsync(condicionUsuarioId.Value, userId, ct))
                return nameof(condicionUsuarioId);
        }

        if (sintomaUsuarioId.HasValue)
        {
            if (!await OwnsSintomaAsync(sintomaUsuarioId.Value, userId, ct))
                return nameof(sintomaUsuarioId);
        }

        if (tratamientoUsuarioId.HasValue)
        {
            if (!await OwnsTratamientoAsync(tratamientoUsuarioId.Value, userId, ct))
                return nameof(tratamientoUsuarioId);
        }

        return null;
    }
}
