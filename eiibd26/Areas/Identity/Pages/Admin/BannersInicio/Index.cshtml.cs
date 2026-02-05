using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.BannersInicio
{
    [Authorize(Roles = "Administrador")]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class BannerRow
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Subtitle { get; set; }

            // Legacy single image (compat)
            public string? ImagePath { get; set; }

            // New separate images
            public string? ImagePathDesktop { get; set; }
            public string? ImagePathMobile { get; set; }

            public bool IsImage { get; set; }
            public bool IsActive { get; set; }
            public bool VisibleToAuthenticated { get; set; }
            public bool VisibleToAnonymous { get; set; }
            public int SortOrder { get; set; }
            public bool Borrado { get; set; }

            public string? AgregadoPorDisplay { get; set; }
            public Guid? AgregadoPor { get; set; }

            // Banner link
            public string? TargetUrl { get; set; }

            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public List<BannerRow> Banners { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Seleccionamos tanto propiedades legacy como las nuevas de metadata/versión
                var raw = await _db.BannersInicio
                    .AsNoTracking()
                    .OrderBy(b => b.SortOrder)
                    .ThenBy(b => b.Id)
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Subtitle,
                        LegacyImage = b.ImagePath,
                        ImagePathDesktop = b.ImagePathDesktop,
                        ImagePathMobile = b.ImagePathMobile,
                        b.IsImage,
                        b.IsActive,
                        b.VisibleToAuthenticated,
                        b.VisibleToAnonymous,
                        b.SortOrder,
                        b.Borrado,
                        AgregadoPorGuid = b.AgregadoPor,
                        AgregadoPorRaw = b.AgregadoPorString,
                        b.TargetUrl,
                        b.CreatedAt,
                        b.UpdatedAt
                    })
                    .ToListAsync();

                // Cargar usuarios por Id (ApplicationUser.Id es Guid)
                var users = await _db.Users
                    .AsNoTracking()
                    .Select(u => new { u.Id, Display = (u.UserName ?? u.Email ?? u.Id.ToString()) })
                    .ToListAsync();

                var usersByIdString = users.ToDictionary(u => u.Id.ToString(), u => u.Display);

                Banners = raw.Select(b =>
                {
                    string display;
                    if (b.AgregadoPorGuid.HasValue)
                    {
                        var key = b.AgregadoPorGuid.Value.ToString();
                        display = usersByIdString.TryGetValue(key, out var d) ? d : key;
                    }
                    else if (!string.IsNullOrWhiteSpace(b.AgregadoPorRaw))
                    {
                        var rawId = b.AgregadoPorRaw;
                        display = usersByIdString.TryGetValue(rawId, out var d2) ? d2 : rawId;
                    }
                    else
                    {
                        display = "-";
                    }

                    // Preferir ImagePathDesktop si existe, si no fallback a LegacyImage
                    var desktopPath = !string.IsNullOrWhiteSpace(b.ImagePathDesktop) ? b.ImagePathDesktop : b.LegacyImage;
                    var mobilePath = b.ImagePathMobile;

                    return new BannerRow
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Subtitle = b.Subtitle,
                        ImagePath = b.LegacyImage,
                        ImagePathDesktop = desktopPath,
                        ImagePathMobile = mobilePath,
                        IsImage = b.IsImage,
                        IsActive = b.IsActive,
                        VisibleToAuthenticated = b.VisibleToAuthenticated,
                        VisibleToAnonymous = b.VisibleToAnonymous,
                        SortOrder = b.SortOrder,
                        Borrado = b.Borrado,
                        AgregadoPor = b.AgregadoPorGuid,
                        AgregadoPorDisplay = display,
                        TargetUrl = b.TargetUrl,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando banners en Index admin.");
                Banners = new List<BannerRow>();
                TempData["Error"] = "Error al cargar banners. Revisa logs.";
            }
        }

        // Soft-delete handler
        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            try
            {
                var banner = await _db.BannersInicio.FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null) return NotFound();

                banner.Borrado = true;
                banner.UpdatedAt = DateTimeOffset.UtcNow;
                _db.BannersInicio.Update(banner);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Banner marcado como eliminado.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando banner {Id} como borrado", id);
                TempData["Error"] = "Error al eliminar el banner.";
                return RedirectToPage();
            }
        }

        // Restore handler
        public async Task<IActionResult> OnPostRestaurarAsync(int id)
        {
            try
            {
                var banner = await _db.BannersInicio.FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null) return NotFound();

                banner.Borrado = false;
                banner.UpdatedAt = DateTimeOffset.UtcNow;
                _db.BannersInicio.Update(banner);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Banner restaurado.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restaurando banner {Id}", id);
                TempData["Error"] = "Error al restaurar el banner.";
                return RedirectToPage();
            }
        }

        // dentro de IndexModel class add:
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostReorderAsync([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return BadRequest(new { success = false, message = "No ids provided" });

            try
            {
                // Update SortOrder according to position in array (0..n-1)
                var banners = await _db.BannersInicio.Where(b => ids.Contains(b.Id)).ToListAsync();

                for (int i = 0; i < ids.Length; i++)
                {
                    var id = ids[i];
                    var b = banners.FirstOrDefault(x => x.Id == id);
                    if (b != null)
                    {
                        b.SortOrder = i;
                        _db.BannersInicio.Update(b);
                    }
                }
                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordenando banners");
                return StatusCode(500, new { success = false, message = "Error interno" });
            }
        }
    }
}