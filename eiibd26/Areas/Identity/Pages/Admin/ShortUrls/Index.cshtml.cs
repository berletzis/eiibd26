using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Services.ShortUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.ShortUrls
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IShortUrlService _service;

        private static readonly HashSet<string> OrigenesPermitidos =
            new(StringComparer.OrdinalIgnoreCase) { "pregunta", "contenido", "glosario", "facebook", "otro" };

        public List<eiibd26.Models.ShortUrl.ShortUrl> Items { get; set; } = new();
        public string NuevoCodigo { get; set; } = "";
        public string BaseUrl { get; set; } = "";

        public IndexModel(ApplicationDbContext db, IShortUrlService service)
        {
            _db = db;
            _service = service;
        }

        public async Task OnGetAsync()
        {
            Items = await _db.ShortUrls
                .Where(s => !s.Eliminado)
                .OrderByDescending(s => s.FechaCreado)
                .AsNoTracking()
                .ToListAsync();
            BaseUrl = $"{Request.Scheme}://{Request.Host}";
        }

        public async Task<IActionResult> OnPostCrearAsync(string? urlDestino, string? origen)
        {
            if (string.IsNullOrWhiteSpace(urlDestino))
            {
                TempData["Error"] = "La URL destino es obligatoria.";
                return RedirectToPage();
            }

            if (origen != null && !OrigenesPermitidos.Contains(origen))
                origen = "otro";

            var codigo = await _service.CrearAsync(urlDestino.Trim(), origen?.Trim());
            TempData["NuevoCodigo"] = codigo;
            TempData["Success"] = $"Short-URL creada: /r/{codigo}";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            var entry = await _db.ShortUrls.FindAsync(id);
            if (entry is not null)
            {
                entry.Eliminado = true;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Short-URL /r/{entry.Codigo} eliminada.";
            }
            return RedirectToPage();
        }
    }
}
