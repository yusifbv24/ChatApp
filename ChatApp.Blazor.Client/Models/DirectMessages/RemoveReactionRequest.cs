using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Request to remove a reaction from a message
/// </summary>
public class RemoveReactionRequest
{
    [Required(ErrorMessage = "Reaction is required")]
    [MaxLength(10, ErrorMessage = "Reaction cannot exceed 10 characters")]
    public string Reaction { get; set; } = string.Empty;
}
