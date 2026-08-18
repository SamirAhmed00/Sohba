namespace Sohba.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string Title => StatusCode switch
        {
            401 => "Sign-in required",
            403 => "Access denied",
            404 => "Page not found",
            429 => "Too many requests",
            _ => "Something went wrong"
        };

        public string FriendlyMessage => StatusCode switch
        {
            401 => "You need to sign in to view this page.",
            403 => "You don't have permission to access this page.",
            404 => "We couldn't find the page you were looking for.",
            429 => "You're sending requests too quickly. Please wait a moment and try again.",
            _ => "Sorry, something went wrong while processing your request."
        };
    }
}