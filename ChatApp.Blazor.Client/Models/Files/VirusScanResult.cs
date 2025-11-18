namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// Virus scan result DTO
/// </summary>
public record VirusScanResult
{
    public bool IsClean { get; set; }
    public string? ThreatName { get; set; }
    public string Details { get; set; } = string.Empty;
}
