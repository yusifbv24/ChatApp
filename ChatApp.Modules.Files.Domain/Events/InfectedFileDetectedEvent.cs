using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Files.Domain.Events
{
    public record InfectedFileDetectedEvent(
        Guid UserId,
        string FileName,
        string ThreatName,
        DateTime DetectedAtUtc
    ) : DomainEvent;
}