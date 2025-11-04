namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    /// <summary>
    /// Service for managing user presence and online status
    /// </summary>
    public interface IPresenceService
    {
        /// <summary>
        /// User went online
        /// </summary>
        Task UserConnectedAsync(Guid userId, string connectionId);


        /// <summary>
        /// User went offline
        /// </summary>
        Task UserDisconnectedAsync(string connectionId);


        /// <summary>
        /// Get online status for multiple users
        /// </summary>
        Task<Dictionary<Guid, bool>> GetUsersOnlineStatusAsync(List<Guid> userIds);



        /// <summary>
        /// Broadcast presence update to all relevant users
        /// </summary>
        Task BroadcastPresenceUpdateAsync(Guid userId, bool isOnline);
    }
}