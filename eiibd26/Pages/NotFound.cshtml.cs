using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace eiibd26.Pages
{
    public class NotFoundModel : PageModel
    {
        private readonly ILogger<NotFoundModel> _logger;
        public int StatusCode { get; private set; }

        public NotFoundModel(ILogger<NotFoundModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // When re-executed by UseStatusCodePagesWithReExecute, the original status code
            // may be in the request feature, but we can also read RouteData if configured.
            StatusCode = HttpContext.Response.StatusCode;
            if (StatusCode == 200)
            {
                // If re-executed, ASP.NET Core sets response status to 200 for the re-executed page;
                // try to get original status code from the feature:
                var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();
                if (feature != null && feature.OriginalStatusCode != 0)
                {
                    StatusCode = feature.OriginalStatusCode;
                }
                else
                {
                    StatusCode = 404;
                }
            }
            // Log the missing path for diagnostics
            var originalPath = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()?.OriginalPath;
            _logger.LogWarning("NotFound page rendered for path {path} (status {status})", originalPath ?? HttpContext.Request.Path, StatusCode);
        }
    }
}