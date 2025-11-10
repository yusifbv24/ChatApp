using System.ComponentModel;

namespace ChatApp.Client.State
{
    public class UserState : INotifyPropertyChanged
    {
        private HashSet<Guid> _onlineUsers = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsUserOnline(Guid userId) => _onlineUsers.Contains(userId);

        public void SetUserOnline(Guid userId)
        {
            if (_onlineUsers.Add(userId))
            {
                OnPropertyChanged(nameof(_onlineUsers));
            }
        }

        public void SetUserOffline(Guid userId)
        {
            if (_onlineUsers.Remove(userId))
            {
                OnPropertyChanged(nameof(_onlineUsers));
            }
        }

        public IReadOnlySet<Guid> OnlineUsers => _onlineUsers;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}