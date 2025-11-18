using ChatApp.Blazor.Client.Models.Files;

namespace ChatApp.Blazor.Client.Features.Files.State;

/// <summary>
/// State management for files module
/// </summary>
public class FileState
{
    private List<FileDto> _myFiles = new();
    private bool _isLoading;

    /// <summary>
    /// Current user's uploaded files
    /// </summary>
    public IReadOnlyList<FileDto> MyFiles => _myFiles;

    /// <summary>
    /// Whether files are currently being loaded
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Event triggered when state changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Sets the loading state
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the list of user's files
    /// </summary>
    public void SetMyFiles(List<FileDto> files)
    {
        _myFiles = files;
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a newly uploaded file to the list
    /// </summary>
    public void AddFile(FileDto file)
    {
        _myFiles.Insert(0, file); // Add to beginning (most recent first)
        NotifyStateChanged();
    }

    /// <summary>
    /// Removes a deleted file from the list
    /// </summary>
    public void RemoveFile(Guid fileId)
    {
        _myFiles.RemoveAll(f => f.Id == fileId);
        NotifyStateChanged();
    }

    /// <summary>
    /// Gets a file by ID
    /// </summary>
    public FileDto? GetFile(Guid fileId)
    {
        return _myFiles.FirstOrDefault(f => f.Id == fileId);
    }

    /// <summary>
    /// Clears all files (e.g., on logout)
    /// </summary>
    public void Clear()
    {
        _myFiles.Clear();
        _isLoading = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
