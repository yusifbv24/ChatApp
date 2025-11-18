using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Channels;

/// <summary>
/// Request to update a channel
/// </summary>
public class UpdateChannelRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Channel name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public ChannelType? Type { get; set; }
}
