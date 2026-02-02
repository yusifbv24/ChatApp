using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Infrastructure.Http;

/// <summary>
/// Implementation of API client for making HTTP requests with standardized error handling
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
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

    public async Task<Result> PostAsync(string endpoint, object? data = null)
    {
        try
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PostAsync(endpoint, content);
            return await ProcessResponseWithoutValue(response);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> PutAsync(string endpoint, object? data = null)
    {
        try
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                : null;

            var response = await _httpClient.PutAsync(endpoint, content);
            return await ProcessResponseWithoutValue(response);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
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

    private async Task<Result> ProcessResponseWithoutValue(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        var error = await ExtractErrorMessage(response);
        return Result.Failure(error);
    }

    private async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return GetDefaultErrorMessage(response.StatusCode);

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);

            if (errorResponse == null)
                return GetDefaultErrorMessage(response.StatusCode);

            // If validation errors exist, combine them into a readable message
            if (errorResponse.Errors is { Count: > 0 })
            {
                var validationMessages = errorResponse.Errors
                    .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"));
                return string.Join("; ", validationMessages);
            }

            return errorResponse.Error ?? GetDefaultErrorMessage(response.StatusCode);
        }
        catch
        {
            return GetDefaultErrorMessage(response.StatusCode);
        }
    }

    private static string GetDefaultErrorMessage(System.Net.HttpStatusCode statusCode) => statusCode switch
    {
        System.Net.HttpStatusCode.BadRequest => "Invalid request. Please check your input.",
        System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
        System.Net.HttpStatusCode.Forbidden => "You don't have permission to perform this action.",
        System.Net.HttpStatusCode.NotFound => "The requested resource was not found.",
        System.Net.HttpStatusCode.Conflict => "A conflict occurred. Please try again.",
        System.Net.HttpStatusCode.TooManyRequests => "Too many requests. Please wait and try again.",
        _ => "An unexpected error occurred. Please try again later."
    };

    private record ApiErrorResponse(
        string? Error,
        Dictionary<string, string[]>? Errors);
}