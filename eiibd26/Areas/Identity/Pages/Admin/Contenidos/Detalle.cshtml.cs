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

        // Campos principales
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

        // Relaciones seleccionadas
        [BindProperty] public List<int> SelectedCondicionesIds { get; set; } = new();
        [BindProperty] public List<int> SelectedSintomasIds { get; set; } = new();
        [BindProperty] public List<int> SelectedTratamientosIds { get; set; } = new();

        // Lookups
        public List<CategoryItem> CategoryItems { get; set; } = new();
        public List<(int seq, string name)> Subcategories { get; set; } = new();
        public List<(string code, string name)> PaisesLista { get; set; } = new();
        public List<(string id, string name)> AdminAuthors { get; set; } = new();

        // Panel derecho
        public List<(int id, string nombre)> AllCondiciones { get; set; } = new();
        public List<(int id, string nombre)> AllSintomas { get; set; } = new();
        public List<(int id, string nombre)> AllTratamientos { get; set; } = new();

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public string DebugInfoHtml { get; set; }

        public class CategoryItem
        {
            public int Sequence { get; set; }
            public int? CategoriaPadre { get; set; }
            public string Nombre { get; set; }
        }

        // GET
        public async Task<IActionResult> OnGetAsync(int? id, bool saved = false)
        {
            await LoadLookupsAsync();
            await LoadRelLookupsAsync();

            if (!id.HasValue)
            {
                EstadoPublicacion = 0;
                BuildSubcategories();
                BuildDebug();
                return Page();
            }

            Id = id.Value;
            var contenido = await _db.Contenidos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Id && !c.Eliminado);

            if (contenido == null)
            {
                ErrorMessage = "Contenido no encontrado.";
                BuildDebug();
                return Page();
            }

            MapContenidoToModel(contenido);
            await LoadSelectedRelationsAsync(Id.Value);
            ResolveCategorySelectionFromRelation();
            BuildSubcategories();
            BuildDebug();

            if (saved) SuccessMessage = "Guardado correctamente.";
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

        // POST
        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                await LoadLookupsAsync();
                await LoadRelLookupsAsync();

                CaptureRelationSelectionsFromForm();

                // Capturar categoría seleccionada explícitamente (padre y sub)
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
                    var entity = await _db.Contenidos.FirstOrDefaultAsync(c => c.Id == Id.Value && !c.Eliminado);
                    if (entity == null) { ErrorMessage = "Contenido no encontrado."; BuildDebug(); return Page(); }

                    entity.ContenidoTitulo = ContenidoTitulo;
                    entity.ContenidoTituloSlug = ContenidoTituloSlug;
                    entity.ContenidoTextoC = ContenidoTextoC;
                    entity.ContenidoTextoL = ContenidoTextoL;
                    entity.EstadoPublicacion = EstadoPublicacion;
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

        private async Task SaveCategoryRelationAsync(int contenidoId, int? selectedCategory, Guid user, DateTime now)
        {
            if (!selectedCategory.HasValue) return;

            // Última relación activa
            var lastActive = await _db.ContenidosCategoriasRelacion
                .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.IdCategoria != null)
                .OrderByDescending(r => r.FechaCreacion)
                .FirstOrDefaultAsync();

            if (lastActive != null && lastActive.IdCategoria == selectedCategory.Value)
                return; // ya está asociada esa categoría, no duplicar

            // Borrar lógicamente relaciones previas distintas
            var toSoftDelete = await _db.ContenidosCategoriasRelacion
                .Where(r => r.IdContenido == contenidoId && !r.Borrado && r.IdCategoria != selectedCategory.Value)
                .ToListAsync();

            foreach (var rel in toSoftDelete)
            {
                rel.Borrado = true;
                rel.FechaModificacion = now;
                rel.UsuarioModificacion = user;
            }

            _db.ContenidosCategoriasRelacion.Add(new ContenidoCategoriaRelacion
            {
                IdContenido = contenidoId,
                IdCategoria = selectedCategory.Value,
                FechaCreacion = now,
                FechaModificacion = now,
                UsuarioCreacion = user,
                UsuarioModificacion = user,
                Borrado = false
            });

            await _db.SaveChangesAsync();
        }

        private void CaptureRelationSelectionsFromForm()
        {
            if (!Request.HasFormContentType) return;
            SelectedCondicionesIds = Request.Form["SelectedCondicionesIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
            SelectedSintomasIds = Request.Form["SelectedSintomasIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
            SelectedTratamientosIds = Request.Form["SelectedTratamientosIds"]
                .Select(v => int.TryParse(v, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
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

        private void ResolveCategorySelectionFromRelation()
        {
            // Determinar categoría y subcategoría a partir de la relación activa más reciente
            if (!Id.HasValue) return;
            var last = _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == Id && !r.Borrado && r.IdCategoria != null)
                .OrderByDescending(r => r.FechaCreacion)
                .FirstOrDefault();

            if (last?.IdCategoria == null) return;

            var catObj = CategoryItems.FirstOrDefault(c => c.Sequence == last.IdCategoria.Value);
            if (catObj == null) return;

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
                    .Select(c => new { c.id, c.nombre })
                    .ToListAsync();
                AllCondiciones = condRaw.Select(c => (c.id, c.nombre ?? string.Empty)).ToList();
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                CategoryItems = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => !c.Borrado)
                    .Select(c => new CategoryItem
                    {
                        Sequence = c.Sequence,
                        CategoriaPadre = c.CategoriaPadre,
                        Nombre = c.Nombre ?? ""
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
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
                        return (u.Id.ToString(), display);
                    })
                    .OrderBy(x => x.display)
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
                TratSel = SelectedTratamientosIds
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
    }
}