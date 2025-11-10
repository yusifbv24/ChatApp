using ChatApp.Client.Models.Identity;

namespace ChatApp.Client.Services.Authentication
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user with username and password
        /// Returns LoginResponse with tokens on success
        /// </summary>
        Task<LoginResponse?> LoginAsync(LoginRequest request);

        /// <summary>
        /// Logs out the current user by clearing tokens and notifying auth state
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Refreshes the access token using the refresh token
        /// Called automatically when access token is about to expire
        /// </summary>
        Task<bool> RefreshTokenAsync();

        /// <summary>
        /// Quick check if user is currently authenticated
        /// Checks for valid token without making API call
        /// </summary>
        Task<bool> IsAuthenticatedAsync();
    }

    /*
        USAGE IN PAGES
        =============
        
        Login Page:
        ----------
        @inject IAuthenticationService AuthService
        
        private async Task HandleLogin()
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };
            
            var response = await AuthService.LoginAsync(request);
            
            if (response != null)
            {
                // Login successful - navigate to dashboard
                Navigation.NavigateTo("/");
            }
            else
            {
                // Login failed - show error message
                errorMessage = "Invalid username or password";
            }
        }
        
        Logout Button:
        -------------
        @inject IAuthenticationService AuthService
        
        private async Task HandleLogout()
        {
            await AuthService.LogoutAsync();
            Navigation.NavigateTo("/login");
        }
        
        App Startup:
        -----------
        @inject IAuthenticationService AuthService
        
        protected override async Task OnInitializedAsync()
        {
            // Check if user has valid session
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            
            if (isAuthenticated)
            {
                // Try to refresh token if needed
                await AuthService.RefreshTokenAsync();
            }
        }
    */
}