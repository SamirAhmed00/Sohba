using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Sohba.Middleware
{
    /// <summary>
    /// Assigns a server-generated correlation id to every HTTP request and
    /// exposes it to all Serilog events emitted during the request via
    /// LogContext (Enrich.FromLogContext is configured in Program.cs).
    /// Registered as the OUTERMOST middleware.
    /// </summary>
    public class RequestCorrelationMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestCorrelationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = Guid.NewGuid().ToString();

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                context.Response.Headers.Append(
                    "X-Correlation-Id",
                    correlationId);

                await _next(context);
            }
        }
    }
}