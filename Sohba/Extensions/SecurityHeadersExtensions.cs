using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sohba.Extensions
{
    /// <summary>
    /// Production security-header baseline.
    /// CSP whitelists only the external origins actually used by the app
    /// (see Production_Three.md section 3.2 for the verified origin table).
    /// cdn.tailwindcss.com is intentionally absent - Tailwind is compiled locally.
    /// </summary>
    public static class SecurityHeadersExtensions
    {
        private const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob: https://ui-avatars.com; " +
            "media-src 'self' blob:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "object-src 'none'";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var response = context.Response;

                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["X-Frame-Options"] = "DENY";
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                response.Headers["Content-Security-Policy"] = ContentSecurityPolicy;

                await next();
            });
        }
    }
}