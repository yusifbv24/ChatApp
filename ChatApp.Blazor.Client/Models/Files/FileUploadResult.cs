namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// Response from file upload
/// </summary>
public record FileUploadResult
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}