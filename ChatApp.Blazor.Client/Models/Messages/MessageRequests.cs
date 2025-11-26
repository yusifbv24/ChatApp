using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// Request to send a direct message
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(4000, ErrorMessage = "Message cannot exceed 4000 characters")]
    public string Content { get; set; } = string.Empty;

    public string? FileId { get; set; }
}

/// <summary>
/// Request to edit a message
/// </summary>
public class EditMessageRequest
{
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(4000, ErrorMessage = "Message cannot exceed 4000 characters")]
    public string NewContent { get; set; } = string.Empty;
}

/// <summary>
/// Request to start a new conversation
/// </summary>
public class StartConversationRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid OtherUserId { get; set; }
}

/// <summary>
/// Request to add/remove a reaction
/// </summary>
public class ReactionRequest
{
    [Required(ErrorMessage = "Reaction is required")]
    public string Reaction { get; set; } = string.Empty;
}

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

    public ChannelType Type { get; set; } = ChannelType.Public;
}

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
