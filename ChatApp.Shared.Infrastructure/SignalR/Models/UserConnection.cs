namespace ChatApp.Shared.Infrastructure.SignalR.Models
{
    public record UserConnection
    {
        public Guid UserId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAtUtc { get; set; }
        public string? DeviceInfo { get; set; }

        public UserConnection(Guid userId,string connectionId,string? deviceInfo=null)
        {
            UserId= userId;
            ConnectionId= connectionId;
            ConnectedAtUtc= DateTime.UtcNow;
            DeviceInfo = deviceInfo;
        }
    }
}