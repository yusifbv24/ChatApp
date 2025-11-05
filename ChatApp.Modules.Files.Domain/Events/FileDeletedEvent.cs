using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Files.Domain.Events
{
    public record FileDeletedEvent(
        Guid FileId,
        string FileName,
        Guid DeletedBy,
        DateTime DeletedAtUtc
    ):DomainEvent;
}