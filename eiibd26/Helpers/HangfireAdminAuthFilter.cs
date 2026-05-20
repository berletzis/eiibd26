using Hangfire.Dashboard;

namespace eiibd26.Helpers
{
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            return http.User.Identity?.IsAuthenticated == true &&
                   http.User.IsInRole("Administrador");
        }
    }
}
