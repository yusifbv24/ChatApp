namespace ChatApp.Modules.Identity.Application.Commands.AssignRole
{
    public class AssignRoleCommand
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid? AssignedBy { get; set; }
    }
}