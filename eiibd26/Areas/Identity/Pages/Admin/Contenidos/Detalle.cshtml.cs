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

        // Form fields
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

        // DTO para evitar materializar entidad con columnas NULL en propiedades no anulables
        public class CategoryItem
        {
            public int Sequence { get; set; }
            public int? CategoriaPadre { get; set; }
            public string Nombre { get; set; }
        }

        // Lookups
        public List<CategoryItem> CategoryItems { get; set; } = new();
        public List<(int seq, string name)> Subcategories { get; set; } = new();
        public List<(string code, string name)> PaisesLista { get; set; } = new();
        public List<(string id, string name)> AdminAuthors { get; set; } = new();

        // Feedback / debug
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public string DebugInfoHtml { get; set; }

        // GET
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadLookupsAsync();

            if (!id.HasValue)
            {
                EstadoPublicacion = 0;
                BuildSubcategories();
                BuildDebug();
                return Page();
            }

            Id = id.Value;

            var contenido = await _db.Contenidos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Id && c.Eliminado == false);

            if (contenido == null)
            {
                ErrorMessage = "Contenido no encontrado.";
                BuildDebug();
                return Page();
            }

            ContenidoTitulo = contenido.ContenidoTitulo;
            ContenidoTituloSlug = contenido.ContenidoTituloSlug;
            ContenidoTextoC = contenido.ContenidoTextoC;
            ContenidoTextoL = contenido.ContenidoTextoL;
            EstadoPublicacion = contenido.EstadoPublicacion ?? 0;
            ContenidoFechaInicio = contenido.ContenidoFechaInicio;
            ContenidoFechaFin = contenido.ContenidoFechaFin;
            Autor = contenido.Autor;
            PaisClave = contenido.PaisClave;
            URLImagenPrincipal = contenido.URLImagenPrincipal;
            SelectedAutorId = contenido.IdAutor != Guid.Empty ? contenido.IdAutor.ToString() : null;

            var rel = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => r.IdContenido == Id && r.Borrado == false && r.IdCategoria != null)
                .OrderByDescending(r => r.FechaCreacion)
                .FirstOrDefaultAsync();

            if (rel != null && rel.IdCategoria.HasValue)
            {
                var cat = CategoryItems.FirstOrDefault(c => c.Sequence == rel.IdCategoria.Value);
                if (cat != null)
                {
                    if (cat.CategoriaPadre.HasValue)
                    {
                        IdCategoria = cat.Sequence;
                        IdCategoriaPadre = cat.CategoriaPadre;
                    }
                    else
                    {
                        IdCategoriaPadre = cat.Sequence;
                        IdCategoria = null;
                    }
                }
            }

            BuildSubcategories();
            BuildDebug();
            return Page();
        }

        // POST Save
        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                await LoadLookupsAsync();

                if (!IdCategoria.HasValue && Request.HasFormContentType)
                {
                    var rawChild = Request.Form["IdCategoria"].FirstOrDefault();
                    if (int.TryParse(rawChild, out var parsedChild)) IdCategoria = parsedChild;
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
                    .AnyAsync(c => c.ContenidoTituloSlug == ContenidoTituloSlug && (!Id.HasValue || c.Id != Id.Value) && c.Eliminado == false);

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

                Guid autorGuid = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(SelectedAutorId) && Guid.TryParse(SelectedAutorId, out var parsedA))
                    autorGuid = parsedA;

                if (Id.HasValue)
                {
                    var entity = await _db.Contenidos.FirstOrDefaultAsync(c => c.Id == Id.Value && c.Eliminado == false);
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
                    entity.FechaModificado = DateTime.UtcNow;

                    if (IdCategoria.HasValue)
                    {
                        _db.ContenidosCategoriasRelacion.Add(new ContenidoCategoriaRelacion
                        {
                            IdContenido = entity.Id,
                            IdCategoria = IdCategoria,
                            FechaCreacion = DateTime.UtcNow,
                            FechaModificacion = DateTime.UtcNow,
                            UsuarioCreacion = GetCurrentUserGuid(),
                            UsuarioModificacion = GetCurrentUserGuid(),
                            Borrado = false
                        });
                    }

                    await _db.SaveChangesAsync();
                    SuccessMessage = "Actualizado.";
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
                        UsuarioCreacion = GetCurrentUserGuid(),
                        FechaCreado = DateTime.UtcNow,
                        Eliminado = false
                    };

                    _db.Contenidos.Add(entity);
                    await _db.SaveChangesAsync();
                    Id = entity.Id;

                    if (IdCategoria.HasValue)
                    {
                        _db.ContenidosCategoriasRelacion.Add(new ContenidoCategoriaRelacion
                        {
                            IdContenido = entity.Id,
                            IdCategoria = IdCategoria,
                            FechaCreacion = DateTime.UtcNow,
                            FechaModificacion = DateTime.UtcNow,
                            UsuarioCreacion = GetCurrentUserGuid(),
                            UsuarioModificacion = GetCurrentUserGuid(),
                            Borrado = false
                        });
                        await _db.SaveChangesAsync();
                    }

                    SuccessMessage = "Creado.";
                }

                await LoadLookupsAsync();
                BuildSubcategories();
                BuildDebug();
                ViewData["SuccessMessage"] = SuccessMessage;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando contenido");
                ErrorMessage = "Error al guardar.";
                BuildDebug();
                return Page();
            }
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // Proyección segura: sólo columnas necesarias y coalesce para evitar NULL -> GetString
                CategoryItems = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => c.Borrado == false)
                    .Select(c => new CategoryItem
                    {
                        Sequence = c.Sequence,
                        CategoriaPadre = c.CategoriaPadre,
                        Nombre = c.Nombre ?? ""   // evita SqlNullValueException
                    })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando categorías (DTO)");
                CategoryItems = new List<CategoryItem>();
            }
        }

        private async Task LoadCountriesAsync()
        {
            try
            {
                var raw = await _db.Paises
                    .AsNoTracking()
                    .Where(p => p.Borrado == false)
                    .OrderBy(p => p.PaisNombre)
                    .Select(p => new { p.PaisCodigo, p.PaisNombre })
                    .ToListAsync();

                PaisesLista = raw.Select(r => (r.PaisCodigo, r.PaisNombre)).ToList();
            }
            catch
            {
                PaisesLista = new List<(string, string)>();
            }
        }

        private async Task LoadAuthorsAsync()
        {
            try
            {
                var adminRoleIds = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.Name != null && r.Name.ToLower().Contains("admin"))
                    .Select(r => r.Id)
                    .ToListAsync();

                if (!adminRoleIds.Any())
                {
                    AdminAuthors = new List<(string id, string name)>();
                    return;
                }

                var userIds = await _db.UserRoles
                    .AsNoTracking()
                    .Where(ur => adminRoleIds.Contains(ur.RoleId))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                if (!userIds.Any())
                {
                    AdminAuthors = new List<(string id, string name)>();
                    return;
                }

                var users = await _db.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();

                var perfiles = await _db.Perfil
                    .AsNoTracking()
                    .Where(p => p.idUser != null)
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
            catch
            {
                AdminAuthors = new List<(string id, string name)>();
            }
        }

        private void BuildSubcategories()
        {
            Subcategories = new List<(int seq, string name)>();
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
                IdCategoriaPadre,
                IdCategoria,
                CategoriesCount = CategoryItems.Count,
                ParentsCount = CategoryItems.Count(c => c.CategoriaPadre == null),
                SubcategoriesCount = Subcategories.Count,
                FirstParents = CategoryItems.Where(c => c.CategoriaPadre == null).Take(5),
                SubSample = Subcategories.Take(5),
                AdminAuthorsCount = AdminAuthors.Count
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