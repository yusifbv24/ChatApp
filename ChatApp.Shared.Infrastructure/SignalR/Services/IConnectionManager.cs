namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    public interface IConnectionManager
    {
        Task AddConnectionAsync(Guid userId, string connectionId);
        Task RemoveConnectionAsync(string connectionId);
        Task<List<string>> GetUserConnectionsAsync(Guid userId);
        Task<Dictionary<Guid, List<string>>> GetUsersConnectionsAsync(List<Guid> userIds);
        Task<Guid?> GetUserIdByConnectionAsync(string connectionId);
        Task<bool> IsUserOnlineAsync(Guid userId);
        Task<int> GetConnectionCountAsync(Guid userId);
    }
}