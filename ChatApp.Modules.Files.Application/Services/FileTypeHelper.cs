using ChatApp.Modules.Files.Domain.Enums;

namespace ChatApp.Modules.Files.Application.Services
{
    public static class FileTypeHelper
    {
        public static readonly Dictionary<string, FileType> ContentTypeMapping = new()
        {
            // Images
            {"image/jpg",FileType.Image },
            {"image/jpeg",FileType.Image },
            {"image/png",FileType.Image },
            {"image/gif",FileType.Image },
            {"image/webp",FileType.Image },
            {"image/svg+xml",FileType.Image },
            {"image/bmp",FileType.Image },


            // Documents
            { "application/pdf",FileType.Document },
            { "application/msword",FileType.Document },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileType.Document },
            { "application/vnd.ms-excel", FileType.Document },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileType.Document },
            { "application/vnd.ms-powerpoint", FileType.Document },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileType.Document },
            { "text/plain", FileType.Document },
            { "text/csv", FileType.Document },

            // Videos
            { "video/mp4", FileType.Video },
            { "video/mpeg", FileType.Video },
            { "video/quicktime", FileType.Video },
            { "video/x-msvideo", FileType.Video },
            { "video/webm", FileType.Video },

            // Audio
            { "audio/mpeg", FileType.Audio },
            { "audio/wav", FileType.Audio },
            { "audio/ogg", FileType.Audio },
            { "audio/webm", FileType.Audio },

            // Archives
            { "application/zip", FileType.Archive },
            { "application/x-rar-compressed", FileType.Archive },
            { "application/x-7z-compressed", FileType.Archive },
            { "application/x-tar", FileType.Archive },
            { "application/gzip", FileType.Archive }
        };

        public static FileType GetFileType(string contentType)
        {
            return ContentTypeMapping.TryGetValue(contentType.ToLowerInvariant(), out var fileType)
                ? fileType
                : FileType.Other;
        }

        public static bool IsAllowedFileType(string contentType)
        {
            return ContentTypeMapping.ContainsKey(contentType.ToLowerInvariant());
        }


        public static string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }

        public static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                // Images
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                "image/bmp" => ".bmp",

                // Documents
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/vnd.ms-powerpoint" => ".ppt",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                "text/plain" => ".txt",
                "text/csv" => ".csv",

                // Videos
                "video/mp4" => ".mp4",
                "video/mpeg" => ".mpeg",
                "video/quicktime" => ".mov",
                "video/x-msvideo" => ".avi",
                "video/webm" => ".webm",

                // Audio
                "audio/mpeg" => ".mp3",
                "audio/wav" => ".wav",
                "audio/ogg" => ".ogg",
                "audio/webm" => ".weba",

                // Archives
                "application/zip" => ".zip",
                "application/x-rar-compressed" => ".rar",
                "application/x-7z-compressed" => ".7z",
                "application/x-tar" => ".tar",
                "application/gzip" => ".gz",

                _ => ""
            };
        }
    }
}