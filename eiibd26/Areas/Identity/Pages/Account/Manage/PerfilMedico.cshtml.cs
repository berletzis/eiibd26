using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using eiibd26.Models.Medico;
using eiibd26.Models.Directorio.Enums;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace eiibd26.Areas.Identity.Pages.Account.Manage;

// ╔══════════════════════════════════════════════════════════════════╗
// ║              MAPA DE DATOS — PerfilMedico                       ║
// ╠══════════════════════════════════════════════════════════════════╣
// ║  TABLA Perfil (cargada/guardada por UserId = Guid):             ║
// ║    Avatar            ← sincronizado desde MedicoPerfilExtendido ║
// ║    PermitirTelefonoReal, PermitirCorreoNoticias, AceptoPP,      ║
// ║    PermitirMostrarPais, PermitirCompartirDatosMedicos           ║
// ║    → BindProperties: Priv* (5 bool individuales)                ║
// ╠══════════════════════════════════════════════════════════════════╣
// ║  TABLA MedicoPerfilExtendido (cargada/guardada por UserId):     ║
// ║    Foto, Biografia, Hospitales (JSON), HorariosAtencion,        ║
// ║    SitioWeb, Telefono, Instagram, LinkedIn, Slug,               ║
// ║    PaisCodigo, Ciudad, Latitud, Longitud                        ║
// ║    → BindProperty: Input (InputModel)                           ║
// ╠══════════════════════════════════════════════════════════════════╣
// ║  TABLA MedicoAreaEii (relación N:M, clave compuesta):           ║
// ║    (MedicoPerfilId, CondicionId)                                 ║
// ║    → BindProperty: AreasEiiSeleccionadas (List<int>)            ║
// ║    Patrón: delete-all + insert-new; solo si ModelState.IsValid  ║
// ╠══════════════════════════════════════════════════════════════════╣
// ║  TABLA MedicosDirectorio (SOLO LECTURA aquí, admin la gestiona):║
// ║    NombreCompleto, Especialidad, Ciudad, Estado                  ║
// ║    → Input.NombreCompleto / Input.Especialidad (readonly en UI) ║
// ╚══════════════════════════════════════════════════════════════════╝
// ORDEN DE SaveChangesAsync en OnPostAsync:
//   1. SaveChanges → MedicoPerfilExtendido  [solo si ModelState.IsValid]
//   2. SaveChanges → MedicoAreaEii          [solo si ModelState.IsValid && Id > 0]
//   3. SaveChanges → Perfil                 [SIEMPRE — privacidad independiente]
// REGLA: antes del SaveChanges de Perfil, desacoplar perfil del DbContext
//   si ModelState es inválido para evitar guardar cambios parciales.
// ════════════════════════════════════════════════════════════════════

// MedicoPendiente incluido a propósito: un profesional recién registrado (aún NO aprobado para
// validar) DEBE poder completar su perfil. La capacidad de VALIDAR es aparte y sigue exigiendo
// "Medico"/"Administrador" en cada página de validación.
[Authorize(Roles = "Medico,MedicoPendiente")]
public class PerfilMedicoModel : PageModel
{
    /// <summary>Lista curada de títulos profesionales para el combo del perfil. "Otro" habilita texto libre.</summary>
    public static readonly string[] TitulosCurados =
        { "Dr.", "Dra.", "Nut.", "Lic. en Nutrición", "Psic.", "Enf.", "Mtro.", "Mtra.", "Q.F.B.", "Otro" };

    private readonly ApplicationDbContext _db;
    private readonly IMedicoBadgeService _badgeService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PerfilMedicoModel> _logger;
    private readonly IConfiguration _configuration;
    private string _googleMapsApiKey = string.Empty;
    public string GoogleMapsApiKey => _googleMapsApiKey;

    public PerfilMedicoModel(
        ApplicationDbContext db,
        IMedicoBadgeService badgeService,
        IWebHostEnvironment env,
        ILogger<PerfilMedicoModel> logger,
        IConfiguration configuration)
    {
        _db               = db;
        _badgeService     = badgeService;
        _env              = env;
        _logger           = logger;
        _configuration    = configuration;
        _googleMapsApiKey = configuration["GoogleMaps:ApiKey"] ?? string.Empty;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public SelectList PaisesSelectList { get; set; } = new(Enumerable.Empty<object>());
    public string? ComunidadCiudad { get; set; }
    public string? ComunidadEstado { get; set; }
    public List<MedicoBadgeDto> TodosLosBadges { get; set; } = new();
    public int NivelActual { get; set; }
    public int? MedicoDirectorioId { get; set; }
    public bool PerfilVinculado { get; set; }
    public List<SelectListItem> AreasEiiDisponibles { get; set; } = new();
    public HashSet<int> AreasSeleccionadas { get; set; } = new();

    [BindProperty]
    public List<int> AreasEiiSeleccionadas { get; set; } = new();

    // Campos de privacidad como bool individuales — evita validar el modelo Perfil completo
    [BindProperty] public bool PrivPermitirTelefonoReal { get; set; }
    [BindProperty] public bool PrivPermitirCorreoNoticias { get; set; }
    [BindProperty] public bool PrivAceptoPP { get; set; }
    [BindProperty] public bool PrivPermitirMostrarPais { get; set; }
    [BindProperty] public bool PrivPermitirCompartirDatosMedicos { get; set; }

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [MaxLength(500)] public string? Foto { get; set; }
        [MaxLength(200)] public string? NombreCompleto { get; set; }
        [MaxLength(200)] public string? Especialidad { get; set; }
        /// <summary>Título profesional elegido de la lista curada (o "Otro" → texto libre corto).
        /// Se guarda en la ficha vinculada (MedicoDirectorio.Titulo). Solo se muestra en público tras
        /// la aprobación del admin (badge verificado).</summary>
        [MaxLength(50)] public string? Titulo { get; set; }
        /// <summary>Tipo de profesional — igual que Titulo, vive en la ficha (MedicoDirectorio) y
        /// lo edita el propio profesional. A diferencia de Especialidad (readonly, la gestiona el
        /// admin), este dato solo GUÍA qué se le sugiere validar: equivocarse no da ni quita permisos.</summary>
        public Models.Directorio.Enums.TipoProfesional? TipoProfesional { get; set; }
        [MaxLength(2000)] public string? Biografia { get; set; }
        public List<string> Hospitales { get; set; } = new();
        [MaxLength(500)] public string? HorariosAtencion { get; set; }
        [MaxLength(300)] public string? SitioWeb { get; set; }
        [MaxLength(50)] public string? Telefono { get; set; }
        [MaxLength(150)] public string? Instagram { get; set; }
        [MaxLength(150)] public string? LinkedIn { get; set; }
        [MaxLength(100)] public string? Slug { get; set; }
        [MaxLength(10)]  public string? PaisCodigo { get; set; }
        [MaxLength(150)] public string? Ciudad { get; set; }
        [MaxLength(50)]  public string? Latitud { get; set; }
        [MaxLength(50)]  public string? Longitud { get; set; }
        [Display(Name = "Foto de perfil")]
        public IFormFile? FotoFile { get; set; }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["ActivePage"] = ManageNavPages.PerfilMedico;
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        // Auto-link: si no hay perfil, o perfil existe pero sin MedicoId vinculado
        if (perfil is null || !perfil.MedicoId.HasValue)
        {
            var userEmail = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var medicoDirectorio = await _db.MedicosDirectorio
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.EstatusReclamacion == EstatusReclamacion.Reclamado &&
                    (m.AspNetUserId == userId ||
                     (!string.IsNullOrWhiteSpace(userEmail) && m.EmailSolicitudClaim == userEmail)) &&
                    !m.Eliminado);

            if (medicoDirectorio != null)
            {
                if (perfil is null)
                {
                    // Buscar por MedicoId o crear nuevo MedicoPerfilExtendido
                    var perfilExistente = await _db.MedicosPerfilExtendido
                        .FirstOrDefaultAsync(p => p.MedicoId == medicoDirectorio.Id);

                    if (perfilExistente != null)
                    {
                        perfilExistente.UserId          = userId;
                        perfilExistente.FechaModificado = DateTime.UtcNow;
                    }
                    else
                    {
                        perfilExistente = new MedicoPerfilExtendido
                        {
                            MedicoId        = medicoDirectorio.Id,
                            UserId          = userId,
                            FechaCreado     = DateTime.UtcNow,
                            FechaModificado = DateTime.UtcNow
                        };
                        _db.MedicosPerfilExtendido.Add(perfilExistente);
                    }
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // perfil existe pero MedicoId es null — vincularlo al directorio
                    var trackedPerfil = await _db.MedicosPerfilExtendido.FindAsync(perfil.Id);
                    if (trackedPerfil != null)
                    {
                        trackedPerfil.MedicoId        = medicoDirectorio.Id;
                        trackedPerfil.FechaModificado = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                }

                _logger.LogInformation(
                    "Auto-vinculado MedicoPerfilExtendido {MedicoId} a UserId {UserId}",
                    medicoDirectorio.Id, userId);

                // Recargar con navegación
                perfil = await _db.MedicosPerfilExtendido
                    .AsNoTracking()
                    .Include(p => p.Medico)
                    .FirstOrDefaultAsync(p => p.UserId == userId);
            }
        }

        if (perfil is not null)
        {
            PerfilVinculado    = perfil.MedicoId.HasValue;
            MedicoDirectorioId = perfil.MedicoId;

            Input.Foto             = perfil.Foto;
            Input.Biografia        = perfil.Biografia;
            Input.HorariosAtencion = perfil.HorariosAtencion;
            Input.SitioWeb         = perfil.SitioWeb;
            Input.Telefono         = perfil.Telefono;
            Input.Instagram        = perfil.Instagram;
            Input.LinkedIn         = perfil.LinkedIn;
            Input.Slug             = perfil.Slug;
            Input.NombreCompleto   = perfil.Medico?.NombreCompleto;
            Input.Especialidad     = perfil.Medico?.Especialidad;
            Input.Titulo           = perfil.Medico?.Titulo;
            Input.TipoProfesional  = perfil.Medico?.TipoProfesional;

            // Ubicación de la comunidad (readonly reference)
            ComunidadCiudad = perfil.Medico?.Ciudad;
            ComunidadEstado = perfil.Medico?.Estado;

            // Ubicación oficial del médico
            Input.PaisCodigo = perfil.PaisCodigo;
            Input.Ciudad     = perfil.Ciudad;
            Input.Latitud         = perfil.Latitud;
            Input.Longitud        = perfil.Longitud;

            if (!string.IsNullOrWhiteSpace(perfil.Hospitales))
            {
                try { Input.Hospitales = JsonSerializer.Deserialize<List<string>>(perfil.Hospitales) ?? new(); }
                catch { Input.Hospitales = new(); }
            }

            if (perfil.MedicoId.HasValue)
            {
                TodosLosBadges = await _badgeService.GetTodosLosBadgesAsync(perfil.MedicoId.Value);
                NivelActual    = await _badgeService.GetNivelActualAsync(perfil.MedicoId.Value);
            }

            AreasSeleccionadas = (await _db.MedicoAreasEii
                .AsNoTracking()
                .Where(a => a.MedicoPerfilId == perfil.Id)
                .Select(a => a.CondicionId)
                .ToListAsync()).ToHashSet();
        }

        await PopulateAreasAsync();
        await PopulatePaisesAsync();

        // Cargar flags de privacidad del Perfil base
        var uid = GetUserId();
        var perfilPriv = await _db.Perfil
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.idUser == uid);
        if (perfilPriv != null)
        {
            PrivPermitirTelefonoReal          = perfilPriv.PermitirTelefonoReal          ?? false;
            PrivPermitirCorreoNoticias        = perfilPriv.PermitirCorreoNoticias        ?? false;
            PrivAceptoPP                      = perfilPriv.AceptoPP                      ?? false;
            PrivPermitirMostrarPais           = perfilPriv.PermitirMostrarPais           ?? false;
            PrivPermitirCompartirDatosMedicos = perfilPriv.PermitirCompartirDatosMedicos ?? false;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSubirFotoAsync(IFormFile? fotoFile)
    {
        if (fotoFile is null || fotoFile.Length == 0)
            return new JsonResult(new { success = false, error = "No se recibió ningún archivo." });

        var userId = GetUserId();

        try
        {
            var fileName   = $"medico-{userId:N}.jpg";
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "medicos");
            Directory.CreateDirectory(uploadsDir);
            var filePath = Path.Combine(uploadsDir, fileName);

            using var image = await SixLabors.ImageSharp.Image.LoadAsync(fotoFile.OpenReadStream());
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(400, 400), Mode = ResizeMode.Crop }));
            await image.SaveAsJpegAsync(filePath);

            var fotoUrl = $"/uploads/medicos/{fileName}";

            var perfil = await _db.MedicosPerfilExtendido
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (perfil is null)
            {
                perfil = new MedicoPerfilExtendido { UserId = userId, FechaCreado = DateTime.UtcNow };
                _db.MedicosPerfilExtendido.Add(perfil);
            }
            perfil.Foto            = fotoUrl;
            perfil.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var perfilBd = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userId);
            if (perfilBd == null)
            {
                perfilBd = new eiibd26.Models.Perfil
                {
                    idUser = userId, Avatar = fotoUrl,
                    Nombre = string.Empty, FechaCreacion = DateTime.UtcNow
                };
                _db.Perfil.Add(perfilBd);
            }
            else
            {
                perfilBd.Avatar = fotoUrl;
            }
            await _db.SaveChangesAsync();

            if (perfil.MedicoId.HasValue)
            {
                try { await _badgeService.EvaluarBadgesAutomaticosAsync(perfil.MedicoId.Value); }
                catch (Exception ex) { _logger.LogWarning(ex, "Error evaluando badges tras subir foto {UserId}", userId); }
            }

            return new JsonResult(new { success = true, url = fotoUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir foto de perfil médico para {UserId}", userId);
            return new JsonResult(new { success = false, error = "Error al procesar la imagen." });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["ActivePage"] = ManageNavPages.PerfilMedico;
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is null)
        {
            perfil = new MedicoPerfilExtendido { UserId = userId, FechaCreado = DateTime.UtcNow };
            _db.MedicosPerfilExtendido.Add(perfil);
        }

        if (Input.FotoFile is { Length: > 0 })
        {
            try
            {
                var fileName   = $"medico-{userId:N}.jpg";
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "medicos");
                Directory.CreateDirectory(uploadsDir);
                var filePath = Path.Combine(uploadsDir, fileName);

                using var image = await SixLabors.ImageSharp.Image.LoadAsync(Input.FotoFile.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(400, 400), Mode = ResizeMode.Crop }));
                await image.SaveAsJpegAsync(filePath);

                perfil.Foto = $"/uploads/medicos/{fileName}";
                Input.Foto  = perfil.Foto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando foto de perfil médico para {UserId}", userId);
            }
        }

        perfil.Biografia        = Input.Biografia;
        perfil.Hospitales       = Input.Hospitales.Any(h => !string.IsNullOrWhiteSpace(h))
            ? JsonSerializer.Serialize(Input.Hospitales.Where(h => !string.IsNullOrWhiteSpace(h)).ToList())
            : null;
        perfil.HorariosAtencion = Input.HorariosAtencion;
        perfil.SitioWeb         = Input.SitioWeb;
        perfil.Telefono         = Input.Telefono;
        perfil.Instagram        = Input.Instagram;
        perfil.LinkedIn         = Input.LinkedIn;

        // Título profesional → vive en la ficha (MedicoDirectorio.Titulo), que alimenta el display.
        // Solo escribible si ya hay ficha vinculada; sin ella el título lo pone el admin al crearla.
        // El display público sigue gateado por el badge verificado (la aprobación avala el título).
        if (perfil.Medico is not null)
        {
            perfil.Medico.Titulo = string.IsNullOrWhiteSpace(Input.Titulo) ? null : Input.Titulo.Trim();
            // Tipo de profesional → misma ficha. Solo guía el orden de lo que se le sugiere validar;
            // el permiso sigue siendo el rol. Null es válido: "general" → TOP clínico.
            perfil.Medico.TipoProfesional = Input.TipoProfesional;
        }

        perfil.PaisCodigo = Input.PaisCodigo;
        perfil.Ciudad     = Input.Ciudad;
        perfil.Latitud          = Input.Latitud;
        perfil.Longitud         = Input.Longitud;
        perfil.FechaModificado  = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(Input.Slug) && Input.Slug != perfil.Slug)
        {
            var slugLimpio = Input.Slug.Trim().ToLowerInvariant();
            var ocupado    = await _db.MedicosPerfilExtendido
                .AnyAsync(p => p.Slug == slugLimpio && p.UserId != userId);
            if (ocupado)
                ModelState.AddModelError("Input.Slug", "Este slug ya está en uso.");
            else
                perfil.Slug = slugLimpio;
        }

        if (ModelState.IsValid)
            await _db.SaveChangesAsync();

        // Persistir áreas EII: delete existentes + insert nuevas
        if (ModelState.IsValid && perfil.Id > 0)
        {
            var existentes = await _db.MedicoAreasEii
                .Where(a => a.MedicoPerfilId == perfil.Id)
                .ToListAsync();
            _db.MedicoAreasEii.RemoveRange(existentes);

            foreach (var condicionId in AreasEiiSeleccionadas.Distinct())
            {
                _db.MedicoAreasEii.Add(new eiibd26.Models.Medico.MedicoAreaEii
                {
                    MedicoPerfilId = perfil.Id,
                    CondicionId    = condicionId
                });
            }
            await _db.SaveChangesAsync();
        }

        // Badges: en try-catch para que un fallo no corte el guardado de privacidad
        if (perfil.MedicoId.HasValue)
        {
            try { await _badgeService.EvaluarBadgesAutomaticosAsync(perfil.MedicoId.Value); }
            catch (Exception ex) { _logger.LogError(ex, "Error evaluando badges para médico {MedicoId}", perfil.MedicoId.Value); }
        }

        // Si ModelState no es válido, desacoplar perfil del contexto para que
        // el SaveChangesAsync de Perfil no guarde cambios parciales de MedicoPerfilExtendido.
        // (Bug raíz: ambas entidades comparten el mismo DbContext; sin esto, campos como
        // Biografia y Hospitales se guardarían silenciosamente aunque el form tenga errores.)
        if (!ModelState.IsValid)
            _db.Entry(perfil).State = EntityState.Detached;

        // Guardar campos de privacidad en Perfil (SIEMPRE, independiente de ModelState o badges)
        {
            var privUid  = GetUserId();
            var perfilBd = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == privUid);
            if (perfilBd == null)
            {
                perfilBd = new eiibd26.Models.Perfil
                {
                    idUser        = privUid,
                    Avatar        = string.Empty,
                    Nombre        = string.Empty,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.Perfil.Add(perfilBd);
            }
            // Sync foto médico → Perfil.Avatar para que el top-menu lo refleje
            if (!string.IsNullOrWhiteSpace(perfil.Foto))
                perfilBd.Avatar = perfil.Foto;

            perfilBd.PermitirTelefonoReal          = PrivPermitirTelefonoReal;
            perfilBd.PermitirCorreoNoticias        = PrivPermitirCorreoNoticias;
            perfilBd.AceptoPP                      = PrivAceptoPP;
            perfilBd.PermitirMostrarPais           = PrivPermitirMostrarPais;
            perfilBd.PermitirCompartirDatosMedicos = PrivPermitirCompartirDatosMedicos;
            perfilBd.FechaModificado               = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (ModelState.IsValid)
        {
            SuccessMessage = "Perfil actualizado correctamente.";
            return RedirectToPage();
        }

        // Construir mensaje descriptivo con los campos específicos que fallaron
        var errores = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e =>
                string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? $"Campo '{x.Key}': valor inválido"
                    : e.ErrorMessage))
            .ToList();

        ErrorMessage = errores.Any()
            ? "Por favor corrige: " + string.Join("; ", errores)
            : "Revisa los campos del formulario e inténtalo de nuevo.";

        await PopulateAreasAsync();
        await PopulatePaisesAsync();
        // Los [BindProperty] bool retienen sus valores del POST — no hace falta repoblarlos
        AreasSeleccionadas = AreasEiiSeleccionadas.ToHashSet();
        return Page();
    }

    public async Task<IActionResult> OnGetCheckSlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return new JsonResult(new { disponible = false });
        var userId  = GetUserId();
        var ocupado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.Slug == slug.Trim().ToLowerInvariant() && p.UserId != userId);
        return new JsonResult(new { disponible = !ocupado });
    }

    public async Task<IActionResult> OnGetGenerateSlugAsync()
    {
        var userId  = GetUserId();
        var perfil  = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var baseName = perfil?.Medico?.NombreCompleto ?? "medico";
        var parts    = baseName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var suggestions = new List<string>();
        if (parts.Length >= 2) suggestions.Add($"dr-{parts[0]}-{parts[^1]}");
        suggestions.Add($"dr-{string.Join("-", parts.Take(2))}");
        suggestions.Add(string.Join("-", parts));

        return new JsonResult(suggestions.Distinct().Take(3).ToList());
    }

    private async Task PopulatePaisesAsync()
    {
        try
        {
            var paises = await _db.Paises
                .Where(p => !p.Borrado && p.VIsibleBuscador)
                .OrderBy(p => p.PaisNombre)
                .ToListAsync();
            PaisesSelectList = new SelectList(paises, "PaisCodigo", "PaisNombre");
        }
        catch
        {
            PaisesSelectList = new SelectList(Enumerable.Empty<object>());
        }
    }

    private async Task PopulateAreasAsync()
    {
        var areas = await _db.condiciones
            .AsNoTracking()
            .Where(c => c.idPadre == null && !c.Eliminado)
            .OrderBy(c => c.nombre)
            .ToListAsync();
        AreasEiiDisponibles = areas.Select(a => new SelectListItem(a.nombre, a.id.ToString())).ToList();
    }
}
