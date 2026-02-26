using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Pages.Contenidos
{
    public class ContentMetaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ContentMetaModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public bool NotFound { get; set; } = false;

        public List<string> Conditions { get; set; } = new List<string>();
        public List<string> Symptoms { get; set; } = new List<string>();
        public List<string> Treatments { get; set; } = new List<string>();

        public class CategoryLink { public string Name { get; set; } public string Url { get; set; } public bool IsPrincipal { get; set; } }
        public List<CategoryLink> Categories { get; set; } = new List<CategoryLink>();

        public class QuestionVm { public Guid Id { get; set; } public string Title { get; set; } public DateTime CreatedAt { get; set; } }
        public List<QuestionVm> RelatedQuestions { get; set; } = new List<QuestionVm>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Validate
            if (id <= 0)
            {
                NotFound = true;
                return Page();
            }

            // Ensure content exists and is not deleted
            var contentExists = await _db.Contenidos.AsNoTracking()
                .Where(c => c.Id == id && !c.Eliminado)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (contentExists == 0)
            {
                NotFound = true;
                return Page();
            }

            // Load conditions
            var conds = await _db.ContenidoCondiciones
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.condiciones.AsNoTracking(), rel => rel.CondicionId, c => c.id, (rel, c) => c.nombre)
                .Distinct()
                .ToListAsync();
            Conditions = conds;

            // Load symptoms
            var snts = await _db.ContenidoSintomas
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.sintomas.AsNoTracking(), rel => rel.SintomaId, s => s.id, (rel, s) => s.nombre)
                .Distinct()
                .ToListAsync();
            Symptoms = snts;

            // Load treatments
            var trts = await _db.ContenidoTratamientos
                .AsNoTracking()
                .Where(r => r.ContenidoId == id && !r.Borrado)
                .Join(_db.tratamientos.AsNoTracking(), rel => rel.TratamientoId, t => t.id, (rel, t) => t.nombre)
                .Distinct()
                .ToListAsync();
            Treatments = trts;

            // Load categories assigned to this content (may be multiple)
            var contentSlug = await _db.Contenidos.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => c.ContenidoTituloSlug)
                .FirstOrDefaultAsync();

            // Load category relations and build links. Show principal first, deduplicate by category slug.
            var catRels = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == id && !r.Borrado && r.IdCategoria != null)
                .Join(_db.ContenidosCategorias.AsNoTracking(),
                      rel => rel.IdCategoria,
                      cat => cat.Sequence,
                      (rel, cat) => new { rel.EsPrincipal, cat.Sequence, cat.Nombre, cat.CategoriaSlug })
                .ToListAsync();

            if (catRels != null && catRels.Any())
            {
                // Normalize entries and deduplicate by category slug (or sequence if slug missing)
                var normalized = catRels
                    .Select(x => new {
                        Seq = x.Sequence,
                        Name = x.Nombre ?? x.Sequence.ToString(),
                        Slug = !string.IsNullOrWhiteSpace(x.CategoriaSlug) ? x.CategoriaSlug.Trim('/') : x.Sequence.ToString(),
                        IsPrincipal = x.EsPrincipal == true
                    })
                    .GroupBy(x => x.Slug)
                    .Select(g => g.OrderByDescending(x => x.IsPrincipal).First())
                    .OrderByDescending(x => x.IsPrincipal)
                    .ToList();

                foreach (var c in normalized)
                {
                    string url;
                    if (!string.IsNullOrWhiteSpace(contentSlug))
                        url = "/" + Uri.EscapeUriString(c.Slug) + "/" + Uri.EscapeUriString(contentSlug);
                    else
                        url = "/" + Uri.EscapeUriString(c.Slug);

                    Categories.Add(new CategoryLink { Name = c.Name, Url = url, IsPrincipal = c.IsPrincipal });
                }
            }

            // Load related questions (limit e.g. 5 latest)
            var qIds = await _db.ContenidosPreguntasRelacion
                .AsNoTracking()
                .Where(r => r.ContenidoId  == id && !r.Borrado)
                .Select(r => r.PreguntaId)
                .Distinct()
                .ToListAsync();

            if (qIds != null && qIds.Any())
            {
                var qs = await _db.Preguntas
                    .AsNoTracking()
                    .Where(p => qIds.Contains(p.Id) && !p.Eliminado)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => new QuestionVm { Id = p.Id, Title = p.Titulo, CreatedAt = p.FechaCreacion.DateTime })
                    .Take(5)
                    .ToListAsync();

                RelatedQuestions = qs;
            }

            return Page();
        }
    }
}