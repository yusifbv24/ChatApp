using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Files;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ChatApp.Blazor.Client.Features.Files.Services;

/// <summary>
/// Implementation of file management service
/// </summary>
public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB for profile pictures

    public FileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<FileUploadResult>> UploadProfilePictureAsync(byte[] fileData, string fileName, string contentType, Guid? targetUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file type
            if (!contentType.StartsWith("image/"))
            {
                return Result.Failure<FileUploadResult>("Only image files are allowed for profile pictures");
            }

            // Validate file size
            if (fileData.Length > MaxFileSize)
            {
                return Result.Failure<FileUploadResult>($"File size must be less than {MaxFileSize / 1024 / 1024} MB");
            }

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var memoryStream = new MemoryStream(fileData);
            using var streamContent = new StreamContent(memoryStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "File", fileName);

            // Build URL with optional targetUserId query parameter
            var url = "/api/files/upload/profile-picture";
            if (targetUserId.HasValue)
            {
                url += $"?targetUserId={targetUserId.Value}";
            }

            // Send request
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken);
                if (result == null)
                {
                    return Result.Failure<FileUploadResult>("Failed to parse upload response");
                }
                return Result.Success(result);
            }

            // Extract error message
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
            return Result.Failure<FileUploadResult>(error?.Error ?? $"Upload failed with status code {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure<FileUploadResult>($"Upload failed: {ex.Message}");
        }
    }

    public async Task<Result<FileUploadResult>> UploadProfilePictureAsync(IBrowserFile file, Guid? targetUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
            {
                return Result.Failure<FileUploadResult>("Only image files are allowed for profile pictures");
            }

            // Validate file size
            if (file.Size > MaxFileSize)
            {
                return Result.Failure<FileUploadResult>($"File size must be less than {MaxFileSize / 1024 / 1024} MB");
            }

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream(MaxFileSize, cancellationToken);
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "File", file.Name);

            // Build URL with optional targetUserId query parameter
            var url = "/api/files/upload/profile-picture";
            if (targetUserId.HasValue)
            {
                url += $"?targetUserId={targetUserId.Value}";
            }

            // Send request
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken);
                if (result == null)
                {
                    return Result.Failure<FileUploadResult>("Failed to parse upload response");
                }
                return Result.Success(result);
            }

            // Extract error message
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
            return Result.Failure<FileUploadResult>(error?.Error ?? $"Upload failed with status code {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure<FileUploadResult>($"Upload failed: {ex.Message}");
        }
    }

    public async Task<Result<FileUploadResult>> UploadFileAsync(IBrowserFile file, Guid? conversationId = null, Guid? channelId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file size (100 MB for general files)
            const long maxGeneralFileSize = 100 * 1024 * 1024;
            if (file.Size > maxGeneralFileSize)
            {
                return Result.Failure<FileUploadResult>($"File size must be less than {maxGeneralFileSize / 1024 / 1024} MB");
            }

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream(maxGeneralFileSize, cancellationToken);
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "File", file.Name);

            // Add conversation/channel context
            if (conversationId.HasValue)
            {
                content.Add(new StringContent(conversationId.Value.ToString()), "ConversationId");
            }
            if (channelId.HasValue)
            {
                content.Add(new StringContent(channelId.Value.ToString()), "ChannelId");
            }

            // Send request
            var response = await _httpClient.PostAsync("/api/files/upload", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken);
                if (result == null)
                {
                    return Result.Failure<FileUploadResult>("Failed to parse upload response");
                }
                return Result.Success(result);
            }

            // Extract error message
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
            return Result.Failure<FileUploadResult>(error?.Error ?? $"Upload failed with status code {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure<FileUploadResult>($"Upload failed: {ex.Message}");
        }
    }

    public async Task<Result<FileUploadResult>> UploadChannelAvatarAsync(byte[] fileData, string fileName, string contentType, Guid channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file type
            if (!contentType.StartsWith("image/"))
            {
                return Result.Failure<FileUploadResult>("Only image files are allowed for channel avatars");
            }

            // Validate file size
            if (fileData.Length > MaxFileSize)
            {
                return Result.Failure<FileUploadResult>($"File size must be less than {MaxFileSize / 1024 / 1024} MB");
            }

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var memoryStream = new MemoryStream(fileData);
            using var streamContent = new StreamContent(memoryStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "File", fileName);

            // Send request
            var response = await _httpClient.PostAsync($"/api/files/upload/channel-avatar/{channelId}", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken);
                if (result == null)
                {
                    return Result.Failure<FileUploadResult>("Failed to parse upload response");
                }
                return Result.Success(result);
            }

            // Extract error message
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
            return Result.Failure<FileUploadResult>(error?.Error ?? $"Upload failed with status code {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return Result.Failure<FileUploadResult>($"Upload failed: {ex.Message}");
        }
    }

    private record ErrorResponse(string? Error);
}