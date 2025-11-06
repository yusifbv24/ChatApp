namespace ChatApp.Modules.Search.Domain.Enums
{
    public enum SearchScope
    {
        All = 0,              // Search everywhere user has access
        Channels = 1,         // Only channel messages
        DirectMessages = 2,   // Only direct messages
        SpecificChannel = 3,  // Search within one channel
        SpecificConversation = 4  // Search within one conversation
    }
}