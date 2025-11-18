using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.DirectMessages;

/// <summary>
/// Request to send a direct message
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [MaxLength(4000, ErrorMessage = "Message cannot exceed 4000 characters")]
    public string Content { get; set; } = string.Empty;

    public string? FileId { get; set; }
}
