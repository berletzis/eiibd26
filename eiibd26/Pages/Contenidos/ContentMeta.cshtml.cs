using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eiibd26.Pages.Contenidos
{
    public class ContentMetaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ContentMetaModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public BlogItemVm Item { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (id <= 0) return NotFound();

            Item = new BlogItemVm { Id = id, Conditions = new List<string>(), Symptoms = new List<string>(), Treatments = new List<string>(), RelatedQuestionsCount = 0 };

            var conds = await _db.ContenidoCondiciones
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.condiciones.AsNoTracking(), rel => rel.CondicionId, c => c.id, (rel, c) => c.nombre)
                .Distinct()
                .ToListAsync();
            Item.Conditions = conds;

            var snts = await _db.ContenidoSintomas
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.sintomas.AsNoTracking(), rel => rel.SintomaId, s => s.id, (rel, s) => s.nombre)
                .Distinct()
                .ToListAsync();
            Item.Symptoms = snts;

            var trts = await _db.ContenidoTratamientos
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.tratamientos.AsNoTracking(), rel => rel.TratamientoId, t => t.id, (rel, t) => t.nombre)
                .Distinct()
                .ToListAsync();
            Item.Treatments = trts;

            var qCount = await _db.ContenidosPreguntasRelacion
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Select(r => r.PreguntaId)
                .Distinct()
                .CountAsync();
            Item.RelatedQuestionsCount = qCount;

            return Page();
        }
    }
}