namespace ChatApp.Modules.Files.Application.DTOs.Responses
{
    public record VirusScanResult
    {
        public bool IsClean { get; set; }
        public string? ThreatName { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}