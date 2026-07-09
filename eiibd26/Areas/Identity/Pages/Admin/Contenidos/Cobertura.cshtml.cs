using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Services.Cobertura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    /// <summary>
    /// Motor de Cobertura — Fase 4. Grid admin del estado de cobertura de temas externos:
    /// para cada página externa escaneada, el mejor artículo propio similar y su %.
    /// Los huecos (baja/nula similitud) son la oportunidad de contenido nuevo.
    /// Filtra el lado propio a ARTÍCULOS reales (árbol General); páginas de sistema nunca aparecen.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class CoberturaModel : PageModel
    {
        private readonly ICoberturaVistaService _svc;
        private readonly ILogger<CoberturaModel> _logger;

        public CoberturaModel(ICoberturaVistaService svc, ILogger<CoberturaModel> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
        public string? Orden { get; set; }

        public bool TablaDisponible { get; private set; } = true;
        public string? Aviso { get; private set; }

        public IReadOnlyList<CoberturaTemaDto> Temas { get; private set; } = new List<CoberturaTemaDto>();
        public int Total => Temas.Count;
        public int Cubiertos { get; private set; }
        public int Debiles { get; private set; }
        public int Huecos { get; private set; }

        public async Task OnGetAsync()
        {
            try
            {
                Temas = await _svc.ObtenerCoberturaTemasAsync(Orden);
                Cubiertos = Temas.Count(t => t.Estado == "Cubierto");
                Debiles = Temas.Count(t => t.Estado == "Débil");
                Huecos = Temas.Count(t => t.Estado == "Hueco");
            }
            catch (Exception ex)
            {
                TablaDisponible = false;
                Aviso = "No se pudo leer la cobertura. ¿Existe la tabla CoberturaSimilitud y hay similitud calculada? Detalle: " + ex.Message;
                _logger.LogWarning(ex, "[Cobertura] No se pudo leer el estado de cobertura.");
            }
        }
    }
}
