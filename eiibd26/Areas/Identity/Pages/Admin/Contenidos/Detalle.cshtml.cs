using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DetalleModel> _logger;

        public DetalleModel(ApplicationDbContext db, IWebHostEnvironment env, ILogger<DetalleModel> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // Campos principales (binds)
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string ContenidoTitulo { get; set; }
        [BindProperty] public string ContenidoTituloSlug { get; set; }
        [BindProperty] public string ContenidoTextoC { get; set; }
        [BindProperty] public string ContenidoTextoL { get; set; }
        [BindProperty] public int EstadoPublicacion { get; set; }
        [BindProperty] public DateTime? ContenidoFechaInicio { get; set; }
        [BindProperty] public DateTime? ContenidoFechaFin { get; set; }
        [BindProperty] public string SelectedAutorId { get; set; }
        [BindProperty] public string Autor { get; set; }
        [BindProperty] public string PaisClave { get; set; }
        [BindProperty] public int? IdCategoriaPadre { get; set; }
        [BindProperty] public int? IdCategoria { get; set; }
        [BindProperty] public IFormFile UploadedImage { get; set; }
        [BindProperty] public string URLImagenPrincipal { get; set; }

        // Relaciones seleccionadas (domain)
        [BindProperty] public List<int> SelectedCondicionesIds { get; set; } = new();
        [BindProperty] public List<int> SelectedSintomasIds { get; set; } = new();
        [BindProperty] public List<int> SelectedTratamientosIds { get; set; } = new();

        // Relaciones manuales (para contenidos relacionados)
        [BindProperty] public List<int> SelectedManualContenidoIds { get; set; } = new();
        // Preguntas como GUIDs
        [BindProperty] public List<Guid> SelectedManualPreguntasIds { get; set; } = new();

        // Lookups
        public List<CategoryItem> CategoryItems { get; set; } = new();
        public List<(int seq, string name)> Subcategories { get; set; } = new();
        public List<(string code, string name)> PaisesLista { get; set; } = new();
        public List<(string id, string name)> AdminAuthors { get; set; } = new();

        // Panel derecho lists
        // Note: condiciones include padre info now
        public List<(int id, string nombre, int? padre)> AllCondiciones { get; set; } = new();
        public List<(int id, string nombre)> AllSintomas { get; set; } = new();
        public List<(int id, string nombre)> AllTratamientos { get; set; } = new();

        // Preguntas candidate
        public List<(Guid id, string title)> AllPreguntasCandidate { get; set; } = new();

        // General contents
        public List<GeneralContentItem> AllGeneralContenidos { get; set; } = new();
        public List<(int Sequence, string Nombre)> ParentCategories { get; set; } = new();

        // Messages / debug used by view
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public string WarningMessage { get; set; }
        public string DebugInfoHtml { get; set; }

        // SEO full URL for the content (computed)
        public string SeoUrl { get; set; }

        // CategoryItem (already used in view)
        public class CategoryItem
        {
            public int Sequence { get; set; }
            public int? CategoriaPadre { get; set; }
            public string Nombre { get; set; }
            public string CategoriaSlug { get; set; }
        }

        // General content helper
        public class GeneralContentItem
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public int? ParentCategorySeq { get; set; }
            public string ParentCategoryName { get; set; }
            public string ParentCategorySlug { get; set; }
        }

        // GET
        public async Task<IActionResult> OnGetAsync(int? id, bool saved = false)
        {
            await LoadLookupsAsync();
            await LoadRelLookupsAsync();
            await LoadManualListsAsync();

            if (!id.HasValue)
            {
                EstadoPublicacion = 0;
                BuildSubcategories();
                BuildDebug();
                await BuildSeoUrlAsync();
                // Ensure ModelState does not override our select values on initial render
                ModelState.Remove(nameof(IdCategoriaPadre));
                ModelState.Remove(nameof(IdCategoria));
                return Page();
            }

            Id = id.Value;
            // Use IgnoreQueryFilters to allow viewing soft-deleted content
            var contenido = await _db.Contenidos
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Id);

            if (contenido == null)
            {
                ErrorMessage = "Contenido no encontrado.";
                BuildDebug();
                await BuildSeoUrlAsync();
                return Page();
            }

            // Show warning if content is soft-deleted
            if (contenido.Eliminado)
            {
                WarningMessage = "⚠️ ADVERTENCIA: Este contenido está marcado como ELIMINADO.";
            }

            MapContenidoToModel(contenido);
            await LoadSelectedRelationsAsync(Id.Value);
            await LoadSelectedManualRelationsAsync(Id.Value);
            await ResolveCategorySelectionFromRelationAsync();
            BuildSubcategories();
            BuildDebug();
            // Ensure ModelState does not override server-assigned category selections
            ModelState.Remove(nameof(IdCategoriaPadre));
            ModelState.Remove(nameof(IdCategoria));
            await BuildSeoUrlAsync();

            // Check for restoration message from TempData
            if (TempData.ContainsKey("RestoredMessage"))
            {
                SuccessMessage = TempData["RestoredMessage"]?.ToString();
            }
            else if (saved)
            {
                SuccessMessage = "Guardado correctamente.";
            }
            return Page();
        }

        // AJAX slug check
        public async Task<IActionResult> OnGetCheckSlugAsync(string slug, int? id)
        {
            slug = (slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug))
                return new JsonResult(new { ok = false, empty = true });

            var exists = await _db.Contenidos
                .AsNoTracking()
                .AnyAsync(c => c.ContenidoTituloSlug == slug && !c.Eliminado && (!id.HasValue || c.Id != id.Value));

            return new JsonResult(new { ok = true, exists });
        }

        // POST Save
        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                await LoadLookupsAsync();
                await LoadRelLookupsAsync();
                await LoadManualListsAsync();

                CaptureRelationSelectionsFromForm();

                // Capture category inputs explicitly from form (padre and sub)
                if (Request.HasFormContentType)
                {
                    var parentRaw = Request.Form["IdCategoriaPadre"].FirstOrDefault();
                    if (int.TryParse(parentRaw, out var parentParsed))
                        IdCategoriaPadre = parentParsed;

                    var subRaw = Request.Form["IdCategoria"].FirstOrDefault();
                    if (int.TryParse(subRaw, out var subParsed))
                        IdCategoria = subParsed;
                    else if (string.IsNullOrEmpty(subRaw))
                        IdCategoria = null;
                }

                BuildSubcategories();

                if (string.IsNullOrWhiteSpace(ContenidoTitulo))
                {
                    ErrorMessage = "El título es obligatorio."; BuildDebug(); return Page();
                }
                if (string.IsNullOrWhiteSpace(ContenidoTextoC))
                {
                    ErrorMessage = "El resumen es obligatorio."; BuildDebug(); return Page();
                }
                if (string.IsNullOrWhiteSpace(ContenidoTextoL))
                {
                    ErrorMessage = "El contenido es obligatorio."; BuildDebug(); return Page();
                }

                if (string.IsNullOrWhiteSpace(ContenidoTituloSlug))
                    ContenidoTituloSlug = Slugify(ContenidoTitulo);

                var slugExists = await _db.Contenidos
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.ContenidoTituloSlug == ContenidoTituloSlug &&
                        !c.Eliminado &&
                        (!Id.HasValue || c.Id != Id.Value));

                if (slugExists)
                {
                    ErrorMessage = "El slug ya existe."; BuildDebug(); return Page();
                }

                if (UploadedImage != null && UploadedImage.Length > 0)
                {
                    var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "contenidos");
                    if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
                    var ext = Path.GetExtension(UploadedImage.FileName);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    using var fs = System.IO.File.Create(Path.Combine(uploadsRoot, fileName));
                    await UploadedImage.CopyToAsync(fs);
                    URLImagenPrincipal = fileName;
                }

                var now = DateTime.UtcNow;
                var currentUser = GetCurrentUserGuid();
                Guid autorGuid = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(SelectedAutorId) && Guid.TryParse(SelectedAutorId, out var parsedAutor))
                    autorGuid = parsedAutor;

                int? selectedCategory = IdCategoria ?? IdCategoriaPadre;

                if (Id.HasValue)
                {
                    var entity = await _db.Contenidos
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.Id == Id.Value);
                    if (entity == null) { ErrorMessage = "Contenido no encontrado."; BuildDebug(); return Page(); }

                    entity.ContenidoTitulo = ContenidoTitulo;
                    entity.ContenidoTituloSlug = ContenidoTituloSlug;
                    entity.ContenidoTextoC = ContenidoTextoC;
                    entity.ContenidoTextoL = ContenidoTextoL;
                    entity.EstadoPublicacion = EstadoPublicacion;

                    // Auto-restore soft-deleted content when publishing
                    if (entity.Eliminado && EstadoPublicacion != 0)
                    {
                        entity.Eliminado = false;
                        TempData["RestoredMessage"] = "✅ Contenido restaurado automáticamente (ya no está eliminado).";
                    }

                    entity.ContenidoFechaInicio = ContenidoFechaInicio;
                    entity.ContenidoFechaFin = ContenidoFechaFin;
                    entity.IdAutor = autorGuid;
                    entity.Autor = autorGuid != Guid.Empty
                        ? (AdminAuthors.FirstOrDefault(a => a.id == SelectedAutorId).name ?? Autor ?? "")
                        : Autor ?? "";
                    entity.PaisClave = PaisClave;
                    if (!string.IsNullOrWhiteSpace(URLImagenPrincipal))
                        entity.URLImagenPrincipal = URLImagenPrincipal;
                    entity.FechaModificado = now;
                    entity.UsuarioModificacion = currentUser;

                    await _db.SaveChangesAsync();

                    await SaveCategoryRelationAsync(entity.Id, selectedCategory, currentUser, now);

                    // save manual relations (contenidos + preguntas)
                    await SaveManualRelationsAsync(entity.Id, SelectedManualContenidoIds, SelectedManualPreguntasIds, currentUser, now);

                    // save domain relations
                    await SaveContenidoRelationsAsync(entity.Id, SelectedCondicionesIds, SelectedSintomasIds, SelectedTratamientosIds);
                }
                else
                {
                    var entity = new Contenido
                    {
                        ContenidoTitulo = ContenidoTitulo,
                        ContenidoTituloSlug = ContenidoTituloSlug,
                        ContenidoTextoC = ContenidoTextoC,
                        ContenidoTextoL = ContenidoTextoL,
                        EstadoPublicacion = EstadoPublicacion,
                        ContenidoFechaInicio = ContenidoFechaInicio,
                        ContenidoFechaFin = ContenidoFechaFin,
                        IdAutor = autorGuid,
                        Autor = autorGuid != Guid.Empty
                            ? (AdminAuthors.FirstOrDefault(a => a.id == SelectedAutorId).name ?? Autor ?? "")
                            : Autor ?? "",
                        PaisClave = PaisClave,
                        URLImagenPrincipal = URLImagenPrincipal,
                        UsuarioCreacion = currentUser,
                        UsuarioModificacion = currentUser,
                        FechaCreado = now,
                        FechaModificado = now,
                        Eliminado = false
                    };

                    _db.Contenidos.Add(entity);
                    await _db.SaveChangesAsync();
                    Id = entity.Id;

                    await SaveCategoryRelationAsync(entity.Id, selectedCategory, currentUser, now);

                    // save manual relations
                    await SaveManualRelationsAsync(entity.Id, SelectedManualContenidoIds, SelectedManualPreguntasIds, currentUser, now);

                    // save domain relations
                    await SaveContenidoRelationsAsync(entity.Id, SelectedCondicionesIds, SelectedSintomasIds, SelectedTratamientosIds);
                }

                // Redirect (PRG) para recargar con selección persistida
                return RedirectToPage(new { id = Id, saved = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando contenido");
                ErrorMessage = "Error al guardar.";
                BuildDebug();
                return Page();
            }
        }

        // ------------------------------
        // SaveCategoryRelationAsync
        // ------------------------------
        private async Task SaveCategoryRelationAsync(int contenidoId, int? selectedCategory, Guid user, DateTime now)
        {
            if (!selectedCategory.HasValue) return;

            var desiredCategoryIds = new List<int> { selectedCategory.Value };

            var selectedCat = await _db.ContenidosCategorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Sequence == selectedCategory.Value);

            if (selectedCat != null && selectedCat.CategoriaPadre.HasValue && selectedCat.CategoriaPadre.Value > 0)
            {
                if (!desiredCategoryIds.Contains(selectedCat.CategoriaPadre.Value))
                    desiredCategoryIds.Add(selectedCat.CategoriaPadre.Value);
            }

            var existingRels = await _db.ContenidosCategoriasRelacion
                .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.IdCategoria != null)
                .ToListAsync();

            var existingCategoryIds = existingRels
                .Where(r => r.IdCategoria.HasValue)
                .Select(r => r.IdCategoria.Value)
                .ToHashSet();

            var toSoftDelete = existingRels
                .Where(r => !r.IdCategoria.HasValue || !desiredCategoryIds.Contains(r.IdCategoria.Value))
                .ToList();

            foreach (var rel in toSoftDelete)
            {
                rel.Borrado = true;
                rel.FechaModificacion = now;
                rel.UsuarioModificacion = user;
            }

            foreach (var catId in desiredCategoryIds.Where(id => !existingCategoryIds.Contains(id)))
            {
                _db.ContenidosCategoriasRelacion.Add(new ContenidoCategoriaRelacion
                {
                    IdContenido = contenidoId,
                    IdCategoria = catId,
                    FechaCreacion = now,
                    FechaModificacion = now,
                    UsuarioCreacion = user,
                    UsuarioModificacion = user,
                    Borrado = false
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task SaveManualRelationsAsync(int contenidoId, List<int> contenidoRelacionadosIds, List<Guid> preguntasRelacionadasGuids, Guid user, DateTime now)
        {
            try
            {
                contenidoRelacionadosIds = contenidoRelacionadosIds?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
                preguntasRelacionadasGuids = preguntasRelacionadasGuids?.Where(g => g != Guid.Empty).Distinct().ToList() ?? new List<Guid>();

                var existingContentRels = await _db.ContenidosRelacionados
                    .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.Tipo == 1)
                    .ToListAsync();

                var existingContentIds = existingContentRels.Select(r => r.IdContenidoRelacionado).ToHashSet();

                foreach (var rel in existingContentRels.Where(r => !contenidoRelacionadosIds.Contains(r.IdContenidoRelacionado)))
                {
                    rel.Borrado = true;
                    rel.FechaModificacion = now;
                    rel.UsuarioModificacion = user;
                }

                foreach (var id in contenidoRelacionadosIds.Where(i => !existingContentIds.Contains(i)))
                {
                    _db.ContenidosRelacionados.Add(new ContenidoRelacionado
                    {
                        IdContenido = contenidoId,
                        IdContenidoRelacionado = id,
                        Tipo = 1,
                        Descripcion = null,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = user,
                        UsuarioModificacion = user,
                        Borrado = false
                    });
                }

                await _db.SaveChangesAsync();

                var cprSet = _db.ContenidosPreguntasRelacion;

                var existingPreguntaRels = await cprSet
                    .Where(r => r.ContenidoId == contenidoId && !r.Borrado)
                    .ToListAsync();

                var existingPreguntaGuids = existingPreguntaRels.Select(r => r.PreguntaId).ToHashSet();

                foreach (var rel in existingPreguntaRels.Where(r => !preguntasRelacionadasGuids.Contains(r.PreguntaId)))
                {
                    rel.Borrado = true;
                    rel.FechaModificacion = now;
                    rel.UsuarioModificacion = user;
                }

                foreach (var guid in preguntasRelacionadasGuids.Where(g => !existingPreguntaGuids.Contains(g)))
                {
                    cprSet.Add(new ContenidoPreguntaRelacion
                    {
                        ContenidoId = contenidoId,
                        PreguntaId = guid,
                        UsuarioCreacion = user,
                        UsuarioModificacion = user,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        Borrado = false
                    });
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando relaciones manuales");
                throw;
            }
        }

        private async Task SaveContenidoRelationsAsync(int contenidoId, List<int> condicionIds, List<int> sintomaIds, List<int> tratamientoIds)
        {
            var now = DateTime.UtcNow;
            var user = GetCurrentUserGuid();

            // Condiciones
            var existingCond = await _db.ContenidoCondiciones.Where(x => x.ContenidoId == contenidoId && !x.Borrado).ToListAsync();
            var existingCondSet = existingCond.Select(x => x.CondicionId).ToHashSet();
            foreach (var rel in existingCond.Where(x => !condicionIds.Contains(x.CondicionId)))
            {
                rel.Borrado = true; rel.FechaModificacion = now; rel.UsuarioModificacion = user;
            }
            foreach (var id in condicionIds.Where(id => !existingCondSet.Contains(id)))
            {
                _db.ContenidoCondiciones.Add(new ContenidoCondicion
                {
                    ContenidoId = contenidoId,
                    CondicionId = id,
                    FechaCreacion = now,
                    FechaModificacion = now,
                    UsuarioCreacion = user,
                    UsuarioModificacion = user,
                    Borrado = false
                });
            }

            // Síntomas
            var existingSint = await _db.ContenidoSintomas.Where(x => x.ContenidoId == contenidoId && !x.Borrado).ToListAsync();
            var existingSintSet = existingSint.Select(x => x.SintomaId).ToHashSet();
            foreach (var rel in existingSint.Where(x => !sintomaIds.Contains(x.SintomaId)))
            {
                rel.Borrado = true; rel.FechaModificacion = now; rel.UsuarioModificacion = user;
            }
            foreach (var id in sintomaIds.Where(id => !existingSintSet.Contains(id)))
            {
                _db.ContenidoSintomas.Add(new ContenidoSintoma
                {
                    ContenidoId = contenidoId,
                    SintomaId = id,
                    FechaCreacion = now,
                    FechaModificacion = now,
                    UsuarioCreacion = user,
                    UsuarioModificacion = user,
                    Borrado = false
                });
            }

            // Tratamientos
            var existingTrat = await _db.ContenidoTratamientos.Where(x => x.ContenidoId == contenidoId && !x.Borrado).ToListAsync();
            var existingTratSet = existingTrat.Select(x => x.TratamientoId).ToHashSet();
            foreach (var rel in existingTrat.Where(x => !tratamientoIds.Contains(x.TratamientoId)))
            {
                rel.Borrado = true; rel.FechaModificacion = now; rel.UsuarioModificacion = user;
            }
            foreach (var id in tratamientoIds.Where(id => !existingTratSet.Contains(id)))
            {
                _db.ContenidoTratamientos.Add(new ContenidoTratamiento
                {
                    ContenidoId = contenidoId,
                    TratamientoId = id,
                    FechaCreacion = now,
                    FechaModificacion = now,
                    UsuarioCreacion = user,
                    UsuarioModificacion = user,
                    Borrado = false
                });
            }

            await _db.SaveChangesAsync();
        }

        private void CaptureRelationSelectionsFromForm()
        {
            if (!Request.HasFormContentType) return;

            SelectedManualContenidoIds = Request.Form["SelectedManualContenidoIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();

            SelectedManualPreguntasIds = Request.Form["SelectedManualPreguntasIds"]
                .Select(v => Guid.TryParse(v, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();

            SelectedCondicionesIds = Request.Form["SelectedCondicionesIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();

            SelectedSintomasIds = Request.Form["SelectedSintomasIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();

            SelectedTratamientosIds = Request.Form["SelectedTratamientosIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
        }

        private async Task LoadSelectedRelationsAsync(int contenidoId)
        {
            SelectedCondicionesIds = await _db.ContenidoCondiciones.AsNoTracking()
                .Where(x => x.ContenidoId == contenidoId && !x.Borrado)
                .Select(x => x.CondicionId)
                .Distinct()
                .ToListAsync();

            SelectedSintomasIds = await _db.ContenidoSintomas.AsNoTracking()
                .Where(x => x.ContenidoId == contenidoId && !x.Borrado)
                .Select(x => x.SintomaId)
                .Distinct()
                .ToListAsync();

            SelectedTratamientosIds = await _db.ContenidoTratamientos.AsNoTracking()
                .Where(x => x.ContenidoId == contenidoId && !x.Borrado)
                .Select(x => x.TratamientoId)
                .Distinct()
                .ToListAsync();
        }

        private async Task LoadSelectedManualRelationsAsync(int contenidoId)
        {
            var manual = await _db.ContenidosRelacionados.AsNoTracking()
                .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.Tipo == 1)
                .ToListAsync();

            SelectedManualContenidoIds = manual.Select(r => r.IdContenidoRelacionado).Distinct().ToList();

            SelectedManualPreguntasIds = await _db.ContenidosPreguntasRelacion.AsNoTracking()
                .Where(r => r.ContenidoId == contenidoId && !r.Borrado)
                .Select(r => r.PreguntaId)
                .Distinct()
                .ToListAsync();
        }

        private async Task ResolveCategorySelectionFromRelationAsync()
        {
            if (!Id.HasValue) return;
            // Prefer a relation pointing to a subcategory (child) when possible.
            var relWithCat = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == Id && !r.Borrado && r.IdCategoria != null)
                .Join(_db.ContenidosCategorias.AsNoTracking(),
                      rel => rel.IdCategoria,
                      cat => cat.Sequence,
                      (rel, cat) => new { Rel = rel, Cat = cat })
                // prefer child categories (Cat.CategoriaPadre != null) so we pick the most specific category
                .OrderBy(x => x.Cat.CategoriaPadre.HasValue ? 0 : 1)
                .ThenByDescending(x => x.Rel.FechaCreacion)
                .FirstOrDefaultAsync();

            if (relWithCat == null) return;

            var cat = relWithCat.Cat;

            // Try to resolve from loaded CategoryItems first
            var catObj = CategoryItems.FirstOrDefault(c => c.Sequence == cat.Sequence);
            if (catObj != null)
            {
                if (catObj.CategoriaPadre.HasValue)
                {
                    IdCategoriaPadre = catObj.CategoriaPadre;
                    IdCategoria = catObj.Sequence;
                }
                else
                {
                    IdCategoriaPadre = catObj.Sequence;
                    IdCategoria = null;
                }
                return;
            }

            // Ensure CategoryItems contains this category and its parent so the UI can populate selects
            if (!CategoryItems.Any(ci => ci.Sequence == cat.Sequence))
            {
                CategoryItems.Add(new CategoryItem
                {
                    Sequence = cat.Sequence,
                    CategoriaPadre = cat.CategoriaPadre,
                    Nombre = cat.Nombre ?? string.Empty,
                    CategoriaSlug = cat.CategoriaSlug
                });
            }
            if (cat.CategoriaPadre.HasValue)
            {
                var parentCat = await _db.ContenidosCategorias.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Sequence == cat.CategoriaPadre.Value);
                if (parentCat != null && !CategoryItems.Any(ci => ci.Sequence == parentCat.Sequence))
                {
                    CategoryItems.Add(new CategoryItem
                    {
                        Sequence = parentCat.Sequence,
                        CategoriaPadre = parentCat.CategoriaPadre,
                        Nombre = parentCat.Nombre ?? string.Empty,
                        CategoriaSlug = parentCat.CategoriaSlug
                    });
                }
            }

            if (cat.CategoriaPadre.HasValue)
            {
                IdCategoriaPadre = cat.CategoriaPadre;
                IdCategoria = cat.Sequence;
            }
            else
            {
                IdCategoriaPadre = cat.Sequence;
                IdCategoria = null;
            }
        }

        private void MapContenidoToModel(Contenido c)
        {
            ContenidoTitulo = c.ContenidoTitulo;
            ContenidoTituloSlug = c.ContenidoTituloSlug;
            ContenidoTextoC = c.ContenidoTextoC;
            ContenidoTextoL = c.ContenidoTextoL;
            EstadoPublicacion = c.EstadoPublicacion ?? 0;
            ContenidoFechaInicio = c.ContenidoFechaInicio;
            ContenidoFechaFin = c.ContenidoFechaFin;
            Autor = c.Autor;
            PaisClave = c.PaisClave;
            URLImagenPrincipal = c.URLImagenPrincipal;
            SelectedAutorId = c.IdAutor != Guid.Empty ? c.IdAutor.ToString() : null;
        }

        private Guid GetCurrentUserGuid()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var g) ? g : Guid.Empty;
        }

        private async Task LoadLookupsAsync()
        {
            await LoadCategoriesAsync();
            await LoadCountriesAsync();
            await LoadAuthorsAsync();
        }

        private async Task LoadRelLookupsAsync()
        {
            try
            {
                var condRaw = await _db.condiciones.AsNoTracking()
                    .Where(c => !c.Eliminado)
                    .OrderBy(c => c.nombre)
                    .Select(c => new { c.id, c.nombre, c.idPadre })
                    .ToListAsync();
                AllCondiciones = condRaw.Select(c => (c.id, c.nombre ?? string.Empty, c.idPadre)).ToList();
            }
            catch { AllCondiciones = new(); }

            try
            {
                var sintRaw = await _db.sintomas.AsNoTracking()
                    .Where(s => !s.Eliminado)
                    .OrderBy(s => s.nombre)
                    .Select(s => new { s.id, s.nombre })
                    .ToListAsync();
                AllSintomas = sintRaw.Select(s => (s.id, s.nombre ?? string.Empty)).ToList();
            }
            catch { AllSintomas = new(); }

            try
            {
                var tratRaw = await _db.tratamientos.AsNoTracking()
                    .Where(t => !t.Eliminado)
                    .OrderBy(t => t.nombre)
                    .Select(t => new { t.id, t.nombre })
                    .ToListAsync();
                AllTratamientos = tratRaw.Select(t => (t.id, t.nombre ?? string.Empty)).ToList();
            }
            catch { AllTratamientos = new(); }
        }

        private async Task LoadManualListsAsync()
        {
            try
            {
                var allowedStatuses = new[] { 1, 2, 3 };

                var contents = await _db.Contenidos.AsNoTracking()
                    .Where(c => !c.Eliminado && allowedStatuses.Contains((c.EstadoPublicacion ?? 0)))
                    .OrderByDescending(c => c.FechaCreado)
                    .Select(c => new { c.Id, Title = c.ContenidoTitulo ?? "", Slug = c.ContenidoTituloSlug })
                    .ToListAsync();

                if (!contents.Any())
                {
                    AllGeneralContenidos = new();
                    ParentCategories = new();
                }
                else
                {
                    var ids = contents.Select(x => x.Id).ToList();

                    var catRels = await _db.ContenidosCategoriasRelacion
                        .AsNoTracking()
                        .Where(r => ids.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                        .Join(_db.ContenidosCategorias.AsNoTracking(),
                              rel => rel.IdCategoria,
                              cat => cat.Sequence,
                              (rel, cat) => new
                              {
                                  rel.IdContenido,
                                  cat.Sequence,
                                  cat.CategoriaSlug,
                                  cat.Nombre,
                                  cat.CategoriaPadre
                              })
                        .ToListAsync();

                    var map = catRels
                        .GroupBy(x => x.IdContenido)
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var chosen = g
                                    .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                                    .ThenBy(x => x.Sequence)
                                    .FirstOrDefault();

                                if (chosen == null)
                                    return (Name: (string)null, Slug: (string)null, Seq: (int?)null, ParentSeq: (int?)null);

                                var seg = !string.IsNullOrWhiteSpace(chosen.CategoriaSlug) ? chosen.CategoriaSlug : chosen.Sequence.ToString();
                                var parentSeq = chosen.CategoriaPadre.HasValue ? chosen.CategoriaPadre.Value : chosen.Sequence;
                                return (Name: chosen.Nombre, Slug: seg, Seq: (int?)chosen.Sequence, ParentSeq: (int?)parentSeq);
                            });

                    var allCats = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(c => !c.Borrado)
                        .Select(c => new { c.Sequence, c.Nombre, c.CategoriaPadre, c.CategoriaSlug })
                        .ToListAsync();

                    ParentCategories = allCats
                        .Where(c => c.CategoriaPadre == null)
                        .OrderBy(c => c.Nombre)
                        .Select(c => (Sequence: c.Sequence, Nombre: c.Nombre))
                        .ToList();

                    var result = new List<GeneralContentItem>(contents.Count);
                    foreach (var c in contents)
                    {
                        map.TryGetValue(c.Id, out var catInfo);

                        int? parentSeq = null;
                        string parentName = null;
                        string parentSlug = null;

                        if (catInfo.Seq.HasValue)
                        {
                            var chosenSeq = catInfo.Seq.Value;
                            var chosenCat = allCats.FirstOrDefault(x => x.Sequence == chosenSeq);
                            if (chosenCat != null)
                            {
                                if (chosenCat.CategoriaPadre.HasValue)
                                {
                                    parentSeq = chosenCat.CategoriaPadre.Value;
                                    var pcat = allCats.FirstOrDefault(x => x.Sequence == parentSeq.Value);
                                    if (pcat != null)
                                    {
                                        parentName = pcat.Nombre;
                                        parentSlug = pcat.CategoriaSlug;
                                    }
                                }
                                else
                                {
                                    parentSeq = chosenCat.Sequence;
                                    parentName = chosenCat.Nombre;
                                    parentSlug = chosenCat.CategoriaSlug;
                                }
                            }
                        }

                        result.Add(new GeneralContentItem
                        {
                            Id = c.Id,
                            Title = c.Title,
                            ParentCategorySeq = parentSeq,
                            ParentCategoryName = parentName,
                            ParentCategorySlug = parentSlug
                        });
                    }

                    if (Id.HasValue)
                        result = result.Where(x => x.Id != Id.Value).ToList();

                    AllGeneralContenidos = result.OrderBy(x => x.Title).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando contenidos generales");
                AllGeneralContenidos = new();
                ParentCategories = new();
            }

            // Preguntas (same as before)
            try
            {
                var preguntaIdsWithVotes = await _db.Votos.AsNoTracking()
                    .Where(v => v.EntidadTipo == "pregunta" && !v.Eliminado)
                    .Select(v => v.EntidadId)
                    .Distinct()
                    .ToListAsync();

                var preguntasFiltered = await _db.Preguntas.AsNoTracking()
                    .Where(p => !p.Eliminado && (p.Respuestas.Any() || preguntaIdsWithVotes.Contains(p.Id)))
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => new { p.Id, Title = p.Titulo })
                    .ToListAsync();

                if (preguntasFiltered == null || !preguntasFiltered.Any())
                {
                    var fallback = await _db.Preguntas.AsNoTracking()
                        .Where(p => !p.Eliminado)
                        .OrderByDescending(p => p.FechaCreacion)
                        .Take(200)
                        .Select(p => new { p.Id, Title = p.Titulo })
                        .ToListAsync();
                    AllPreguntasCandidate = fallback.Select(f => (f.Id, f.Title ?? string.Empty)).ToList();
                }
                else
                {
                    AllPreguntasCandidate = preguntasFiltered.Select(f => (f.Id, f.Title ?? string.Empty)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando preguntas candidatas; devolviendo lista vacía.");
                AllPreguntasCandidate = new();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // include CategoriaSlug so view/model can use it if needed
                var raw = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => !c.Borrado)
                    .Select(c => new { c.Sequence, c.CategoriaPadre, c.Nombre, c.CategoriaSlug })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                CategoryItems = raw.Select(r => new CategoryItem
                {
                    Sequence = r.Sequence,
                    CategoriaPadre = r.CategoriaPadre,
                    Nombre = r.Nombre ?? "",
                    CategoriaSlug = r.CategoriaSlug
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error categorías");
                CategoryItems = new();
            }
        }

        private async Task LoadCountriesAsync()
        {
            try
            {
                var raw = await _db.Paises.AsNoTracking()
                    .Where(p => !p.Borrado)
                    .OrderBy(p => p.PaisNombre)
                    .Select(p => new { p.PaisCodigo, p.PaisNombre })
                    .ToListAsync();
                PaisesLista = raw.Select(r => (r.PaisCodigo, r.PaisNombre)).ToList();
            }
            catch { PaisesLista = new(); }
        }

        private async Task LoadAuthorsAsync()
        {
            try
            {
                var adminRoleIds = await _db.Roles.AsNoTracking()
                    .Where(r => r.Name != null && r.Name.ToLower().Contains("admin"))
                    .Select(r => r.Id)
                    .ToListAsync();

                if (!adminRoleIds.Any()) { AdminAuthors = new(); return; }

                var userIds = await _db.UserRoles.AsNoTracking()
                    .Where(ur => adminRoleIds.Contains(ur.RoleId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                if (!userIds.Any()) { AdminAuthors = new(); return; }

                var users = await _db.Users.AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();

                var perfiles = await _db.Perfil.AsNoTracking()
                    .Select(p => new { p.idUser, p.Nombre })
                    .ToListAsync();

                AdminAuthors = users
                    .Select(u =>
                    {
                        var perf = perfiles.FirstOrDefault(p => p.idUser == u.Id);
                        var display = perf != null && !string.IsNullOrWhiteSpace(perf.Nombre)
                            ? perf.Nombre
                            : (u.UserName ?? u.Email ?? u.Id.ToString());
                        return (id: u.Id.ToString(), name: display);
                    })
                    .OrderBy(x => x.name)
                    .ToList();
            }
            catch { AdminAuthors = new(); }
        }

        private void BuildSubcategories()
        {
            Subcategories = new();
            if (!CategoryItems.Any()) return;

            int? parent = IdCategoriaPadre;
            if (!parent.HasValue && IdCategoria.HasValue)
            {
                var child = CategoryItems.FirstOrDefault(c => c.Sequence == IdCategoria.Value);
                parent = child?.CategoriaPadre;
            }

            if (parent.HasValue)
            {
                Subcategories = CategoryItems
                    .Where(c => c.CategoriaPadre == parent.Value)
                    .OrderBy(c => c.Nombre)
                    .Select(c => (c.Sequence, c.Nombre))
                    .ToList();
            }
        }

        private void BuildDebug()
        {
            var dbg = new
            {
                Id,
                Slug = ContenidoTituloSlug,
                CatPadre = IdCategoriaPadre,
                SubCat = IdCategoria,
                CondSel = SelectedCondicionesIds,
                SintSel = SelectedSintomasIds,
                TratSel = SelectedTratamientosIds,
                ManualContSel = SelectedManualContenidoIds,
                ManualPregSel = SelectedManualPreguntasIds
            };
            DebugInfoHtml = JsonSerializer.Serialize(dbg, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var s = value.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var chars = s.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            var cleaned = new string(chars);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^a-z0-9\s-]", "").Trim();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", "-");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"-+", "-");
            return cleaned;
        }

        // BuildSeoUrlAsync: compute SEO URL using the content's immediate category slug (do NOT prepend parent)
        private async Task BuildSeoUrlAsync()
        {
            try
            {
                SeoUrl = null;
                var slug = (ContenidoTituloSlug ?? "").Trim();
                if (string.IsNullOrWhiteSpace(slug))
                {
                    SeoUrl = null;
                    return;
                }

                if (!Id.HasValue)
                {
                    // Not saved yet - cannot resolve category relation reliably
                    SeoUrl = "/" + Uri.EscapeUriString(slug);
                    return;
                }

                var rel = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => r.IdContenido == Id.Value && !r.Borrado && r.IdCategoria != null)
                    .OrderByDescending(r => r.FechaCreacion)
                    .FirstOrDefaultAsync();

                if (rel == null || rel.IdCategoria == null)
                {
                    SeoUrl = "/" + Uri.EscapeUriString(slug);
                    return;
                }

                var cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Sequence == rel.IdCategoria.Value && !c.Borrado);

                if (cat == null)
                {
                    SeoUrl = "/" + Uri.EscapeUriString(slug);
                    return;
                }

                // Use only the category slug (no parent)
                string catSeg;
                if (!string.IsNullOrWhiteSpace(cat.CategoriaSlug))
                    catSeg = cat.CategoriaSlug.Trim('/');
                else
                    catSeg = cat.Sequence.ToString();

                var path = "/" + Uri.EscapeUriString(catSeg) + "/" + Uri.EscapeUriString(slug);
                SeoUrl = path;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo construir URL SEO; usando slug simple.");
                SeoUrl = !string.IsNullOrWhiteSpace(ContenidoTituloSlug) ? "/" + Uri.EscapeUriString(ContenidoTituloSlug) : null;
            }
        }
    }
}