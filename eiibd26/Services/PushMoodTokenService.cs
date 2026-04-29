using Microsoft.AspNetCore.DataProtection;
using System;

namespace eiibd26.Services;

/// <summary>
/// Genera y valida tokens de corta duración (5 min) para registrar mood
/// desde una notificación push sin necesidad de sesión activa.
/// El token es un payload cifrado y firmado por Data Protection:
///   "{userId}:{expiresUtcTicks}"
/// </summary>
public interface IPushMoodTokenService
{
    /// <summary>Genera un token válido por 5 minutos para el usuario indicado.</summary>
    string GenerarToken(Guid userId);

    /// <summary>
    /// Valida el token y devuelve el userId si es válido y no ha expirado.
    /// Devuelve null si el token es inválido, fue manipulado o expiró.
    /// </summary>
    Guid? ValidarToken(string token);
}

public sealed class PushMoodTokenService : IPushMoodTokenService
{
    private static readonly TimeSpan Vida = TimeSpan.FromMinutes(5);
    private const string Propósito = "PushMoodToken.v1";

    private readonly IDataProtector _protector;
    private readonly TimeProvider _clock;

    public PushMoodTokenService(IDataProtectionProvider provider, TimeProvider clock)
    {
        _protector = provider.CreateProtector(Propósito);
        _clock = clock;
    }

    public string GenerarToken(Guid userId)
    {
        var expira = _clock.GetUtcNow().Add(Vida).UtcTicks;
        var payload = $"{userId}:{expira}";
        return _protector.Protect(payload);
    }

    public Guid? ValidarToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        string payload;
        try
        {
            payload = _protector.Unprotect(token);
        }
        catch
        {
            // Token manipulado o de una clave diferente
            return null;
        }

        var parts = payload.Split(':');
        if (parts.Length != 2)
            return null;

        if (!Guid.TryParse(parts[0], out var userId))
            return null;

        if (!long.TryParse(parts[1], out var expiraTicks))
            return null;

        if (_clock.GetUtcNow().UtcTicks > expiraTicks)
            return null; // expirado

        return userId;
    }
}
