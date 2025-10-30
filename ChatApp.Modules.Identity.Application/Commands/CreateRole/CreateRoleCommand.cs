namespace ChatApp.Modules.Identity.Application.Commands.CreateRole
{
    public class CreateRoleCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; }= string.Empty;
    }
}