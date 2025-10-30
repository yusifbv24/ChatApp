namespace ChatApp.Modules.Identity.Application.Commands.AssignPermission
{
    public class AssignPermissionCommand
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
    }
}