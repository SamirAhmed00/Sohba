using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sohba.Middleware
{
    /// <summary>
    /// Emits exactly one structured log event per non-static,
    /// non-probe HTTP request after the pipeline completes.
    ///
    /// Logged fields:
    /// Method, Path, StatusCode, ElapsedMs, UserId.
    ///
    /// CorrelationId is attached automatically through LogContext
    /// by RequestCorrelationMiddleware.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private static readonly string[] SkippedPrefixes =
        {
            "/notificationHub"
        };

        private static readonly string[] SkippedExtensions =
        {
            ".css",
            ".js",
            ".map",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".svg",
            ".webp",
            ".ico",
            ".woff",
            ".woff2",
            ".ttf",
            ".mp4",
            ".mov",
            ".json",
            ".txt",
            ".webmanifest"
        };

        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                await _next(context);
            }
            finally
            {
                LogRequest(context, startedAt);
            }
        }

        private static void LogRequest(
            HttpContext context,
            DateTime startedAt)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (ShouldSkip(path))
            {
                return;
            }

            var logger = context.RequestServices
                .GetRequiredService<ILogger<RequestLoggingMiddleware>>();

            var status = context.Response.StatusCode;

            var elapsedMs =
                (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;

            var userId = context.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";

            var level = status >= 500
                ? LogLevel.Error
                : status >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

            logger.Log(
                level,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (user {UserId})",
                context.Request.Method,
                path,
                status,
                elapsedMs,
                userId);
        }

        private static bool ShouldSkip(string path)
        {
            if (path.Equals(
                "/healthz",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var prefix in SkippedPrefixes)
            {
                if (path.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var extension = System.IO.Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                foreach (var skipped in SkippedExtensions)
                {
                    if (extension.Equals(
                        skipped,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}