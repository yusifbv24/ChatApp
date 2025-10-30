using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class Role:Entity
    {
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public bool IsSystemRole { get; private set; }

        // Navigation properties
        private readonly List<RolePermission> _rolePermissions = new();
        public IReadOnlyCollection<RolePermission> RolePermissions=>_rolePermissions.AsReadOnly();


        private readonly List<UserRole> _userRoles = new();
        public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

        private Role() : base() { }

        public Role(string name,string description,bool isSystemRole = false) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty", nameof(name));

            Name = name;
            Description = description ?? string.Empty;
            IsSystemRole = isSystemRole;
        }


        public void UpdateName(string newName)
        {
            if(string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Role name cannot be empty",nameof(newName));

            Name= newName;
            UpdateTimestamp();
        }


        public void UpdateDescription(string newDescription)
        {
            Description = newDescription ?? string.Empty;
            UpdateTimestamp();
        }


        public void AddPermission(RolePermission rolePermission)
        {
            if (_rolePermissions.Any(rp => rp.PermissionId == rolePermission.PermissionId))
                throw new InvalidOperationException("Role already has this permission");

            _rolePermissions.Add(rolePermission);
            UpdateTimestamp();
        }


        public void RemovePermission(Guid permissionId)
        {
            var rolPermission=_rolePermissions.FirstOrDefault(rp=>rp.PermissionId == permissionId);

            if (rolPermission != null)
            {
                _rolePermissions.Remove(rolPermission);
                UpdateTimestamp();
            }
        }
    }
}