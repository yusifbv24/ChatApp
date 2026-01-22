using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// Junction entity for User-Permission relationship.
    /// Represents individual permissions assigned to specific users.
    /// Administrators have all permissions by default (checked in code, not stored here).
    /// Permission names are static constants from Permissions class.
    /// </summary>
    public class UserPermission : Entity
    {
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;

        public string PermissionName { get; private set; } = null!;

        // Private constructor for EF Core
        private UserPermission() : base() { }

        public UserPermission(Guid userId, string permissionName) : base()
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (string.IsNullOrWhiteSpace(permissionName))
                throw new ArgumentException("Permission name cannot be empty", nameof(permissionName));

            UserId = userId;
            PermissionName = permissionName;
        }
    }
}