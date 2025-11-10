using ChatApp.Client.Models.Common;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatApp.Client.Services.Api
{

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ChatApp.Api");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive JSON deserialization
            };
        }

        public async Task<Result<T>> GetAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                return await HandleResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<T>.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        public async Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request);
                return await HandleResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResponse>.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        public async Task<Result> PostAsync<TRequest>(string url, TRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request);
                return await HandleResponse(response);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        public async Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(url, request);
                return await HandleResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResponse>.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        public async Task<Result> PutAsync<TRequest>(string url, TRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(url, request);
                return await HandleResponse(response);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        public async Task<Result> DeleteAsync(string url)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                return await HandleResponse(response);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure($"Network error: {ex.Message}", 0);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Unexpected error: {ex.Message}", 0);
            }
        }

        private async Task<Result<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return data != null
                    ? Result<T>.Success(data)
                    : Result<T>.Failure("Empty response", (int)response.StatusCode);
            }

            var error = await response.Content.ReadAsStringAsync();
            return Result<T>.Failure(error, (int)response.StatusCode);
        }

        private async Task<Result> HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            return Result.Failure(error, (int)response.StatusCode);
        }
    }
}