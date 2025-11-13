using eiibd26.Data;
using eiibd26.Models;
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

        public class PreguntaVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public string Cuerpo { get; set; } = "";
            public Guid UsuarioId { get; set; }
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public DateTimeOffset FechaCreacion { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public bool EsMia { get; set; } = false;
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
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 6;

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        public PreguntaVm Pregunta { get; set; } = new PreguntaVm();
        public List<RespuestaVm> Respuestas { get; set; } = new List<RespuestaVm>();

        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            if (Guid.TryParse(v, out var g)) return g;
            return null;
        }

        public async Task<IActionResult> OnGetAsync(Guid id, int? page, int? pageSize, string search)
        {
            Id = id;
            if (page.HasValue) Page = Math.Max(1, page.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);
            if (search != null) Search = search.Trim();

            var userId = GetUserIdGuid();

            var pregunta = await _db.Preguntas.AsNoTracking()
                .Where(p => p.Id == id && !p.Eliminado)
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Cuerpo,
                    p.UsuarioId,
                    p.FechaCreacion,
                    Score = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .FirstOrDefaultAsync();

            if (pregunta == null) return NotFound();

            // load author info from Perfil
            try
            {
                var perfil = await _db.Set<Perfil>().AsNoTracking()
                    .Where(p => p.idUser == pregunta.UsuarioId && (p.Eliminado == null || p.Eliminado == false))
                    .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                    .FirstOrDefaultAsync();

                Pregunta = new PreguntaVm
                {
                    Id = pregunta.Id,
                    Titulo = pregunta.Titulo,
                    Cuerpo = pregunta.Cuerpo,
                    UsuarioId = pregunta.UsuarioId,
                    AutorNombre = perfil != null ? (string.IsNullOrWhiteSpace(perfil.Apellidos) ? (perfil.Nombre ?? "Usuario") : $"{(perfil.Nombre ?? "Usuario")} {perfil.Apellidos}") : "Usuario",
                    AutorAvatarUrl = perfil != null ? (string.IsNullOrWhiteSpace(perfil.Avatar) ? "/img/avatar-placeholder.png" : perfil.Avatar) : "/img/avatar-placeholder.png",
                    FechaCreacion = pregunta.FechaCreacion,
                    Score = pregunta.Score,
                    UsuarioVoto = 0,
                    EsMia = userId.HasValue && pregunta.UsuarioId == userId.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener perfil de la pregunta; usando valores por defecto.");
                Pregunta = new PreguntaVm
                {
                    Id = pregunta.Id,
                    Titulo = pregunta.Titulo,
                    Cuerpo = pregunta.Cuerpo,
                    UsuarioId = pregunta.UsuarioId,
                    AutorNombre = "Usuario",
                    AutorAvatarUrl = "/img/avatar-placeholder.png",
                    FechaCreacion = pregunta.FechaCreacion,
                    Score = pregunta.Score,
                    UsuarioVoto = 0,
                    EsMia = userId.HasValue && pregunta.UsuarioId == userId.Value
                };
            }

            // user's vote for the question (active only)
            if (userId.HasValue)
            {
                var votoQ = await _db.Votos.AsNoTracking()
                    .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == id && v.UsuarioId == userId.Value && !v.Eliminado)
                    .OrderByDescending(v => v.FechaCreacion)
                    .FirstOrDefaultAsync();
                if (votoQ != null) Pregunta.UsuarioVoto = votoQ.Valor;
            }

            // load responses (paged + optional search)
            var baseAnsQ = _db.Respuestas.AsNoTracking().Where(r => r.PreguntaId == id && !r.Eliminado);
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.ToLower();
                baseAnsQ = baseAnsQ.Where(r => r.Cuerpo.ToLower().Contains(s));
            }

            TotalItems = await baseAnsQ.CountAsync();

            var ansPageQ = baseAnsQ.OrderByDescending(r => r.FechaCreacion).Skip((Page - 1) * PageSize).Take(PageSize);

            var ansItems = await ansPageQ.Select(r => new
            {
                r.Id,
                r.Cuerpo,
                r.UsuarioId,
                r.FechaCreacion,
                Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0
            }).ToListAsync();

            var ansUserIds = ansItems.Select(a => a.UsuarioId).Distinct().ToArray();

            // load answer authors from Perfil
            Dictionary<Guid, (string name, string avatar)> ansAuthors = new Dictionary<Guid, (string, string)>();
            if (ansUserIds.Length > 0)
            {
                try
                {
                    var perfiles = await _db.Set<Perfil>().AsNoTracking()
                        .Where(p => ansUserIds.Contains(p.idUser) && (p.Eliminado == null || p.Eliminado == false))
                        .Select(p => new { p.idUser, p.Nombre, p.Apellidos, p.Avatar })
                        .ToListAsync();

                    ansAuthors = perfiles.ToDictionary(
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
                    _logger.LogWarning(ex, "No se pudo obtener perfiles de autores de respuestas; usando por defecto.");
                    ansAuthors = new Dictionary<Guid, (string, string)>();
                }
            }

            // user's votes on answers (active)
            Dictionary<Guid, int> votosUsuarioAns = new Dictionary<Guid, int>();
            if (userId.HasValue && ansItems.Count > 0)
            {
                var ansIds = ansItems.Select(a => a.Id).ToArray();
                var votos = await _db.Votos.AsNoTracking()
                    .Where(v => v.UsuarioId == userId.Value && v.EntidadTipo == "respuesta" && ansIds.Contains(v.EntidadId) && !v.Eliminado)
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                    .ToListAsync();

                votosUsuarioAns = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
            }

            Respuestas = ansItems.Select(a => new RespuestaVm
            {
                Id = a.Id,
                Cuerpo = a.Cuerpo,
                UsuarioId = a.UsuarioId,
                AutorNombre = ansAuthors.ContainsKey(a.UsuarioId) ? ansAuthors[a.UsuarioId].name : "Usuario",
                AutorAvatarUrl = ansAuthors.ContainsKey(a.UsuarioId) ? ansAuthors[a.UsuarioId].avatar : "/img/avatar-placeholder.png",
                FechaCreacion = a.FechaCreacion,
                Score = a.Score,
                UsuarioVoto = votosUsuarioAns.TryGetValue(a.Id, out var vv) ? vv : 0
            }).ToList();

            return Page();
        }
    }
}