using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace QuizApp.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var originalPath = context.Request.Path;
            var method = context.Request.Method;

            try
            {
                _logger.LogInformation("[START] {Method} {Path}", method, originalPath);
                await _next(context);
                sw.Stop();

                _logger.LogInformation(
                    "[END] {Method} {Path} completed in {ElapsedMs}ms with status code {StatusCode}",
                    method, originalPath, sw.ElapsedMilliseconds, context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "[ERROR] {Method} {Path} failed after {ElapsedMs}ms: {Message}",
                    method, originalPath, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    // Extension method for easy middleware registration
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}