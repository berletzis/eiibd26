using eiibd26.Models;
using eiibd26.Models.Campanas;
using System.Linq;

namespace eiibd26.Services.Campanas
{
    public interface ICampanaTargetingService
    {
        /// <summary>
        /// Aplica el filtro de público a la query base de usuarios.
        /// Todos los filtros son traducibles a SQL por EF Core (no client-eval).
        /// Siempre agrega EmailConfirmed = true y Email != null/vacío.
        /// </summary>
        IQueryable<ApplicationUser> AplicarCriterio(IQueryable<ApplicationUser> baseQuery, PublicoCampana publico);
    }
}
