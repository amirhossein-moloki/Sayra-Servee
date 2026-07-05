using Microsoft.AspNetCore.Http;

namespace Sayra.Server.ProductionHardeningFinal.Middleware;

public class AdminPrivilegeMiddleware
{
    private readonly RequestDelegate _next;

    public AdminPrivilegeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            // Strict check for Admin role in JWT or session
            var isAdmin = context.User.IsInRole("Admin");

            if (!isAdmin)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Administrative privilege required.");
                return;
            }
        }

        await _next(context);
    }
}
