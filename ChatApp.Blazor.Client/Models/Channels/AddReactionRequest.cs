using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to add a reaction to a message
/// </summary>
public class AddReactionRequest
{
    [Required(ErrorMessage = "Reaction is required")]
    public string Reaction { get; set; } = string.Empty;
}
