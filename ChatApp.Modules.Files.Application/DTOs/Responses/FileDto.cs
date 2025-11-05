using ChatApp.Modules.Files.Domain.Enums;

namespace ChatApp.Modules.Files.Application.DTOs.Responses
{
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
        DateTime UploadedAtUtc);
}