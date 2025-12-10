using System.ComponentModel.DataAnnotations;

namespace ChatApp.Blazor.Client.Models.Messages
{
    public class SendMessageRequests
    {
        [Required(ErrorMessage = "Message content is required")]
        [StringLength(4000, ErrorMessage = "Message content cannot exceed 4000 characters")]
        public string Content { get; set; } = string.Empty;

        public string? FileId { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        public bool IsForwarded { get; set; }
    }



    public class EditMessageRequests
    {
        [Required(ErrorMessage = "Message content is required")]
        [StringLength(4000, ErrorMessage = "Message content cannot exceed 4000 characters")]
        public string NewContent { get; set; } = string.Empty;
    }



    public class StartConversationRequests
    {
        [Required(ErrorMessage ="User ID is required")]
        public Guid OtherUserId { get; set; }
    }



    public class ReactionRequest
    {
        [Required(ErrorMessage ="Reaction is required")]
        public string Reaction { get; set; } = string.Empty;
    }



    public class ReactionToggleResponse
    {
        public bool WasAdded { get; set; }
        public bool WasRemoved { get; set; }
        public bool WasReplaced { get; set; }
        public string Reaction { get; set; } = string.Empty;
        public List<ReactionSummary> Reactions { get; set; } = new();
    }



    public class ReactionSummary
    {
        public string Emoji { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<Guid> UserIds { get; set; } = new();
    }




    public class CreateChannelRequest
    {
        [Required(ErrorMessage ="Channel name is required")]
        [StringLength(100,MinimumLength =2,ErrorMessage ="Channel name must between 2 and 100 characters")]
        public string Name { get; set; }=string.Empty;

        [StringLength(500,ErrorMessage ="Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public ChannelType Type { get; set; } = ChannelType.Public;
    }



    public class UpdateChannelRequest
    {
        [StringLength(100, MinimumLength =2, ErrorMessage ="Channel name must between 2 and 100 characters")]
        public string? Name { get; set; }


        [StringLength(500,ErrorMessage ="Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public ChannelType? Type { get; set; }
    }
}