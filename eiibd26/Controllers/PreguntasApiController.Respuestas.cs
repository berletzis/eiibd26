using eiibd26.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    // Partial class para extensión del controller principal
    public partial class PreguntasApiController
    {
        // GET api/preguntas/{id}/respuestas
        /// <summary>
        /// Obtiene las respuestas de una pregunta ordenadas según prioridad
        /// (Aceptadas primero, luego humanas, luego IA)
        /// </summary>
        [HttpGet("{id:guid}/respuestas")]
        public async Task<IActionResult> ObtenerRespuestas(Guid id)
        {
            var pregunta = await _db.Preguntas
                .Include(p => p.Respuestas)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);

            if (pregunta == null)
                return NotFound(new { ok = false, error = "Pregunta no encontrada" });

            var preguntaId = pregunta.Id;

            // Score calculado en runtime desde Votos (Puntuacion en el modelo es stale)
            var scores = await _db.Votos
                .AsNoTracking()
                .Where(v => v.EntidadTipo == "respuesta" && !v.Eliminado &&
                            pregunta.Respuestas.Select(r => r.Id).Contains(v.EntidadId))
                .GroupBy(v => v.EntidadId)
                .Select(g => new { Id = g.Key, Score = g.Select(v => (int?)v.Valor).Sum() ?? 0 })
                .ToListAsync();

            var scoreMap = scores.ToDictionary(x => x.Id, x => x.Score);

            var respuestas = pregunta.Respuestas
                .Where(r => !r.Eliminado)
                .OrderByDescending(r => r.EsAceptada)
                .ThenBy(r => r.EsIA)
                .ThenByDescending(r => scoreMap.TryGetValue(r.Id, out var s) ? s : 0)
                .ThenBy(r => r.FechaCreacion)
                .Select(r => new
                {
                    r.Id,
                    r.Cuerpo,
                    r.EsAceptada,
                    r.EsIA,
                    r.ModeloIA,
                    r.EsColapsada,
                    Score = scoreMap.TryGetValue(r.Id, out var sc) ? sc : 0,
                    r.FechaCreacion,
                    r.UsuarioId
                })
                .ToList();

            return Ok(new
            {
                ok = true,
                preguntaId = pregunta.Id,
                tieneRespuestaIA = pregunta.TieneRespuestaIA,
                totalRespuestas = respuestas.Count,
                respuestas
            });
        }
    }
}
