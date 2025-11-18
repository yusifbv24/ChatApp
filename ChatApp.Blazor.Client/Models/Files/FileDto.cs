namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// File metadata response DTO
/// </summary>
public record FileDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSizeInBytes,
    FileType FileType,
    Guid UploadedBy,
    string UploaderUsername,
    string UploaderDisplayName,
    int? Width,
    int? Height,
    bool HasThumbnail,
    DateTime UploadedAtUtc
);
