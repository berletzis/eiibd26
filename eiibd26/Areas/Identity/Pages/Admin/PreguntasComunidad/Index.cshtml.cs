using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.PreguntasComunidad
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<(int Id, string Nombre)> CondicionesHoja { get; set; } = new();

        public async Task OnGetAsync()
        {
            var todas = await _db.condiciones
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .Select(c => new { c.id, c.nombre, c.idPadre })
                .ToListAsync();

            var idsConHijos = todas
                .Where(c => c.idPadre.HasValue)
                .Select(c => c.idPadre!.Value)
                .ToHashSet();

            CondicionesHoja = todas
                .Where(c => !idsConHijos.Contains(c.id))
                .OrderBy(c => c.nombre)
                .Select(c => (c.id, c.nombre ?? ""))
                .ToList();
        }
    }
}
