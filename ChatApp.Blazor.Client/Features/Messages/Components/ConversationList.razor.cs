using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using ChatApp.Blazor.Client.Helpers;
using MudBlazor;
using System.Globalization;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class ConversationList : IAsyncDisposable
{
    #region Injected Services

    [Inject] private UserState UserState { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    #endregion

    #region Parameters - Data

    /// <summary>
    /// Direct conversation siyahısı.
    /// </summary>
    [Parameter] public List<DirectConversationDto> Conversations { get; set; } = [];

    /// <summary>
    /// Channel siyahısı.
    /// </summary>
    [Parameter] public List<ChannelDto> Channels { get; set; } = [];

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
    [Parameter] public Dictionary<Guid, bool> ConversationTypingState { get; set; } = [];

    /// <summary>
    /// Hər channel üçün typing edən istifadəçilər.
    /// Key: ChannelId, Value: List of display names
    /// </summary>
    [Parameter] public Dictionary<Guid, List<string>> ChannelTypingUsers { get; set; } = [];

    #endregion

    #region Parameters - Draft

    /// <summary>
    /// Mesaj draft-ları.
    /// Key: "conv_{id}" və ya "chan_{id}", Value: draft text
    /// </summary>
    [Parameter] public Dictionary<string, string> MessageDrafts { get; set; } = [];

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
    private string _searchTerm = string.Empty;
    private string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm != value)
            {
                _searchTerm = value;
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// New chat menu-su açıqdır?
    /// </summary>
    private bool showNewMenu = false;

    /// <summary>
    /// More menu açıqdır?
    /// </summary>
    private bool showMoreMenu = false;

    /// <summary>
    /// More menu-nun açıq olduğu item ID-si.
    /// </summary>
    private Guid? moreMenuItemId = null;

    /// <summary>
    /// Hover edən conversation item ID-si.
    /// </summary>
    private Guid? hoveredItemId = null;

    #endregion

    #region Private Fields - Cache

    /// <summary>
    /// Cached unified list - yalnız data dəyişdikdə yenilənir.
    /// </summary>
    private List<UnifiedChatItem>? _cachedUnifiedList;

    /// <summary>
    /// Cache-in etibarlı olub-olmadığı.
    /// </summary>
    private bool _isCacheValid = false;

    /// <summary>
    /// Əvvəlki Conversations reference-i (dəyişiklik detect üçün).
    /// </summary>
    private List<DirectConversationDto>? _previousConversations;

    /// <summary>
    /// Əvvəlki Channels reference-i (dəyişiklik detect üçün).
    /// </summary>
    private List<ChannelDto>? _previousChannels;

    /// <summary>
    /// Əvvəlki seçilmiş conversation ID.
    /// </summary>
    private Guid? _previousSelectedConversationId;

    /// <summary>
    /// Əvvəlki seçilmiş channel ID.
    /// </summary>
    private Guid? _previousSelectedChannelId;

    #endregion

    #region Unified List Model

    /// <summary>
    /// Conversation və Channel-ı birləşdirən model.
    /// Siyahıda unified şəkildə göstərmək üçün.
    /// </summary>
    private class UnifiedChatItem
    {
        public Guid Id { get; set; }

        public Guid? OtherUserId { get; set; } // DirectMessage üçün qarşı tərəfin ID-si (avatar rəngi üçün)

        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? LastMessage { get; set; }

        public DateTime? LastActivityTime { get; set; }

        public int UnreadCount { get; set; }

        public bool HasUnreadMentions { get; set; }

        public bool IsChannel { get; set; }

        public bool IsPrivate { get; set; }

        public int MemberCount { get; set; }

        public bool HasDraft { get; set; }

        public string? DraftText { get; set; }

        public Guid? LastReadLaterMessageId { get; set; }

        public bool IsMyLastMessage { get; set; }

        public string? LastMessageStatus { get; set; }

        public bool IsNotes { get; set; }

        public string? LastMessageSenderAvatarUrl { get; set; }

        public DirectConversationDto? DirectConversation { get; set; }

        public ChannelDto? Channel { get; set; }
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Reference to this component for JS callbacks.
    /// </summary>
    private DotNetObjectReference<ConversationList>? _dotNetRef;

    /// <summary>
    /// Flag to track if JS listeners are initialized.
    /// </summary>
    private bool _jsListenersInitialized = false;

    /// <summary>
    /// Parameter dəyişiklikləri olduqda cache-i yenilə.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Data reference dəyişibsə cache-i invalidate et
        if (!ReferenceEquals(Conversations, _previousConversations) ||
            !ReferenceEquals(Channels, _previousChannels) ||
            SelectedConversationId != _previousSelectedConversationId ||
            SelectedChannelId != _previousSelectedChannelId)
        {
            InvalidateCache();
            _previousConversations = Conversations;
            _previousChannels = Channels;
            _previousSelectedConversationId = SelectedConversationId;
            _previousSelectedChannelId = SelectedChannelId;
        }
    }

    /// <summary>
    /// After render - initialize JS scroll listeners.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_jsListenersInitialized)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("conversationListHelper.initialize", _dotNetRef);
            _jsListenersInitialized = true;
        }
    }

    /// <summary>
    /// JS callback - called when window scrolls or clicks outside.
    /// </summary>
    [JSInvokable]
    public void OnGlobalInteraction()
    {
        if (showMoreMenu)
        {
            CloseMoreMenu();
            StateHasChanged();
        }
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Cache-i etibarsız edir - növbəti access-də yenidən hesablanacaq.
    /// </summary>
    private void InvalidateCache()
    {
        _isCacheValid = false;
    }

    /// <summary>
    /// Cache-i yenidən qurur.
    /// </summary>
    private void RebuildCache()
    {
        var items = new List<UnifiedChatItem>(Conversations.Count + Channels.Count);

        // Direct conversation-ları əlavə et
        foreach (var conv in Conversations)
        {
            items.Add(CreateConversationItem(conv));
        }

        // Channel-ları əlavə et
        foreach (var channel in Channels)
        {
            items.Add(CreateChannelItem(channel));
        }

        // Axtarış filtri
        if (!string.IsNullOrEmpty(_searchTerm))
        {
            items = items.Where(i =>
                i.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // Son aktivliyə görə sırala (ən yeni birinci)
        items.Sort((a, b) =>
            (b.LastActivityTime ?? DateTime.MinValue).CompareTo(a.LastActivityTime ?? DateTime.MinValue));

        _cachedUnifiedList = items;
        _isCacheValid = true;
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Conversation və Channel-ların birləşmiş siyahısı.
    /// Cache-lənmiş - yalnız data dəyişdikdə yenidən hesablanır.
    /// </summary>
    private List<UnifiedChatItem> UnifiedList
    {
        get
        {
            if (!_isCacheValid || _cachedUnifiedList == null)
            {
                RebuildCache();
            }
            return _cachedUnifiedList!;
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
            OtherUserId = conv.OtherUserId, // Avatar rəngi üçün qarşı tərəfin ID-si
            Name = conv.IsNotes ? "Notes" : conv.OtherUserDisplayName,
            AvatarUrl = conv.OtherUserAvatarUrl,
            LastMessage = conv.LastMessageContent,
            LastActivityTime = conv.LastMessageAtUtc,
            UnreadCount = conv.UnreadCount,
            HasUnreadMentions = conv.HasUnreadMentions,
            IsChannel = false,
            HasDraft = hasDraft,
            DraftText = hasDraft ? draftText : null,
            LastReadLaterMessageId = conv.LastReadLaterMessageId,
            IsMyLastMessage = conv.LastMessageSenderId == UserState.UserId,
            LastMessageStatus = conv.LastMessageStatus,
            IsNotes = conv.IsNotes,
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
            HasUnreadMentions = channel.HasUnreadMentions,
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

    #region More Menu Methods

    /// <summary>
    /// More menu toggle.
    /// </summary>
    private void ToggleMoreMenu(Guid itemId, bool isChannel)
    {
        if (showMoreMenu && moreMenuItemId == itemId)
        {
            CloseMoreMenu();
        }
        else
        {
            showMoreMenu = true;
            moreMenuItemId = itemId;
        }
    }

    /// <summary>
    /// More menu-nu bağlayır.
    /// </summary>
    private void CloseMoreMenu()
    {
        showMoreMenu = false;
        moreMenuItemId = null;
    }

    /// <summary>
    /// Right-click event handler (context menu).
    /// </summary>
    private void HandleContextMenu(Guid itemId, bool isChannel)
    {
        ToggleMoreMenu(itemId, isChannel);
    }

    /// <summary>
    /// Hover event - item üzərinə gəldikdə.
    /// </summary>
    private void HandleMouseEnter(Guid itemId)
    {
        hoveredItemId = itemId;
    }

    /// <summary>
    /// Hover event - item-dan çıxdıqda.
    /// </summary>
    private void HandleMouseLeave()
    {
        hoveredItemId = null;
    }

    /// <summary>
    /// Scroll event handler - scroll edərkən menu bağlanır.
    /// </summary>
    private void HandleScroll()
    {
        if (showMoreMenu)
        {
            CloseMoreMenu();
        }
    }

    #endregion

    #region Formatting Methods

    /// <summary>
    /// Tarixi formatlanmış string-ə çevirir.
    /// "Now", "5m", "14:30", "Mon", "15/01/24"
    /// </summary>
    private static string FormatTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1) return "Now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return dateTime.ToLocalTime().ToString("HH:mm");
        if (diff.TotalDays < 7) return dateTime.ToLocalTime().ToString("ddd", CultureInfo.InvariantCulture);
        return dateTime.ToLocalTime().ToString("dd/MM/yy");
    }

    /// <summary>
    /// Mesajı qısaldır (35 simvol).
    /// </summary>
    private static string TruncateMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "No messages yet";
        return message.Length > 35 ? message[..35] + "..." : message;
    }

    /// <summary>
    /// Mesaj statusuna görə icon qaytarır.
    /// </summary>
    private static string GetStatusIcon(string status)
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

    #region IAsyncDisposable

    /// <summary>
    /// Dispose - cleanup JS listeners and DotNetObjectReference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_jsListenersInitialized && _dotNetRef != null)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("menuPanelHelper.unregister", _dotNetRef);
            }
            catch (JSDisconnectedException)
            {
                // Ignore - circuit already disconnected
            }
        }

        _dotNetRef?.Dispose();
    }

    #endregion
}