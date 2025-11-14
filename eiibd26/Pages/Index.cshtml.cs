using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Block 1
        public int RegisteredUsersCount { get; set; }            // users with EmailConfirmed == true
        public int UsersWithConditionCount { get; set; }         // users with an active condicionUsuario (Eliminado == false)

        // Mood counts (MuyBien, Bien, Neutral, Mal, MuyMal)
        public Dictionary<string, int> MoodCounts { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Block 2 - Top 5 questions
        public class TopQuestionVm
        {
            public Guid Id { get; set; }
            public string Titulo { get; set; } = "";
            public int VotesSum { get; set; }
            public int RespuestasCount { get; set; }
            public int Score { get; set; }
        }

        public List<TopQuestionVm> TopQuestions { get; set; } = new List<TopQuestionVm>();

        public async Task OnGetAsync()
        {
            // 1) Registered users: count ApplicationUser where EmailConfirmed == true
            // Use _db.Users (ApplicationUser) which is part of the model.
            RegisteredUsersCount = await _db.Users
                                            .AsNoTracking()
                                            .CountAsync(u => u.EmailConfirmed)
                                            .ConfigureAwait(false);

            // 2) Users with a condition: count distinct users that have condicionUsuario entries not deleted
            UsersWithConditionCount = await _db.condicionUsuario
                                              .AsNoTracking()
                                              .Where(cu => !cu.Eliminado)
                                              .Select(cu => cu.idUsuario)
                                              .Distinct()
                                              .CountAsync()
                                              .ConfigureAwait(false);

            // 3) Mood counts - count EstadoAnimoUsuario grouped by EstadoMood
            var moodGroups = await _db.EstadoAnimoUsuario
                                     .AsNoTracking()
                                     .GroupBy(e => e.EstadoMood)
                                     .Select(g => new { Mood = g.Key, Count = g.Count() })
                                     .ToListAsync()
                                     .ConfigureAwait(false);

            // ensure keys for all 5 moods exist with 0 if no records
            var moodKeys = new[] { "MuyBien", "Bien", "Neutral", "Mal", "MuyMal" };
            MoodCounts = moodKeys.ToDictionary(k => k, k =>
            {
                var found = moodGroups.FirstOrDefault(x => string.Equals(x.Mood, k, StringComparison.OrdinalIgnoreCase));
                return found?.Count ?? 0;
            }, StringComparer.OrdinalIgnoreCase);

            // 4) Top 5 questions: score = sum(votos.valor) + 2 * respuestasCount
            // Compute votes sum per pregunta and responses count per pregunta.
            var preguntaStats = await _db.Preguntas
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    VotesSum = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id).Select(v => (int?)v.Valor).Sum() ?? 0,
                    RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id)
                })
                .ToListAsync()
                .ConfigureAwait(false);

            TopQuestions = preguntaStats
                .Select(x => new TopQuestionVm
                {
                    Id = x.Id,
                    Titulo = x.Titulo ?? "",
                    VotesSum = x.VotesSum,
                    RespuestasCount = x.RespuestasCount,
                    Score = x.VotesSum + 2 * x.RespuestasCount
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.VotesSum)
                .ThenByDescending(x => x.RespuestasCount)
                .Take(5)
                .ToList();
        }
    }
}