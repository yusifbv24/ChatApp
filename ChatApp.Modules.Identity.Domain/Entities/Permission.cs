using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class Permission:Entity
    {
        public string Name { get; private set; }=string.Empty;
        public string? Description { get; private set; }
        public string? Module { get; private set;  }

        private readonly List<RolePermission> _rolePermissions = new();
        public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

        private Permission() : base() { }

        public Permission(string name,string description,string module) : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("Permission name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException("Module cannot be empty", nameof(description));

            Name = name;
            Description = description ?? string.Empty;
            Module = module;
        }

        public void UpdateDescription(string newDescription)
        {
            Description = newDescription ?? string.Empty;
            UpdateTimestamp();
        }
    }
}