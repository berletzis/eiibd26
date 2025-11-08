using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    // DTO "local" para combo (lo puedes dejar aquí)
    public class CondicionUsuarioViewModel
    {
        public int id { get; set; }
        public string nombre { get; set; }
    }

    public class UsuarioEstadoAnimoModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public List<CondicionUsuarioViewModel> CondicionesUsuario { get; set; } = new List<CondicionUsuarioViewModel>();

        public UsuarioEstadoAnimoModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public void OnGet()
        {
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (System.Guid.TryParse(userIdStr, out var userId))
            {
                CondicionesUsuario = _db.condicionUsuario
                    .Where(c => c.idUsuario == userId && !c.Eliminado)
                    .Select(c => new CondicionUsuarioViewModel
                    {
                        id = c.id,
                        nombre = c.Condicion.nombre
                    })
                    .ToList();
            }
        }
    }
}