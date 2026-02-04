using ChatApp.Blazor.Client.Models.Files;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class FileSelectionPanel : ComponentBase
{
    [Parameter] public List<SelectedFile> SelectedFiles { get; set; } = new();
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<(List<SelectedFile> Files, string Message)> OnSend { get; set; }
    [Parameter] public EventCallback<SelectedFile> OnRetry { get; set; }

    private string MessageText { get; set; } = string.Empty;
    private bool IsSending { get; set; } = false;
    private Guid? hoveredFileId = null;

    private async Task OnCloseClick()
    {
        await OnClose.InvokeAsync();
    }

    private async Task OnDeleteClick(SelectedFile file)
    {
        SelectedFiles.Remove(file);

        // Close panel if no files left
        if (SelectedFiles.Count == 0)
        {
            await OnClose.InvokeAsync();
        }
        else
        {
            StateHasChanged();
        }
    }

    private async Task OnRetryClick(SelectedFile file)
    {
        // Reset state for retry
        file.State = UploadState.Pending;
        file.UploadProgress = 0;
        file.ErrorMessage = null;
        StateHasChanged();

        // Trigger upload via callback
        await OnRetry.InvokeAsync(file);
    }

    private async Task OnCancelClick(SelectedFile file)
    {
        file.State = UploadState.Cancelled;
        SelectedFiles.Remove(file);

        if (SelectedFiles.Count == 0)
        {
            await OnClose.InvokeAsync();
        }
        else
        {
            StateHasChanged();
        }
    }

    private async Task OnSendClick()
    {
        if (IsSending || SelectedFiles.Count == 0)
            return;

        IsSending = true;

        // Faylları və mesajı kopyala (panel bağlandıqdan sonra istifadə üçün)
        var filesToSend = SelectedFiles.ToList();
        var messageToSend = MessageText;

        // Panel dərhal bağlanır - optimistic UI pattern
        // Upload prosesi arxa fonda davam edəcək
        await OnSend.InvokeAsync((filesToSend, messageToSend));

        // NOT: IsSending = false etmirik çünki panel artıq bağlanıb
    }

    private string GetFileIcon(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => Icons.Material.Filled.PictureAsPdf,
            ".doc" or ".docx" => Icons.Material.Filled.Description,
            ".xls" or ".xlsx" => Icons.Material.Filled.TableChart,
            ".ppt" or ".pptx" => Icons.Material.Filled.Slideshow,
            ".zip" or ".rar" or ".7z" => Icons.Material.Filled.FolderZip,
            ".mp4" or ".avi" or ".mov" or ".mkv" => Icons.Material.Filled.VideoFile,
            ".mp3" or ".wav" or ".flac" => Icons.Material.Filled.AudioFile,
            ".txt" => Icons.Material.Filled.TextSnippet,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => Icons.Material.Filled.Image,
            _ => Icons.Material.Filled.InsertDriveFile
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
