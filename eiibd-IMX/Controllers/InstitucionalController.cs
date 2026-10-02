using eiibd_IMX.Models.Configuracion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eiibd_IMX.Controllers;

/// Las tres paginas institucionales enlazadas desde el footer —o sea, desde
/// TODAS las paginas del sitio—. Hasta ahora daban 404, y "sin enlaces rotos"
/// es punto explicito de la Website Policy de Google Ad Grants: el sitio se
/// revisa entero al aprobar la cuenta.
///
/// Sus rutas se registran con segmento literal en Program.cs y van ANTES de los
/// catch-all de categoria/articulo: si se registraran despues, /quienes-somos
/// entraria como slug de categoria, no existiria en la base y volveria a dar 404.
public sealed class InstitucionalController : Controller
{
    private readonly OrganizacionOptions _org;
    public InstitucionalController(IOptions<OrganizacionOptions> org) => _org = org.Value;

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult QuienesSomos() => View(_org);

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Transparencia() => View(_org);

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Contacto() => View(_org);

    /// Aviso de privacidad (LFPDPPP). Va indexable a proposito: un aviso
    /// accesible y rastreable es parte de lo que un revisor de Ad Grants espera
    /// encontrar, y esconderlo con noindex no le sirve a nadie.
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult AvisoPrivacidad() => View(_org);
}
