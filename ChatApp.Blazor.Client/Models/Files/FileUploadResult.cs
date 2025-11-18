namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// File upload result DTO
/// </summary>
public record FileUploadResult(
    Guid FileId,
    string FileName,
    long FileSizeInBytes,
    string DownloadUrl
);
