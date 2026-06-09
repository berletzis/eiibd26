using eiibd26.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Dashboard
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly IAdminDashboardService _dashboardService;
        public IndexModel(IAdminDashboardService dashboardService)
            => _dashboardService = dashboardService;

        public AdminDashboardDto Dto { get; set; } = new();

        public async Task OnGetAsync()
        {
            Dto = await _dashboardService.GetDashboardAsync();
        }
    }
}
