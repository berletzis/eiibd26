using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Helpers;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    public class UusuarioPreguntaDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UusuarioPreguntaDetalleModel> _logger;
        private readonly IServiceProvider _serviceProvider;

        public UusuarioPreguntaDetalleModel(
            ApplicationDbContext db, 
            ILogger<UusuarioPreguntaDetalleModel> logger,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [BindProperty] public Guid? Id { get; set; }
        [BindProperty] public string Titulo { get; set; } = "";
        [BindProperty] public string Cuerpo { get; set; } = "";

        [BindProperty] public List<int> SelectedCondiciones { get; set; } = new();
        [BindProperty] public List<int> SelectedSintomas { get; set; } = new();
        [BindProperty] public List<int> SelectedTratamientos { get; set; } = new();

        public List<(int id, string nombre)> CondicionesLista { get; set; } = new();
        public List<(int id, string nombre)> SintomasLista { get; set; } = new();
        public List<(int id, string nombre)> TratamientosLista { get; set; } = new();

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public bool CanModify { get; set; } = true;
        public int RespuestasActivasCount { get; set; }
        public string CreatedQuestionSlug { get; set; } // Para mostrar el link después de crear
        public string CurrentSlug { get; set; } // Para mostrar el slug siempre (no solo después de crear)
        public bool TieneRespuestaIA { get; set; } // Para mostrar badge de IA
        public DateTimeOffset? FechaGeneracionIA { get; set; } // Fecha de generación de IA

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var g) ? g : Guid.Empty;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Challenge();

            await LoadUserListsAsync(userId);

            // If TempData contains success message (from PRG), assign it so the view can show it
            if (TempData.ContainsKey("SuccessMessage"))
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }

            // If TempData contains the created question slug, load it
            if (TempData.ContainsKey("CreatedQuestionSlug"))
            {
                CreatedQuestionSlug = TempData["CreatedQuestionSlug"]?.ToString();
            }

            if (id.HasValue)
            {
                Id = id;
                var pregunta = await _db.Preguntas
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id.Value && p.UsuarioId == userId);

                if (pregunta == null)
                {
                    ErrorMessage = "Pregunta no encontrada o no es tuya.";
                    return Page();
                }

                RespuestasActivasCount = await _db.Respuestas
                    .AsNoTracking()
                    .CountAsync(r => r.PreguntaId == pregunta.Id && !r.Eliminado);

                CanModify = RespuestasActivasCount == 0;
                Titulo = pregunta.Titulo;
                Cuerpo = pregunta.Cuerpo;

                // Cargar slug actual para mostrarlo siempre
                CurrentSlug = pregunta.Slug;

                // Cargar estado de IA
                TieneRespuestaIA = pregunta.TieneRespuestaIA;
                FechaGeneracionIA = pregunta.FechaGeneracionIA;

                SelectedCondiciones = await _db.PreguntaCondiciones
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == pregunta.Id)
                    .Select(x => x.CondicionId)
                    .ToListAsync();

                SelectedSintomas = await _db.PreguntaSintomas
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == pregunta.Id)
                    .Select(x => x.SintomaId)
                    .ToListAsync();

                SelectedTratamientos = await _db.PreguntaTratamientos
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == pregunta.Id)
                    .Select(x => x.TratamientoId)
                    .ToListAsync();
            }

            return Page();
        }

        // FIX: usar PRG: despu�s de crear/editar redirigimos a la GET con id para evitar re-POST y as� el siguiente Guardar ser� actualizaci�n.
        public async Task<IActionResult> OnPostSaveAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Challenge();

            await LoadUserListsAsync(userId);

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ErrorMessage = "El título es obligatorio.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(Cuerpo))
            {
                ErrorMessage = "El cuerpo es obligatorio.";
                return Page();
            }

            Titulo = Titulo.Trim();
            Cuerpo = Cuerpo.Trim();

            if (Id.HasValue)
            {
                var pregunta = await _db.Preguntas
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == Id.Value && p.UsuarioId == userId);

                if (pregunta == null)
                {
                    ErrorMessage = "No se encontr� la pregunta para editar.";
                    return Page();
                }

                RespuestasActivasCount = await _db.Respuestas
                    .IgnoreQueryFilters()
                    .CountAsync(r => r.PreguntaId == pregunta.Id && !r.Eliminado);

                if (RespuestasActivasCount > 0)
                {
                    ErrorMessage = "La pregunta no se puede editar porque ya tiene respuestas.";
                    CanModify = false;
                    Id = pregunta.Id;
                    await ReloadRelationsSelectionsAsync(pregunta.Id);
                    return Page();
                }

                // Capture original title to decide whether to regenerate slug
                var originalTitle = pregunta.Titulo ?? string.Empty;
                // Update fields
                pregunta.Titulo = Titulo;
                pregunta.Cuerpo = Cuerpo;
                pregunta.FechaModificacion = DateTimeOffset.UtcNow;

                // Regenerate slug if missing OR title changed (case-insensitive)
                try
                {
                    var shouldRegen = string.IsNullOrWhiteSpace(pregunta.Slug)
                        || !string.Equals(originalTitle?.Trim(), Titulo?.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (shouldRegen)
                    {
                        // Use the same helper as other handlers to guarantee uniqueness
                        var newSlug = await SlugHelper.GenerateUniqueSlugForPregunta(_db, Titulo);
                        if (!string.IsNullOrWhiteSpace(newSlug))
                        {
                            pregunta.Slug = newSlug;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo generar slug único para la pregunta (update). Se conservará el slug actual si existe.");
                }

                await ReplaceRelationsAsync(pregunta.Id);
                await _db.SaveChangesAsync();

                // use TempData and redirect (PRG) so subsequent clicks will edit instead of create
                TempData["SuccessMessage"] = "Pregunta actualizada.";
                return RedirectToPage(new { id = pregunta.Id });
            }
            else
            {
                var nueva = new Pregunta
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = userId,
                    Titulo = Titulo,
                    Cuerpo = Cuerpo,
                    Resuelta = false,
                    Eliminado = false,
                    FechaCreacion = DateTimeOffset.UtcNow
                };

                // Generate unique slug before saving
                try
                {
                    var slug = await SlugHelper.GenerateUniqueSlugForPregunta(_db, Titulo);
                    if (!string.IsNullOrWhiteSpace(slug))
                        nueva.Slug = slug;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo generar slug único para la nueva pregunta. Se guardará sin slug.");
                }

                _db.Preguntas.Add(nueva);
                await _db.SaveChangesAsync();

                await InsertRelationsAsync(nueva.Id);
                await _db.SaveChangesAsync();

                // ===== ENCOLAR JOB DE IA EN FIRE-AND-FORGET (igual que el modal) =====
                _logger.LogDebug("[UusuarioPreguntaDetalle] Job de IA programado para pregunta {PreguntaId}", nueva.Id);
                _logger.LogDebug("[UusuarioPreguntaDetalle] Pregunta guardada - ID: {Id}, Slug: {Slug}", 
                    nueva.Id, nueva.Slug);

                var preguntaIdCapture = nueva.Id;

                // Usar Task.Factory.StartNew con LongRunning para asegurar un thread separado
                var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        _logger.LogTrace("[BG-AI] Esperando delay antes de procesar pregunta {PreguntaId}", preguntaIdCapture);
                        await Task.Delay(3000);

                        _logger.LogDebug("[BG-AI] Iniciando procesamiento de IA para pregunta {PreguntaId}", preguntaIdCapture);

                        using var scope = _serviceProvider.CreateScope();
                        var aiJob = scope.ServiceProvider.GetRequiredService<eiibd26.Jobs.AiAnswerJob>();

                        if (aiJob == null)
                        {
                            _logger.LogError("[BG-AI] AiAnswerJob es NULL para pregunta {PreguntaId}", preguntaIdCapture);
                            return;
                        }

                        await aiJob.ProcesarPreguntaAsync(preguntaIdCapture);

                        _logger.LogInformation("[BG-AI] Respuesta IA generada para pregunta {PreguntaId}", preguntaIdCapture);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("[BG-AI] Job cancelado para pregunta {PreguntaId}", preguntaIdCapture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[BG-AI] Error procesando IA para pregunta {PreguntaId}", preguntaIdCapture);
                    }
                }, TaskCreationOptions.LongRunning).Unwrap();

                _logger.LogDebug("[UusuarioPreguntaDetalle] Task de IA encolado para pregunta {PreguntaId}", nueva.Id);
                // =============================================

                // Guardar el slug en TempData para mostrarlo en la página
                if (!string.IsNullOrWhiteSpace(nueva.Slug))
                {
                    TempData["CreatedQuestionSlug"] = nueva.Slug;
                }

                // use TempData and redirect (PRG) so the page becomes the edit state and further saves update
                TempData["SuccessMessage"] = "✅ Pregunta creada exitosamente. La IA está procesando tu pregunta...";
                return RedirectToPage(new { id = nueva.Id });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid? id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Challenge();
            if (!id.HasValue)
            {
                ErrorMessage = "Identificador inv�lido.";
                return Page();
            }

            var pregunta = await _db.Preguntas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.UsuarioId == userId);

            if (pregunta == null)
            {
                ErrorMessage = "Pregunta no encontrada o no es tuya.";
                return Page();
            }

            var respuestasActivas = await _db.Respuestas
                .IgnoreQueryFilters()
                .CountAsync(r => r.PreguntaId == pregunta.Id && !r.Eliminado);

            if (respuestasActivas > 0)
            {
                ErrorMessage = "No se puede eliminar: la pregunta ya tiene respuestas.";
                RespuestasActivasCount = respuestasActivas;
                CanModify = false;
                Id = pregunta.Id;
                await ReloadRelationsSelectionsAsync(pregunta.Id);
                return Page();
            }

            pregunta.Eliminado = true;
            pregunta.FechaModificacion = DateTimeOffset.UtcNow;

            await DeletePreguntaRelationsAsync(pregunta.Id);
            await _db.SaveChangesAsync();

            // P�gina de listado (evita error de RedirectToPage)
            return Redirect("~/Identity/Usuario/usuarioPreguntasRespuestas");
        }

        private async Task DeletePreguntaRelationsAsync(Guid preguntaId)
        {
            var cond = await _db.PreguntaCondiciones.Where(x => x.PreguntaId == preguntaId).ToListAsync();
            var sint = await _db.PreguntaSintomas.Where(x => x.PreguntaId == preguntaId).ToListAsync();
            var trat = await _db.PreguntaTratamientos.Where(x => x.PreguntaId == preguntaId).ToListAsync();

            _db.PreguntaCondiciones.RemoveRange(cond);
            _db.PreguntaSintomas.RemoveRange(sint);
            _db.PreguntaTratamientos.RemoveRange(trat);
        }

        private async Task ReloadRelationsSelectionsAsync(Guid preguntaId)
        {
            SelectedCondiciones = await _db.PreguntaCondiciones
                .AsNoTracking()
                .Where(x => x.PreguntaId == preguntaId)
                .Select(x => x.CondicionId)
                .ToListAsync();

            SelectedSintomas = await _db.PreguntaSintomas
                .AsNoTracking()
                .Where(x => x.PreguntaId == preguntaId)
                .Select(x => x.SintomaId)
                .ToListAsync();

            SelectedTratamientos = await _db.PreguntaTratamientos
                .AsNoTracking()
                .Where(x => x.PreguntaId == preguntaId)
                .Select(x => x.TratamientoId)
                .ToListAsync();
        }

        private async Task LoadUserListsAsync(Guid userId)
        {
            // PROYECCI�N SEGURA: primero an�nimo, luego tuplas en memoria.
            try
            {
                var rawCond = await _db.condicionUsuario
                    .AsNoTracking()
                    .Where(cu => cu.idUsuario == userId && !cu.Eliminado && cu.idCondicion != null)
                    .Join(_db.condiciones,
                        cu => cu.idCondicion,
                        c => c.id,
                        (cu, c) => new { c.id, c.nombre })
                    .OrderBy(x => x.nombre)
                    .ToListAsync();

                CondicionesLista = rawCond
                    .Select(x => (x.id, x.nombre ?? ""))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando condiciones");
                CondicionesLista = new();
            }

            try
            {
                var rawSint = await _db.sintomasUsuario
                    .AsNoTracking()
                    .Where(su => su.idUsuario == userId && !su.Eliminado && su.idSintoma != null)
                    .Join(_db.sintomas,
                        su => su.idSintoma,
                        s => s.id,
                        (su, s) => new { s.id, s.nombre })
                    .OrderBy(x => x.nombre)
                    .ToListAsync();

                SintomasLista = rawSint
                    .Select(x => (x.id, x.nombre ?? ""))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando s�ntomas");
                SintomasLista = new();
            }

            try
            {
                var rawTrat = await _db.tratamientoUsuario
                    .AsNoTracking()
                    .Where(tu => tu.idUsuario == userId && !tu.Eliminado && tu.idTratamiento != null)
                    .Join(_db.tratamientos,
                        tu => tu.idTratamiento,
                        t => t.id,
                        (tu, t) => new { t.id, t.nombre })
                    .OrderBy(x => x.nombre)
                    .ToListAsync();

                TratamientosLista = rawTrat
                    .Select(x => (x.id, x.nombre ?? ""))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando tratamientos");
                TratamientosLista = new();
            }
        }

        private async Task ReplaceRelationsAsync(Guid preguntaId)
        {
            try
            {
                _db.PreguntaCondiciones.RemoveRange(_db.PreguntaCondiciones.Where(x => x.PreguntaId == preguntaId));
                _db.PreguntaSintomas.RemoveRange(_db.PreguntaSintomas.Where(x => x.PreguntaId == preguntaId));
                _db.PreguntaTratamientos.RemoveRange(_db.PreguntaTratamientos.Where(x => x.PreguntaId == preguntaId));
                await _db.SaveChangesAsync();
                await InsertRelationsAsync(preguntaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reemplazando relaciones {PreguntaId}", preguntaId);
            }
        }

        private async Task InsertRelationsAsync(Guid preguntaId)
        {
            try
            {
                if (SelectedCondiciones?.Any() == true)
                {
                    _db.PreguntaCondiciones.AddRange(
                        SelectedCondiciones.Distinct().Select(id => new PreguntaCondicion
                        {
                            Id = Guid.NewGuid(),
                            PreguntaId = preguntaId,
                            CondicionId = id,
                            FechaCreacion = DateTimeOffset.UtcNow
                        }));
                }
                if (SelectedSintomas?.Any() == true)
                {
                    _db.PreguntaSintomas.AddRange(
                        SelectedSintomas.Distinct().Select(id => new PreguntaSintoma
                        {
                            Id = Guid.NewGuid(),
                            PreguntaId = preguntaId,
                            SintomaId = id,
                            FechaCreacion = DateTimeOffset.UtcNow
                        }));
                }
                if (SelectedTratamientos?.Any() == true)
                {
                    _db.PreguntaTratamientos.AddRange(
                        SelectedTratamientos.Distinct().Select(id => new PreguntaTratamiento
                        {
                            Id = Guid.NewGuid(),
                            PreguntaId = preguntaId,
                            TratamientoId = id,
                            FechaCreacion = DateTimeOffset.UtcNow
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error insertando relaciones {PreguntaId}", preguntaId);
            }
        }
    }
}