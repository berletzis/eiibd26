using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
    [Authorize]
    public class PreguntasRespuestasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PreguntasRespuestasModel> _logger;

        public PreguntasRespuestasModel(ApplicationDbContext db, ILogger<PreguntasRespuestasModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class PreguntaCardVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string TituloPreview { get; set; } = "";
            public string CuerpoPreview { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int RespuestasCount { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public List<string> RespondersAvatars { get; set; } = new List<string>();
            public bool EsMia { get; set; } = false;
        }

        public List<PreguntaCardVm> Preguntas { get; set; } = new List<PreguntaCardVm>();

        // Paging
        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 12;
        public int Total { get; set; }

        // Optional dropdown lists (may be empty)
        public IEnumerable<object> Condiciones { get; set; } = Array.Empty<object>();
        public IEnumerable<object> Sintomas { get; set; } = Array.Empty<object>();
        public IEnumerable<object> Tratamientos { get; set; } = Array.Empty<object>();

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            if (Guid.TryParse(v, out var g)) return g;
            return null;
        }

        // Utility: remove HTML tags for preview and avoid broken HTML in truncated output
        private static string StripHtml(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // remove tags
            var noTags = Regex.Replace(input, "<.*?>", string.Empty);
            // collapse whitespace
            noTags = Regex.Replace(noTags, @"\s+", " ").Trim();
            return noTags;
        }

        public async Task<IActionResult> OnGetAsync(int? page, int? pageSize)
        {
            if (page.HasValue) Page = Math.Max(1, page.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);

            var userId = GetUserIdGuid();
            if (!userId.HasValue) return Forbid();

            // Base query: questions created by user OR where user has answered
            var baseQ = _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado &&
                (p.UsuarioId == userId.Value ||
                 _db.Respuestas.Any(r => r.PreguntaId == p.Id && !r.Eliminado && r.UsuarioId == userId.Value)));

            Total = await baseQ.CountAsync();

            var pageQ = baseQ.OrderByDescending(p => p.FechaCreacion).Skip((Page - 1) * PageSize).Take(PageSize);

            var items = await pageQ.Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Cuerpo,
                p.UsuarioId,
                p.FechaCreacion,
                RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado),
                Score = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0
            }).ToListAsync();

            var preguntaIds = items.Select(i => i.Id).ToArray();
            var authorIds = items.Select(i => i.UsuarioId).Distinct().ToArray();

            // Load Perfil for authors
            var authors = new Dictionary<Guid, (string name, string avatar)>();
            if (authorIds.Length > 0)
            {
                try
                {
                    var perfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => authorIds.Contains(p.idUser) && (p.Eliminado == null || p.Eliminado == false))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    authors = perfiles.ToDictionary(
                        x => x.idUser,
                        x =>
                        {
                            var full = string.IsNullOrWhiteSpace(x.Apellidos) ? (x.Nombre ?? "Usuario") : $"{(x.Nombre ?? "Usuario")} {x.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(x.Avatar) ? "/img/avatar-placeholder.png" : x.Avatar;
                            return (name: full, avatar: avatar);
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error leyendo perfiles de autores en usuarioPreguntasRespuestas.");
                }
            }

            // responders per question
            var responderRows = new List<(Guid PreguntaId, Guid UsuarioId)>();
            if (preguntaIds.Length > 0)
            {
                var respuestas = await _db.Respuestas.AsNoTracking()
                    .Where(r => preguntaIds.Contains(r.PreguntaId) && !r.Eliminado)
                    .Select(r => new { r.PreguntaId, r.UsuarioId })
                    .ToListAsync();

                responderRows = respuestas.Select(r => (r.PreguntaId, r.UsuarioId)).ToList();
            }

            var responderUserIds = responderRows.Select(r => r.UsuarioId).Distinct().ToArray();
            var responderProfiles = new Dictionary<Guid, (string name, string avatar)>();
            if (responderUserIds.Length > 0)
            {
                try
                {
                    var rperfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => responderUserIds.Contains(p.idUser) && (p.Eliminado == null || p.Eliminado == false))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    responderProfiles = rperfiles.ToDictionary(
                        x => x.idUser,
                        x =>
                        {
                            var full = string.IsNullOrWhiteSpace(x.Apellidos) ? (x.Nombre ?? "Usuario") : $"{(x.Nombre ?? "Usuario")} {x.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(x.Avatar) ? "/img/avatar-placeholder.png" : x.Avatar;
                            return (name: full, avatar: avatar);
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error leyendo perfiles de responders en usuarioPreguntasRespuestas.");
                }
            }

            // user's votes for these questions
            var votosUsuario = new Dictionary<Guid, int>();
            if (preguntaIds.Length > 0)
            {
                var votos = await _db.Votos.AsNoTracking()
                    .Where(v => v.EntidadTipo == "pregunta" && preguntaIds.Contains(v.EntidadId) && v.UsuarioId == userId.Value && !v.Eliminado)
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                    .ToListAsync();

                votosUsuario = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
            }

            // Build VMs - apply truncation limits for title and body (server-side)
            const int titleLimit = 80;
            const int bodyLimit = 140;

            var currentUserId = GetUserIdGuid();

            Preguntas = items.Select(i =>
            {
                var rawTitle = i.Titulo ?? "";
                var rawBody = StripHtml(i.Cuerpo ?? "");

                var tituloPreview = string.IsNullOrWhiteSpace(rawTitle) ? "" :
                    (rawTitle.Length > titleLimit ? rawTitle.Substring(0, titleLimit).TrimEnd() + "…" : rawTitle);

                var cuerpoText = string.IsNullOrWhiteSpace(rawBody) ? "" :
                    (rawBody.Length > bodyLimit ? rawBody.Substring(0, bodyLimit).TrimEnd() + "…" : rawBody);

                var vm = new PreguntaCardVm
                {
                    Id = i.Id,
                    Titulo = i.Titulo,
                    TituloPreview = tituloPreview,
                    CuerpoPreview = cuerpoText,
                    UsuarioId = i.UsuarioId,
                    AutorNombre = authors.ContainsKey(i.UsuarioId) ? authors[i.UsuarioId].name : "Usuario",
                    AutorAvatarUrl = authors.ContainsKey(i.UsuarioId) ? authors[i.UsuarioId].avatar : "/img/avatar-placeholder.png",
                    FechaCreacion = i.FechaCreacion,
                    RespuestasCount = i.RespuestasCount,
                    Score = i.Score,
                    UsuarioVoto = votosUsuario.TryGetValue(i.Id, out var vv) ? vv : 0,
                    EsMia = (currentUserId.HasValue && currentUserId.Value == i.UsuarioId)
                };

                var rForQ = responderRows.Where(r => r.PreguntaId == i.Id)
                                        .Select(r => r.UsuarioId)
                                        .Where(uid => uid != i.UsuarioId)
                                        .Distinct()
                                        .Take(6)
                                        .ToList();

                foreach (var rid in rForQ)
                {
                    if (responderProfiles.TryGetValue(rid, out var rp))
                        vm.RespondersAvatars.Add(rp.avatar);
                    else
                        vm.RespondersAvatars.Add("/img/avatar-placeholder.png");
                }

                return vm;
            }).ToList();

            return Page();
        }

        // OnPostCrearPreguntaAsync unchanged
        public async Task<IActionResult> OnPostCrearPreguntaAsync()
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue)
            {
                return new JsonResult(new { ok = false, error = "Usuario no autenticado" }) { StatusCode = 401 };
            }

            var titulo = (Request.Form["titulo"].FirstOrDefault() ?? "").Trim();
            var cuerpo = (Request.Form["cuerpo"].FirstOrDefault() ?? "").Trim();

            if (string.IsNullOrWhiteSpace(titulo)) return new JsonResult(new { ok = false, error = "El título es obligatorio" }) { StatusCode = 400 };
            if (string.IsNullOrWhiteSpace(cuerpo)) return new JsonResult(new { ok = false, error = "El cuerpo es obligatorio" }) { StatusCode = 400 };

            try
            {
                var pregunta = new Pregunta
                {
                    Id = Guid.NewGuid(),
                    Titulo = titulo,
                    Cuerpo = cuerpo,
                    UsuarioId = userId.Value,
                    FechaCreacion = DateTime.UtcNow,
                    Eliminado = false
                };

                _db.Preguntas.Add(pregunta);
                await _db.SaveChangesAsync();

                return new JsonResult(new { ok = true, id = pregunta.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando pregunta nueva en usuarioPreguntasRespuestas");
                return StatusCode(500, new { ok = false, error = "Error interno al crear la pregunta" });
            }
        }
    }
}