using Blazored.LocalStorage;
using ChatApp.Client;
using ChatApp.Client.Handlers;
using ChatApp.Client.Services.Api;
using ChatApp.Client.Services.Authentication;
using ChatApp.Client.Services.Notification;
using ChatApp.Client.Services.SignalR;
using ChatApp.Client.Services.Storage;
using ChatApp.Client.State;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ========================================
// ROOT COMPONENTS CONFIGURATION
// ========================================
// The root components define where your Blazor application "lives" in the HTML DOM.
// App.razor is the root component that contains your entire application structure.
// HeadOutlet is a special component that allows us to modify the <head> section of the HTML
// from within Blazor components (useful for dynamically setting page titles, meta tags, etc.).
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ========================================
// API CONFIGURATION
// ========================================
// We need to tell the application where your backend API is located.
// This reads the API base URL from appsettings.json so you can easily change it
// between development, staging, and production environments without recompiling.
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("API Base URL is not configured");

// ========================================
// HTTP CLIENT CONFIGURATION WITH AUTHENTICATION
// ========================================
// This is where we configure how the application talks to your backend.
// We're setting up a named HttpClient specifically for API calls.
// The AuthorizationMessageHandler is crucial - it automatically attaches the JWT token
// to every outgoing request to your API, so you don't have to manually add it everywhere.
builder.Services.AddScoped<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("ChatApp.Api", client =>
{
    // Set the base address so we can use relative URLs in our service calls
    // For example: httpClient.GetAsync("/api/users/me") instead of the full URL
    client.BaseAddress = new Uri(apiBaseUrl);

    // Set a reasonable timeout for API calls
    // 100 seconds is generous for most operations, but important for file uploads
    client.Timeout = TimeSpan.FromSeconds(100);
})
.AddHttpMessageHandler<AuthorizationMessageHandler>(); // This attaches JWT to requests

// ========================================
// DEFAULT HTTP CLIENT FOR NON-API CALLS
// ========================================
// Some services might need to make HTTP calls to external services
// (like downloading avatars, checking external APIs, etc.)
// This default client doesn't include authentication headers.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ========================================
// LOCAL STORAGE CONFIGURATION
// ========================================
// Blazored.LocalStorage is a library that provides a clean C# API for browser localStorage.
// We use this to persist the JWT token so users don't have to log in every time
// they refresh the page. The token survives browser refreshes and even closing/reopening.
builder.Services.AddBlazoredLocalStorage();

// ========================================
// AUTHENTICATION & AUTHORIZATION
// ========================================
// This is the heart of your security system in the frontend.
// AuthenticationStateProvider is a Blazor service that tracks whether a user is logged in
// and what their permissions are. Our custom AuthStateProvider reads the JWT token
// from localStorage, validates it, and extracts the user's claims (ID, username, permissions).
// Once registered, you can use [Authorize] attributes on pages and check permissions.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ========================================
// API SERVICE LAYER
// ========================================
// These are the services that make HTTP calls to your backend endpoints.
// Each service is scoped, meaning a new instance is created for each user session/scope.
// We separate these into individual services (UserService, RoleService, etc.) following
// the Single Responsibility Principle - each service knows how to talk to one part of your API.
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// ========================================
// SIGNALR CONFIGURATION
// ========================================
// SignalR provides real-time, bidirectional communication between your frontend and backend.
// This is essential for your chat application - when someone sends a message, everyone
// in that channel needs to see it instantly without refreshing the page.
// The service manages the connection, reconnection logic, and event handling.
builder.Services.AddScoped<ISignalRService, SignalRService>();

// ========================================
// STATE MANAGEMENT
// ========================================
// State management services maintain global application state that multiple components
// need to access. For example, the current user's information needs to be available
// throughout the application (in the top bar, sidebar, profile page, etc.).
// By using scoped services for state, we ensure all components share the same data
// and updates to state automatically reflect everywhere.
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<OnlineUsersState>();

// ========================================
// UTILITY SERVICES
// ========================================
// These provide cross-cutting concerns used throughout the application.
// LocalStorageService wraps Blazored.LocalStorage with additional convenience methods.
// NotificationService wraps MudBlazor's Snackbar to show toast notifications consistently.
builder.Services.AddScoped<ChatApp.Client.Services.Storage.ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ========================================
// UI COMPONENT LIBRARY - MUDBLAZOR
// ========================================
// MudBlazor provides pre-built Material Design components (buttons, dialogs, tables, etc.)
// This saves us from building basic UI components from scratch and ensures a
// consistent, professional look throughout the application.
// The configuration sets default behaviors like where snackbars appear on screen.
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000; // 4 seconds
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// ========================================
// BUILD AND RUN
// ========================================
// This builds the configured application and starts it running in the browser.
// At this point, all services are registered and ready to be injected into components.
await builder.Build().RunAsync();