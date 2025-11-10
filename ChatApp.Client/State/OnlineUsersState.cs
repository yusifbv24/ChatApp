namespace ChatApp.Client.State
{
    public class OnlineUsersState
    {
        private readonly HashSet<Guid> _onlineUserIds = new();
        public event Action? OnChange;

        public void AddUser(Guid userId)
        {
            if (_onlineUserIds.Add(userId))
            {
                OnChange?.Invoke();
            }
        }

        public void RemoveUser(Guid userId)
        {
            if (_onlineUserIds.Remove(userId))
            {
                OnChange?.Invoke();
            }
        }

        public bool IsUserOnline(Guid userId) => _onlineUserIds.Contains(userId);
        public IReadOnlySet<Guid> GetOnlineUsers() => _onlineUserIds;
    }
}