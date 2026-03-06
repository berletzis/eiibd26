using eiibd26.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/search")]
    [AllowAnonymous]
    public class SearchApiController : ControllerBase
    {
        private readonly SearchSuggestionService _searchService;
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(
            SearchSuggestionService searchService,
            ILogger<SearchApiController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/search/suggestions?q=texto&condicionId=1
        /// Busca contenido relacionado (preguntas, artículos, respuestas) para mostrar como sugerencias
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions(
            [FromQuery] string q,
            [FromQuery] int? condicionId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new
                {
                    ok = true,
                    preguntas = new List<object>(),
                    articulos = new List<object>(),
                    respuestas = new List<object>()
                });
            }

            if (q.Length < 20)
            {
                return Ok(new
                {
                    ok = true,
                    message = "Mínimo 20 caracteres",
                    preguntas = new List<object>(),
                    articulos = new List<object>(),
                    respuestas = new List<object>()
                });
            }

            try
            {
                _logger.LogInformation(
                    "🔍 [Search API] Búsqueda: '{Query}', CondicionId: {CondicionId}",
                    q, condicionId);

                var result = await _searchService.GetSuggestionsAsync(q, condicionId, cancellationToken);

                return Ok(new
                {
                    ok = true,
                    preguntas = result.Preguntas.Select(p => new
                    {
                        id = p.Id,
                        titulo = p.Titulo,
                        slug = p.Slug,
                        respuestasCount = p.RespuestasCount,
                        fechaCreacion = p.FechaCreacion,
                        url = !string.IsNullOrWhiteSpace(p.Slug)
                            ? $"/Preguntas/{p.Slug}"
                            : $"/Preguntas/Detalles?id={p.Id}"
                    }),
                    articulos = result.Articulos.Select(a => new
                    {
                        id = a.Id,
                        titulo = a.Titulo,
                        slug = a.Slug,
                        resumen = a.Resumen,
                        imagenUrl = a.ImagenUrl,
                        // ⭐ MEJORADO: Usar categoriaSlug/slug si existe, sino fallback
                        url = !string.IsNullOrWhiteSpace(a.Slug)
                            ? (!string.IsNullOrWhiteSpace(a.CategoriaSlug)
                                ? $"/{a.CategoriaSlug}/{a.Slug}"  // familia-y-relaciones/embarazo-y-parto
                                : $"/c/{a.Slug}")                  // /c/embarazo-y-parto
                            : $"/Contenidos/Detalle?id={a.Id}"     // Fallback con ID
                    }),
                    respuestas = result.Respuestas.Select(r => new
                    {
                        id = r.Id,
                        preguntaId = r.PreguntaId,
                        preguntaTitulo = r.PreguntaTitulo,
                        cuerpoPreview = GetPreview(r.Cuerpo, 150),
                        puntuacion = r.Puntuacion,
                        url = !string.IsNullOrWhiteSpace(r.PreguntaSlug)
                            ? $"/Preguntas/{r.PreguntaSlug}#respuesta-{r.Id}"
                            : $"/Preguntas/Detalles?id={r.PreguntaId}#respuesta-{r.Id}"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Search API] Error en búsqueda");
                return StatusCode(500, new { ok = false, error = "Error en búsqueda" });
            }
        }

        private string GetPreview(string html, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            // Remover tags HTML
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength).TrimEnd() + "...";
        }
    }
}
