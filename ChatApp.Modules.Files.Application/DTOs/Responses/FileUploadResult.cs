namespace ChatApp.Modules.Files.Application.DTOs.Responses
{
    public record FileUploadResult(
        Guid FileId,
        string FileName,
        long FileSizeInBytes,
        string DownloadUrl);
}