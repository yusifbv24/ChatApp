using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// Model for a file selected for upload
/// </summary>
public class SelectedFile
{
    public Guid TempId { get; set; } = Guid.NewGuid();
    public IBrowserFile BrowserFile { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public bool IsImage { get; set; }

    // Upload state
    public UploadState State { get; set; } = UploadState.Pending;
    public int UploadProgress { get; set; } = 0;
    public string? UploadedFileId { get; set; }
    public string? ErrorMessage { get; set; }

    // Preview (for images)
    public string? PreviewDataUrl { get; set; }

    // Cancellation support (for upload cancellation)
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    // Əlaqəli mesaj ID-si (optimistic UI üçün)
    public Guid? AssociatedMessageId { get; set; }
}

public enum UploadState
{
    Pending,
    Uploading,
    Completed,
    Failed,
    Cancelled
}