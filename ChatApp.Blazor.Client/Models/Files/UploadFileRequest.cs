using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Models.Files;

/// <summary>
/// File upload request
/// </summary>
public class UploadFileRequest
{
    public IBrowserFile? File { get; set; }
}
