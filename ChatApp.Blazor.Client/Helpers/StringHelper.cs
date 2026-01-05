namespace ChatApp.Blazor.Client.Helpers;

public static class StringHelper
{
    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : name[0].ToString().ToUpper();
    }
    /// <summary>
    /// Mətni qısaldır
    /// </summary>
    public static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length > maxLength ? string.Concat(text.AsSpan(0, maxLength), "...") : text;
    }
}