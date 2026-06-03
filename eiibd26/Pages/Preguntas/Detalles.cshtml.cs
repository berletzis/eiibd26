using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Validacion;
using eiibd26.Helpers;
using eiibd26.Services;
using eiibd26.Services.Validacion;
using eiibd26.Services.Medico;
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
        private readonly SearchSuggestionService _suggestionService;
        private readonly IValidacionRespuestaService _validacionRespuestaService;
        private readonly IMedicoBadgeService _badgeService;

        public DetallesModel(
            ApplicationDbContext db,
            ILogger<DetallesModel> logger,
            SearchSuggestionService suggestionService,
            IValidacionRespuestaService validacionRespuestaService,
            IMedicoBadgeService badgeService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _suggestionService = suggestionService ?? throw new ArgumentNullException(nameof(suggestionService));
            _validacionRespuestaService = validacionRespuestaService ?? throw new ArgumentNullException(nameof(validacionRespuestaService));
            _badgeService = badgeService ?? throw new ArgumentNullException(nameof(badgeService));
        }

        public record AuthorInfo(string Name, string Avatar, string Slug = "");

        public class PreguntaVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string Cuerpo { get; set; } = "";
            public string Slug { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorSlug { get; set; } = "";
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
            public string AutorSlug { get; set; } = "";
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public bool EsMia { get; set; } = false;

            // AI Fields
            public bool EsIA { get; set; } = false;
            public string? ModeloIA { get; set; }

            // Distintivo de profesional de salud (médico verificado)
            public bool EsProfesionalSalud { get; set; } = false;

            // Validación de respuesta por médicos
            public bool YaValidada { get; set; } = false;
            public int ValidacionesCount { get; set; } = 0;
        }

        // VM para el partial de validación de respuesta
        public class ValidacionRespuestaPartialVm
        {
            public Guid RespuestaId { get; set; }
            public string Slug { get; set; } = "";
            public bool YaValidada { get; set; }
            public string? ComentarioExistente { get; set; }
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

        // ===== NUEVO: Propiedades para UI de respuesta de IA =====
        public RespuestaVm RespuestaIA { get; set; }
        public bool TienePreguntaPendienteIA { get; set; } = false;
        public int TotalRespuestasHumanas { get; set; } = 0;
        // =========================================================

        // ===== NUEVO: Contenido Relacionado =====
        public List<SuggestionDto> PreguntasRelacionadas { get; set; } = new();
        public List<SuggestionDto> ArticulosRelacionados { get; set; } = new();
        public List<SuggestionDto> RespuestasRelacionadas { get; set; } = new();
        // =========================================

        public int TotalItems { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 1;

        // Gate de validación de respuestas por médicos
        public bool CanValidateRespuestas { get; set; } = false;
        public Guid? CurrentUserId { get; set; }

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
            CurrentUserId = currentUserId;

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
                    .Select(p => new { p.Nombre, p.Apellidos, p.Avatar, p.slug })
                    .FirstOrDefaultAsync();

                if (perfilAutor != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(perfilAutor.Apellidos)
                        ? (perfilAutor.Nombre ?? "Usuario")
                        : $"{(perfilAutor.Nombre ?? "Usuario")} {perfilAutor.Apellidos}";
                    var avatar = string.IsNullOrWhiteSpace(perfilAutor.Avatar)
                        ? $"uploads/avatars/{preguntaUsuarioId}/avatar-64.png"
                        : perfilAutor.Avatar.Replace("\\", "/");
                    preguntaAutor = new AuthorInfo(nombre, avatar, perfilAutor.slug ?? "");
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
                AutorSlug = preguntaAutor.Slug ?? "",
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
                        r.EsIA,
                        r.ModeloIA,
                        r.EsAceptada,
                        Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado)
                                         .Select(v => (int?)v.Valor).Sum() ?? 0
                    })
                    .OrderByDescending(x => x.EsAceptada)  // Accepted first
                    .ThenBy(x => x.EsIA)                    // Human before AI
                    .ThenByDescending(x => x.Score)         // By score
                    .ThenBy(x => x.FechaCreacion)           // Older first
                    .Take(20) // Get more to ensure we get all answers including AI
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
                            .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar, p.slug })
                            .ToListAsync();

                        foreach (var pf in perfiles)
                        {
                            var full = string.IsNullOrWhiteSpace(pf.Apellidos) ? (pf.Nombre ?? "Usuario")
                                : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(pf.Avatar) ? $"uploads/avatars/{pf.idUser}/avatar-64.png" : pf.Avatar.Replace("\\", "/");
                            authorMap[pf.idUser] = new AuthorInfo(full, avatar, pf.slug ?? "");
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
                            authorMap[u.Id] = new AuthorInfo(string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, $"uploads/avatars/{u.Id}/avatar-64.png", "");
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
                            var resp = new RespuestaVm
                        {
                            Id = x.Id,
                            Cuerpo = x.Cuerpo,
                            UsuarioId = x.UsuarioId,
                                AutorNombre = autor.Name,
                                AutorAvatarUrl = autor.Avatar,
                                AutorSlug = autor.Slug ?? "",
                            FechaCreacion = x.FechaCreacion,
                            Score = x.Score,
                            UsuarioVoto = votosUsuarioTop.TryGetValue(x.Id, out var vv) ? vv : 0,
                            EsMia = currentUserId.HasValue && x.UsuarioId == currentUserId.Value,
                            EsIA = x.EsIA,
                            ModeloIA = x.ModeloIA
                            };
                            // author slug stored in resp.AutorSlug above
                            return resp;
                    }).ToList();

                    if (mappedTop.Count > 0)
                    {
                        AcceptedAnswer = mappedTop.FirstOrDefault(x => !x.EsIA && topAnswers.First(t => t.Id == x.Id).EsAceptada);
                        TopSuggestedAnswers = mappedTop.Where(x => !x.EsIA && (AcceptedAnswer == null || x.Id != AcceptedAnswer.Id)).Take(5).ToList();
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
                    r.EsIA,
                    r.ModeloIA,
                    r.EsAceptada,
                    Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado)
                                     .Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .OrderByDescending(x => x.EsAceptada)  // Accepted first
                .ThenBy(x => x.EsIA)                    // Human before AI
                .ThenByDescending(x => x.Score)         // By score
                .ThenBy(x => x.FechaCreacion)           // Older first
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
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar, p.slug })
                        .ToListAsync();

                    foreach (var pf in perfiles)
                    {
                        var full = string.IsNullOrWhiteSpace(pf.Apellidos)
                            ? (pf.Nombre ?? "Usuario")
                            : $"{(pf.Nombre ?? "Usuario")} {pf.Apellidos}";
                        var avatar = string.IsNullOrWhiteSpace(pf.Avatar)
                            ? $"uploads/avatars/{pf.idUser}/avatar-64.png"
                            : pf.Avatar.Replace("\\", "/");
                        ansAuthors[pf.idUser] = new AuthorInfo(full, avatar, pf.slug ?? "");
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
                                ansAuthors[u.Id] = new AuthorInfo(string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, $"uploads/avatars/{u.Id}/avatar-64.png", "");
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
                    : new AuthorInfo("Usuario", $"uploads/avatars/{a.UsuarioId}/avatar-64.png", "");

                var resp = new RespuestaVm
                {
                    Id = a.Id,
                    Cuerpo = a.Cuerpo,
                    UsuarioId = a.UsuarioId,
                    AutorNombre = autorInfo.Name,
                    AutorAvatarUrl = autorInfo.Avatar,
                    AutorSlug = autorInfo.Slug ?? "",
                    FechaCreacion = a.FechaCreacion,
                    Score = a.Score,
                    UsuarioVoto = votosUsuarioAns.TryGetValue(a.Id, out var vv) ? vv : 0,
                    EsMia = currentUserId.HasValue && a.UsuarioId == currentUserId.Value,
                    EsIA = a.EsIA,
                    ModeloIA = a.ModeloIA
                };
                return resp;
            }).ToList();

            // ===== NUEVO: Cargar respuesta de IA y verificar estado =====
            try
            {
                // Verificar si la pregunta tiene respuesta de IA
                var preguntaConIA = await _db.Preguntas.AsNoTracking()
                    .Where(p => p.Id == preguntaId)
                    .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
                    .FirstOrDefaultAsync();

                if (preguntaConIA != null)
                {
                    if (preguntaConIA.TieneRespuestaIA)
                    {
                        // Ya tiene respuesta, cargarla
                        var respIA = await _db.Respuestas.AsNoTracking()
                            .Where(r => r.PreguntaId == preguntaId && !r.Eliminado && r.EsIA)
                            .OrderByDescending(r => r.FechaCreacion)
                            .Select(r => new
                            {
                                r.Id,
                                r.Cuerpo,
                                r.UsuarioId,
                                r.FechaCreacion,
                                r.EsIA,
                                r.ModeloIA,
                                Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado)
                                    .Select(v => (int?)v.Valor).Sum() ?? 0
                            })
                            .FirstOrDefaultAsync();

                        if (respIA != null)
                        {
                            var autorInfo = ansAuthors.TryGetValue(respIA.UsuarioId, out var ai)
                                ? ai
                                : new AuthorInfo("IA - Claude", "/img/ai-avatar.png", "");

                            RespuestaIA = new RespuestaVm
                            {
                                Id = respIA.Id,
                                Cuerpo = respIA.Cuerpo,
                                UsuarioId = respIA.UsuarioId,
                                AutorNombre = autorInfo.Name,
                                AutorAvatarUrl = autorInfo.Avatar,
                                AutorSlug = autorInfo.Slug ?? "",
                                FechaCreacion = respIA.FechaCreacion,
                                Score = respIA.Score,
                                UsuarioVoto = currentUserId.HasValue && votosUsuarioAns.TryGetValue(respIA.Id, out var vv) ? vv : 0,
                                EsMia = false,
                                EsIA = respIA.EsIA,
                                ModeloIA = respIA.ModeloIA
                            };
                        }
                    }
                    else
                    {
                        // No tiene respuesta, verificar si está pendiente (pregunta reciente sin respuesta)
                        var minutosDesdeCreacion = (DateTimeOffset.UtcNow - preguntaFechaCreacion).TotalMinutes;
                        TienePreguntaPendienteIA = minutosDesdeCreacion < 5; // Mostrar "Procesando..." si tiene menos de 5 minutos
                    }
                }

                // Contar respuestas humanas (sin IA)
                TotalRespuestasHumanas = await _db.Respuestas.AsNoTracking()
                    .Where(r => r.PreguntaId == preguntaId && !r.Eliminado && !r.EsIA)
                    .CountAsync();

                _logger.LogInformation(
                    "📊 Pregunta {PreguntaId}: TieneIA={TieneIA}, PendienteIA={PendienteIA}, RespuestasHumanas={Humanas}",
                    preguntaId, RespuestaIA != null, TienePreguntaPendienteIA, TotalRespuestasHumanas);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error verificando estado de respuesta IA");
            }
            // =========================================================

            // ===== Batch: detectar médicos verificados entre respondedores (1 query, no N+1) =====
            // Patrón replicado de GlossaryService.FilterCommentsByVerifiedDoctorAsync
            try
            {
                var nonIaUserIds = Respuestas
                    .Where(r => !r.EsIA).Select(r => r.UsuarioId)
                    .Concat(AcceptedAnswer != null && !AcceptedAnswer.EsIA
                        ? new[] { AcceptedAnswer.UsuarioId }
                        : Array.Empty<Guid>())
                    .Concat(TopSuggestedAnswers.Where(r => !r.EsIA).Select(r => r.UsuarioId))
                    .Distinct().ToList();

                if (nonIaUserIds.Any())
                {
                    var perfilesMedico = await _db.MedicosPerfilExtendido
                        .AsNoTracking()
                        .Where(p => p.UserId.HasValue && nonIaUserIds.Contains(p.UserId.Value) && p.MedicoId != null)
                        .Select(p => new { p.UserId, p.MedicoId })
                        .ToListAsync();

                    var medicoIds = perfilesMedico.Where(p => p.MedicoId.HasValue).Select(p => p.MedicoId!.Value).ToList();
                    var medicoVerificadoUserIds = new HashSet<Guid>();

                    if (medicoIds.Any())
                    {
                        var idsConBadge = await _db.MedicosPerfilBadge
                            .AsNoTracking()
                            .Where(pb => medicoIds.Contains(pb.MedicoId))
                            .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id,
                                  (pb, b) => new { pb.MedicoId, b.Codigo })
                            .Where(x => x.Codigo == "perfil_reclamado" || x.Codigo == "verificado")
                            .Select(x => x.MedicoId)
                            .Distinct()
                            .ToListAsync();

                        medicoVerificadoUserIds = perfilesMedico
                            .Where(p => p.MedicoId.HasValue && idsConBadge.Contains(p.MedicoId!.Value))
                            .Select(p => p.UserId!.Value)
                            .ToHashSet();
                    }

                    if (medicoVerificadoUserIds.Any())
                    {
                        foreach (var r in Respuestas.Where(r => !r.EsIA && medicoVerificadoUserIds.Contains(r.UsuarioId)))
                            r.EsProfesionalSalud = true;
                        if (AcceptedAnswer != null && !AcceptedAnswer.EsIA && medicoVerificadoUserIds.Contains(AcceptedAnswer.UsuarioId))
                            AcceptedAnswer.EsProfesionalSalud = true;
                        foreach (var r in TopSuggestedAnswers.Where(r => !r.EsIA && medicoVerificadoUserIds.Contains(r.UsuarioId)))
                            r.EsProfesionalSalud = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detectando médicos verificados para respuestas de pregunta {PreguntaId}", preguntaId);
            }
            // =====================================================================

            // ===== Gate validación de respuestas (Médico Nivel ≥ 4 o Admin) =====
            if (currentUserId.HasValue)
            {
                var isAdmin  = User.IsInRole("Administrador");
                var isMedico = User.IsInRole("Medico");
                if (isAdmin)
                {
                    CanValidateRespuestas = true;
                }
                else if (isMedico)
                {
                    try
                    {
                        var perfilMedico = await _db.MedicosPerfilExtendido.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == currentUserId.Value && p.MedicoId != null);
                        if (perfilMedico?.MedicoId != null)
                        {
                            var nivel = await _badgeService.GetNivelActualAsync(perfilMedico.MedicoId.Value);
                            CanValidateRespuestas = nivel >= 4;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error obteniendo nivel médico para gate de validación respuestas");
                    }
                }
            }

            // Batch: ¿cuáles respuestas de esta página ya validó el médico actual?
            if (CanValidateRespuestas && currentUserId.HasValue)
            {
                try
                {
                    var userIdStr = currentUserId.Value.ToString();
                    var allIds = Respuestas.Select(r => r.Id)
                        .Concat(AcceptedAnswer != null ? new[] { AcceptedAnswer.Id } : Array.Empty<Guid>())
                        .Concat(TopSuggestedAnswers.Select(r => r.Id))
                        .Concat(RespuestaIA != null ? new[] { RespuestaIA.Id } : Array.Empty<Guid>())
                        .Distinct().ToList();

                    if (allIds.Any())
                    {
                        var yaValidadas = (await _db.ValidacionesRespuestaProfesional
                            .AsNoTracking()
                            .Where(v => allIds.Contains(v.RespuestaId) && v.UsuarioMedicoId == userIdStr)
                            .Select(v => v.RespuestaId)
                            .ToListAsync()).ToHashSet();

                        foreach (var r in Respuestas.Where(r => yaValidadas.Contains(r.Id)))
                            r.YaValidada = true;
                        if (AcceptedAnswer != null && yaValidadas.Contains(AcceptedAnswer.Id))
                            AcceptedAnswer.YaValidada = true;
                        foreach (var r in TopSuggestedAnswers.Where(r => yaValidadas.Contains(r.Id)))
                            r.YaValidada = true;
                        if (RespuestaIA != null && yaValidadas.Contains(RespuestaIA.Id))
                            RespuestaIA.YaValidada = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cargando estado de validaciones para pregunta {PreguntaId}", preguntaId);
                }
            }
            // =====================================================================

            // ===== NUEVO: Cargar contenido relacionado =====
            try
            {
                _logger.LogInformation("🔍 [Related] Cargando contenido relacionado para pregunta: {Titulo}", preguntaTitulo);

                // Construir query de búsqueda (título + primeras 200 chars del cuerpo)
                var searchQuery = preguntaTitulo;
                if (!string.IsNullOrWhiteSpace(preguntaCuerpo))
                {
                    var cuerpoCorto = preguntaCuerpo.Length > 200 
                        ? preguntaCuerpo.Substring(0, 200) 
                        : preguntaCuerpo;
                    searchQuery += " " + cuerpoCorto;
                }

                // Obtener condición principal (si existe)
                int? condicionId = null;
                try
                {
                    var primeraCondicion = await _db.PreguntaCondiciones.AsNoTracking()
                        .Where(pc => pc.PreguntaId == preguntaId)
                        .Select(pc => pc.CondicionId)
                        .FirstOrDefaultAsync();

                    if (primeraCondicion > 0)
                    {
                        condicionId = primeraCondicion;
                    }
                }
                catch
                {
                    // Ignorar errores al obtener condición
                }

                // Llamar al servicio de sugerencias
                var suggestions = await _suggestionService.GetSuggestionsAsync(
                    searchQuery, 
                    condicionId,
                    CancellationToken.None);

                // Mapear resultados a DTOs simples
                PreguntasRelacionadas = suggestions.Preguntas.Select(p => new SuggestionDto
                {
                    Titulo = p.Titulo,
                    Url = !string.IsNullOrWhiteSpace(p.Slug) ? $"/Preguntas/{p.Slug}" : $"/Preguntas/Detalles?id={p.Id}",
                    Subtitulo = $"{p.RespuestasCount} respuestas"
                }).ToList();

                ArticulosRelacionados = suggestions.Articulos.Select(a => new SuggestionDto
                {
                    Titulo = a.Titulo,
                    // ⭐ MEJORADO: Usar categoriaSlug/slug si existe, sino fallback
                    Url = !string.IsNullOrWhiteSpace(a.Slug)
                        ? (!string.IsNullOrWhiteSpace(a.CategoriaSlug)
                            ? $"/{a.CategoriaSlug}/{a.Slug}"  // familia-y-relaciones/embarazo-y-parto
                            : $"/c/{a.Slug}")                  // /c/embarazo-y-parto
                        : $"/Contenidos/Detalle?id={a.Id}",    // Fallback con ID
                    Subtitulo = !string.IsNullOrWhiteSpace(a.Resumen) && a.Resumen.Length > 100 
                        ? a.Resumen.Substring(0, 100) + "..." 
                        : a.Resumen
                }).ToList();

                RespuestasRelacionadas = suggestions.Respuestas.Select(r => new SuggestionDto
                {
                    Titulo = r.PreguntaTitulo ?? "Ver respuesta",
                    Url = !string.IsNullOrWhiteSpace(r.PreguntaSlug) ? $"/Preguntas/{r.PreguntaSlug}" : $"/Preguntas/Detalles?id={r.PreguntaId}",
                    Subtitulo = $"+{r.Puntuacion} puntos"
                }).ToList();

                _logger.LogInformation(
                    "✅ [Related] Cargado: {Preguntas} preguntas, {Articulos} artículos, {Respuestas} respuestas",
                    PreguntasRelacionadas.Count, ArticulosRelacionados.Count, RespuestasRelacionadas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ [Related] Error cargando contenido relacionado (continuando sin sidebar)");
                // Inicializar listas vacías para que la vista no falle
                PreguntasRelacionadas = new List<SuggestionDto>();
                ArticulosRelacionados = new List<SuggestionDto>();
                RespuestasRelacionadas = new List<SuggestionDto>();
            }
            // ==============================================

            return Page();
        }

        public async Task<IActionResult> OnPostValidarRespuestaAsync(
            Guid respuestaId, string? slug, string? comentario)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Challenge();

            var currentUserId = GetUserIdGuid();
            if (!currentUserId.HasValue) return Forbid();

            var isAdmin  = User.IsInRole("Administrador");
            var isMedico = User.IsInRole("Medico");

            if (!isAdmin && !isMedico)
            {
                TempData["ValidationError"] = "No tienes permiso para validar respuestas.";
                return RedirectToPage(new { slug });
            }

            // Gate de nivel (Admin siempre puede; Médico requiere Nivel ≥ 4)
            if (isMedico && !isAdmin)
            {
                var perfilMedico = await _db.MedicosPerfilExtendido.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == currentUserId.Value && p.MedicoId != null);
                if (perfilMedico?.MedicoId == null)
                {
                    TempData["ValidationError"] = "No se encontró tu perfil médico.";
                    return RedirectToPage(new { slug });
                }
                var nivel = await _badgeService.GetNivelActualAsync(perfilMedico.MedicoId.Value);
                if (nivel < 4)
                {
                    TempData["ValidationError"] = "Necesitas Nivel 4 para validar respuestas.";
                    return RedirectToPage(new { slug });
                }
            }

            // Bloquear validar respuesta propia
            var respuesta = await _db.Respuestas.AsNoTracking()
                .Where(r => r.Id == respuestaId && !r.Eliminado)
                .Select(r => new { r.Id, r.UsuarioId })
                .FirstOrDefaultAsync();

            if (respuesta == null)
            {
                TempData["ValidationError"] = "Respuesta no encontrada.";
                return RedirectToPage(new { slug });
            }

            if (respuesta.UsuarioId == currentUserId.Value)
            {
                TempData["ValidationError"] = "No puedes validar tu propia respuesta.";
                return RedirectToPage(new { slug });
            }

            var result = await _validacionRespuestaService.GuardarValidacionAsync(
                respuestaId, currentUserId.Value.ToString(), comentario);

            TempData["ValidationMessage"] = result switch
            {
                UpsertResult.Creada      => "✅ Respuesta validada correctamente.",
                UpsertResult.Actualizada => "✅ Validación actualizada.",
                UpsertResult.SinCambios  => "Sin cambios en la validación.",
                _                        => "⚠️ Error al guardar la validación."
            };

            return RedirectToPage(new { slug });
        }

        // ===== NUEVO: DTO para suggestions en vista =====
        public class SuggestionDto
        {
            public string Titulo { get; set; } = "";
            public string Url { get; set; } = "";
            public string Subtitulo { get; set; } = "";
        }
        // ==============================================
    }
}