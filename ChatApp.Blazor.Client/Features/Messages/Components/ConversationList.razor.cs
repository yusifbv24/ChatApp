using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using MudBlazor;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

/// <summary>
/// ConversationList - Sol paneldə conversation/channel siyahısını göstərən komponent.
///
/// Bu komponent aşağıdakı funksionallıqları təmin edir:
/// - Direct conversation və channel-ların unified siyahısı
/// - Axtarış funksionallığı
/// - New message/channel menu
/// - Typing indicator
/// - Draft indicator
/// - Unread badge
/// - Read Later badge
/// - Last message status (Sent, Delivered, Read)
///
/// Komponent partial class pattern istifadə edir:
/// - ConversationList.razor: HTML template
/// - ConversationList.razor.cs: C# code-behind (bu fayl)
/// </summary>
public partial class ConversationList
{
    #region Injected Services

    [Inject] private UserState UserState { get; set; } = default!;

    #endregion

    #region Parameters - Data

    /// <summary>
    /// Direct conversation siyahısı.
    /// </summary>
    [Parameter] public List<DirectConversationDto> Conversations { get; set; } = new();

    /// <summary>
    /// Channel siyahısı.
    /// </summary>
    [Parameter] public List<ChannelDto> Channels { get; set; } = new();

    /// <summary>
    /// Seçilmiş conversation ID-si.
    /// </summary>
    [Parameter] public Guid? SelectedConversationId { get; set; }

    /// <summary>
    /// Seçilmiş channel ID-si.
    /// </summary>
    [Parameter] public Guid? SelectedChannelId { get; set; }

    /// <summary>
    /// Yükləmə statusu.
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    #endregion

    #region Parameters - Typing State

    /// <summary>
    /// Hər conversation üçün typing statusu.
    /// Key: ConversationId, Value: true = typing
    /// </summary>
    [Parameter] public Dictionary<Guid, bool> ConversationTypingState { get; set; } = new();

    /// <summary>
    /// Hər channel üçün typing edən istifadəçilər.
    /// Key: ChannelId, Value: List of display names
    /// </summary>
    [Parameter] public Dictionary<Guid, List<string>> ChannelTypingUsers { get; set; } = new();

    #endregion

    #region Parameters - Draft

    /// <summary>
    /// Mesaj draft-ları.
    /// Key: "conv_{id}" və ya "chan_{id}", Value: draft text
    /// </summary>
    [Parameter] public Dictionary<string, string> MessageDrafts { get; set; } = new();

    #endregion

    #region Parameters - Event Callbacks

    /// <summary>
    /// Conversation seçildiyi zaman callback.
    /// </summary>
    [Parameter] public EventCallback<DirectConversationDto> OnConversationSelected { get; set; }

    /// <summary>
    /// Channel seçildiyi zaman callback.
    /// </summary>
    [Parameter] public EventCallback<ChannelDto> OnChannelSelected { get; set; }

    /// <summary>
    /// Yeni conversation yaratmaq callback-i.
    /// </summary>
    [Parameter] public EventCallback OnNewConversation { get; set; }

    /// <summary>
    /// Yeni channel yaratmaq callback-i.
    /// </summary>
    [Parameter] public EventCallback OnNewChannel { get; set; }

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Axtarış termini.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// New chat menu-su açıqdır?
    /// </summary>
    private bool showNewMenu = false;

    #endregion

    #region Unified List Model

    /// <summary>
    /// Conversation və Channel-ı birləşdirən model.
    /// Siyahıda unified şəkildə göstərmək üçün.
    /// </summary>
    private class UnifiedChatItem
    {
        /// <summary>
        /// Conversation və ya Channel ID-si.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Göstərilən ad (conversation: digər istifadəçi, channel: channel adı).
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Avatar URL-i (yalnız conversation üçün).
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Son mesajın məzmunu.
        /// </summary>
        public string? LastMessage { get; set; }

        /// <summary>
        /// Son aktivlik vaxtı (sıralama üçün).
        /// </summary>
        public DateTime? LastActivityTime { get; set; }

        /// <summary>
        /// Oxunmamış mesaj sayı.
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Bu item channel-dır? (false = conversation)
        /// </summary>
        public bool IsChannel { get; set; }

        /// <summary>
        /// Channel private-dır? (yalnız channel üçün)
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Channel üzv sayı (yalnız channel üçün).
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Draft var?
        /// </summary>
        public bool HasDraft { get; set; }

        /// <summary>
        /// Draft mətni.
        /// </summary>
        public string? DraftText { get; set; }

        /// <summary>
        /// Read Later marker ID-si.
        /// </summary>
        public Guid? LastReadLaterMessageId { get; set; }

        /// <summary>
        /// Son mesaj cari istifadəçi tərəfindən göndərilib?
        /// </summary>
        public bool IsMyLastMessage { get; set; }

        /// <summary>
        /// Son mesajın statusu (Sent, Delivered, Read).
        /// </summary>
        public string? LastMessageStatus { get; set; }

        /// <summary>
        /// Channel son mesajını göndərənin avatar URL-i.
        /// </summary>
        public string? LastMessageSenderAvatarUrl { get; set; }

        /// <summary>
        /// Orijinal DirectConversationDto (seçim üçün).
        /// </summary>
        public DirectConversationDto? DirectConversation { get; set; }

        /// <summary>
        /// Orijinal ChannelDto (seçim üçün).
        /// </summary>
        public ChannelDto? Channel { get; set; }
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Conversation və Channel-ların birləşmiş siyahısı.
    /// Son aktivliyə görə sıralanır.
    /// </summary>
    private IEnumerable<UnifiedChatItem> UnifiedList
    {
        get
        {
            var items = new List<UnifiedChatItem>();

            // Direct conversation-ları əlavə et
            foreach (var conv in Conversations)
            {
                var item = CreateConversationItem(conv);
                items.Add(item);
            }

            // Channel-ları əlavə et
            foreach (var channel in Channels)
            {
                var item = CreateChannelItem(channel);
                items.Add(item);
            }

            // Axtarış filtri
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                items = items.Where(i =>
                    i.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Son aktivliyə görə sırala (ən yeni birinci)
            return items.OrderByDescending(i => i.LastActivityTime ?? DateTime.MinValue);
        }
    }

    #endregion

    #region Item Creation Methods

    /// <summary>
    /// DirectConversationDto-dan UnifiedChatItem yaradır.
    /// </summary>
    private UnifiedChatItem CreateConversationItem(DirectConversationDto conv)
    {
        // Draft yoxlaması - seçili conversation-da draft gizlədilir
        var draftKey = $"conv_{conv.Id}";
        var isSelected = SelectedConversationId == conv.Id;
        MessageDrafts.TryGetValue(draftKey, out var draftText);
        var hasDraft = !isSelected && !string.IsNullOrWhiteSpace(draftText);

        return new UnifiedChatItem
        {
            Id = conv.Id,
            Name = conv.OtherUserDisplayName,
            AvatarUrl = conv.OtherUserAvatarUrl,
            LastMessage = conv.LastMessageContent,
            LastActivityTime = conv.LastMessageAtUtc,
            UnreadCount = conv.UnreadCount,
            IsChannel = false,
            HasDraft = hasDraft,
            DraftText = hasDraft ? draftText : null,
            LastReadLaterMessageId = conv.LastReadLaterMessageId,
            IsMyLastMessage = conv.LastMessageSenderId == UserState.UserId,
            LastMessageStatus = conv.LastMessageStatus,
            DirectConversation = conv
        };
    }

    /// <summary>
    /// ChannelDto-dan UnifiedChatItem yaradır.
    /// </summary>
    private UnifiedChatItem CreateChannelItem(ChannelDto channel)
    {
        // Draft yoxlaması - seçili channel-da draft gizlədilir
        var draftKey = $"chan_{channel.Id}";
        var isSelected = SelectedChannelId == channel.Id;
        MessageDrafts.TryGetValue(draftKey, out var draftText);
        var hasDraft = !isSelected && !string.IsNullOrWhiteSpace(draftText);

        return new UnifiedChatItem
        {
            Id = channel.Id,
            Name = channel.Name,
            LastMessage = channel.LastMessageContent,
            LastActivityTime = channel.LastMessageAtUtc ?? channel.CreatedAtUtc,
            UnreadCount = channel.UnreadCount,
            IsChannel = true,
            IsPrivate = channel.Type == ChannelType.Private,
            MemberCount = channel.MemberCount,
            HasDraft = hasDraft,
            DraftText = hasDraft ? draftText : null,
            LastReadLaterMessageId = channel.LastReadLaterMessageId,
            IsMyLastMessage = channel.LastMessageSenderId == UserState.UserId,
            LastMessageStatus = channel.LastMessageStatus,
            LastMessageSenderAvatarUrl = channel.LastMessageSenderAvatarUrl,
            Channel = channel
        };
    }

    #endregion

    #region Menu Methods

    /// <summary>
    /// New chat menu-sunu toggle edir.
    /// </summary>
    private void ToggleNewMenu()
    {
        showNewMenu = !showNewMenu;
    }

    /// <summary>
    /// New chat menu-sunu bağlayır.
    /// </summary>
    private void CloseNewMenu()
    {
        showNewMenu = false;
    }

    #endregion

    #region Search Methods

    /// <summary>
    /// Axtarışı təmizləyir.
    /// </summary>
    private void ClearSearch()
    {
        SearchTerm = "";
    }

    #endregion

    #region Selection Methods

    /// <summary>
    /// Conversation seçir.
    /// </summary>
    private async Task SelectConversationItem(UnifiedChatItem item)
    {
        if (item.DirectConversation != null)
        {
            await OnConversationSelected.InvokeAsync(item.DirectConversation);
        }
    }

    /// <summary>
    /// Channel seçir.
    /// </summary>
    private async Task SelectChannelItem(UnifiedChatItem item)
    {
        if (item.Channel != null)
        {
            await OnChannelSelected.InvokeAsync(item.Channel);
        }
    }

    /// <summary>
    /// New conversation click handler.
    /// </summary>
    private async Task OnNewConversationClick()
    {
        showNewMenu = false;
        await OnNewConversation.InvokeAsync();
    }

    /// <summary>
    /// New channel click handler.
    /// </summary>
    private async Task OnNewChannelClick()
    {
        showNewMenu = false;
        await OnNewChannel.InvokeAsync();
    }

    #endregion

    #region Formatting Methods

    /// <summary>
    /// Tarixi formatlanmış string-ə çevirir.
    /// "Now", "5m", "14:30", "Mon", "15/01/24"
    /// </summary>
    private string FormatTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1) return "Now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return dateTime.ToLocalTime().ToString("HH:mm");
        if (diff.TotalDays < 7) return dateTime.ToLocalTime().ToString("ddd", System.Globalization.CultureInfo.InvariantCulture);
        return dateTime.ToLocalTime().ToString("dd/MM/yy");
    }

    /// <summary>
    /// Mesajı qısaldır (35 simvol).
    /// </summary>
    private string TruncateMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "No messages yet";
        return message.Length > 35 ? message[..35] + "..." : message;
    }

    /// <summary>
    /// Mesaj statusuna görə icon qaytarır.
    /// </summary>
    private string GetStatusIcon(string status)
    {
        return status switch
        {
            "Sent" => Icons.Material.Filled.Check,           // Tək checkmark
            "Delivered" => Icons.Material.Filled.DoneAll,    // İkiqat checkmark (boz)
            "Read" => Icons.Material.Filled.DoneAll,         // İkiqat checkmark (mavi - CSS ilə)
            _ => Icons.Material.Filled.Check
        };
    }

    #endregion
}
