using Blazored.LocalStorage;
using Blazored.SessionStorage;
using ChatApp.Blazor.Client;
using ChatApp.Blazor.Client.Infrastructure.Auth;
using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Infrastructure.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure base address
var apiBaseUrl = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7000";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// MudBlazor services
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

// Local and Session Storage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();


// Storage service
builder.Services.AddScoped<IStorageService, StorageService>();

// Authentication & Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>(provider =>
    (CustomAuthStateProvider)provider.GetRequiredService<AuthenticationStateProvider>());

// Http client with Authentication
builder.Services.AddScoped<AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient("ChatApp.Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChatApp.Api"));

// Api Client
builder.Services.AddScoped<IApiClient, ApiClient>();

// SignalR
builder.Services.AddScoped<IChatHubConnection, ChatHubConnection>();
builder.Services.AddScoped<ISignalRService, SignalRService>();

// Feature Services
builder.Services.AddFeatureServices();

// State Management
builder.Services.AddStateManagement();

await builder.Build().RunAsync();