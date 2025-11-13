using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to add a member to a channel
/// </summary>
public class AddMemberRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }
}
