using ChatApp.Client.Models.Identity;
using System.ComponentModel;

namespace ChatApp.Client.State
{
    /// <summary>
    /// Application State Management
    /// ============================
    /// In Blazor, when data changes, components don't automatically re-render unless they know
    /// something changed. INotifyPropertyChanged is the .NET pattern that tells subscribers
    /// "hey, this property changed, you might want to update!"
    /// 
    /// BLAZOR-SPECIFIC CONCEPT: StateHasChanged()
    /// ==========================================
    /// When a component subscribes to PropertyChanged event and receives notification,
    /// it calls StateHasChanged() which tells Blazor to re-render that component.
    /// 
    /// Usage in component:
    /// protected override void OnInitialized()
    /// {
    ///     AppState.PropertyChanged += (s, e) => StateHasChanged();
    /// }
    /// 
    /// Now whenever AppState changes, component automatically re-renders!
    /// </summary>
    public class AppState : INotifyPropertyChanged
    {
        private UserDto? _currentUser;
        private bool _isLoading;
        private int _unreadNotificationCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public UserDto? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    OnPropertyChanged(nameof(CurrentUser));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public int UnreadNotificationCount
        {
            get => _unreadNotificationCount;
            set
            {
                if (_unreadNotificationCount != value)
                {
                    _unreadNotificationCount = value;
                    OnPropertyChanged(nameof(UnreadNotificationCount));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Clear()
        {
            CurrentUser = null;
            UnreadNotificationCount = 0;
        }
    }
}