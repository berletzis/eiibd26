using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize] // Solo usuarios autenticados pueden ver sus preguntas/respuestas/votos relacionadas
    public class PreguntasRespuestasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PreguntasRespuestasModel> _logger;

        public PreguntasRespuestasModel(ApplicationDbContext db, ILogger<PreguntasRespuestasModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // View models
        public class PreguntaCardVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; }
            public string CuerpoPreview { get; set; }
            public Guid UsuarioId { get; set; }
            public DateTimeOffset FechaCreacion { get; set; }
            public bool Resuelta { get; set; }
            public int Score { get; set; }
            public int RespuestasCount { get; set; }
            public List<Guid> EtiquetaIds { get; set; } = new List<Guid>();
            public string AutorNombre { get; set; } = "";
            // Voto actual del usuario para esta pregunta: 1, -1 o 0 (sin voto)
            public int UsuarioVoto { get; set; } = 0;
        }

        public class EtiquetaOptionVm
        {
            public Guid Id { get; set; }
            public string NombreCanonico { get; set; }
            public string Nombre { get; set; }
        }

        // Properties bound to page
        public List<PreguntaCardVm> Preguntas { get; set; } = new List<PreguntaCardVm>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int Total { get; set; }

        // Etiquetas para los selects (detalladas por tipo)
        public List<EtiquetaOptionVm> Condiciones { get; set; } = new List<EtiquetaOptionVm>();
        public List<EtiquetaOptionVm> Sintomas { get; set; } = new List<EtiquetaOptionVm>();
        public List<EtiquetaOptionVm> Tratamientos { get; set; } = new List<EtiquetaOptionVm>();

        // Helper to obtain current user id guid
        private Guid? GetUserIdGuid()
        {
            var uid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(uid)) return null;
            if (Guid.TryParse(uid, out var g)) return g;
            return null;
        }

        public async Task<IActionResult> OnGetAsync(int page = 1, int pageSize = 12)
        {
            var userId = GetUserIdGuid();
            if (userId == null) return Challenge();

            Page = Math.Max(1, page);
            PageSize = Math.Max(6, pageSize);

            // Cargar listas de etiquetas por tipo para el modal (detalle en los selects)
            Condiciones = await _db.Etiquetas
                .AsNoTracking()
                .Where(e => !e.Eliminado && e.Tipo == "condicion")
                .OrderBy(e => e.NombreCanonico)
                .Select(e => new EtiquetaOptionVm { Id = e.Id, Nombre = e.Nombre, NombreCanonico = e.NombreCanonico })
                .ToListAsync();

            Sintomas = await _db.Etiquetas
                .AsNoTracking()
                .Where(e => !e.Eliminado && e.Tipo == "sintoma")
                .OrderBy(e => e.NombreCanonico)
                .Select(e => new EtiquetaOptionVm { Id = e.Id, Nombre = e.Nombre, NombreCanonico = e.NombreCanonico })
                .ToListAsync();

            Tratamientos = await _db.Etiquetas
                .AsNoTracking()
                .Where(e => !e.Eliminado && e.Tipo == "tratamiento")
                .OrderBy(e => e.NombreCanonico)
                .Select(e => new EtiquetaOptionVm { Id = e.Id, Nombre = e.Nombre, NombreCanonico = e.NombreCanonico })
                .ToListAsync();

            // Consultas auxiliares: preguntas respondidas y preguntas votadas por el usuario
            var qFromAnswers = _db.Respuestas
                .Where(r => !r.Eliminado && r.UsuarioId == userId.Value)
                .Select(r => r.PreguntaId);

            var qFromVotes = _db.Votos
                .Where(v => !v.Eliminado && v.UsuarioId == userId.Value && v.EntidadTipo == "pregunta")
                .Select(v => v.EntidadId);

            // Query principal: preguntas del usuario OR a las que respondió OR a las que votó
            var baseQuery = _db.Preguntas
                .AsNoTracking()
                .Where(p => !p.Eliminado &&
                            (p.UsuarioId == userId.Value || qFromAnswers.Contains(p.Id) || qFromVotes.Contains(p.Id)))
                .OrderByDescending(p => p.FechaCreacion);

            Total = await baseQuery.CountAsync();

            var items = await baseQuery
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new PreguntaCardVm
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    CuerpoPreview = (p.Cuerpo.Length > 320 ? p.Cuerpo.Substring(0, 320) + "…" : p.Cuerpo),
                    UsuarioId = p.UsuarioId,
                    FechaCreacion = p.FechaCreacion,
                    Resuelta = p.Resuelta,
                    Score = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0,
                    RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado),
                    EtiquetaIds = p.PreguntaEtiquetas.Where(pe => !pe.Eliminado).Select(pe => pe.EtiquetaId).ToList()
                }).ToListAsync();

            // Obtener votos del usuario para las preguntas listadas para indicar estado en UI
            var preguntaIds = items.Select(i => i.Id).ToArray();
            if (preguntaIds.Length > 0)
            {
                var votosUsuario = await _db.Votos
                    .AsNoTracking()
                    .Where(v => !v.Eliminado && v.UsuarioId == userId.Value && v.EntidadTipo == "pregunta" && preguntaIds.Contains(v.EntidadId))
                    .ToDictionaryAsync(v => v.EntidadId, v => v.Valor);

                foreach (var it in items)
                {
                    it.UsuarioVoto = votosUsuario.TryGetValue(it.Id, out var val) ? val : 0;
                    it.AutorNombre = it.UsuarioId == userId.Value ? "Tú" : "Usuario";
                }
            }
            else
            {
                foreach (var it in items)
                {
                    it.UsuarioVoto = 0;
                    it.AutorNombre = it.UsuarioId == userId.Value ? "Tú" : "Usuario";
                }
            }

            Preguntas = items;
            return Page();
        }

        // Handler: crear pregunta (desde modal). Requiere autenticación.
        public async Task<IActionResult> OnPostCrearPreguntaAsync(
            [FromForm] string titulo,
            [FromForm] string cuerpo,
            [FromForm] Guid? relacionCondicionId,
            [FromForm] Guid? relacionSintomaId,
            [FromForm] Guid? relacionTratamientoId)
        {
            if (!User.Identity.IsAuthenticated) return Challenge();

            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(cuerpo))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { error = "Título y cuerpo son requeridos." });
                ModelState.AddModelError(string.Empty, "Título y cuerpo son requeridos.");
                return RedirectToPage();
            }

            var userId = GetUserIdGuid().Value;

            var p = new Pregunta
            {
                Id = Guid.NewGuid(),
                UsuarioId = userId,
                Titulo = titulo.Trim(),
                Cuerpo = cuerpo.Trim(),
                FechaCreacion = DateTimeOffset.UtcNow,
                Resuelta = false,
                Eliminado = false
            };

            _db.Preguntas.Add(p);

            // Relacionar las etiquetas seleccionadas (si aplica). Creamos una relación por cada tipo seleccionado.
            var relaciones = new List<PreguntaEtiqueta>();
            if (relacionCondicionId.HasValue)
            {
                var et = await _db.Etiquetas.FindAsync(relacionCondicionId.Value);
                if (et != null && !et.Eliminado && et.Tipo == "condicion")
                {
                    relaciones.Add(new PreguntaEtiqueta { Id = Guid.NewGuid(), PreguntaId = p.Id, EtiquetaId = et.Id, FechaRelacion = DateTimeOffset.UtcNow, Eliminado = false });
                }
            }
            if (relacionSintomaId.HasValue)
            {
                var et = await _db.Etiquetas.FindAsync(relacionSintomaId.Value);
                if (et != null && !et.Eliminado && et.Tipo == "sintoma")
                {
                    relaciones.Add(new PreguntaEtiqueta { Id = Guid.NewGuid(), PreguntaId = p.Id, EtiquetaId = et.Id, FechaRelacion = DateTimeOffset.UtcNow, Eliminado = false });
                }
            }
            if (relacionTratamientoId.HasValue)
            {
                var et = await _db.Etiquetas.FindAsync(relacionTratamientoId.Value);
                if (et != null && !et.Eliminado && et.Tipo == "tratamiento")
                {
                    relaciones.Add(new PreguntaEtiqueta { Id = Guid.NewGuid(), PreguntaId = p.Id, EtiquetaId = et.Id, FechaRelacion = DateTimeOffset.UtcNow, Eliminado = false });
                }
            }

            if (relaciones.Count > 0) _db.PreguntaEtiquetas.AddRange(relaciones);

            await _db.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var score = 0;
                return new JsonResult(new
                {
                    ok = true,
                    id = p.Id,
                    titulo = p.Titulo,
                    cuerpoPreview = p.Cuerpo.Length > 320 ? p.Cuerpo.Substring(0, 320) + "…" : p.Cuerpo,
                    fechaCreacion = p.FechaCreacion,
                    score
                });
            }

            TempData["SuccessMessage"] = "Pregunta creada correctamente.";
            return RedirectToPage(new { page = 1 });
        }

        // Handler: crear respuesta (desde modal). Requiere autenticación.
        public async Task<IActionResult> OnPostCrearRespuestaAsync([FromForm] Guid preguntaId, [FromForm] string cuerpo)
        {
            if (!User.Identity.IsAuthenticated) return Challenge();

            if (string.IsNullOrWhiteSpace(cuerpo))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { error = "El cuerpo de la respuesta es requerido." });
                ModelState.AddModelError(string.Empty, "El cuerpo de la respuesta es requerido.");
                return RedirectToPage();
            }

            var pregunta = await _db.Preguntas.FirstOrDefaultAsync(p => p.Id == preguntaId && !p.Eliminado);
            if (pregunta == null) return NotFound();

            var userId = GetUserIdGuid().Value;

            var r = new Respuesta
            {
                Id = Guid.NewGuid(),
                PreguntaId = preguntaId,
                UsuarioId = userId,
                Cuerpo = cuerpo.Trim(),
                FechaCreacion = DateTimeOffset.UtcNow,
                EsAceptada = false,
                Eliminado = false
            };

            _db.Respuestas.Add(r);
            await _db.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return new JsonResult(new
                {
                    ok = true,
                    id = r.Id,
                    preguntaId = r.PreguntaId,
                    cuerpo = r.Cuerpo,
                    fechaCreacion = r.FechaCreacion
                });
            }

            TempData["SuccessMessage"] = "Respuesta publicada.";
            return RedirectToPage();
        }
    }
}