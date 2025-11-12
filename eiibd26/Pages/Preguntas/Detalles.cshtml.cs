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

namespace eiibd26.Pages.Preguntas
{
    [AllowAnonymous]
    public class DetallesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DetallesModel> _logger;

        public DetallesModel(ApplicationDbContext db, ILogger<DetallesModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class PreguntaVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; }
            public string Cuerpo { get; set; }
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
            public Guid PreguntaId { get; set; }
            public Guid? ParentId { get; set; }
            public Guid UsuarioId { get; set; }
            public bool EsMia { get; set; } = false;
            public string AutorNombre { get; set; } = "Usuario";
            public string AutorAvatarUrl { get; set; } = "/img/avatar-placeholder.png";
            public string Cuerpo { get; set; }
            public DateTimeOffset FechaCreacion { get; set; }
            public int Score { get; set; }
            public int UsuarioVoto { get; set; } = 0;
            public List<RespuestaVm> Children { get; set; } = new List<RespuestaVm>();
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public PreguntaVm Pregunta { get; set; }

        public List<RespuestaVm> Respuestas { get; set; } = new List<RespuestaVm>();

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 6;
        [BindProperty(SupportsGet = true)]
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

        public async Task<IActionResult> OnGetAsync(Guid id, int? page, int? pageSize, string search)
        {
            Id = id;
            if (page.HasValue) Page = Math.Max(1, page.Value);
            if (pageSize.HasValue) PageSize = Math.Max(1, pageSize.Value);
            if (search != null) Search = search.Trim();

            var userId = GetUserIdGuid();

            var q = await _db.Preguntas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Id && !p.Eliminado);
            if (q == null) return NotFound();

            var qScore = await _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == q.Id && !v.Eliminado).Select(v => (int?)v.Valor).SumAsync() ?? 0;
            int qUsuarioVoto = 0;
            if (userId.HasValue)
            {
                var vq = await _db.Votos.AsNoTracking().FirstOrDefaultAsync(v => !v.Eliminado && v.UsuarioId == userId.Value && v.EntidadTipo == "pregunta" && v.EntidadId == q.Id);
                qUsuarioVoto = vq?.Valor ?? 0;
            }

            string autorName = "Usuario";
            string autorAvatar = "/img/avatar-placeholder.png";
            try
            {
                var au = await _db.Set<IdentityUserLite>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == q.UsuarioId);
                if (au != null) { autorName = string.IsNullOrWhiteSpace(au.UserName) ? "Usuario" : au.UserName; autorAvatar = string.IsNullOrWhiteSpace(au.AvatarUrl) ? "/img/avatar-placeholder.png" : au.AvatarUrl; }
            }
            catch { }

            Pregunta = new PreguntaVm
            {
                Id = q.Id,
                Titulo = q.Titulo,
                Cuerpo = q.Cuerpo,
                UsuarioId = q.UsuarioId,
                AutorNombre = autorName,
                AutorAvatarUrl = autorAvatar,
                FechaCreacion = q.FechaCreacion,
                Score = qScore,
                UsuarioVoto = qUsuarioVoto,
                EsMia = userId.HasValue && q.UsuarioId == userId.Value
            };

            string[] parentCandidates = new[] { "ParentRespuestaId", "ParentId", "RespuestaPadreId" };
            string parentPropName = null;
            var entityType = _db.Model.FindEntityType(typeof(Respuesta));
            if (entityType != null)
            {
                foreach (var cand in parentCandidates)
                {
                    if (entityType.FindProperty(cand) != null) { parentPropName = cand; break; }
                }
            }

            var allAnswersQuery = _db.Respuestas.AsNoTracking().Where(r => r.PreguntaId == Id && !r.Eliminado);
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.ToLower();
                allAnswersQuery = allAnswersQuery.Where(r => r.Cuerpo.ToLower().Contains(s));
            }

            var allAnswers = await allAnswersQuery
                .Select(r => new
                {
                    r.Id,
                    r.PreguntaId,
                    r.UsuarioId,
                    r.Cuerpo,
                    r.EsAceptada,
                    r.FechaCreacion,
                    ParentId = parentPropName != null ? EF.Property<Guid?>(r, parentPropName) : (Guid?)null,
                    Score = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .ToListAsync();

            var answerUserIds = allAnswers.Select(a => a.UsuarioId).Distinct().ToArray();
            var answerUsers = new Dictionary<Guid, (string name, string avatar)>();
            if (answerUserIds.Length > 0)
            {
                try
                {
                    var aus = await _db.Set<IdentityUserLite>().AsNoTracking().Where(u => answerUserIds.Contains(u.Id)).Select(u => new { u.Id, u.UserName, u.AvatarUrl }).ToListAsync();
                    answerUsers = aus.ToDictionary(x => x.Id, x => (x.UserName ?? "Usuario", string.IsNullOrWhiteSpace(x.AvatarUrl) ? "/img/avatar-placeholder.png" : x.AvatarUrl));
                }
                catch { answerUsers = new Dictionary<Guid, (string, string)>(); }
            }

            var answerIds = allAnswers.Select(a => a.Id).ToArray();
            var votosUsuario = new Dictionary<Guid, int>();
            if (userId.HasValue && answerIds.Length > 0)
            {
                var votos = await _db.Votos.AsNoTracking().Where(v => !v.Eliminado && v.UsuarioId == userId.Value && v.EntidadTipo == "respuesta" && answerIds.Contains(v.EntidadId))
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Valor = g.Select(x => (int?)x.Valor).FirstOrDefault() })
                    .ToListAsync();
                votosUsuario = votos.ToDictionary(x => x.Id, x => x.Valor ?? 0);
            }

            var map = allAnswers.ToDictionary(
                a => a.Id,
                a => new RespuestaVm
                {
                    Id = a.Id,
                    PreguntaId = a.PreguntaId,
                    ParentId = a.ParentId,
                    UsuarioId = a.UsuarioId,
                    EsMia = userId.HasValue && a.UsuarioId == userId.Value,
                    AutorNombre = answerUsers.ContainsKey(a.UsuarioId) ? answerUsers[a.UsuarioId].name : "Usuario",
                    AutorAvatarUrl = answerUsers.ContainsKey(a.UsuarioId) ? answerUsers[a.UsuarioId].avatar : "/img/avatar-placeholder.png",
                    Cuerpo = a.Cuerpo,
                    FechaCreacion = a.FechaCreacion,
                    Score = a.Score,
                    UsuarioVoto = votosUsuario.TryGetValue(a.Id, out var vv) ? vv : 0,
                    Children = new List<RespuestaVm>()
                });

            foreach (var vm in map.Values)
            {
                if (vm.ParentId.HasValue && map.ContainsKey(vm.ParentId.Value))
                {
                    map[vm.ParentId.Value].Children.Add(vm);
                }
            }

            var topLevelAll = map.Values.Where(r => !r.ParentId.HasValue).OrderByDescending(r => r.FechaCreacion).ToList();
            TotalItems = topLevelAll.Count;

            var pagedTop = topLevelAll.Skip((Page - 1) * PageSize).Take(PageSize).ToList();

            Respuestas = pagedTop;

            return Page();
        }

        private class IdentityUserLite
        {
            public Guid Id { get; set; }
            public string UserName { get; set; }
            public string AvatarUrl { get; set; }
        }
    }
}