using eiibd26.Models;
using eiibd26.Models.Campanas;
using System.Linq;

namespace eiibd26.Services.Campanas
{
    public class CampanaTargetingService : ICampanaTargetingService
    {
        /// <inheritdoc/>
        public IQueryable<ApplicationUser> AplicarCriterio(IQueryable<ApplicationUser> baseQuery, PublicoCampana publico)
        {
            // Base común: email confirmado y no vacío (siempre requerido)
            var query = baseQuery.Where(u =>
                u.EmailConfirmed &&
                u.Email != null &&
                u.Email != "");

            switch (publico)
            {
                case PublicoCampana.UsuariosViejos:
                    // EF Core traduce string.Length a LEN() en SQL Server → ejecuta en servidor ✓
                    // LEN([PasswordHash]) = 68 identifica hashes del formato anterior (no Identity v3)
                    return query.Where(u => u.PasswordHash != null && u.PasswordHash.Length == 68);

                case PublicoCampana.UsuariosNuevos:
                    // EF Core traduce StartsWith a LIKE 'AQAAAA%' en SQL Server → ejecuta en servidor ✓
                    // 'AQAAAA' = base64 del byte marcador 0x01 de Identity v3 (PBKDF2/SHA-256)
                    return query.Where(u => u.PasswordHash != null && u.PasswordHash.StartsWith("AQAAAA"));

                case PublicoCampana.TodosConfirmados:
                default:
                    return query;
            }
        }
    }
}
