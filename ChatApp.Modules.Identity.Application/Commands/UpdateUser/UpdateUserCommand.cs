namespace ChatApp.Modules.Identity.Application.Commands.UpdateUser
{
    public class UpdateUserCommand
    {
        public Guid UserId { get; set;  }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
    }
}