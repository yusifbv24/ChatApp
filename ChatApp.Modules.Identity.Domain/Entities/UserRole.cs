using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class UserRole:Entity
    {
        public Guid UserId { get; private set; }
        public Guid RoleId { get; private set;  }
        public DateTime AssignedAtUtc { get; private set;  }
        public Guid? AssignedBy { get; private set; }

        // Navigation property
        public User User { get; private set; } = null!;
        public Role Role { get; private set; } = null!;

        private UserRole() : base() { }

        public UserRole(Guid userId,Guid roleId,Guid? assignedBy=null):base()
        {
            UserId=userId;
            RoleId=roleId;
            AssignedAtUtc = DateTime.UtcNow;
            AssignedBy=assignedBy;
        }
    }
}