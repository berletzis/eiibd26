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

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [IgnoreAntiforgeryToken]
    public class UusuarioPreguntaDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UusuarioPreguntaDetalleModel> _logger;

        public UusuarioPreguntaDetalleModel(ApplicationDbContext db, ILogger<UusuarioPreguntaDetalleModel> logger)
        {
            _db = db;
            _logger = logger;
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

                Titulo = pregunta.Titulo;
                Cuerpo = pregunta.Cuerpo;

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

        public async Task<IActionResult> OnPostSaveAsync(Guid? id)
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

            if (id.HasValue)
            {
                var pregunta = await _db.Preguntas
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == id.Value && p.UsuarioId == userId);

                if (pregunta == null)
                {
                    ErrorMessage = "No se encontró la pregunta para editar.";
                    return Page();
                }

                pregunta.Titulo = Titulo;
                pregunta.Cuerpo = Cuerpo;
                pregunta.FechaModificacion = DateTimeOffset.UtcNow;

                await ReplaceRelationsAsync(pregunta.Id);
                await _db.SaveChangesAsync();

                SuccessMessage = "Pregunta actualizada.";
                Id = pregunta.Id;
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

                _db.Preguntas.Add(nueva);
                await _db.SaveChangesAsync();

                await InsertRelationsAsync(nueva.Id);
                await _db.SaveChangesAsync();

                SuccessMessage = "Pregunta creada.";
                Id = nueva.Id;
            }

            if (Id.HasValue)
            {
                SelectedCondiciones = await _db.PreguntaCondiciones
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == Id.Value)
                    .Select(x => x.CondicionId)
                    .ToListAsync();

                SelectedSintomas = await _db.PreguntaSintomas
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == Id.Value)
                    .Select(x => x.SintomaId)
                    .ToListAsync();

                SelectedTratamientos = await _db.PreguntaTratamientos
                    .AsNoTracking()
                    .Where(x => x.PreguntaId == Id.Value)
                    .Select(x => x.TratamientoId)
                    .ToListAsync();
            }

            return Page();
        }

        private async Task LoadUserListsAsync(Guid userId)
        {
            // Condiciones
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
                CondicionesLista = new List<(int, string)>();
            }

            // Síntomas (ajusta si el modelo difiere)
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
                _logger.LogWarning(ex, "Error cargando síntomas");
                SintomasLista = new List<(int, string)>();
            }

            // Tratamientos (ajusta si difiere el modelo)
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
                TratamientosLista = new List<(int, string)>();
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
                        SelectedCondiciones
                            .Distinct()
                            .Select(id => new PreguntaCondicion
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
                        SelectedSintomas
                            .Distinct()
                            .Select(id => new PreguntaSintoma
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
                        SelectedTratamientos
                            .Distinct()
                            .Select(id => new PreguntaTratamiento
                            {
                                Id = Guid.NewGuid(),
                                PreguntaId = preguntaId,
                                TratamientoId = id,
                                FechaCreacion = DateTimeOffset.UtcNow
                            }));
                }
                // SaveChanges en caller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error insertando relaciones {PreguntaId}", preguntaId);
            }
        }
    }
}