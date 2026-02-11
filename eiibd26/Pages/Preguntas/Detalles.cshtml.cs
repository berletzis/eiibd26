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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eiibd26.Pages.Preguntas
{
    [AllowAnonymous]
    public class DetallesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DetallesModel> _logger;

        public DetallesModel(ApplicationDbContext db, ILogger<DetallesModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public record AuthorInfo(string Name, string Avatar);

        public class PreguntaVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string Cuerpo { get; set; } = "";
            public string Slug { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public bool EsMia { get; set; } = false;
            public List<string> Condiciones { get; set; } = new();
            public List<string> Sintomas { get; set; } = new();
            public List<string> Tratamientos { get; set; } = new();
        }

        public class RespuestaVm
        {
            public Guid Id { get; set; }
            public string Cuerpo { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public bool EsMia { get; set; } = false;
        }

        [BindProperty(SupportsGet = true)]
        public string Slug { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int AnswersPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 12;

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        public PreguntaVm Pregunta { get; set; } = new();
        public List<RespuestaVm> Respuestas { get; set; } = new();

        public RespuestaVm AcceptedAnswer { get; set; }
        public List<RespuestaVm> TopSuggestedAnswers { get; set; } = new();

        // New: accepted/top suggested answers for JSON-LD and to render accepted to bots


        public int TotalItems { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 1;

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            return Guid.TryParse(v, out var g) ? g : null;
        }

        public async Task<IActionResult> OnGetAsync(string slug, Guid? id, int? answersPage, int? pageSize, string search)
        {
            // Normalize incoming params
            if (answersPage.HasValue) AnswersPage = Math.Max(1, answersPage.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);
            if (search != null) Search = search.Trim();

            var currentUserId = GetUserIdGuid();

            // Resolve the question by slug (preferred) or by id
            object preguntaQuery = null;

            if (!string.IsNullOrWhiteSpace(slug))
            {
                Slug = slug.Trim();
                preguntaQuery = await _db.Preguntas
                    .AsNoTracking()
                    .Where(p => p.Slug == Slug && !p.Eliminado)
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.Cuerpo,
                        p.Slug,
                        p.UsuarioId,
                        p.FechaCreacion,
                        Score = _db.Votos
                            .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado)
                            .Select(v => (int?)v.Valor).Sum() ?? 0
                    })
                    .FirstOrDefaultAsync();
            }
            else if (id.HasValue)
            {
                preguntaQuery = await _db.Preguntas
                    .AsNoTracking()
                    .Where(p => p.Id == id.Value && !p.Eliminado)
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.Cuerpo,
                        p.Slug,
                        p.UsuarioId,
                        p.FechaCreacion,
                        Score = _db.Votos
                            .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado)
                            .Select(v => (int?)v.Valor).Sum() ?? 0
                    })
                    .FirstOrDefaultAsync();

                if (preguntaQuery != null)
                {
                    Slug = ((dynamic)preguntaQuery).Slug ?? "";
                }
            }
            else
            {
                return NotFound();
            }

            if (preguntaQuery == null)
                return NotFound();

            // Map anonymous projection into strongly typed locals
            var proj = (dynamic)preguntaQuery;
            Guid preguntaId = (Guid)proj.Id;
            string preguntaTitulo = proj.Titulo ?? "";
            string preguntaCuerpo = proj.Cuerpo ?? "";
            string preguntaSlug = proj.Slug ?? "";
            Guid preguntaUsuarioId = (Guid)proj.UsuarioId;
            DateTimeOffset preguntaFechaCreacion = proj.FechaCreacion;
            int preguntaScore = (int)proj.Score;

            // Redirect to SEO slug if necessary
            if (id.HasValue && !string.IsNullOrWhiteSpace(preguntaSlug))
            {
                var seoPath = $"/Preguntas/{preguntaSlug}";
                var currentPath = Request.Path.Value ?? "";
                if (!currentPath.Equals(seoPath, StringComparison.OrdinalIgnoreCase))
                {
                    var queryParts = new List<string>();
                    if (AnswersPage > 1) queryParts.Add($"answersPage={AnswersPage}");
                    if (PageSize > 0 && PageSize != 12) queryParts.Add($"pageSize={PageSize}");
                    if (!string.IsNullOrWhiteSpace(Search)) queryParts.Add($"search={Uri.EscapeDataString(Search)}");

                    var qs = queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : string.Empty;
                    var redirectUrl = seoPath + qs;
                    return RedirectPermanent(redirectUrl);
                }
            }

            // Load author profile
            var preguntaAutor = new AuthorInfo("Usuario", "/img/avatar-placeholder.png");
            try
            {
                var perfilAutor = await _db.Set<Perfil>().AsNoTracking()
                    .Where(p => p.idUser == preguntaUsuarioId)
                    .Select(p => new { p.Nombre, p.Apellidos, p.Avatar })
                    .FirstOrDefaultAsync();

                if (perfilAutor != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(perfilAutor.Apellidos)
                        ? (perfilAutor.Nombre ?? "Usuario")
                        : $"{(perfilAutor.Nombre ?? "Usuario")} {perfilAutor.Apellidos}";
                    var avatar = string.IsNullOrWhiteSpace(perfilAutor.Avatar)
                        ? $"uploads/avatars/{preguntaUsuarioId}/avatar-64.png"
                        : perfilAutor.Avatar.Replace("\\", "/");
                    preguntaAutor = new AuthorInfo(nombre, avatar);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo perfil del autor de la pregunta.");
            }

            // Load relations
            var condiciones = new List<string>();
            var sintomas = new List<string>();
            var tratamientos = new List<string>();

            try
            {
                condiciones = await _db.PreguntaCondiciones.AsNoTracking()
                    .Where(pc => pc.PreguntaId == preguntaId)
                    .Join(_db.condiciones, pc => pc.CondicionId, c => c.id, (pc, c) => c.nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error cargando condiciones."); }

            try
            {
                sintomas = await _db.PreguntaSintomas.AsNoTracking()
                    .Where(ps => ps.PreguntaId == preguntaId)
                    .Join(_db.sintomas, ps => ps.SintomaId, s => s.id, (ps, s) => s.nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error cargando sintomas."); }

            try
            {
                tratamientos = await _db.PreguntaTratamientos.AsNoTracking()
                    .Where(pt => pt.PreguntaId == preguntaId)
                    .Join(_db.tratamientos, pt => pt.TratamientoId, t => t.id, (pt, t) => t.nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error cargando tratamientos."); }

            Pregunta = new PreguntaVm
            {
                Id = preguntaId,
                Titulo = preguntaTitulo,
                Cuerpo = preguntaCuerpo,
                Slug = preguntaSlug,
                UsuarioId = preguntaUsuarioId,
                AutorNombre = preguntaAutor.Name,
                AutorAvatarUrl = preguntaAutor.Avatar,
                FechaCreacion = preguntaFechaCreacion,
                Score = preguntaScore,
                UsuarioVoto = 0,
                EsMia = currentUserId.HasValue && preguntaUsuarioId == currentUserId.Value,
                Condiciones = condiciones,
                Sintomas = sintomas,
                Tratamientos = tratamientos
            };

            // User vote for question
            if (currentUserId.HasValue)
            {
                try
                {
                    var votoQ = await _db.Votos.AsNoTracking()
                        .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == preguntaId && v.UsuarioId == currentUserId.Value && !v.Eliminado)
                        .OrderByDescending(v => v.FechaCreacion)
                        .FirstOrDefaultAsync();
                    if (votoQ != null)
                        Pregunta.UsuarioVoto = votoQ.Valor;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo voto usuario para pregunta {PreguntaId}", preguntaId);
                }
            }

            // --- Load top answers for accepted/suggested (global, not paged) ---
            try
            {
                var topAnswers = await _db.Respuestas.AsNoTracking()
                    .Where(r => r.PreguntaId == preguntaId && !r.Eliminado)
                    .Select(r => new
                    {
                        r.Id,
                        r.Cuerpo,
                        r.UsuarioId,
                        r.FechaCreacion,
                        Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado)
                                         .Select(v => (int?)v.Valor).Sum() ?? 0
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.FechaCreacion)
                    .Take(6) // accepted + up to 5 suggested
                    .ToListAsync();

                if (topAnswers != null && topAnswers.Count > 0)
                {
                    var topIds = topAnswers.Select(x => x.Id).ToArray();

                    // Map authors for these top answers
                    var userIdsTop = topAnswers.Select(x => x.UsuarioId).Distinct().ToArray();
                    var authorMap = new Dictionary<Guid, AuthorInfo>();
                    try
                    {
                        var perfiles = await _db.Set<Perfil>().AsNoTracking()
                            .Where(p => userIdsTop.Contains(p.idUser))
                            .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                            .ToListAsync();

                        foreach (var pf in perfiles)
                        {
                            var full = string.IsNullOrWhiteSpace(pf.Apellidos) ? (pf.Nombre ?? "Usuario")
                                : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(pf.Avatar) ? $"uploads/avatars/{pf.idUser}/avatar-64.png" : pf.Avatar.Replace("\\", "/");
                            authorMap[pf.idUser] = new AuthorInfo(full, avatar);
                        }

                        var missing = userIdsTop.Except(authorMap.Keys).ToArray();
                        if (missing.Length > 0)
                        {
                            var users = await _db.Users.AsNoTracking()
                                .Where(u => missing.Contains(u.Id))
                                .Select(u => new { u.Id, u.UserName })
                                .ToListAsync();

                            foreach (var u in users)
                            {
                                if (!authorMap.ContainsKey(u.Id))
                                    authorMap[u.Id] = new AuthorInfo(string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, $"uploads/avatars/{u.Id}/avatar-64.png");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error obteniendo perfiles autores top answers");
                    }

                    // Map votes by current user for top answers (if logged)
                    var votosUsuarioTop = new Dictionary<Guid, int>();
                    if (currentUserId.HasValue)
                    {
                        try
                        {
                            var votos = await _db.Votos.AsNoTracking()
                                .Where(v => v.UsuarioId == currentUserId.Value && v.EntidadTipo == "respuesta" && topIds.Contains(v.EntidadId) && !v.Eliminado)
                                .GroupBy(v => v.EntidadId)
                                .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                                .ToListAsync();
                            votosUsuarioTop = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error obteniendo votos usuario para top answers");
                        }
                    }

                    var mappedTop = topAnswers.Select(x =>
                    {
                        var autor = authorMap.TryGetValue(x.UsuarioId, out var ai) ? ai : new AuthorInfo("Usuario", $"uploads/avatars/{x.UsuarioId}/avatar-64.png");
                        return new RespuestaVm
                        {
                            Id = x.Id,
                            Cuerpo = x.Cuerpo,
                            UsuarioId = x.UsuarioId,
                            AutorNombre = autor.Name,
                            AutorAvatarUrl = autor.Avatar,
                            FechaCreacion = x.FechaCreacion,
                            Score = x.Score,
                            UsuarioVoto = votosUsuarioTop.TryGetValue(x.Id, out var vv) ? vv : 0,
                            EsMia = currentUserId.HasValue && x.UsuarioId == currentUserId.Value
                        };
                    }).ToList();

                    if (mappedTop.Count > 0)
                    {
                        AcceptedAnswer = mappedTop.First();
                        TopSuggestedAnswers = mappedTop.Skip(1).Take(5).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo accepted/top suggested answers.");
            }

            // --- Now fetch paged answers for the UI (unchanged behavior) ---
            var baseAnsQ = _db.Respuestas.AsNoTracking()
                .Where(r => r.PreguntaId == preguntaId && !r.Eliminado);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.ToLower();
                baseAnsQ = baseAnsQ.Where(r => r.Cuerpo.ToLower().Contains(s));
            }

            TotalItems = await baseAnsQ.CountAsync();
            if (PageSize <= 0) PageSize = 12;
            if (AnswersPage < 1) AnswersPage = 1;
            if (TotalPages > 0 && AnswersPage > TotalPages) AnswersPage = TotalPages;

            var ansPageQ = baseAnsQ
                .Select(r => new
                {
                    r.Id,
                    r.Cuerpo,
                    r.UsuarioId,
                    r.FechaCreacion,
                    Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado)
                                     .Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.FechaCreacion)
                .Skip((AnswersPage - 1) * PageSize)
                .Take(PageSize);

            var ansItems = await ansPageQ.ToListAsync();
            var ansUserIds = ansItems.Select(a => a.UsuarioId).Distinct().ToArray();

            var ansAuthors = new Dictionary<Guid, AuthorInfo>();
            if (ansUserIds.Length > 0)
            {
                try
                {
                    var perfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => ansUserIds.Contains(p.idUser))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    foreach (var pf in perfiles)
                    {
                        var full = string.IsNullOrWhiteSpace(pf.Apellidos)
                            ? (pf.Nombre ?? "Usuario")
                            : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";
                        var avatar = string.IsNullOrWhiteSpace(pf.Avatar)
                            ? $"uploads/avatars/{pf.idUser}/avatar-64.png"
                            : pf.Avatar.Replace("\\", "/");
                        ansAuthors[pf.idUser] = new AuthorInfo(full, avatar);
                    }

                    var missing = ansUserIds.Except(ansAuthors.Keys).ToArray();
                    if (missing.Length > 0)
                    {
                        var users = await _db.Users.AsNoTracking()
                            .Where(u => missing.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName })
                            .ToListAsync();

                        foreach (var u in users)
                        {
                            if (!ansAuthors.ContainsKey(u.Id))
                                ansAuthors[u.Id] = new AuthorInfo(string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, $"uploads/avatars/{u.Id}/avatar-64.png");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo perfiles autores respuestas");
                }
            }

            var votosUsuarioAns = new Dictionary<Guid, int>();
            if (currentUserId.HasValue && ansItems.Count > 0)
            {
                try
                {
                    var ansIds = ansItems.Select(a => a.Id).ToArray();
                    var votos = await _db.Votos.AsNoTracking()
                        .Where(v => v.UsuarioId == currentUserId.Value
                                    && v.EntidadTipo == "respuesta"
                                    && ansIds.Contains(v.EntidadId)
                                    && !v.Eliminado)
                        .GroupBy(v => v.EntidadId)
                        .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                        .ToListAsync();

                    votosUsuarioAns = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo votos usuario respuestas");
                }
            }

            Respuestas = ansItems.Select(a =>
            {
                var autorInfo = ansAuthors.TryGetValue(a.UsuarioId, out var ai)
                    ? ai
                    : new AuthorInfo("Usuario", $"uploads/avatars/{a.UsuarioId}/avatar-64.png");

                return new RespuestaVm
                {
                    Id = a.Id,
                    Cuerpo = a.Cuerpo,
                    UsuarioId = a.UsuarioId,
                    AutorNombre = autorInfo.Name,
                    AutorAvatarUrl = autorInfo.Avatar,
                    FechaCreacion = a.FechaCreacion,
                    Score = a.Score,
                    UsuarioVoto = votosUsuarioAns.TryGetValue(a.Id, out var vv) ? vv : 0,
                    EsMia = currentUserId.HasValue && a.UsuarioId == currentUserId.Value
                };
            }).ToList();

            return Page();
        }
    }
}