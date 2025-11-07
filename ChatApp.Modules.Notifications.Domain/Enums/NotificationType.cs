namespace ChatApp.Modules.Notifications.Domain.Enums
{
    public enum NotificationType
    {
        ChannelMessage=1,
        DirectMessage=2,
        ChannelMention=3,
        DirectMention=4,
        FileShared=5,
        ChannelInvite=6,
        ReactionAdded=7
    }
}