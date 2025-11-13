using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to remove a reaction from a message
/// </summary>
public class RemoveReactionRequest
{
    [Required(ErrorMessage = "Reaction is required")]
    public string Reaction { get; set; } = string.Empty;
}
