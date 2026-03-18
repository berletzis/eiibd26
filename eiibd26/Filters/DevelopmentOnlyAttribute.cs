using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eiibd26.Filters
{
    /// <summary>
    /// Restricts an endpoint to the Development environment only.
    /// Returns HTTP 404 for all other environments so diagnostic routes
    /// are inaccessible in production without removing the controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class DevelopmentOnlyAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var env = context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment())
                context.Result = new NotFoundResult();
        }

        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }
}
