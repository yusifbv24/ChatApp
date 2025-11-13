using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to send a message to a channel
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 4000 characters")]
    public string Content { get; set; } = string.Empty;

    public string? FileId { get; set; }
}
