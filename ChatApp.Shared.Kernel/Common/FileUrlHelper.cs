namespace ChatApp.Shared.Kernel.Common;

/// <summary>
/// StoragePath-i statik URL-ə çevirmək üçün helper.
/// Həm relative path (yeni format), həm full path (köhnə format) dəstəkləyir.
/// Məsələn:
///   "generic/conversation_xxx/images/file.jpg" → "/uploads/generic/conversation_xxx/images/file.jpg"
///   "D:\ChatAppUploads\generic\conversation_xxx\images\file.jpg" → "/uploads/generic/conversation_xxx/images/file.jpg"
/// </summary>
public static class FileUrlHelper
{
    private const string UploadsPrefix = "/uploads/";

    public static string? ToUrl(string? storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            return null;

        // Full path-dirsə (absolute), relative hissəni çıxar
        // Windows: "D:\ChatAppUploads\generic\..." → "generic/..."
        // Linux: "/var/uploads/generic/..." → "generic/..."
        var relativePath = storagePath;

        if (Path.IsPathRooted(storagePath))
        {
            // Full path-dən fayl adını və directory strukturunu çıxar
            // "D:\ChatAppUploads\generic\conv\images\file.jpg" kimi path-lərdə
            // base directory-dən sonrakı hissəni tapırıq
            var normalized = storagePath.Replace('\\', '/');

            // Known base path markers - "ChatAppUploads/" və ya "uploads/" sonrasını götür
            var markers = new[] { "ChatAppUploads/", "uploads/" };
            foreach (var marker in markers)
            {
                var idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    relativePath = normalized[(idx + marker.Length)..];
                    break;
                }
            }

            // Heç bir marker tapılmadısa, son directory + fayl adını istifadə et
            if (relativePath == storagePath)
            {
                relativePath = normalized;
            }
        }

        // Backslash-ları forward slash-a çevir
        relativePath = relativePath.Replace('\\', '/');

        return $"{UploadsPrefix}{relativePath}";
    }
}
