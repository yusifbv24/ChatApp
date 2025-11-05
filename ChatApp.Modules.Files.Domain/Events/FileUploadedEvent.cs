using ChatApp.Modules.Files.Domain.Enums;
using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Files.Domain.Events
{
    public record FileUploadedEvent(
        Guid FileId,
        string FileName,
        FileType FileType,
        long FileSizeInBytes,
        Guid UploadedBy,
        DateTime UploadedAtUtc
    ): DomainEvent;
}