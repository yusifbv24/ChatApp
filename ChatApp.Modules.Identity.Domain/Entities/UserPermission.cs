using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// Represents a direct permission grant to a user (overrides role-based permissions)
    /// </summary>
    public class UserPermission : Entity
    {
        public Guid UserId { get; private set; }
        public Guid PermissionId { get; private set; }
        public bool IsGranted { get; private set; } // true = granted, false = revoked (explicit deny)
        public DateTime AssignedAtUtc { get; private set; }
        public Guid? AssignedBy { get; private set; }

        // Navigation properties
        public User User { get; private set; } = null!;
        public Permission Permission { get; private set; } = null!;

        private UserPermission() : base() { }

        public UserPermission(Guid userId, Guid permissionId, bool isGranted, Guid? assignedBy = null) : base()
        {
            UserId = userId;
            PermissionId = permissionId;
            IsGranted = isGranted;
            AssignedAtUtc = DateTime.UtcNow;
            AssignedBy = assignedBy;
        }
    }
}