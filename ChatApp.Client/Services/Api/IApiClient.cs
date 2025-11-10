using ChatApp.Client.Models.Common;

namespace ChatApp.Client.Services.Api
{
    public interface IApiClient
    {
        Task<Result<T>> GetAsync<T>(string url);
        Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest request);
        Task<Result> PostAsync<TRequest>(string url, TRequest request);
        Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest request);
        Task<Result> PutAsync<TRequest>(string url, TRequest request);
        Task<Result> DeleteAsync(string url);
    }
}