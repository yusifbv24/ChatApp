using ChatApp.Shared.Kernel.Common;

namespace ChatApp.Modules.Identity.Domain.Entities
{
    public class RolePermission:Entity
    {
        public Guid RoleId { get; private set; }
        public Guid PermissionId { get; private set; }

        public Role Role { get; private set; } = null!;
        public Permission Permission { get; private set; }=null!;

        private RolePermission():base(){}

        public RolePermission(Guid roleId,Guid permissionId) : base()
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}