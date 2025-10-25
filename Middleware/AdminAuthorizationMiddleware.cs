using WaterService.Models;

namespace WaterService.Middleware
{
    public class AdminAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Các route cần quyền admin
            var adminRoutes = new[] { "/customer", "/meterreading", "/invoice" };
            
            if (adminRoutes.Any(route => path?.StartsWith(route) == true))
            {
                var role = context.Session.GetString("Role");
                
                if (string.IsNullOrEmpty(role) || role != UserRole.Admin.ToString())
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await _next(context);
        }
    }
}
