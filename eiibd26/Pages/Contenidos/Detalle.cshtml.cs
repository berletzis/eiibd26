// eiibd26/Pages/Contenidos/Detalle.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DetalleModel> _logger;
        private const int WordsPerMinute = 200;

        public DetalleModel(ApplicationDbContext db, ILogger<DetalleModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string slug { get; set; }

        [BindProperty(SupportsGet = true)]
        public string categorySlug { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        public ContenidoDetailViewModel Item { get; set; }
        public List<BreadcrumbItem> CategoryCrumbs { get; set; } = new List<BreadcrumbItem>();
        public string CanonicalUrl { get; set; }

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

            if (entity == null)
            {
                return NotFound();
            }

            var vm = new ContenidoDetailViewModel
            {
                Id = entity.Id,
                Title = entity.ContenidoTitulo ?? "",
                Excerpt = entity.ContenidoTextoC ?? "",
                ContentHtml = entity.ContenidoTextoL ?? "",
                ImageUrl = string.IsNullOrWhiteSpace(entity.URLImagenPrincipal)
                    ? null
                    : "/uploads/contenidos/" + entity.URLImagenPrincipal,
                Author = string.IsNullOrWhiteSpace(entity.Autor) ? "Autor" : entity.Autor,
                CreatedAt = entity.FechaCreado,
                Slug = entity.ContenidoTituloSlug ?? ""
            };

            // 2) Read minutes
            var plain = StripHtml(vm.ContentHtml ?? vm.Excerpt ?? "");
            var words = CountWords(plain);
            vm.ReadMinutes = Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));

            // 3) Obtener categorías del contenido
            var catIds = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == entity.Id && !r.Borrado && r.IdCategoria != null)
                .Select(r => r.IdCategoria.Value)
                .Distinct()
                .ToListAsync();

            string primaryCategorySlug = null;

            if (catIds.Any())
            {
                var catNames = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => catIds.Contains(c.Sequence) && !c.Borrado)
                    .Select(c => c.Nombre)
                    .ToListAsync();

                vm.Categories = catNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

                // Determinar categoría primaria (preferir hijo sobre padre)
                var primaryCat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => catIds.Contains(c.Sequence) && !c.Borrado)
                    .OrderBy(c => c.CategoriaPadre.HasValue ? 0 : 1)
                    .ThenBy(c => c.Sequence)
                    .FirstOrDefaultAsync();

                if (primaryCat != null)
                {
                    primaryCategorySlug = !string.IsNullOrWhiteSpace(primaryCat.CategoriaSlug)
                        ? primaryCat.CategoriaSlug
                        : primaryCat.Sequence.ToString();

                    // Build breadcrumbs
                    var crumbsReversed = new List<BreadcrumbItem>();
                    int? current = primaryCat.Sequence;

                    while (current.HasValue && current.Value > 0)
                    {
                        var cat = await _db.ContenidosCategorias.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Sequence == current.Value && !c.Borrado);

                        if (cat == null) break;

                        var segment = !string.IsNullOrWhiteSpace(cat.CategoriaSlug)
                            ? cat.CategoriaSlug
                            : cat.Sequence.ToString();

                        crumbsReversed.Add(new BreadcrumbItem
                        {
                            Title = cat.Nombre ?? $"Categoría {cat.Sequence}",
                            Url = $"/{segment}"
                        });

                        current = cat.CategoriaPadre;
                    }

                    crumbsReversed.Reverse();
                    CategoryCrumbs = crumbsReversed;
                }
            }

            // 4) Determinar URL canónica SEO
            if (!string.IsNullOrWhiteSpace(primaryCategorySlug))
            {
                CanonicalUrl = $"/{primaryCategorySlug}/{vm.Slug}";
            }
            else
            {
                CanonicalUrl = $"/c/{vm.Slug}";
            }

            // 5) Manual relations
            var manualFrom = await _db.ContenidosRelacionados.AsNoTracking()
                .Where(r => r.IdContenido == entity.Id && !r.Borrado)
                .ToListAsync();

            var manualTo = await _db.ContenidosRelacionados.AsNoTracking()
                .Where(r => r.IdContenidoRelacionado == entity.Id && !r.Borrado)
                .ToListAsync();

            var allManualRelations = manualFrom.Concat(manualTo).ToList();

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

                foreach (var idVal in orderedManualContentIds)
                {
                    var m = contents.FirstOrDefault(x => x.Id == idVal);
                    if (m != null) manualContents.Add(m);
                    if (manualContents.Count >= 5) break;
                }

                foreach (var m in manualContents)
                {
                    var rel = allManualRelations.FirstOrDefault(r =>
                        (r.Tipo == 1) &&
                        ((r.IdContenido == entity.Id && r.IdContenidoRelacionado == m.Id) ||
                         (r.IdContenidoRelacionado == entity.Id && r.IdContenido == m.Id))
                    );
                    if (rel != null) m.Note = rel.Descripcion ?? "";
                }
            }

            vm.ManualRelated = manualContents;

            // 6) Automatic related by categories
            var automaticItems = new List<RelatedContenidoVm>();
            if (catIds.Any())
            {
                var relatedIds = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria != null && catIds.Contains(r.IdCategoria.Value))
                    .Select(r => r.IdContenido)
                    .Distinct()
                    .ToListAsync();

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

            // 7) Combine AllRelated
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

            // 8) Manual domain relations
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

            // 9) Manual related QUESTIONS
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
                        .Select(p => new {
                            p.Id,
                            Title = p.Titulo,
                            Slug = p.Slug ?? "",
                            CreatedAt = p.FechaCreacion
                        })
                        .ToListAsync();

                    foreach (var p in preguntas)
                    {
                        vm.ManualPreguntas.Add(new ContenidoDetailViewModel.RelatedPreguntaVm
                        {
                            Id = p.Id,
                            Title = p.Title ?? "",
                            Slug = p.Slug,
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