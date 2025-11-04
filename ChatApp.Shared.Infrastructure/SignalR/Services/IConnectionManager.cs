namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    public interface IConnectionManager
    {
        Task AddConnectionAsync(Guid userId, string connectionId, string? deviceInfo=null);
        Task RemoveConnectionAsync(string connectionId);
        Task<List<string>> GetUserConnectionsAsync(Guid userId);
        Task<Dictionary<Guid, List<string>>> GetUsersConnectionsAsync(List<Guid> userIds);
        Task<Guid?> GetUserIdByConnectionAsync(string connectionId);
        Task<bool> IsUserOnlineAsync(Guid userId);
        Task<List<Guid>> GetOnlineUsersAsync();
        Task<int> GetConnectionCountAsync(Guid userId);
    }
}