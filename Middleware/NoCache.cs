using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QuizApp.Middleware
{
    public class NoCache
    {
        private readonly RequestDelegate _next;

        public NoCache(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Apply no-cache headers for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, post-check=0, pre-check=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            await _next(context);
        }
    }

    // Extension method for easier middleware registration
    public static class NoCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseNoCache(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<NoCache>();
        }
    }
}