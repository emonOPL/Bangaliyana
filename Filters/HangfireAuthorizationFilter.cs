using Hangfire.Dashboard;

namespace Bangaliyana.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Only allow authenticated users
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Only allow SuperAdmin and Admin roles
            return httpContext.User.IsInRole("SuperAdmin") || httpContext.User.IsInRole("Admin");
        }
    }
}
