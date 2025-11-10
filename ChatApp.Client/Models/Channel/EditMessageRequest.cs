namespace ChatApp.Client.Models.Channels
{
    public record EditMessageRequest
    {
        public string Content { get; init; } = string.Empty;
    }
}