namespace ChatApp.Blazor.Client.Models.Common
{
    public class ErrorResponse
    {
        // For simple error responses: { "error": "message" }
        public string? Error { get; set; }

        // For validation error responses: { "message": "...", "errors": {...} }
        public string? Message { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public int? StatusCode { get; set; }
    }
}