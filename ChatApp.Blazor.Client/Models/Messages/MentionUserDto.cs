namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// Mention panel-də göstəriləcək istifadəçi məlumatları.
/// </summary>
public class MentionUserDto
{
    /// <summary>
    /// İstifadəçinin ID-si. "All" üçün Guid.Empty.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// İstifadəçinin adı. Channel-də "All" ola bilər.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// İstifadəçinin avatar URL-i (opsional).
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Channel-də member olub-olmadığı (bildiriş göndəriləcəkmi).
    /// </summary>
    public bool IsMember { get; set; }

    /// <summary>
    /// "All" mention-i olub-olmadığı.
    /// </summary>
    public bool IsAll { get; set; }
}