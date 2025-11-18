using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to edit a message
/// </summary>
public class EditMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 4000 characters")]
    public string NewContent { get; set; } = string.Empty;
}
