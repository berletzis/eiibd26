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
using eiibd26.Models.Validacion;
using eiibd26.Services.Validacion;
using eiibd26.Services.Cobertura;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;

namespace eiibd26.Pages.Contenidos
{
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IValidacionContenidoService _validacionService;
        private readonly UserManager<eiibd26.Models.ApplicationUser> _userManager;
        private readonly ICoberturaVistaService _coberturaVista;
        private readonly ILogger<DetalleModel> _logger;
        private const int WordsPerMinute = 200;

        private static readonly string[] _validationRoles = ["Medico", "Administrador"];

        public DetalleModel(
            ApplicationDbContext db,
            IValidacionContenidoService validacionService,
            UserManager<eiibd26.Models.ApplicationUser> userManager,
            ICoberturaVistaService coberturaVista,
            ILogger<DetalleModel> logger)
        {
            _db                = db;
            _validacionService = validacionService;
            _userManager       = userManager;
            _coberturaVista    = coberturaVista;
            _logger            = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string slug { get; set; }

        [BindProperty(SupportsGet = true)]
        public string categorySlug { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        public ContenidoDetailViewModel Item { get; set; }
        public List<BreadcrumbItem> CategoryCrumbs { get; set; } = new List<BreadcrumbItem>();

        public bool CanValidate { get; set; }
        public ValidacionExistenteDto? MiValidacionDesc { get; set; }
        public List<ValidacionPublicaDto> ValidacionesPublicas { get; set; } = new();

        // Motor de Cobertura (Fase 6): externos similares desde el motor de EMBEDDINGS, dos tabs.
        // Tab "Artículos Similares" (subtema, Score ≥ 0.78).
        public IReadOnlyList<ExternoSimilarDto> SimilaresItems { get; set; } = new List<ExternoSimilarDto>();
        public bool SimilaresHasMore { get; set; }
        public int SimilaresNextOffset { get; set; }
        // Tab "Explora el tema" (área, 0.55 ≤ Score < 0.78).
        public IReadOnlyList<ExternoSimilarDto> AreaItems { get; set; } = new List<ExternoSimilarDto>();
        public bool AreaHasMore { get; set; }
        public int AreaNextOffset { get; set; }
        // Id del artículo (para los botones "Ver más" AJAX).
        public int ArticuloId { get; set; }
        // ¿Es artículo real (árbol General)? Gatea el bloque de tabs del Radar.
        public bool EsArticuloCobertura { get; set; }

        // Tamaño del bloque inicial y de cada "Ver más".
        private const int BloqueExternos = 5;

        // Relative canonical path (e.g. "/categoria/slug")
        public string CanonicalUrl { get; set; }

        // Absolute SEO url exposed to the view (e.g. "https://www.eiibd.com/categoria/slug")
        public string SeoUrl { get; set; }

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
                    .Include(c => c.AutorPerfil)
                    .Where(c => !c.Eliminado && c.ContenidoTituloSlug == slug)
                    .FirstOrDefaultAsync();
            }

            if (entity == null && id.HasValue)
            {
                entity = await _db.Contenidos.AsNoTracking()
                    .Include(c => c.AutorPerfil)
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
                ContentHtml = StripWysiwygNoiseAttributes(entity.ContenidoTextoL ?? ""),
                ImageUrl = string.IsNullOrWhiteSpace(entity.URLImagenPrincipal)
                    ? null
                    : (entity.URLImagenPrincipal.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? entity.URLImagenPrincipal
                        : "/uploads/contenidos/" + entity.URLImagenPrincipal),
                // Prefer the profile display name when available, fallback to the raw Autor field
                Author = (entity.AutorPerfil != null && !string.IsNullOrWhiteSpace(entity.AutorPerfil.Nombre))
                    ? entity.AutorPerfil.Nombre
                    : (string.IsNullOrWhiteSpace(entity.Autor) ? "Autor" : entity.Autor),
                AuthorImageUrl = (entity.AutorPerfil != null && !string.IsNullOrWhiteSpace(entity.AutorPerfil.Avatar)) ? entity.AutorPerfil.Avatar : null,
                AuthorSlug = (entity.AutorPerfil != null && !string.IsNullOrWhiteSpace(entity.AutorPerfil.slug)) ? entity.AutorPerfil.slug : "",
                AuthorId = (entity.AutorPerfil != null) ? entity.AutorPerfil.idUser : (Guid?)null,
                CreatedAt = entity.FechaCreado,
                Slug = entity.ContenidoTituloSlug ?? ""
            };

            // 2) Read minutes
            var plain = StripHtml(vm.ContentHtml ?? vm.Excerpt ?? "");
            var words = CountWords(plain);
            vm.ReadMinutes = Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));

            // 3) Obtener categor�as del contenido
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

                // Buscar la categor�a principal (EsPrincipal)
                var principalRel = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                    .Where(r => r.IdContenido == entity.Id && !r.Borrado && r.IdCategoria != null && r.EsPrincipal == true)
                    .FirstOrDefaultAsync();

                ContenidoCategoria principalCat = null;
                if (principalRel != null && principalRel.IdCategoria.HasValue)
                {
                    principalCat = await _db.ContenidosCategorias.AsNoTracking()
                        .Where(c => c.Sequence == principalRel.IdCategoria.Value && !c.Borrado)
                        .FirstOrDefaultAsync();
                }

                // Si no hay principal, usar la m�s profunda
                if (principalCat == null)
                {
                    var allCats = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(c => catIds.Contains(c.Sequence) && !c.Borrado)
                        .ToListAsync();

                    List<(List<ContenidoCategoria> chain, ContenidoCategoria leaf)> chains = new();
                    foreach (var cat in allCats)
                    {
                        var chain = new List<ContenidoCategoria>();
                        var current = cat;
                        while (current != null)
                        {
                            chain.Add(current);
                            if (current.CategoriaPadre.HasValue && current.CategoriaPadre.Value > 0)
                            {
                                current = allCats.FirstOrDefault(c => c.Sequence == current.CategoriaPadre.Value);
                            }
                            else
                            {
                                current = null;
                            }
                        }
                        chain.Reverse();
                        chains.Add((chain, cat));
                    }
                    // Elegir la cadena m�s profunda (con m�s niveles)
                    var deepestChain = chains.OrderByDescending(x => x.chain.Count).FirstOrDefault();
                    if (deepestChain.chain != null && deepestChain.chain.Count > 0)
                    {
                        principalCat = deepestChain.leaf;
                    }
                }

                if (principalCat != null)
                {
                    primaryCategorySlug = !string.IsNullOrWhiteSpace(principalCat.CategoriaSlug)
                        ? principalCat.CategoriaSlug
                        : principalCat.Sequence.ToString();

                    // Armar la cadena de padres desde la principal
                    var crumbsTemp = new List<(string Title, string Segment)>();
                    var current = principalCat;
                    while (current != null)
                    {
                        var segment = !string.IsNullOrWhiteSpace(current.CategoriaSlug)
                            ? current.CategoriaSlug
                            : current.Sequence.ToString();
                        crumbsTemp.Add((current.Nombre ?? $"Categor�a {current.Sequence}", segment));
                        if (current.CategoriaPadre.HasValue && current.CategoriaPadre.Value > 0)
                        {
                            current = await _db.ContenidosCategorias.AsNoTracking()
                                .FirstOrDefaultAsync(c => c.Sequence == current.CategoriaPadre.Value && !c.Borrado);
                        }
                        else
                        {
                            current = null;
                        }
                    }
                    crumbsTemp.Reverse();
                    var accum = "";
                    var finalCrumbs = new List<BreadcrumbItem>();
                    foreach (var c in crumbsTemp)
                    {
                        accum = accum + "/" + c.Segment;
                        finalCrumbs.Add(new BreadcrumbItem
                        {
                            Title = c.Title,
                            Url = accum
                        });
                    }
                    CategoryCrumbs = finalCrumbs;
                }
            }

            // 4) Determinar URL can�nica SEO (relative)
            if (!string.IsNullOrWhiteSpace(primaryCategorySlug))
            {
                CanonicalUrl = $"/{primaryCategorySlug}/{vm.Slug}";
            }
            else
            {
                CanonicalUrl = $"/c/{vm.Slug}";
            }

            // Build absolute SeoUrl from CanonicalUrl (exposed to the view)
            try
            {
                if (!string.IsNullOrWhiteSpace(CanonicalUrl))
                {
                    var path = CanonicalUrl.StartsWith("/") ? CanonicalUrl : "/" + CanonicalUrl;
                    SeoUrl = $"{Request.Scheme}://{Request.Host}{path}";
                }
                else if (!string.IsNullOrWhiteSpace(vm.Slug))
                {
                    SeoUrl = $"{Request.Scheme}://{Request.Host}/{vm.Slug.Trim('/')}";
                }
                else
                {
                    SeoUrl = HttpContext.Request.GetDisplayUrl();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo construir SeoUrl; usando URL de la petici�n.");
                SeoUrl = HttpContext.Request.GetDisplayUrl();
            }

            // 5) Manual relations
            // (rest of logic remains unchanged; omitted here for brevity � already in your file)
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

                // Construir URL SEO y agregar nota para cada contenido manual relacionado
                foreach (var m in manualContents)
                {
                    // Obtener categor�a primaria del contenido relacionado
                    var relCatIds = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                        .Where(r => r.IdContenido == m.Id && !r.Borrado && r.IdCategoria != null)
                        .Select(r => r.IdCategoria.Value)
                        .Distinct()
                        .ToListAsync();

                    if (relCatIds.Any())
                    {
                        var relPrimaryCat = await _db.ContenidosCategorias.AsNoTracking()
                            .Where(c => relCatIds.Contains(c.Sequence) && !c.Borrado)
                            .OrderBy(c => c.CategoriaPadre.HasValue ? 0 : 1)
                            .ThenBy(c => c.Sequence)
                            .FirstOrDefaultAsync();

                        if (relPrimaryCat != null)
                        {
                            var catSlug = !string.IsNullOrWhiteSpace(relPrimaryCat.CategoriaSlug)
                                ? relPrimaryCat.CategoriaSlug
                                : relPrimaryCat.Sequence.ToString();
                            m.SeoUrl = $"/{catSlug}/{m.Slug}";
                        }
                    }

                    // Fallback: si no tiene categor�a, usar /c/slug
                    if (string.IsNullOrWhiteSpace(m.SeoUrl))
                    {
                        m.SeoUrl = $"/c/{m.Slug}";
                    }

                    // Agregar nota de relaci�n manual
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

                    // Construir URL SEO para cada contenido autom�tico relacionado
                    foreach (var a in automaticItems)
                    {
                        // Obtener categor�a primaria del contenido relacionado
                        var relCatIds = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                            .Where(r => r.IdContenido == a.Id && !r.Borrado && r.IdCategoria != null)
                            .Select(r => r.IdCategoria.Value)
                            .Distinct()
                            .ToListAsync();

                        if (relCatIds.Any())
                        {
                            var relPrimaryCat = await _db.ContenidosCategorias.AsNoTracking()
                                .Where(c => relCatIds.Contains(c.Sequence) && !c.Borrado)
                                .OrderBy(c => c.CategoriaPadre.HasValue ? 0 : 1)
                                .ThenBy(c => c.Sequence)
                                .FirstOrDefaultAsync();

                            if (relPrimaryCat != null)
                            {
                                var catSlug = !string.IsNullOrWhiteSpace(relPrimaryCat.CategoriaSlug)
                                    ? relPrimaryCat.CategoriaSlug
                                    : relPrimaryCat.Sequence.ToString();
                                a.SeoUrl = $"/{catSlug}/{a.Slug}";
                            }
                        }

                        // Fallback: si no tiene categor�a, usar /c/slug
                        if (string.IsNullOrWhiteSpace(a.SeoUrl))
                        {
                            a.SeoUrl = $"/c/{a.Slug}";
                        }
                    }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading related questions for content {ContentId}", entity.Id);
                vm.ManualPreguntas = new List<ContenidoDetailViewModel.RelatedPreguntaVm>();
            }

            Item = vm;

            // Motor de Cobertura (Fase 6): externos similares desde embeddings, dos tabs.
            // Solo si es artículo real (árbol General). Carga el primer bloque de cada tab;
            // el resto se pide con "Ver más" (AJAX, handler MasExternos).
            try
            {
                EsArticuloCobertura = await _coberturaVista.EsArticuloAsync(entity.Id);
                if (EsArticuloCobertura)
                {
                    ArticuloId = entity.Id;

                    var similares = await _coberturaVista.ObtenerSimilaresAsync(entity.Id, "similares", 0, BloqueExternos);
                    SimilaresItems = similares.Items;
                    SimilaresHasMore = similares.HasMore;
                    SimilaresNextOffset = similares.NextOffset;

                    var area = await _coberturaVista.ObtenerSimilaresAsync(entity.Id, "area", 0, BloqueExternos);
                    AreaItems = area.Items;
                    AreaHasMore = area.HasMore;
                    AreaNextOffset = area.NextOffset;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar externos similares para artículo {ContentId}", entity.Id);
            }

            // Cargar estado de validación de contenido por profesionales
            try
            {
                ValidacionesPublicas = await _validacionService.ObtenerValidacionesPublicasAsync(
                    TipoContenidoValidado.Articulo, entity.Id);

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        CanValidate = roles.Any(r => _validationRoles.Contains(r));
                        if (CanValidate)
                        {
                            MiValidacionDesc = await _validacionService.ObtenerMiValidacionAsync(
                                TipoContenidoValidado.Articulo, entity.Id, user.Id.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar info de validación para artículo {ContentId}", entity.Id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarValidacionAsync(
            int? contenidoId,
            string? slug,
            string? comentario)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Challenge();

            if (contenidoId == null)
            {
                TempData["ValidationMessage"] = "Datos de artículo inválidos.";
                TempData["ValidationSuccess"]  = false;
                return RedirectToPage(new { slug });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => _validationRoles.Contains(r)))
            {
                TempData["ValidationMessage"] = "No tienes permiso para validar contenido.";
                TempData["ValidationSuccess"]  = false;
                return RedirectToPage(new { slug });
            }

            var result = await _validacionService.GuardarValidacionAsync(
                TipoContenidoValidado.Articulo,
                contenidoId.Value,
                user.Id.ToString(),
                comentario);

            (TempData["ValidationSuccess"], TempData["ValidationMessage"]) = result switch
            {
                UpsertResult.Creada      => ((object)true, (object)"Validación registrada correctamente."),
                UpsertResult.Actualizada => (true,  "Comentario actualizado correctamente."),
                UpsertResult.SinCambios  => (true,  "No hay cambios que guardar."),
                _                        => (false, "Error al guardar la validación. Intenta de nuevo.")
            };

            return RedirectToPage(new { slug });
        }

        private static string StripWysiwygNoiseAttributes(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            return Regex.Replace(html, @"\s+data-(start|end)=""[^""]*""", "");
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

        /// <summary>
        /// Motor de Cobertura (Fase 6) — "Ver más" AJAX del Radar. Devuelve el siguiente bloque de
        /// externos del tab pedido como fragmento HTML (partial), con X-Has-More / X-Next-Offset en
        /// headers para que el JS sepa si seguir mostrando el botón. Solo lectura de embeddings;
        /// re-valida que el AId sea un artículo real (el servicio devuelve vacío si no lo es).
        /// </summary>
        public async Task<IActionResult> OnGetMasExternosAsync(int aid, string tab, int offset)
        {
            var pagina = await _coberturaVista.ObtenerSimilaresAsync(aid, tab, offset, BloqueExternos);
            Response.Headers["X-Has-More"] = pagina.HasMore ? "1" : "0";
            Response.Headers["X-Next-Offset"] = pagina.NextOffset.ToString();
            return Partial("_ExternosSimilares", pagina.Items);
        }
    }
}