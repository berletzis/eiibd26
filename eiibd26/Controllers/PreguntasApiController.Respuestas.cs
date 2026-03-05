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
                    .ThenInclude(r => r.Pregunta)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);

            if (pregunta == null)
                return NotFound(new { ok = false, error = "Pregunta no encontrada" });

            // Ordenar respuestas según prioridad AI
            var respuestas = pregunta.Respuestas
                .Where(r => !r.Eliminado)
                .OrderByDescending(r => r.EsAceptada)     // 1. Respuestas aceptadas primero
                .ThenBy(r => r.EsIA)                       // 2. Humanas antes que IA
                .ThenByDescending(r => r.Puntuacion)      // 3. Por puntuación
                .ThenBy(r => r.FechaCreacion)             // 4. Más antiguas primero
                .Select(r => new
                {
                    r.Id,
                    r.Cuerpo,
                    r.EsAceptada,
                    r.EsIA,
                    r.ModeloIA,
                    r.EsColapsada,
                    r.Puntuacion,
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
