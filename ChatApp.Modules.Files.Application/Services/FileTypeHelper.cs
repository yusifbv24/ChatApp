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
    }
}