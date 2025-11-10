namespace ChatApp.Client.Models.Channels
{
    public record SendMessageRequest
    {
        public string Content { get; init; } = string.Empty;
        public List<Guid> AttachmentIds { get; init; } = new();
    }
}