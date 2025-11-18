using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChatApp.Blazor.Client.Infrastructure.Http
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public Task<Result> DeleteAsync(string endpoint)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response=await _httpClient.GetAsync(endpoint);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return Result.Failure<T>(ex.Message);
            }
        }

        public async Task<Result<T>> PostAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                var content = data != null
                    ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PostAsync(endpoint, content);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return Result.Failure<T>(ex.Message);
            }
        }

        public async Task<Result> PostAsync(string endpoint,object? data=null)
        {
            try
            {
                var content = data != null
                    ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PostAsync(endpoint,content);
                return await ProcessResponseWithoutValue(response);
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public async Task<Result<T>> PutAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                var content = data != null
                    ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PutAsync(endpoint, content);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return Result.Failure<T>(ex.Message);
            }
        }

        public async Task<Result> PutAsync(string endpoint,object? data=null)
        {
            try
            {
                var content = data != null
                    ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PutAsync(endpoint,content);
                return await ProcessResponseWithoutValue(response);
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private async Task<Result<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return Result.Success(data!);
            }

            var error = await ExtractErrorMessage(response);
            return Result.Failure<T>(error);
        }

        private async Task<string> ExtractErrorMessage(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);

                // Handle validation errors with detailed field-level messages
                if (errorResponse?.Errors != null && errorResponse.Errors.Count > 0)
                {
                    var errorMessages = new List<string>();
                    foreach(var error in errorResponse.Errors)
                    {
                        foreach(var message in error.Value)
                        {
                            errorMessages.Add(message);
                        }
                    }
                    return string.Join(" . ", errorMessages);
                }

                // Handle simple error responses
                if (!string.IsNullOrEmpty(errorResponse?.Error))
                {
                    return errorResponse.Error;
                }

                // Handle message-based error responses
                if (!string.IsNullOrEmpty(errorResponse?.Message))
                {
                    return errorResponse.Message;
                }
                return $"Request failed with status code {response.StatusCode}";
            }
            catch
            {
                return $"Request failed with status code {response.StatusCode}";
            }
        }

        private async Task<Result> ProcessResponseWithoutValue(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var error = await ExtractErrorMessage(response);
            return Result.Failure(error);
        }
    }
}