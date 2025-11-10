using System.ComponentModel;

namespace ChatApp.Client.State
{
    /// <summary>
    /// Tracks SignalR connection state for UI display
    /// </summary>
    public class ConnectionState : INotifyPropertyChanged
    {
        private bool _isConnected;
        private bool _isReconnecting;
        private string _status = "Disconnected";

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public bool IsReconnecting
        {
            get => _isReconnecting;
            set
            {
                if (_isReconnecting != value)
                {
                    _isReconnecting = value;
                    OnPropertyChanged(nameof(IsReconnecting));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}