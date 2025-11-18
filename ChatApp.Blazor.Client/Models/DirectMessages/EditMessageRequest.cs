using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Request to edit a direct message
/// </summary>
public class EditMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [MaxLength(4000, ErrorMessage = "Message cannot exceed 4000 characters")]
    public string NewContent { get; set; } = string.Empty;
}
