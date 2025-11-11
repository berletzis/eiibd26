using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient; // para capturar SqlNullValueException
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
// ImageSharp — instala si aún no lo hiciste:
// dotnet add package SixLabors.ImageSharp
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

namespace eiibd26.Areas.Identity.Pages.Usuario
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

        /// <summary>
        /// OnGetAsync: carga/crea el perfil. Envuelve en try/catch y detecta campos NULL/empty para revisar qué falta.
        /// </summary>
        public async Task<IActionResult> OnGetAsync(Guid? id = null)
        {
            var missingFields = new List<string>();

            try
            {
                // Cargar lista de países para los select antes de cualquier return
                await PopulatePaisesAsync();

                // Si no se pasó id en la URL, intentar usar el usuario autenticado
                if (!id.HasValue)
                {
                    var current = GetUserIdGuid();
                    if (current == null)
                    {
                        // No hay usuario autenticado: redirigir a login (o ajustar según tu UX)
                        return RedirectToPage("/Account/Login", new { area = "Identity" });
                    }
                    id = current.Value;
                }

                // Intento normal de materializar Perfil vía EF
                try
                {
                    Perfil = await _db.Perfil
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.idUser == id.Value);
                }
                catch (SqlNullValueException sqlEx)
                {
                    // Ocurrió al materializar: leer de forma segura proyectando columnas (evita SqlNullValueException)
                    _logger.LogWarning(sqlEx, "SqlNullValueException al leer Perfil para id={Id}. Haciendo lectura segura.", id);

                    var perfilRow = await _db.Perfil
                        .Where(p => p.idUser == id.Value)
                        .Select(p => new
                        {
                            p.idUser,
                            Avatar = p.Avatar,
                            Titulo = p.Titulo,
                            Nombre = p.Nombre,
                            Apellidos = p.Apellidos,
                            Telefono = p.Telefono,
                            Email = p.Email,
                            UsoPlataforma = p.UsoPlataforma,
                            Latitud = p.Latitud,
                            Longitud = p.Longitud,
                            p.NombreCiudad,
                            p.NombrePais,
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
                        // Mapear rellenando valores por defecto si es null y recopilar campos faltantes
                        Perfil = new Perfil
                        {
                            idUser = perfilRow.idUser,
                            Avatar = perfilRow.Avatar ?? $"https://ui-avatars.com/api/?name=Usuario+{perfilRow.idUser.ToString().Substring(0, 6)}&size=110",
                            Titulo = perfilRow.Titulo ?? string.Empty,
                            Nombre = perfilRow.Nombre ?? string.Empty,
                            Apellidos = perfilRow.Apellidos,
                            Telefono = perfilRow.Telefono ?? string.Empty,
                            Email = perfilRow.Email ?? string.Empty,
                            UsoPlataforma = perfilRow.UsoPlataforma ?? string.Empty,
                            Latitud = perfilRow.Latitud ?? string.Empty,
                            Longitud = perfilRow.Longitud ?? string.Empty,
                            NombreCiudad = perfilRow.NombreCiudad,
                            NombrePais = perfilRow.NombrePais,
                            FechaCreado = perfilRow.FechaCreado ?? DateTime.UtcNow,
                            FechaCreacion = perfilRow.FechaCreacion ?? DateTime.UtcNow,
                            UltimaActividad = perfilRow.UltimaActividad ?? DateTime.UtcNow,
                            slug = perfilRow.slug
                        };

                        // Detectar columnas NULL originales para revisión (las que proyectamos)
                        if (perfilRow.Avatar == null) missingFields.Add(nameof(Perfil.Avatar));
                        if (perfilRow.Titulo == null) missingFields.Add(nameof(Perfil.Titulo));
                        if (perfilRow.Nombre == null) missingFields.Add(nameof(Perfil.Nombre));
                        if (perfilRow.Telefono == null) missingFields.Add(nameof(Perfil.Telefono));
                        if (perfilRow.Email == null) missingFields.Add(nameof(Perfil.Email));
                        if (perfilRow.UsoPlataforma == null) missingFields.Add(nameof(Perfil.UsoPlataforma));
                        if (perfilRow.Latitud == null) missingFields.Add(nameof(Perfil.Latitud));
                        if (perfilRow.Longitud == null) missingFields.Add(nameof(Perfil.Longitud));
                    }
                }

                // Si la lectura normal no devolvió perfil -> crear por defecto (y marcar campos vacíos)
                if (Perfil == null)
                {
                    Perfil = new Perfil
                    {
                        idUser = id.Value,
                        Avatar = $"https://ui-avatars.com/api/?name=Usuario+{id.Value.ToString().Substring(0, 6)}&size=110",
                        Titulo = string.Empty,
                        Nombre = string.Empty,
                        Apellidos = string.Empty,
                        Telefono = string.Empty,
                        Email = string.Empty,
                        UsoPlataforma = string.Empty,
                        Latitud = string.Empty,
                        Longitud = string.Empty,
                        FechaCreacion = DateTime.UtcNow,
                        FechaCreado = DateTime.UtcNow
                    };

                    // Todos los campos 'requeridos' estarán vacíos en este caso
                    missingFields.AddRange(new[]
                    {
                        nameof(Perfil.Titulo),
                        nameof(Perfil.Nombre),
                        nameof(Perfil.Telefono),
                        nameof(Perfil.Email),
                        nameof(Perfil.UsoPlataforma),
                        nameof(Perfil.Latitud),
                        nameof(Perfil.Longitud)
                    });
                }
                else
                {
                    // Validación adicional: comprobar qué campos están vacíos o whitespace
                    if (string.IsNullOrWhiteSpace(Perfil.Avatar)) missingFields.Add(nameof(Perfil.Avatar));
                    if (string.IsNullOrWhiteSpace(Perfil.Titulo)) missingFields.Add(nameof(Perfil.Titulo));
                    if (string.IsNullOrWhiteSpace(Perfil.Nombre)) missingFields.Add(nameof(Perfil.Nombre));
                    if (string.IsNullOrWhiteSpace(Perfil.Telefono)) missingFields.Add(nameof(Perfil.Telefono));
                    if (string.IsNullOrWhiteSpace(Perfil.Email)) missingFields.Add(nameof(Perfil.Email));
                    if (string.IsNullOrWhiteSpace(Perfil.UsoPlataforma)) missingFields.Add(nameof(Perfil.UsoPlataforma));
                    if (string.IsNullOrWhiteSpace(Perfil.Latitud)) missingFields.Add(nameof(Perfil.Latitud));
                    if (string.IsNullOrWhiteSpace(Perfil.Longitud)) missingFields.Add(nameof(Perfil.Longitud));
                }

                // Crear select lists y retornar la página
                CreateSelectLists();

                // Si hay campos faltantes, pasar info a la vista para revisión
                if (missingFields.Any())
                {
                    // Guardamos en TempData y Log para que puedas revisarlo desde UI/logs
                    ErrorMessage = "Faltan campos requeridos en el perfil. Revisa la lista de campos faltantes.";
                    TempData["MissingFields"] = string.Join(",", missingFields);
                    _logger.LogInformation("Campos faltantes para perfil {Id}: {Missing}", id, string.Join(", ", missingFields));
                }
                else
                {
                    // Limpiamos cualquier mensaje previo
                    TempData.Remove("MissingFields");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnGetAsync al cargar perfil id={Id}", id);
                // Devolver un error controlado; puedes redirigir a una página de error si lo prefieres
                ErrorMessage = "Ocurrió un error al cargar el perfil. Revisa los logs.";
                CreateSelectLists();
                return Page();
            }
        }

        // Upload avatar: server centers/crops and saves 256/110/64 PNGs
        public async Task<JsonResult> OnPostUploadAvatarAsync(IFormFile avatar)
        {
            if (avatar == null)
                return new JsonResult(new { error = "No file received" }) { StatusCode = 400 };

            // Opcional: restringir tipos; si quieres admitir webp añade soporte según tu ImageSharp version
            var allowed = new[] { "image/jpeg", "image/png" /*, "image/webp"*/ };
            if (!allowed.Contains(avatar.ContentType))
                return new JsonResult(new { error = "Tipo de archivo no permitido" }) { StatusCode = 400 };

            if (avatar.Length > 5 * 1024 * 1024)
                return new JsonResult(new { error = "El archivo supera 5 MB" }) { StatusCode = 400 };

            var userId = GetUserIdGuid();
            if (userId == null) return new JsonResult(new { error = "Usuario no autenticado" }) { StatusCode = 401 };

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads", "avatars", userId.Value.ToString());
            Directory.CreateDirectory(uploadsRoot);

            // Obtener el semáforo para este usuario (serializa uploads para evitar colisión)
            var sem = _avatarUploadLocks.GetOrAdd(userId.Value, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                // Guardar el upload en archivo temporal
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}");
                using (var fs = System.IO.File.Create(tempFile))
                {
                    await avatar.CopyToAsync(fs);
                }

                try
                {
                    // Cargar la imagen desde archivo temporal (evita problemas con streams)
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(tempFile))
                    {
                        // Generar en temporales dentro del mismo folder de uploads (pero con nombres únicos)
                        var tmp256 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-256.png");
                        var tmp110 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-110.png");
                        var tmp64 = Path.Combine(uploadsRoot, $"tmp-{Guid.NewGuid()}-avatar-64.png");

                        // Resize + Save (PNG)
                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(256, 256), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp256, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        }
                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(110, 110), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp110, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        }
                        using (var clone = image.Clone(ctx => ctx.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions { Size = new SixLabors.ImageSharp.Size(64, 64), Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop })))
                        {
                            await clone.SaveAsync(tmp64, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                        }

                        // Rutas finales
                        var final256 = Path.Combine(uploadsRoot, "avatar-256.png");
                        var final110 = Path.Combine(uploadsRoot, "avatar-110.png");
                        var final64 = Path.Combine(uploadsRoot, "avatar-64.png");

                        // Reemplazo atómico: eliminar los finales si existen y mover los temporales
                        // Primero eliminar ficheros finales si existen (para evitar excepciones de Move)
                        try { if (System.IO.File.Exists(final256)) System.IO.File.Delete(final256); } catch { /*ignore*/ }
                        try { if (System.IO.File.Exists(final110)) System.IO.File.Delete(final110); } catch { /*ignore*/ }
                        try { if (System.IO.File.Exists(final64)) System.IO.File.Delete(final64); } catch { /*ignore*/ }

                        // Mover temporales a finales
                        System.IO.File.Move(tmp256, final256);
                        System.IO.File.Move(tmp110, final110);
                        System.IO.File.Move(tmp64, final64);

                        // Construir ruta relativa a retornar/guardar en DB
                        var relativeUrl = $"/uploads/avatars/{userId.Value}/avatar-110.png";

                        var perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userId.Value);
                        if (perfil != null)
                        {
                            perfil.Avatar = relativeUrl;
                            perfil.FechaModificado = DateTime.UtcNow;
                            perfil.UsuarioModificacion = userId;
                            _db.Perfil.Update(perfil);
                            await _db.SaveChangesAsync();
                        }

                        return new JsonResult(new { url = relativeUrl });
                    }
                }
                finally
                {
                    // borrar temporal de upload (si existe)
                    try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { /*ignore*/ }
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
                // opcional: limpiar semáforos inactivos (no obligatorio)
            }
        }

        // Remove avatar
        public async Task<IActionResult> OnPostRemoveAvatarAsync()
        {
            var userId = GetUserIdGuid();
            if (userId == null) return Unauthorized();

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
                            // seguridad adicional: eliminar solo archivos dentro del folder
                            var fileName = Path.GetFileName(f);
                            var fullPath = Path.Combine(uploadsRoot, fileName);
                            if (System.IO.File.Exists(fullPath))
                                System.IO.File.Delete(fullPath);
                        }
                        catch { /*ignore*/ }
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

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando avatar para {User}", userId);
                return StatusCode(500);
            }
        }

        // GenerateSlug
        public async Task<JsonResult> OnGetGenerateSlugAsync(string baseText)
        {
            if (string.IsNullOrWhiteSpace(baseText))
                return new JsonResult(new { slug = "" });

            var baseSlug = Slugify(baseText);
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = "usuario";

            string candidate = baseSlug;
            int suffix = 0;

            while (await _db.Perfil.AnyAsync(p => p.slug == candidate))
            {
                suffix++;
                candidate = $"{baseSlug}-{suffix}";
                if (suffix > 1000) break;
            }

            return new JsonResult(new { slug = candidate });
        }

        // CheckSlug
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

            if (string.IsNullOrWhiteSpace(Perfil.Avatar))
            {
                Perfil.Avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(Perfil.Nombre ?? "Usuario")}&size=110";
                ModelState.Remove(nameof(Perfil.Avatar));
                ModelState.Remove("Perfil.Avatar");
            }

            ModelState.Remove(nameof(Perfil.Usuario));
            ModelState.Remove("Perfil.Usuario");

            Perfil.PermitirTelefonoReal = FormBool("Perfil.PermitirTelefonoReal");
            Perfil.PermitirCorreoNoticias = FormBool("Perfil.PermitirCorreoNoticias");
            Perfil.PermitirMostrarPais = FormBool("Perfil.PermitirMostrarPais");

            Perfil.Activo = FormBool("Perfil.Activo");
            Perfil.AceptoPP = FormBool("Perfil.AceptoPP");
            Perfil.Eliminado = FormBool("Perfil.Eliminado");

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
                    Perfil.FechaCreado = DateTime.UtcNow;
                    Perfil.FechaCreacion = DateTime.UtcNow;
                    Perfil.UltimaActividad = DateTime.UtcNow;
                    Perfil.UsuarioCreacion = GetUserIdGuid();

                    _db.Perfil.Add(Perfil);
                    await _db.SaveChangesAsync();

                    SuccessMessage = "Actualizacion de datos Correcta";
                    return RedirectToPage(new { id = Perfil.idUser });
                }
                else
                {
                    existing.Avatar = string.IsNullOrWhiteSpace(Perfil.Avatar) ? existing.Avatar : Perfil.Avatar;
                    existing.imagenFondo = Perfil.imagenFondo;
                    existing.Titulo = Perfil.Titulo;
                    existing.Activo = Perfil.Activo;
                    existing.Nombre = Perfil.Nombre;
                    existing.Apellidos = Perfil.Apellidos;
                    existing.Telefono = Perfil.Telefono;
                    existing.Email = Perfil.Email;
                    existing.FechaDeNacimiento = Perfil.FechaDeNacimiento;
                    existing.UsoPlataforma = Perfil.UsoPlataforma;
                    existing.idZone = Perfil.idZone;
                    existing.slug = Perfil.slug;
                    existing.Genero = Perfil.Genero;
                    existing.Latitud = Perfil.Latitud;
                    existing.Longitud = Perfil.Longitud;
                    existing.NombreCiudad = Perfil.NombreCiudad;
                    existing.NombrePais = Perfil.NombrePais;
                    existing.AceptoPP = Perfil.AceptoPP;
                    existing.UltimosEstudios = Perfil.UltimosEstudios;
                    existing.ExperienciaLaboral = Perfil.ExperienciaLaboral;
                    existing.UltimaCertificacion = Perfil.UltimaCertificacion;
                    existing.AcercaDe = Perfil.AcercaDe;
                    existing.Extras = Perfil.Extras;
                    existing.FechaModificado = DateTime.UtcNow;
                    existing.UsuarioModificacion = GetUserIdGuid();
                    existing.Eliminado = Perfil.Eliminado;

                    existing.PermitirTelefonoReal = Perfil.PermitirTelefonoReal;
                    existing.PermitirCorreoNoticias = Perfil.PermitirCorreoNoticias;
                    existing.PermitirMostrarPais = Perfil.PermitirMostrarPais;
                    existing.Activo = Perfil.Activo;
                    existing.AceptoPP = Perfil.AceptoPP;

                    _db.Perfil.Update(existing);
                    await _db.SaveChangesAsync();

                    SuccessMessage = "Actualizacion de datos Correcta";
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