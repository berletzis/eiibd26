using Microsoft.AspNetCore.Mvc.RazorPages;
using eiibd26.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    public class UsuarioEstadoAnimoModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UsuarioEstadoAnimoModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<EstadoAnimoUsuario> UltimosEstados { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var guid = Guid.Parse(userId);

            UltimosEstados = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid)
                .OrderByDescending(x => x.FechaRegistro)
                .Take(30)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Include(x => x.SintomaUsuario).ThenInclude(s => s.Sintoma)
                .Include(x => x.TratamientoUsuario).ThenInclude(t => t.Tratamiento)
                .ToListAsync();
        }
    }
}