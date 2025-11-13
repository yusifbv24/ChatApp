using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to update a member's role
/// </summary>
public class UpdateMemberRoleRequest
{
    [Required(ErrorMessage = "New role is required")]
    public MemberRole NewRole { get; set; }
}
