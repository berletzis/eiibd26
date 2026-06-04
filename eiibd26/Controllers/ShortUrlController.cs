using System.Threading.Tasks;
using eiibd26.Services.ShortUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiibd26.Controllers
{
    [Route("r")]
    [AllowAnonymous]
    public class ShortUrlController : Controller
    {
        private readonly IShortUrlService _service;

        public ShortUrlController(IShortUrlService service) => _service = service;

        [HttpGet("{codigo}")]
        public async Task<IActionResult> Go(string codigo)
        {
            var url = await _service.ResolverYContarAsync(codigo);
            if (url is null) return Redirect("/");
            return Redirect(url);
        }
    }
}
