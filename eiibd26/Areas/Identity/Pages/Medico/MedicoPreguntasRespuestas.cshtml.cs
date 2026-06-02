// DEUDA #5-F1: Esta vista duplica intencionalmente Areas/Identity/Pages/Usuario/usuarioPreguntasRespuestas.cshtml.cs.
// Si se cambia la lógica de preguntas (votos, avatares, render de card), actualizar AMBAS vistas.
// La diferencia clave: el médico ve preguntas donde respondió (no las suyas ni donde votó).
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Medico
{
    [Authorize(Roles = "Medico,Administrador")]
    public class MedicoPreguntasRespuestasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MedicoPreguntasRespuestasModel> _logger;

        public MedicoPreguntasRespuestasModel(
            ApplicationDbContext db,
            ILogger<MedicoPreguntasRespuestasModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class PreguntaCardVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string TituloPreview { get; set; } = "";

            [MaxLength(255)]
            public string? Slug { get; set; }

            public string CuerpoPreview { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int RespuestasCount { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public List<string> RespondersAvatars { get; set; } = new();
            public bool EsMia { get; set; } = false;

            public List<string> Condiciones { get; set; } = new();
            public List<string> Sintomas { get; set; } = new();
            public List<string> Tratamientos { get; set; } = new();
        }

        public List<PreguntaCardVm> Preguntas { get; set; } = new();
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 12;
        [BindProperty(SupportsGet = true)] public string Search { get; set; } = "";

        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public int Total { get; set; }

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            return Guid.TryParse(v, out var g) ? g : null;
        }

        private static string StripHtml(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var noTags = Regex.Replace(input, "<.*?>", string.Empty);
            noTags = Regex.Replace(noTags, @"\s+", " ").Trim();
            return noTags;
        }

        public async Task<IActionResult> OnGetAsync(int? pageNumber, int? pageSize)
        {
            if (pageNumber.HasValue) PageNumber = Math.Max(1, pageNumber.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);

            if (PageSize <= 0) PageSize = 12;
            if (PageNumber < 1) PageNumber = 1;

            var userId = GetUserIdGuid();
            if (!userId.HasValue) return Forbid();

            // Solo preguntas donde el médico respondió
            var baseQ = _db.Preguntas.AsNoTracking()
                .Where(p => !p.Eliminado &&
                            _db.Respuestas.Any(r => r.PreguntaId == p.Id && !r.Eliminado && r.UsuarioId == userId.Value));

            if (!string.IsNullOrWhiteSpace(Search))
                baseQ = baseQ.Where(p => p.Titulo.Contains(Search));

            TotalItems = await baseQ.CountAsync();
            Total = TotalItems;

            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

            var pageQ = baseQ
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Cuerpo,
                    Slug = p.Slug ?? "",
                    p.UsuarioId,
                    p.FechaCreacion,
                    RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado),
                    Score = _db.Votos
                        .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado)
                        .Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.FechaCreacion)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize);

            var items = await pageQ.ToListAsync();

            var preguntaIds = items.Select(i => i.Id).ToArray();
            var authorIds = items.Select(i => i.UsuarioId).Distinct().ToArray();

            var authors = new Dictionary<Guid, (string name, string avatar)>();
            if (authorIds.Length > 0)
            {
                try
                {
                    var perfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => authorIds.Contains(p.idUser))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    foreach (var pf in perfiles)
                    {
                        var full = string.IsNullOrWhiteSpace(pf.Apellidos)
                            ? (pf.Nombre ?? "Usuario")
                            : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";

                        var avatar = string.IsNullOrWhiteSpace(pf.Avatar)
                            ? $"uploads/avatars/{pf.idUser}/avatar-64.png"
                            : pf.Avatar.Replace("\\", "/").Trim();

                        authors[pf.idUser] = (full, avatar);
                    }

                    var missing = authorIds.Except(authors.Keys).ToArray();
                    if (missing.Length > 0)
                    {
                        var users = await _db.Users.AsNoTracking()
                            .Where(u => missing.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName })
                            .ToListAsync();

                        foreach (var u in users)
                        {
                            if (!authors.ContainsKey(u.Id))
                            {
                                var name = string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName;
                                var avatar = $"uploads/avatars/{u.Id}/avatar-64.png";
                                authors[u.Id] = (name, avatar);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error leyendo perfiles de autores.");
                }
            }

            var responderRows = new List<(Guid PreguntaId, Guid UsuarioId)>();
            if (preguntaIds.Length > 0)
            {
                try
                {
                    var respuestas = await _db.Respuestas.AsNoTracking()
                        .Where(r => preguntaIds.Contains(r.PreguntaId) && !r.Eliminado)
                        .Select(r => new { r.PreguntaId, r.UsuarioId })
                        .ToListAsync();

                    responderRows = respuestas.Select(r => (r.PreguntaId, r.UsuarioId)).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo responders.");
                }
            }

            var responderUserIds = responderRows.Select(r => r.UsuarioId).Distinct().ToArray();
            var responderProfiles = new Dictionary<Guid, (string name, string avatar)>();
            if (responderUserIds.Length > 0)
            {
                try
                {
                    var rperfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => responderUserIds.Contains(p.idUser))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    foreach (var pf in rperfiles)
                    {
                        var full = string.IsNullOrWhiteSpace(pf.Apellidos)
                            ? (pf.Nombre ?? "Usuario")
                            : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";

                        var avatar = string.IsNullOrWhiteSpace(pf.Avatar)
                            ? $"uploads/avatars/{pf.idUser}/avatar-64.png"
                            : pf.Avatar.Replace("\\", "/").Trim();

                        responderProfiles[pf.idUser] = (full, avatar);
                    }

                    var missingR = responderUserIds.Except(responderProfiles.Keys).ToArray();
                    if (missingR.Length > 0)
                    {
                        var rusers = await _db.Users.AsNoTracking()
                            .Where(u => missingR.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName })
                            .ToListAsync();

                        foreach (var u in rusers)
                        {
                            if (!responderProfiles.ContainsKey(u.Id))
                            {
                                var name = string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName;
                                var avatar = $"uploads/avatars/{u.Id}/avatar-64.png";
                                responderProfiles[u.Id] = (name, avatar);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error leyendo perfiles de responders.");
                }
            }

            var votosUsuario = new Dictionary<Guid, int>();
            if (preguntaIds.Length > 0)
            {
                try
                {
                    var votos = await _db.Votos.AsNoTracking()
                        .Where(v => v.EntidadTipo == "pregunta" &&
                                    preguntaIds.Contains(v.EntidadId) &&
                                    v.UsuarioId == userId.Value &&
                                    !v.Eliminado)
                        .GroupBy(v => v.EntidadId)
                        .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                        .ToListAsync();

                    votosUsuario = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo votos del usuario.");
                }
            }

            var condicionesMap = new Dictionary<Guid, List<string>>();
            var sintomasMap = new Dictionary<Guid, List<string>>();
            var tratamientosMap = new Dictionary<Guid, List<string>>();

            if (preguntaIds.Length > 0)
            {
                try
                {
                    condicionesMap = await _db.PreguntaCondiciones.AsNoTracking()
                        .Where(pc => preguntaIds.Contains(pc.PreguntaId))
                        .Join(_db.condiciones,
                              pc => pc.CondicionId,
                              c => c.id,
                              (pc, c) => new { pc.PreguntaId, Nombre = c.nombre })
                        .Where(x => x.Nombre != null && x.Nombre != "")
                        .GroupBy(x => x.PreguntaId)
                        .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Nombre).Distinct().OrderBy(n => n).Take(12).ToList());
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Error cargando condiciones preguntas."); }

                try
                {
                    sintomasMap = await _db.PreguntaSintomas.AsNoTracking()
                        .Where(ps => preguntaIds.Contains(ps.PreguntaId))
                        .Join(_db.sintomas,
                              ps => ps.SintomaId,
                              s => s.id,
                              (ps, s) => new { ps.PreguntaId, Nombre = s.nombre })
                        .Where(x => x.Nombre != null && x.Nombre != "")
                        .GroupBy(x => x.PreguntaId)
                        .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Nombre).Distinct().OrderBy(n => n).Take(12).ToList());
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Error cargando síntomas preguntas."); }

                try
                {
                    tratamientosMap = await _db.PreguntaTratamientos.AsNoTracking()
                        .Where(pt => preguntaIds.Contains(pt.PreguntaId))
                        .Join(_db.tratamientos,
                              pt => pt.TratamientoId,
                              t => t.id,
                              (pt, t) => new { pt.PreguntaId, Nombre = t.nombre })
                        .Where(x => x.Nombre != null && x.Nombre != "")
                        .GroupBy(x => x.PreguntaId)
                        .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Nombre).Distinct().OrderBy(n => n).Take(12).ToList());
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Error cargando tratamientos preguntas."); }
            }

            const int titleLimit = 80;
            const int bodyLimit = 160;

            Preguntas = items.Select(i =>
            {
                var rawTitle = i.Titulo ?? "";
                var rawBody = StripHtml(i.Cuerpo ?? "");

                var tituloPreview = rawTitle.Length > titleLimit
                    ? rawTitle.Substring(0, titleLimit).TrimEnd() + "…"
                    : rawTitle;

                var cuerpoPreview = rawBody.Length > bodyLimit
                    ? rawBody.Substring(0, bodyLimit).TrimEnd() + "…"
                    : rawBody;

                var vm = new PreguntaCardVm
                {
                    Id = i.Id,
                    Titulo = rawTitle,
                    TituloPreview = tituloPreview,
                    CuerpoPreview = cuerpoPreview,
                    Slug = i.Slug ?? SlugHelper.GenerateSlug(rawTitle),
                    UsuarioId = i.UsuarioId,
                    AutorNombre = authors.TryGetValue(i.UsuarioId, out var info) ? info.name : "Usuario",
                    AutorAvatarUrl = authors.TryGetValue(i.UsuarioId, out var info2) ? info2.avatar : $"uploads/avatars/{i.UsuarioId}/avatar-64.png",
                    FechaCreacion = i.FechaCreacion,
                    RespuestasCount = i.RespuestasCount,
                    Score = i.Score,
                    UsuarioVoto = votosUsuario.TryGetValue(i.Id, out var vv) ? vv : 0,
                    EsMia = false, // el médico no es autor de estas preguntas (es respondedor)
                    Condiciones = condicionesMap.TryGetValue(i.Id, out var cList) ? cList : new List<string>(),
                    Sintomas = sintomasMap.TryGetValue(i.Id, out var sList) ? sList : new List<string>(),
                    Tratamientos = tratamientosMap.TryGetValue(i.Id, out var tList) ? tList : new List<string>()
                };

                var rForQ = responderRows
                    .Where(r => r.PreguntaId == i.Id)
                    .Select(r => r.UsuarioId)
                    .Where(uid => uid != i.UsuarioId)
                    .Distinct()
                    .Take(8)
                    .ToList();

                foreach (var rid in rForQ)
                {
                    if (responderProfiles.TryGetValue(rid, out var rp))
                        vm.RespondersAvatars.Add(rp.avatar);
                    else
                        vm.RespondersAvatars.Add($"uploads/avatars/{rid}/avatar-64.png");
                }

                return vm;
            }).ToList();

            return Page();
        }
    }
}
