using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Request to add a reaction to a message
/// </summary>
public class AddReactionRequest
{
    [Required(ErrorMessage = "Reaction is required")]
    [MaxLength(10, ErrorMessage = "Reaction cannot exceed 10 characters")]
    public string Reaction { get; set; } = string.Empty;
}
