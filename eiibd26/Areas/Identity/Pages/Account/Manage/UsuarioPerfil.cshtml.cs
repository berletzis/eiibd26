using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace eiibd26.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class UsuarioPerfilModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioPerfilModel> _logger;
        private readonly IWebHostEnvironment _env;
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _avatarUploadLocks = new();

        public UsuarioPerfilModel(ApplicationDbContext db, ILogger<UsuarioPerfilModel> logger, IWebHostEnvironment env)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            PaisesLista = new List<Paises>();
        }

        [BindProperty]
        public Perfil Perfil { get; set; }

        public List<Paises> PaisesLista { get; set; }

        public IEnumerable<SelectListItem> GenerosList { get; set; }
        public SelectList PaisesSelectList { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id = null)
        {
            var missingFields = new List<string>();
            ViewData["ActivePage"] = ManageNavPages.UsuarioPerfil;
            try
            {
                await PopulatePaisesAsync();

                if (!id.HasValue)
                {
                    var current = GetUserIdGuid();
                    if (current == null)
                    {
                        return RedirectToPage("/Account/Login", new { area = "Identity" });
                    }
                    id = current.Value;
                }

                try
                {
                    Perfil = await _db.Perfil
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.idUser == id.Value);
                }
                catch (SqlNullValueException sqlEx)
                {
                    _logger.LogWarning(sqlEx, "SqlNullValueException al leer Perfil para id={Id}. Haciendo lectura segura.", id);

                    var perfilRow = await _db.Perfil
                        .Where(p => p.idUser == id.Value)
                        .Select(p => new
                        {
                            p.idUser,
                            Avatar = p.Avatar,
                            Nombre = p.Nombre,
                            Apellidos = p.Apellidos,
                            EstoyAqui = p.EstoyAqui,
                            Latitud = p.Latitud,
                            Longitud = p.Longitud,
                            p.NombreCiudad,
                            p.NombrePais,
                            p.AceptoPP,
                            FechaCreado = (DateTime?)p.FechaCreado,
                            FechaCreacion = (DateTime?)p.FechaCreacion,
                            UltimaActividad = (DateTime?)p.UltimaActividad,
                            p.slug
                        })
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (perfilRow == null)
                    {
                        Perfil = null;
                    }
                    else
                    {
                        Perfil = new Perfil
                        {
                            idUser = perfilRow.idUser,
                            Avatar = perfilRow.Avatar ?? $"https://ui-avatars.com/api/?name=Usuario+{perfilRow.idUser.ToString().Substring(0, 6)}&size=110",
                            Nombre = perfilRow.Nombre ?? string.Empty,
                            Apellidos = perfilRow.Apellidos,
                            EstoyAqui = perfilRow.EstoyAqui,
                            Latitud = perfilRow.Latitud ?? string.Empty,
                            Longitud = perfilRow.Longitud ?? string.Empty,
                            NombreCiudad = perfilRow.NombreCiudad,
                            NombrePais = perfilRow.NombrePais,
                            AceptoPP = perfilRow.AceptoPP,
                            FechaCreado = perfilRow.FechaCreado ?? DateTime.UtcNow,
                            FechaCreacion = perfilRow.FechaCreacion ?? DateTime.UtcNow,
                            UltimaActividad = perfilRow.UltimaActividad ?? DateTime.UtcNow,
                            slug = perfilRow.slug
                        };

                        if (perfilRow.Avatar == null) missingFields.Add(nameof(Perfil.Avatar));
                        if (perfilRow.Nombre == null) missingFields.Add(nameof(Perfil.Nombre));
                        if (perfilRow.Latitud == null) missingFields.Add(nameof(Perfil.Latitud));
                        if (perfilRow.Longitud == null) missingFields.Add(nameof(Perfil.Longitud));
                    }
                }

                if (Perfil == null)
                {
                    Perfil = new Perfil
                    {
                        idUser = id.Value,
                        Avatar = $"https://ui-avatars.com/api/?name=Usuario+{id.Value.ToString().Substring(0, 6)}&size=110",
                        Nombre = string.Empty,
                        Apellidos = string.Empty,
                        EstoyAqui = 0,
                        Latitud = string.Empty,
                        Longitud = string.Empty,
                        AceptoPP = false,
                        FechaCreacion = DateTime.UtcNow,
                        FechaCreado = DateTime.UtcNow
                    };

                    missingFields.AddRange(new[]
                    {
                        nameof(Perfil.Nombre),
                        nameof(Perfil.Latitud),
                        nameof(Perfil.Longitud)
                    });
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Perfil.Avatar)) missingFields.Add(nameof(Perfil.Avatar));
                    if (string.IsNullOrWhiteSpace(Perfil.Nombre)) missingFields.Add(nameof(Perfil.Nombre));
                    if (string.IsNullOrWhiteSpace(Perfil.Latitud)) missingFields.Add(nameof(Perfil.Latitud));
                    if (string.IsNullOrWhiteSpace(Perfil.Longitud)) missingFields.Add(nameof(Perfil.Longitud));
                }

                CreateSelectLists();

                if (missingFields.Any())
                {
                    ErrorMessage = "Faltan campos requeridos en el perfil. Revisa la lista de campos faltantes.";
                    TempData["MissingFields"] = string.Join(",", missingFields);
                    _logger.LogInformation("Campos faltantes para perfil {Id}: {Missing}", id, string.Join(", ", missingFields));
                }
                else
                {
                    TempData.Remove("MissingFields");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnGetAsync al cargar perfil id={Id}", id);
                ErrorMessage = "Ocurrió un error al cargar el perfil. Revisa los logs.";
                CreateSelectLists();
                return Page();
            }
        }

        public async Task<JsonResult> OnPostUploadAvatarAsync(IFormFile avatar)
        {
            if (avatar == null)
                return new JsonResult(new { error = "No file received" }) { StatusCode = 400 };

            var allowed = new[] { "image/jpeg", "image/png" };
            if (!allowed.Contains(avatar.ContentType))
                return new JsonResult(new { error = "Tipo de archivo no permitido" }) { StatusCode = 400 };

            if (avatar.Length > 5 * 1024 * 1024)
                return new JsonResult(new { error = "El archivo supera 5 MB" }) { StatusCode = 400 };

            var userId = GetUserIdGuid();
            if (userId == null) return new JsonResult(new { error = "Usuario no autenticado" }) { StatusCode = 401 };

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads", "avatars", userId.Value.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var sem = _avatarUploadLocks.GetOrAdd(userId.Value, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}");
                using (var fs = System.IO.File.Create(tempFile))
                {
                    await avatar.CopyToAsync(fs);
                }

                try
                {
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(tempFile))
                    {
                        var tmp256 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-256.png");
                        var tmp110 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-110.png");
                        var tmp64 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-64.png");

                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(256, 256), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp256, new PngEncoder());
                        }
                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(110, 110), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp110, new PngEncoder());
                        }
                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(64, 64), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp64, new PngEncoder());
                        }

                        var final256 = Path.Combine(uploadsRoot, "avatar-256.png");
                        var final110 = Path.Combine(uploadsRoot, "avatar-110.png");
                        var final64 = Path.Combine(uploadsRoot, "avatar-64.png");

                        try { if (System.IO.File.Exists(final256)) System.IO.File.Delete(final256); } catch { }
                        try { if (System.IO.File.Exists(final110)) System.IO.File.Delete(final110); } catch { }
                        try { if (System.IO.File.Exists(final64)) System.IO.File.Delete(final64); } catch { }

                        System.IO.File.Move(tmp256, final256);
                        System.IO.File.Move(tmp110, final110);
                        System.IO.File.Move(tmp64, final64);

                        var relativeUrl = $"/uploads/avatars/{userId.Value}/avatar-110.png";
                        // Build absolute URL to avoid origin/proxy issues in clients
                        // Respect reverse proxy headers (X-Forwarded-Proto) when building absolute URL
                        var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
                        var scheme = !string.IsNullOrWhiteSpace(forwardedProto) ? forwardedProto : (Request.Scheme ?? "https");
                        // Force https in non-development environments to avoid mixed-content issues
                        if (!_env.IsDevelopment() && !string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
                        {
                            scheme = "https";
                        }
                        var host = Request.Host.HasValue ? Request.Host.Value : "eiibd.com";
                        var absoluteUrl = scheme + "://" + host + relativeUrl;
                        // Add a cache-busting query string for the immediate response so clients/CDNs
                        // fetch the new image. Don't persist this query string in the DB.
                        var versionedUrl = absoluteUrl + "?v=" + DateTime.UtcNow.Ticks.ToString();

                        var perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userId.Value);
                        if (perfil != null)
                        {
                            perfil.Avatar = relativeUrl;
                            perfil.FechaModificado = DateTime.UtcNow;
                            perfil.UsuarioModificacion = userId;
                            _db.Perfil.Update(perfil);
                            await _db.SaveChangesAsync();
                        }

                        // Ensure JSON response content type and consistent shape
                        return new JsonResult(new { url = versionedUrl }) { StatusCode = 200 };
                    }
                }
                finally
                {
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando avatar para {User}", userId);
#if DEBUG
                var message = ex.Message;
#else
                var message = "Error procesando imagen";
#endif
                return new JsonResult(new { error = message }) { StatusCode = 500 };
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<JsonResult> OnPostRemoveAvatarAsync()
        {
            var userId = GetUserIdGuid();
            if (userId == null)
            {
                return new JsonResult(new { error = "Usuario no autenticado" }) { StatusCode = 401 };
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads", "avatars", userId.Value.ToString());

            try
            {
                if (Directory.Exists(uploadsRoot))
                {
                    foreach (var f in Directory.EnumerateFiles(uploadsRoot))
                    {
                        try
                        {
                            var fileName = Path.GetFileName(f);
                            var fullPath = Path.Combine(uploadsRoot, fileName);
                            if (System.IO.File.Exists(fullPath))
                                System.IO.File.Delete(fullPath);
                        }
                        catch { }
                    }
                }

                var perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userId.Value);
                if (perfil != null)
                {
                    var name = string.IsNullOrWhiteSpace(perfil.Nombre) ? "Usuario" : perfil.Nombre;
                    var def = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(name)}&size=110";
                    perfil.Avatar = def;
                    perfil.FechaModificado = DateTime.UtcNow;
                    perfil.UsuarioModificacion = userId;
                    _db.Perfil.Update(perfil);
                    await _db.SaveChangesAsync();
                }

                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando avatar para {User}", userId);
                return new JsonResult(new { error = "Error eliminando avatar" }) { StatusCode = 500 };
            }
        }

        public async Task<JsonResult> OnGetGenerateSlugAsync(string baseText, Guid? userId = null)
        {
            if (string.IsNullOrWhiteSpace(baseText))
                return new JsonResult(new { slug = "" });

            var baseSlug = Slugify(baseText);
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = "usuario";

            string candidate = baseSlug;
            int suffix = 0;

            while (await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == candidate && (!userId.HasValue || p.idUser != userId.Value)))
            {
                suffix++;
                candidate = $"{baseSlug}-{suffix}";
                if (suffix > 1000) break;
            }

            return new JsonResult(new { slug = candidate });
        }

        public async Task<JsonResult> OnGetCheckSlugAsync(string slug, Guid? userId = null)
        {
            var s = Slugify(slug ?? "");
            if (string.IsNullOrWhiteSpace(s))
                return new JsonResult(new { exists = false, suggestion = "" });

            bool exists = await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == s && (!userId.HasValue || p.idUser != userId.Value));
            string suggestion = s;
            if (exists)
            {
                int suffix = 1;
                while (await _db.Perfil.AsNoTracking().AnyAsync(p => p.slug == suggestion && (!userId.HasValue || p.idUser != userId.Value)))
                {
                    suggestion = $"{s}-{suffix}";
                    suffix++;
                    if (suffix > 1000) break;
                }
            }

            return new JsonResult(new { exists, suggestion });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await PopulatePaisesAsync();
            ViewData["ActivePage"] = ManageNavPages.UsuarioPerfil;
            var form = Request.Form;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var pairs = form.Keys.Select(k =>
                {
                    var vals = form[k];
                    var joined = string.Join(",", vals);
                    return $"{k}=[{joined}]";
                });
                _logger.LogDebug("Form pairs: {pairs}", string.Join(" | ", pairs));
            }

            bool FormBool(string name)
            {
                if (form.TryGetValue(name, out StringValues values))
                {
                    foreach (var v in values)
                    {
                        if (string.IsNullOrEmpty(v)) continue;
                        var s = v.Trim().ToLowerInvariant();
                        if (s == "true" || s == "on" || s == "1") return true;
                        var tokens = s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var t in tokens)
                        {
                            var tt = t.Trim();
                            if (tt == "true" || tt == "on" || tt == "1") return true;
                        }
                    }
                }
                return false;
            }

            if (Perfil == null)
            {
                ErrorMessage = "Datos de perfil inválidos.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                CreateSelectLists();
                return Page();
            }

            // ASIGNAR idUser PRIMERO
            if (Perfil.idUser == Guid.Empty)
            {
                var current = GetUserIdGuid();
                if (current == null)
                {
                    ErrorMessage = "Usuario no autenticado.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    CreateSelectLists();
                    return Page();
                }
                Perfil.idUser = current.Value;
            }

            // PROCESAR CHECKBOXES
            Perfil.PermitirTelefonoReal = FormBool("Perfil.PermitirTelefonoReal");
            Perfil.PermitirCorreoNoticias = FormBool("Perfil.PermitirCorreoNoticias");
            Perfil.PermitirMostrarPais = FormBool("Perfil.PermitirMostrarPais");
            Perfil.AceptoPP = FormBool("Perfil.AceptoPP");

            // LOG DEBUG
            _logger.LogDebug("AceptoPP después de FormBool: {AceptoPP}", Perfil.AceptoPP);

            if (string.IsNullOrWhiteSpace(Perfil.Avatar))
            {
                Perfil.Avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(Perfil.Nombre ?? "Usuario")}&size=110";
                ModelState.Remove(nameof(Perfil.Avatar));
                ModelState.Remove("Perfil.Avatar");
            }

            ModelState.Remove(nameof(Perfil.Usuario));
            ModelState.Remove("Perfil.Usuario");

            // VALIDAR SLUG
            if (!string.IsNullOrWhiteSpace(Perfil.slug))
            {
                var sanitized = Slugify(Perfil.slug);
                var conflict = await _db.Perfil
                    .AsNoTracking()
                    .Where(p => p.slug == sanitized && p.idUser != Perfil.idUser)
                    .AnyAsync();

                if (conflict)
                {
                    ModelState.AddModelError("Perfil.slug", "El slug ya existe. Genera otro o modifica el actual.");
                }
                else
                {
                    Perfil.slug = sanitized;
                }
            }

            if (!string.IsNullOrWhiteSpace(Perfil.NombreCiudad) &&
                (string.IsNullOrWhiteSpace(Perfil.Latitud) || string.IsNullOrWhiteSpace(Perfil.Longitud)))
            {
                ModelState.AddModelError(nameof(Perfil.NombreCiudad), "Debes seleccionar una ciudad válida para obtener latitud/longitud.");
            }

            CreateSelectLists();

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Corrige los errores del formulario.";
                if (!ModelState.Any(ms => ms.Key == string.Empty))
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            try
            {
                var existing = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == Perfil.idUser);

                if (existing == null)
                {
                    // CREAR NUEVO
                    Perfil.FechaCreado = DateTime.UtcNow;
                    Perfil.FechaCreacion = DateTime.UtcNow;
                    Perfil.UltimaActividad = DateTime.UtcNow;
                    Perfil.UsuarioCreacion = GetUserIdGuid();

                    _db.Perfil.Add(Perfil);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Perfil creado para {UserId}. AceptoPP={AceptoPP}", Perfil.idUser, Perfil.AceptoPP);

                    SuccessMessage = "Perfil creado correctamente.";
                    return RedirectToPage(new { id = Perfil.idUser });
                }
                else
                {
                    // ACTUALIZAR EXISTENTE
                    existing.Avatar = string.IsNullOrWhiteSpace(Perfil.Avatar) ? existing.Avatar : Perfil.Avatar;
                    existing.imagenFondo = Perfil.imagenFondo;
                    existing.Nombre = Perfil.Nombre;
                    existing.Apellidos = Perfil.Apellidos;
                    existing.FechaDeNacimiento = Perfil.FechaDeNacimiento;
                    existing.EstoyAqui = Perfil.EstoyAqui;
                    existing.idZone = Perfil.idZone;
                    existing.slug = Perfil.slug;
                    existing.Genero = Perfil.Genero;
                    existing.Latitud = Perfil.Latitud;
                    existing.Longitud = Perfil.Longitud;
                    existing.NombreCiudad = Perfil.NombreCiudad;
                    existing.NombrePais = Perfil.NombrePais;
                    existing.AcercaDe = Perfil.AcercaDe;
                    existing.PermitirTelefonoReal = Perfil.PermitirTelefonoReal;
                    existing.PermitirCorreoNoticias = Perfil.PermitirCorreoNoticias;
                    existing.PermitirMostrarPais = Perfil.PermitirMostrarPais;
                    existing.AceptoPP = Perfil.AceptoPP; // ← CRÍTICO
                    existing.FechaModificado = DateTime.UtcNow;
                    existing.UsuarioModificacion = GetUserIdGuid();

                    _db.Perfil.Update(existing);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Perfil actualizado para {UserId}. AceptoPP={AceptoPP}", existing.idUser, existing.AceptoPP);

                    SuccessMessage = "Perfil actualizado correctamente.";
                    return RedirectToPage(new { id = existing.idUser });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando perfil para {User}", Perfil?.idUser);
                ErrorMessage = "Ocurrió un error al guardar. Intenta más tarde.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }
        }

        private async Task PopulatePaisesAsync()
        {
            try
            {
                PaisesLista = await _db.Paises
                    .Where(p => !p.Borrado && p.VIsibleBuscador)
                    .OrderBy(p => p.PaisNombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando lista de países");
                PaisesLista = new List<Paises>();
            }
        }

        private void CreateSelectLists()
        {
            GenerosList = new List<SelectListItem>
            {
                new SelectListItem("Masculino","Masculino", Perfil?.Genero == "Masculino"),
                new SelectListItem("Femenino","Femenino", Perfil?.Genero == "Femenino")
            };

            PaisesSelectList = new SelectList(PaisesLista ?? new List<Paises>(), "PaisCodigo", "PaisNombre", Perfil?.NombrePais);
        }

        private string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.ToLowerInvariant().Trim();

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            text = sb.ToString().Normalize(NormalizationForm.FormC);

            text = Regex.Replace(text, @"[^a-z0-9]+", "-");
            text = Regex.Replace(text, @"-+", "-").Trim('-');
            return text;
        }

        private Guid? GetUserIdGuid()
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;
            if (Guid.TryParse(userId, out var g)) return g;
            return null;
        }
    }
}