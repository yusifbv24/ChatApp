using System.Collections.Concurrent;
using System.ComponentModel;

namespace ChatApp.Client.State
{
    /// <summary>
    /// Tracks online/offline status of all users
    /// Thread-safe, reactive state management
    /// </summary>
    public class PresenceState : INotifyPropertyChanged
    {
        private readonly ConcurrentDictionary<Guid, UserPresence> _onlineUsers = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetUserOnline(Guid userId)
        {
            _onlineUsers.AddOrUpdate(userId,
                new UserPresence { UserId = userId, Status = "Online", LastSeen = DateTime.UtcNow },
                (_, existing) => existing with { Status = "Online", LastSeen = DateTime.UtcNow });
            OnPropertyChanged(nameof(OnlineUsers));
        }

        public void SetUserOffline(Guid userId)
        {
            if (_onlineUsers.TryRemove(userId, out _))
            {
                OnPropertyChanged(nameof(OnlineUsers));
            }
        }

        public void UpdateUserStatus(Guid userId, string status)
        {
            _onlineUsers.AddOrUpdate(userId,
                new UserPresence { UserId = userId, Status = status, LastSeen = DateTime.UtcNow },
                (_, existing) => existing with { Status = status, LastSeen = DateTime.UtcNow });
            OnPropertyChanged(nameof(OnlineUsers));
        }

        public bool IsUserOnline(Guid userId) => _onlineUsers.ContainsKey(userId);

        public string GetUserStatus(Guid userId) =>
            _onlineUsers.TryGetValue(userId, out var presence) ? presence.Status : "Offline";

        public IReadOnlyList<UserPresence> OnlineUsers => _onlineUsers.Values.ToList();

        public int OnlineCount => _onlineUsers.Count;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record UserPresence
    {
        public Guid UserId { get; init; }
        public string Status { get; init; } = "Online";
        public DateTime LastSeen { get; init; }
    }
}