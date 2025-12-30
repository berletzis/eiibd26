using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;

namespace eiibd26.Pages.Contenidos
{
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private const int WordsPerMinute = 200;

        public DetalleModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)]
        public string slug { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        public ContenidoDetailViewModel Item { get; set; }

        // New: breadcrumb items (category chain) exposed to view
        public List<BreadcrumbItem> CategoryCrumbs { get; set; } = new List<BreadcrumbItem>();

        public class BreadcrumbItem
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1) Load content by slug or id
            Models.Contenido entity = null;

            if (!string.IsNullOrWhiteSpace(slug))
            {
                entity = await _db.Contenidos.AsNoTracking()
                    .Where(c => !c.Eliminado && c.ContenidoTituloSlug == slug)
                    .FirstOrDefaultAsync();
            }

            if (entity == null && id.HasValue)
            {
                entity = await _db.Contenidos.AsNoTracking()
                    .Where(c => !c.Eliminado && c.Id == id.Value)
                    .FirstOrDefaultAsync();
            }

            if (entity == null) return NotFound();

            var vm = new ContenidoDetailViewModel
            {
                Id = entity.Id,
                Title = entity.ContenidoTitulo ?? "",
                Excerpt = entity.ContenidoTextoC ?? "",
                ContentHtml = entity.ContenidoTextoL ?? "",
                ImageUrl = string.IsNullOrWhiteSpace(entity.URLImagenPrincipal) ? null : "/uploads/contenidos/" + entity.URLImagenPrincipal,
                Author = string.IsNullOrWhiteSpace(entity.Autor) ? "Autor" : entity.Autor,
                CreatedAt = entity.FechaCreado,
                Slug = entity.ContenidoTituloSlug ?? ""
            };

            // 2) Read minutes
            var plain = StripHtml(vm.ContentHtml ?? vm.Excerpt ?? "");
            var words = CountWords(plain);
            vm.ReadMinutes = Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));

            // 3) Categories for the current content
            var catIds = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == entity.Id && !r.Borrado && r.IdCategoria != null)
                .Select(r => r.IdCategoria.Value)
                .Distinct()
                .ToListAsync();

            if (catIds.Any())
            {
                var catNames = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => catIds.Contains(c.Sequence) && !c.Borrado)
                    .Select(c => c.Nombre)
                    .ToListAsync();

                vm.Categories = catNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            }

            // Build category breadcrumb chain (choose primary category = first cat id if available)
            if (catIds.Any())
            {
                // pick first category id as primary
                int primaryCatId = catIds.First();

                // walk up parents collecting (name, segment)
                var crumbsReversed = new List<BreadcrumbItem>();
                int? current = primaryCatId;
                while (current.HasValue && current.Value > 0)
                {
                    var cat = await _db.ContenidosCategorias.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Sequence == current.Value && !c.Borrado);
                    if (cat == null) break;

                    var segment = !string.IsNullOrWhiteSpace(cat.CategoriaSlug) ? cat.CategoriaSlug : cat.Sequence.ToString();
                    var url = Url.Content($"/Contenidos/categoria/{segment}");
                    crumbsReversed.Add(new BreadcrumbItem { Title = cat.Nombre ?? $"Categoría {cat.Sequence}", Url = url });

                    current = cat.CategoriaPadre;
                }

                // reverse so top-most parent first
                crumbsReversed.Reverse();
                CategoryCrumbs = crumbsReversed;
            }
            else
            {
                CategoryCrumbs = new List<BreadcrumbItem>();
            }

            // 4) Manual relations (both directions) — only consider Tipo == 1 (Contenido) for manual "articles"
            var manualFrom = await _db.ContenidosRelacionados.AsNoTracking()
                .Where(r => r.IdContenido == entity.Id && !r.Borrado)
                .ToListAsync();

            // incoming relations (others linking to this content)
            var manualTo = await _db.ContenidosRelacionados.AsNoTracking()
                .Where(r => r.IdContenidoRelacionado == entity.Id && !r.Borrado)
                .ToListAsync();

            // Combine preserving order: first relations originating from this content, then incoming
            var allManualRelations = manualFrom.Concat(manualTo).ToList();

            // Collect related content ids (Tipo == 1) preserving order and uniqueness
            var orderedManualContentIds = new List<int>();
            foreach (var r in allManualRelations)
            {
                if (r.Tipo == 1)
                {
                    int relatedId = (r.IdContenido == entity.Id) ? r.IdContenidoRelacionado : r.IdContenido;
                    if (relatedId > 0 && !orderedManualContentIds.Contains(relatedId))
                    {
                        orderedManualContentIds.Add(relatedId);
                    }
                }
            }

            // Fetch manual content items (preserve order, then limit to 5)
            var manualContents = new List<RelatedContenidoVm>();
            if (orderedManualContentIds.Any())
            {
                var contents = await _db.Contenidos.AsNoTracking()
                    .Where(c => orderedManualContentIds.Contains(c.Id) && !c.Eliminado)
                    .Select(c => new RelatedContenidoVm
                    {
                        Id = c.Id,
                        Title = c.ContenidoTitulo ?? "",
                        ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : "/uploads/contenidos/" + c.URLImagenPrincipal,
                        CreatedAt = c.FechaCreado,
                        Slug = c.ContenidoTituloSlug ?? "",
                        IsManual = true,
                        Type = RelatedType.Contenido
                    })
                    .ToListAsync();

                // Order according to orderedManualContentIds and limit to 5
                foreach (var idVal in orderedManualContentIds)
                {
                    var m = contents.FirstOrDefault(x => x.Id == idVal);
                    if (m != null) manualContents.Add(m);
                    if (manualContents.Count >= 5) break;
                }

                // Attach notes from relation rows (first matching relation)
                foreach (var m in manualContents)
                {
                    var rel = allManualRelations.FirstOrDefault(r =>
                        (r.Tipo == 1) &&
                        ((r.IdContenido == entity.Id && r.IdContenidoRelacionado == m.Id) || (r.IdContenidoRelacionado == entity.Id && r.IdContenido == m.Id))
                    );
                    if (rel != null) m.Note = rel.Descripcion ?? "";
                }
            }

            vm.ManualRelated = manualContents;

            // 5) Automatic related by categories (limit up to 5, excluding manual ids)
            var automaticItems = new List<RelatedContenidoVm>();
            if (catIds.Any())
            {
                var relatedIds = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria != null && catIds.Contains(r.IdCategoria.Value))
                    .Select(r => r.IdContenido)
                    .Distinct()
                    .ToListAsync();

                // remove current and manual ids
                relatedIds.Remove(entity.Id);
                foreach (var mid in orderedManualContentIds) relatedIds.Remove(mid);

                if (relatedIds.Any())
                {
                    automaticItems = await _db.Contenidos.AsNoTracking()
                        .Where(c => relatedIds.Contains(c.Id) && !c.Eliminado)
                        .OrderByDescending(c => c.FechaCreado)
                        .Take(5)
                        .Select(c => new RelatedContenidoVm
                        {
                            Id = c.Id,
                            Title = c.ContenidoTitulo ?? "",
                            ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : "/uploads/contenidos/" + c.URLImagenPrincipal,
                            CreatedAt = c.FechaCreado,
                            Slug = c.ContenidoTituloSlug ?? "",
                            IsManual = false,
                            Type = RelatedType.Contenido
                        })
                        .ToListAsync();
                }
            }

            vm.RelatedByCategories = automaticItems;

            // 6) Combine AllRelated: manual (up to 5 already) + automatic (up to 5) without duplicates
            var combined = new List<RelatedContenidoVm>();
            var seen = new HashSet<int>();

            foreach (var m in vm.ManualRelated)
            {
                if (m != null && !seen.Contains(m.Id))
                {
                    combined.Add(m);
                    seen.Add(m.Id);
                }
            }

            foreach (var a in vm.RelatedByCategories)
            {
                if (a != null && !seen.Contains(a.Id))
                {
                    combined.Add(a);
                    seen.Add(a.Id);
                }
            }

            vm.AllRelated = combined;

            // 7) Manual domain relations: Condiciones / Sintomas / Tratamientos (show the ones already selected)
            var condNames = await _db.ContenidoCondiciones.AsNoTracking()
                .Where(r => r.ContenidoId == entity.Id && !r.Borrado)
                .Join(_db.condiciones.AsNoTracking(), rel => rel.CondicionId, c => c.id, (rel, c) => c.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToListAsync();
            vm.ManualCondiciones = condNames;

            var sintNames = await _db.ContenidoSintomas.AsNoTracking()
                .Where(r => r.ContenidoId == entity.Id && !r.Borrado)
                .Join(_db.sintomas.AsNoTracking(), rel => rel.SintomaId, s => s.id, (rel, s) => s.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToListAsync();
            vm.ManualSintomas = sintNames;

            var tratNames = await _db.ContenidoTratamientos.AsNoTracking()
                .Where(r => r.ContenidoId == entity.Id && !r.Borrado)
                .Join(_db.tratamientos.AsNoTracking(), rel => rel.TratamientoId, t => t.id, (rel, t) => t.nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToListAsync();
            vm.ManualTratamientos = tratNames;

            // 8) Manual related QUESTIONS (ContenidoPreguntaRelacion -> Preguntas)
            try
            {
                var preguntaRels = await _db.ContenidosPreguntasRelacion.AsNoTracking()
                    .Where(r => r.ContenidoId == entity.Id && !r.Borrado)
                    .ToListAsync();

                var preguntaIds = preguntaRels.Select(r => r.PreguntaId).Distinct().ToList();

                vm.ManualPreguntas = new List<ContenidoDetailViewModel.RelatedPreguntaVm>();

                if (preguntaIds.Any())
                {
                    var preguntas = await _db.Preguntas.AsNoTracking()
                        .Where(p => preguntaIds.Contains(p.Id) && !p.Eliminado)
                        .OrderByDescending(p => p.FechaCreacion)
                        .Select(p => new { p.Id, Title = p.Titulo, CreatedAt = p.FechaCreacion })
                        .ToListAsync();

                    foreach (var p in preguntas)
                    {
                        vm.ManualPreguntas.Add(new ContenidoDetailViewModel.RelatedPreguntaVm
                        {
                            Id = p.Id,
                            Title = p.Title ?? "",
                            CreatedAt = p.CreatedAt.UtcDateTime,
                            Note = ""
                        });
                    }
                }
            }
            catch
            {
                vm.ManualPreguntas = new List<ContenidoDetailViewModel.RelatedPreguntaVm>();
            }

            Item = vm;

            // 9) Redirect to SEO URL if needed
            if (string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(vm.Slug))
            {
                return RedirectToPage("/Contenidos/Detalle", new { slug = vm.Slug });
            }

            return Page();
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var noScript = Regex.Replace(input, @"<script[\s\S]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            noScript = Regex.Replace(noScript, @"<style[\s\S]*?>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            var text = Regex.Replace(noScript, @"<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var parts = Regex.Split(text.Trim(), @"\s+");
            return parts.Length;
        }
    }
}