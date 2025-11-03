using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record RoleAssignedEvent:DomainEvent
    {
        public Guid UserId { get; }
        public Guid RoleId { get; }
        public RoleAssignedEvent(Guid userId,Guid roleId)
        {
            UserId = userId;
            RoleId = roleId;
        }
    }
}