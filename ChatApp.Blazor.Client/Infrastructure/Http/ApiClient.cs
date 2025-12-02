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
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            return errorResponse?.Error ?? $"Request failed with status code {response.StatusCode}";
        }
        catch
        {
            return $"Request failed with status code {response.StatusCode}";
        }
    }

    private record ErrorResponse(string? Error);
}