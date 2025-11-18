using ChatApp.Blazor.Client.Infrastructure.Http;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Files;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;

namespace ChatApp.Blazor.Client.Features.Files.Services;

/// <summary>
/// Implementation of file service
/// Maps to: /api/files
/// </summary>
public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    private readonly IApiClient _apiClient;

    public FileService(HttpClient httpClient, IApiClient apiClient)
    {
        _httpClient = httpClient;
        _apiClient = apiClient;
    }

    /// <summary>
    /// Uploads a file (max 100MB)
    /// POST /api/files/upload
    /// Requires: File.Upload permission
    /// </summary>
    public async Task<Result<FileUploadResult>> UploadFileAsync(IBrowserFile file, long maxFileSize = 104857600)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("/api/files/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>();
                return result != null
                    ? Result<FileUploadResult>.Success(result)
                    : Result<FileUploadResult>.Failure("Failed to parse upload response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return Result<FileUploadResult>.Failure(error ?? "File upload failed");
        }
        catch (Exception ex)
        {
            return Result<FileUploadResult>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Uploads a profile picture (max 10MB, creates 400x400 thumbnail)
    /// POST /api/files/upload/profile-picture
    /// Requires: File.Upload permission
    /// </summary>
    public async Task<Result<FileUploadResult>> UploadProfilePictureAsync(IBrowserFile file, long maxFileSize = 10485760)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("/api/files/upload/profile-picture", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResult>();
                return result != null
                    ? Result<FileUploadResult>.Success(result)
                    : Result<FileUploadResult>.Failure("Failed to parse upload response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return Result<FileUploadResult>.Failure(error ?? "Profile picture upload failed");
        }
        catch (Exception ex)
        {
            return Result<FileUploadResult>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Gets file metadata by ID
    /// GET /api/files/{fileId}
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<FileDto>> GetFileAsync(Guid fileId)
    {
        return await _apiClient.GetAsync<FileDto>($"/api/files/{fileId}");
    }

    /// <summary>
    /// Gets download URL for a file
    /// GET /api/files/{fileId}/download
    /// Requires: File.Download permission
    /// </summary>
    public string GetDownloadUrl(Guid fileId)
    {
        return $"{_httpClient.BaseAddress}api/files/{fileId}/download";
    }

    /// <summary>
    /// Gets thumbnail URL for an image
    /// GET /api/files/{fileId}/thumbnail
    /// Requires: File.Download permission
    /// </summary>
    public string GetThumbnailUrl(Guid fileId)
    {
        return $"{_httpClient.BaseAddress}api/files/{fileId}/thumbnail";
    }

    /// <summary>
    /// Gets user's uploaded files with pagination
    /// GET /api/files/my-files?pageSize={pageSize}&skip={skip}
    /// Requires: Messages.Read permission
    /// </summary>
    public async Task<Result<List<FileDto>>> GetMyFilesAsync(int pageSize = 50, int skip = 0)
    {
        return await _apiClient.GetAsync<List<FileDto>>($"/api/files/my-files?pageSize={pageSize}&skip={skip}");
    }

    /// <summary>
    /// Deletes a file (soft delete)
    /// DELETE /api/files/{fileId}
    /// Requires: File.Delete permission (only uploader can delete)
    /// </summary>
    public async Task<Result> DeleteFileAsync(Guid fileId)
    {
        return await _apiClient.DeleteAsync($"/api/files/{fileId}");
    }
}
