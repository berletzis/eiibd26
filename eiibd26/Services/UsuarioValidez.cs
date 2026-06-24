using eiibd26.Data;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Services
{
    /// <summary>
    /// Criterio único de "usuario válido" para todos los conteos y listados públicos.
    /// Válido = NO eliminado (soft-delete) Y NO suspendido (LockoutEnd nulo o pasado).
    /// Tocar este filtro propaga automáticamente a dashboard, mapas y audiencias.
    /// </summary>
    public static class UsuarioValidez
    {
        /// <summary>
        /// Filtra la query dejando solo usuarios NO eliminados y NO suspendidos.
        /// Uso: _userManager.Users.SoloValidos() o _db.Users.SoloValidos()
        /// </summary>
        public static IQueryable<ApplicationUser> SoloValidos(this IQueryable<ApplicationUser> q)
            => q.Where(u => !u.Eliminado && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow));

        /// <summary>
        /// Subquery de IDs válidos para filtrar tablas secundarias (Perfil, Mood, etc.) en BD.
        /// Uso: var ids = UsuarioValidez.IdsValidosQuery(_db);
        ///      perfil.Where(p => ids.Contains(p.idUser))
        /// Preferir sobre IdsValidosAsync para queries EF Core (queda como subquery SQL).
        /// </summary>
        public static IQueryable<Guid> IdsValidosQuery(ApplicationDbContext db)
            => db.Users.SoloValidos().Select(u => u.Id);

        /// <summary>
        /// HashSet en memoria de IDs válidos.
        /// Usar solo cuando no sea posible un subquery EF (ej. queries ya materializadas).
        /// </summary>
        public static async Task<HashSet<Guid>> IdsValidosAsync(ApplicationDbContext db)
        {
            var ids = await IdsValidosQuery(db).ToListAsync();
            return new HashSet<Guid>(ids);
        }
    }
}
