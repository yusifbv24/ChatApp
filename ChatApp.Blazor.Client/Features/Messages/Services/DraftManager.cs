namespace ChatApp.Blazor.Client.Features.Messages.Services;

/// <summary>
/// Mesaj draft-larını idarə edir.
/// KEY FORMAT: conv_{id} | chan_{id} | pending_{userId}
/// </summary>
public class DraftManager
{
    private readonly Dictionary<string, string> _drafts = [];

    public string CurrentDraft { get; set; } = string.Empty;

    /// <summary>
    /// Bütün draft-ları qaytarır (ConversationList preview üçün).
    /// </summary>
    public IReadOnlyDictionary<string, string> AllDrafts => _drafts;

    public void Save(Guid? conversationId, Guid? channelId, Guid? pendingUserId, string draft)
    {
        var key = BuildKey(conversationId, channelId, pendingUserId);
        if (key == null) return;

        if (string.IsNullOrWhiteSpace(draft))
            _drafts.Remove(key);
        else
            _drafts[key] = draft;
    }

    public string Load(Guid? conversationId, Guid? channelId, Guid? pendingUserId = null)
    {
        var key = BuildKey(conversationId, channelId, pendingUserId);
        if (key == null) return string.Empty;

        return _drafts.TryGetValue(key, out var draft) ? draft : string.Empty;
    }

    public void Remove(Guid? conversationId, Guid? channelId, Guid? pendingUserId = null)
    {
        var key = BuildKey(conversationId, channelId, pendingUserId);
        if (key != null) _drafts.Remove(key);
    }

    private static string? BuildKey(Guid? conversationId, Guid? channelId, Guid? pendingUserId)
    {
        if (conversationId.HasValue) return $"conv_{conversationId.Value}";
        if (channelId.HasValue) return $"chan_{channelId.Value}";
        if (pendingUserId.HasValue) return $"pending_{pendingUserId.Value}";
        return null;
    }
}