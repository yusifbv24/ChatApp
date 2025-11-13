using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to create a new channel
/// </summary>
public class CreateChannelRequest
{
    [Required(ErrorMessage = "Channel name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Channel name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Channel type is required")]
    public ChannelType Type { get; set; } = ChannelType.Public;

    public Guid CreatedBy { get; set; }
}
