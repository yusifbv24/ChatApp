using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// Interface for making HTTP API calls with standardized error handling
/// </summary>
public interface IApiClient
{
    Task<Result<T>> GetAsync<T>(string endpoint);
    Task<Result<T>> PostAsync<T>(string endpoint, object? data = null);
    Task<Result<T>> PutAsync<T>(string endpoint, object? data = null);
    Task<Result> DeleteAsync(string endpoint);
    Task<Result> PostAsync(string endpoint, object? data = null);
    Task<Result> PutAsync(string endpoint, object? data = null);
}
