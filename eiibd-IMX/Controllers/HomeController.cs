using eiibd_IMX.Models.Configuracion;
using eiibd_IMX.Models.ViewModels;
using eiibd_IMX.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eiibd_IMX.Controllers;

public sealed class HomeController : Controller
{
    private readonly IContenidoService _contenido;
    private readonly IConfiguration _cfg;
    private readonly OrganizacionOptions _org;

    public HomeController(IContenidoService contenido, IConfiguration cfg,
                          IOptions<OrganizacionOptions> org)
    {
        _contenido = contenido;
        _cfg = cfg;
        _org = org.Value;
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new HomeVm();

        // Las filas se declaran en appsettings: agregar una tercera categoria
        // no requiere tocar codigo.
        var filas = _cfg.GetSection("Filas").Get<List<FilaConfig>>() ?? new();

        foreach (var f in filas)
        {
            var fila = await _contenido.ObtenerFilaAsync(
                f.CategoriaSlug, f.Titulo, f.Descripcion, f.Ancla, f.Top, ct);
            if (fila is not null) vm.Filas.Add(fila);
        }

        vm.Programas = _cfg.GetSection("Programas").Get<List<ProgramaVm>>() ?? new();
        vm.Proyecto  = _cfg.GetSection("Proyecto").Get<ProyectoVm>() ?? new();
        vm.Organizacion = _org;

        return View(vm);
    }

    public IActionResult Error(int? code)
    {
        Response.StatusCode = code is >= 400 and < 600 ? code.Value : 500;
        ViewData["Code"] = Response.StatusCode;
        return View();
    }

    public sealed class FilaConfig
    {
        public string CategoriaSlug { get; set; } = "";
        public string Ancla { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public int Top { get; set; } = 10;
    }
}
