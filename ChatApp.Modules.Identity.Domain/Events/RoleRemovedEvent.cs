using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Events
{
    public record RoleRemovedEvent:DomainEvent
    {
        public Guid UserID { get; set;  }
        public Guid RoleId {  get; set; }

        public RoleRemovedEvent(Guid userId,Guid roleId)
        {
            UserID = userId;
            RoleId = roleId;
        }
    }
}