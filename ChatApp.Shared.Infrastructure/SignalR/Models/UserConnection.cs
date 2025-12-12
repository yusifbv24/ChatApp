namespace ChatApp.Shared.Infrastructure.SignalR.Models
{
    public class UserConnection
    {
        public Guid UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAtUtc { get; set; }
        public UserConnection(Guid userId,string connectionId)
        {
            UserId= userId;
            ConnectionId= connectionId;
            ConnectedAtUtc= DateTime.UtcNow;
        }
    }
}