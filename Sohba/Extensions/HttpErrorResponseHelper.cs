using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sohba.Extensions
{
    /// <summary>
    /// Shared helper for producing consistent, non-leaking error responses
    /// (401/403/404/429/500) across full-page navigations and AJAX/JSON calls.
    /// Used by Program.cs (UseStatusCodePages, UseExceptionHandler, rate limiter OnRejected).
    /// </summary>
    public static class HttpErrorResponseHelper
    {
        public static bool IsAjaxOrJsonRequest(HttpRequest request)
        {
            bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";
            bool expectsJson = request.Headers["Accept"].ToString().Contains("application/json")
                                || request.ContentType?.Contains("application/json") == true;
            return isAjax || expectsJson;
        }

        public static Task WriteJsonErrorAsync(HttpResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            return response.WriteAsJsonAsync(new { success = false, error = message });
        }

        public static string GetFriendlyMessage(int statusCode) => statusCode switch
        {
            401 => "You need to log in to continue.",
            403 => "You do not have permission to perform this action.",
            404 => "The requested resource was not found.",
            429 => "Too many requests. Please wait a moment and try again.",
            _ => "An unexpected error occurred while processing your request."
        };
    }
}