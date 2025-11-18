using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Request to start a new conversation
/// </summary>
public class StartConversationRequest
{
    [Required(ErrorMessage = "Please select a user")]
    public Guid OtherUserId { get; set; }
}
