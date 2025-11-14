using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eiibd26.Pages
{
    [AllowAnonymous]
    public class PreguntasModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PreguntasModel> _logger;

        public PreguntasModel(ApplicationDbContext db, ILogger<PreguntasModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class PreguntaCardVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string CuerpoPreview { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int RespuestasCount { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public bool EsMia { get; set; } = false;
            public List<string> RespondersAvatars { get; set; } = new List<string>();
        }

        public List<PreguntaCardVm> Preguntas { get; set; } = new List<PreguntaCardVm>();

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 12;

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            if (Guid.TryParse(v, out var g)) return g;
            return null;
        }

        public async Task OnGetAsync(int? page, int? pageSize, string search)
        {
            _logger.LogInformation("Preguntas.OnGetAsync called: page={page}, pageSize={pageSize}, search={search}, user={user}", page, pageSize, search, User?.Identity?.Name ?? "(anon)");

            if (page.HasValue) Page = Math.Max(1, page.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);
            if (search != null) Search = search.Trim();

            var userId = GetUserIdGuid();

            var baseQ = _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.ToLower();
                baseQ = baseQ.Where(p => p.Titulo.ToLower().Contains(s) || p.Cuerpo.ToLower().Contains(s));
            }

            TotalItems = await baseQ.CountAsync();

            var pageQ = baseQ
                .OrderByDescending(p => p.FechaCreacion)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize);

            var items = await pageQ
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Cuerpo,
                    p.UsuarioId,
                    p.FechaCreacion,
                    RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado),
                    Score = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .ToListAsync();

            var preguntaIds = items.Select(i => i.Id).ToArray();
            var userIds = items.Select(i => i.UsuarioId).Distinct().ToArray();

            // Read names and avatars from Perfil table
            var authors = new Dictionary<Guid, (string userName, string avatar)>();
            if (userIds.Length > 0)
            {
                try
                {
                    var perfiles = await _db.Set<Perfil>()
                        .AsNoTracking()
                        .Where(p => userIds.Contains(p.idUser) && (p.Eliminado == null || p.Eliminado == false))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    authors = perfiles.ToDictionary(
                        x => x.idUser,
                        x =>
                        {
                            var full = string.IsNullOrWhiteSpace(x.Apellidos) ? (x.Nombre ?? "Usuario") : $"{(x.Nombre ?? "Usuario")} {x.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(x.Avatar) ? "/img/avatar-placeholder.png" : x.Avatar;
                            return (userName: full, avatar: avatar);
                        });

                    // find missing perfil userIds and fallback to ApplicationUser.UserName
                    var missing = userIds.Except(authors.Keys).ToArray();
                    if (missing.Length > 0)
                    {
                        _logger.LogInformation("Preguntas: perfiles faltantes para userIds: {Missing}", string.Join(", ", missing));
                        var users = await _db.Users.AsNoTracking()
                            .Where(u => missing.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName })
                            .ToListAsync();

                        foreach (var u in users)
                        {
                            if (!authors.ContainsKey(u.Id))
                                authors[u.Id] = (userName: string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, avatar: "/img/avatar-placeholder.png");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No fue posible obtener perfiles; usando valores por defecto.");
                    authors = new Dictionary<Guid, (string, string)>();
                }
            }

            // responders (authors of answers) -> use Perfil too
            var responderRows = new List<(Guid PreguntaId, Guid UsuarioId)>();
            if (preguntaIds.Length > 0 && await _db.Respuestas.AnyAsync())
            {
                var respuestas = await _db.Respuestas
                    .AsNoTracking()
                    .Where(r => preguntaIds.Contains(r.PreguntaId) && !r.Eliminado)
                    .Select(r => new { r.PreguntaId, r.UsuarioId })
                    .ToListAsync();

                responderRows = respuestas.Select(r => (r.PreguntaId, r.UsuarioId)).ToList();
            }

            var responderUserIds = responderRows.Select(r => r.UsuarioId).Distinct().ToArray();
            var responderUsers = new Dictionary<Guid, (string userName, string avatar)>();
            if (responderUserIds.Length > 0)
            {
                try
                {
                    var rperfiles = await _db.Set<Perfil>()
                        .AsNoTracking()
                        .Where(p => responderUserIds.Contains(p.idUser) && (p.Eliminado == null || p.Eliminado == false))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    responderUsers = rperfiles.ToDictionary(
                        x => x.idUser,
                        x =>
                        {
                            var full = string.IsNullOrWhiteSpace(x.Apellidos) ? (x.Nombre ?? "Usuario") : $"{(x.Nombre ?? "Usuario")} {x.Apellidos}";
                            var avatar = string.IsNullOrWhiteSpace(x.Avatar) ? "/img/avatar-placeholder.png" : x.Avatar;
                            return (userName: full, avatar: avatar);
                        });

                    var missingR = responderUserIds.Except(responderUsers.Keys).ToArray();
                    if (missingR.Length > 0)
                    {
                        _logger.LogInformation("Preguntas: responder perfiles faltantes para userIds: {MissingR}", string.Join(", ", missingR));
                        var rusers = await _db.Users.AsNoTracking()
                            .Where(u => missingR.Contains(u.Id))
                            .Select(u => new { u.Id, u.UserName })
                            .ToListAsync();

                        foreach (var u in rusers)
                        {
                            if (!responderUsers.ContainsKey(u.Id))
                                responderUsers[u.Id] = (userName: string.IsNullOrWhiteSpace(u.UserName) ? "Usuario" : u.UserName, avatar: "/img/avatar-placeholder.png");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No fue posible obtener perfiles para responders; using defaults.");
                    responderUsers = new Dictionary<Guid, (string, string)>();
                }
            }

            // user's votes for the page (only active votes)
            Dictionary<Guid, int> votosUsuario = new Dictionary<Guid, int>();
            if (userId.HasValue && preguntaIds.Length > 0 && await _db.Votos.AnyAsync())
            {
                var votos = await _db.Votos
                    .AsNoTracking()
                    .Where(v => v.UsuarioId == userId.Value && v.EntidadTipo == "pregunta" && preguntaIds.Contains(v.EntidadId) && !v.Eliminado)
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                    .ToListAsync();

                votosUsuario = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
            }

            Preguntas = items.Select(i =>
            {
                var vm = new PreguntaCardVm
                {
                    Id = i.Id,
                    Titulo = i.Titulo,
                    CuerpoPreview = (i.Cuerpo?.Length > 400) ? i.Cuerpo.Substring(0, 400) + "…" : (i.Cuerpo ?? ""),
                    UsuarioId = i.UsuarioId,
                    AutorNombre = authors.ContainsKey(i.UsuarioId) ? authors[i.UsuarioId].userName : "Usuario",
                    AutorAvatarUrl = authors.ContainsKey(i.UsuarioId) ? authors[i.UsuarioId].avatar : "/img/avatar-placeholder.png",
                    FechaCreacion = i.FechaCreacion,
                    RespuestasCount = i.RespuestasCount,
                    Score = i.Score,
                    UsuarioVoto = votosUsuario.TryGetValue(i.Id, out var vv) ? vv : 0,
                    EsMia = userId.HasValue && i.UsuarioId == userId.Value
                };

                var respondersForQ = responderRows.Where(r => r.PreguntaId == i.Id).Select(r => r.UsuarioId).Distinct().Take(5).ToList();
                foreach (var rid in respondersForQ)
                {
                    if (responderUsers.TryGetValue(rid, out var ru))
                        vm.RespondersAvatars.Add(ru.avatar);
                    else
                        vm.RespondersAvatars.Add("/img/avatar-placeholder.png");
                }

                return vm;
            }).ToList();
        }

        public async Task<IActionResult> OnPostEliminarPreguntaAsync([FromForm] Guid id)
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue) return Forbid();

            var p = await _db.Preguntas.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            if (p.UsuarioId != userId.Value) return Forbid();

            p.Eliminado = true;
            _db.Preguntas.Update(p);
            await _db.SaveChangesAsync();

            // redirect to same page after deletion
            return RedirectToPage();
        }

    }
}