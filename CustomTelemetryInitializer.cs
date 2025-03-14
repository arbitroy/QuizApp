using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace QuizApp
{
    public class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context != null)
            {
                telemetry.Context.User.Id = context.User?.Identity?.Name ?? "Anonymous";

                // Add additional properties to track
                if (telemetry is ISupportProperties telemetryWithProperties)
                {
                    // Add custom dimensions for more detailed tracking
                    telemetryWithProperties.Properties["UserAgent"] =
                        context.Request.Headers["User-Agent"].ToString();
                    telemetryWithProperties.Properties["Endpoint"] =
                        $"{context.Request.Method} {context.Request.Path}";
                }
            }
        }
    }
}