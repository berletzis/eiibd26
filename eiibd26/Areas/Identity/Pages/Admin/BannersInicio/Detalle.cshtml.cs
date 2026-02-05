using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace eiibd26.Areas.Identity.Pages.Admin.BannersInicio
{
    [Authorize(Roles = "Administrador")]
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DetalleModel> _logger;
        private readonly IWebHostEnvironment _env;

        public DetalleModel(ApplicationDbContext db, ILogger<DetalleModel> logger, IWebHostEnvironment env)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        [BindProperty]
        public BannerInicio Input { get; set; } = new BannerInicio();

        // File inputs
        [BindProperty]
        public IFormFile? UploadedImageDesktop { get; set; }

        [BindProperty]
        public IFormFile? UploadedImageMobile { get; set; }

        // Compatibility: some existing forms may still post "UploadedImage"
        [BindProperty]
        public IFormFile? UploadedImage { get; set; }

        // New: flags to remove existing images
        [BindProperty]
        public bool RemoveImageDesktop { get; set; }

        [BindProperty]
        public bool RemoveImageMobile { get; set; }

        // Validation limits and allowed types
        private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };

        // Image width tuning
        private const int DesktopMaxWidth = 1140;
        private const int MobileMaxWidth = 480;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var e = await _db.BannersInicio.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id.Value);
                if (e == null) return RedirectToPage("./Index");
                Input = e;
            }
            else
            {
                Input = new BannerInicio
                {
                    IsActive = true,
                    VisibleToAuthenticated = true,
                    VisibleToAnonymous = true,
                    SortOrder = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Compatibility mapping for legacy input
            if (UploadedImage != null && UploadedImageDesktop == null) UploadedImageDesktop = UploadedImage;

            // Validate uploads if present
            if (UploadedImageDesktop != null)
            {
                var err = ValidateImageFile(UploadedImageDesktop);
                if (err != null) { ModelState.AddModelError("UploadedImageDesktop", err); return Page(); }
            }
            if (UploadedImageMobile != null)
            {
                var err = ValidateImageFile(UploadedImageMobile);
                if (err != null) { ModelState.AddModelError("UploadedImageMobile", err); return Page(); }
            }

            try
            {
                var rawUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Guid? userGuid = null;
                if (!string.IsNullOrWhiteSpace(rawUserId) && Guid.TryParse(rawUserId, out var g)) userGuid = g;

                if (Input.Id == 0)
                {
                    // New banner
                    Input.CreatedAt = DateTimeOffset.UtcNow;
                    Input.UpdatedAt = null;
                    Input.Borrado = false;

                    Input.AgregadoPorString = rawUserId;
                    Input.AgregadoPor = userGuid;

                    // Desktop upload if provided
                    if (UploadedImageDesktop != null)
                    {
                        var meta = await ProcessAndSaveImageAsync(UploadedImageDesktop, DesktopMaxWidth);
                        Input.ImagePathDesktop = meta.PublicPath;
                        Input.ImageFileNameDesktop = meta.FileName;
                        Input.ImageContentTypeDesktop = meta.ContentType;
                        Input.ImageSizeDesktop = meta.Size;
                    }

                    // Mobile upload if provided
                    if (UploadedImageMobile != null)
                    {
                        var meta = await ProcessAndSaveImageAsync(UploadedImageMobile, MobileMaxWidth);
                        Input.ImagePathMobile = meta.PublicPath;
                        Input.ImageFileNameMobile = meta.FileName;
                        Input.ImageContentTypeMobile = meta.ContentType;
                        Input.ImageSizeMobile = meta.Size;
                    }

                    _db.BannersInicio.Add(Input);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var existing = await _db.BannersInicio.FirstOrDefaultAsync(b => b.Id == Input.Id);
                    if (existing == null) return NotFound();

                    // DESKTOP: priority: upload > remove flag > keep existing
                    if (UploadedImageDesktop != null)
                    {
                        var meta = await ProcessAndSaveImageAsync(UploadedImageDesktop, DesktopMaxWidth);
                        TryDeleteExistingFile(existing.ImagePathDesktop);
                        existing.ImagePathDesktop = meta.PublicPath;
                        existing.ImageFileNameDesktop = meta.FileName;
                        existing.ImageContentTypeDesktop = meta.ContentType;
                        existing.ImageSizeDesktop = meta.Size;
                    }
                    else if (RemoveImageDesktop)
                    {
                        // delete existing file and clear DB fields
                        TryDeleteExistingFile(existing.ImagePathDesktop);
                        existing.ImagePathDesktop = null;
                        existing.ImageFileNameDesktop = null;
                        existing.ImageContentTypeDesktop = null;
                        existing.ImageSizeDesktop = null;
                    }

                    // MOBILE: same logic
                    if (UploadedImageMobile != null)
                    {
                        var meta = await ProcessAndSaveImageAsync(UploadedImageMobile, MobileMaxWidth);
                        TryDeleteExistingFile(existing.ImagePathMobile);
                        existing.ImagePathMobile = meta.PublicPath;
                        existing.ImageFileNameMobile = meta.FileName;
                        existing.ImageContentTypeMobile = meta.ContentType;
                        existing.ImageSizeMobile = meta.Size;
                    }
                    else if (RemoveImageMobile)
                    {
                        TryDeleteExistingFile(existing.ImagePathMobile);
                        existing.ImagePathMobile = null;
                        existing.ImageFileNameMobile = null;
                        existing.ImageContentTypeMobile = null;
                        existing.ImageSizeMobile = null;
                    }

                    // Update other fields
                    existing.Title = Input.Title;
                    existing.Subtitle = Input.Subtitle;
                    existing.IsImage = Input.IsImage;
                    existing.IsActive = Input.IsActive;
                    existing.VisibleToAuthenticated = Input.VisibleToAuthenticated;
                    existing.VisibleToAnonymous = Input.VisibleToAnonymous;
                    existing.SortOrder = Input.SortOrder;
                    existing.TargetUrl = Input.TargetUrl;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;

                    existing.AgregadoPorString = existing.AgregadoPorString ?? rawUserId;
                    existing.AgregadoPor = existing.AgregadoPor ?? userGuid;

                    _db.BannersInicio.Update(existing);
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = "Cambios guardados correctamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando banner");
                TempData["Error"] = "Error al guardar el banner: " + ex.Message;
                return Page();
            }
        }

        private string? ValidateImageFile(IFormFile file)
        {
            if (file.Length == 0) return "El archivo está vacío.";
            if (file.Length > MaxUploadBytes) return $"El archivo excede el tamaño máximo permitido ({MaxUploadBytes / (1024 * 1024)} MB).";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) return "Extensión no permitida. Usa .jpg/.jpeg/.png/.gif/.webp";

            var contentType = file.ContentType?.ToLowerInvariant() ?? "";
            if (!AllowedContentTypes.Contains(contentType)) return $"Tipo MIME no permitido: {contentType}";

            try
            {
                using var sr = file.OpenReadStream();
                Span<byte> header = stackalloc byte[12];
                var read = sr.Read(header);
                if (contentType == "image/jpeg" && !(header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)) return "El archivo no parece ser una imagen JPEG válida.";
                if (contentType == "image/png" && !(header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)) return "El archivo no parece ser una imagen PNG válida.";
                if (contentType == "image/gif" && !(header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)) return "El archivo no parece ser una imagen GIF válida.";
            }
            catch
            {
                return "No se pudo validar el contenido del archivo.";
            }

            return null;
        }

        private void TryDeleteExistingFile(string? publicPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicPath)) return;
                if (!publicPath.StartsWith("/uploads/banners/", StringComparison.OrdinalIgnoreCase)) return;

                var relative = publicPath.TrimStart('/');
                var fileFs = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relative.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fileFs))
                {
                    System.IO.File.Delete(fileFs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar archivo previo (no crítico).");
            }
        }

        // existing ProcessAndSaveImageAsync (keeps old implementation or optimized as you already have)
        private async Task<(string PublicPath, string FileName, string ContentType, long Size)> ProcessAndSaveImageAsync(IFormFile file, int targetWidth)
        {
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "banners");
            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsRoot, fileName);

            using (var inStream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(inStream))
            {
                if (image.Width > targetWidth)
                {
                    var newHeight = (int)Math.Round(image.Height * (targetWidth / (double)image.Width));
                    image.Mutate(x => x.Resize(targetWidth, newHeight));
                }

                if (ext == ".jpg" || ext == ".jpeg")
                {
                    var enc = new JpegEncoder { Quality = 80 };
                    await image.SaveAsync(savePath, enc);
                }
                else if (ext == ".png")
                {
                    var enc = new PngEncoder { CompressionLevel = PngCompressionLevel.DefaultCompression };
                    await image.SaveAsync(savePath, enc);
                }
                else if (ext == ".webp")
                {
                    var enc = new WebpEncoder { Quality = 80 };
                    await image.SaveAsync(savePath, enc);
                }
                else
                {
                    await using var fs = System.IO.File.Create(savePath);
                    inStream.Position = 0;
                    await inStream.CopyToAsync(fs);
                }
            }

            var publicPath = $"/uploads/banners/{fileName}";
            var fi = new FileInfo(savePath);
            return (publicPath, fileName, file.ContentType ?? "application/octet-stream", fi.Length);
        }
    }
}