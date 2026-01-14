namespace ChatApp.Shared.Kernel
{
    public enum MessageStatus
    {
        Pending,   // Message being sent (optimistic UI)
        Sent,      // Message confirmed by backend
        Delivered, // Message delivered to recipient (at least one person read in channels)Mess
        Read,      // Message read by recipient (DM) or all members (Channel)
        Failed     // Message failed to send
    }
}