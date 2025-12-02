using ChatApp.Blazor.Client;
using ChatApp.Blazor.Client.Extensions;
using ChatApp.Blazor.Client.Infrastructure.Auth;
using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Infrastructure.SignalR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure base address
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "http://localhost:7000";

// Register cookie handler to ensure credentials (cookies) are sent with requests
builder.Services.AddTransient<CookieHandler>();

// Register JWT authorization handler for automatic token refresh
builder.Services.AddTransient<JwtAuthorizationMessageHandler>();

// Default HttpClient with cookie support (no JWT handler to avoid circular calls during refresh)
builder.Services.AddHttpClient("Default", client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
})
.AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

// MudBlazor Services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
});

// Authentication & Authorization (using secure HttpOnly cookies)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

// HTTP Client with Authentication, JWT refresh, and Cookie support
builder.Services.AddTransient<AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient("ChatApp.Api", client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<CookieHandler>()
.AddHttpMessageHandler<JwtAuthorizationMessageHandler>()  // Add JWT handler for auto token refresh
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChatApp.Api"));

// API Client
builder.Services.AddScoped<IApiClient, ApiClient>();

// SignalR
builder.Services.AddScoped<IChatHubConnection, ChatHubConnection>();
builder.Services.AddScoped<ISignalRService, SignalRService>();

// Feature Services
builder.Services.AddFeatureServices();

// State Management
builder.Services.AddStateManagement();

await builder.Build().RunAsync();