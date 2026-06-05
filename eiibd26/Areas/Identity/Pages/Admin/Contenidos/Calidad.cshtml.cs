using eiibd26.Services.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class CalidadModel : PageModel
    {
        private readonly IContenidoCalidadService _calidad;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public CalidadModel(IContenidoCalidadService calidad)
            => _calidad = calidad;

        public void OnGet() { }

        /// <summary>
        /// Handler para análisis por batch. Devuelve JSON:
        /// { total: N, items: [ ContenidoCalidadDto... ] }
        /// Los enums se serializan como strings ("Critico", "Mejorable", "Ok").
        /// </summary>
        public async Task<IActionResult> OnPostAnalizarBatchAsync(
            [FromForm] int skip,
            [FromForm] int take = 10)
        {
            if (take <= 0 || take > 50) take = 10;
            if (skip < 0) skip = 0;

            var resultado = await _calidad.AnalizarBatchAsync(skip, take);
            return new JsonResult(resultado, JsonOpts);
        }
    }
}
