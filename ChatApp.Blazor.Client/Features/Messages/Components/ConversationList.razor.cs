using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using ChatApp.Blazor.Client.Features.Messages.Services;
using MudBlazor;
using System.Globalization;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class ConversationList : IAsyncDisposable
{
    #region Injected Services

    [Inject] private UserState UserState { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IChannelService ChannelService { get; set; } = default!;
    [Inject] private IConversationService ConversationService { get; set; } = default!;

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

    /// <summary>
    /// Conversation mute toggle edildiyi zaman callback.
    /// Parameter: (Guid conversationId, bool isMuted)
    /// </summary>
    [Parameter] public EventCallback<(Guid, bool)> OnConversationMuteToggled { get; set; }

    /// <summary>
    /// Conversation bağlandığında (hide olduqda) callback.
    /// </summary>
    [Parameter] public EventCallback OnConversationClosed { get; set; }

    /// <summary>
    /// Channel bağlandığında (hide olduqda) callback.
    /// </summary>
    [Parameter] public EventCallback OnChannelClosed { get; set; }

    /// <summary>
    /// "Find chats with this user" butonuna klik edildikdə callback.
    /// Parameter: Guid conversationId
    /// </summary>
    [Parameter] public EventCallback<Guid> OnFindChatsWithUser { get; set; }

    /// <summary>
    /// Channel leave edildiyi zaman callback (More menu Leave button).
    /// Parameter: Guid channelId
    /// </summary>
    [Parameter] public EventCallback<Guid> OnChannelLeave { get; set; }

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

        public Guid? LastMessageId { get; set; }

        public bool IsNotes { get; set; }

        public string? LastMessageSenderAvatarUrl { get; set; }

        public bool IsPinned { get; set; }

        public bool IsMuted { get; set; }

        public bool IsMarkedReadLater { get; set; }

        public DirectConversationDto? DirectConversation { get; set; }

        public ChannelDto? Channel { get; set; }
    }

    #endregion

    #region Lifecycle Methods

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

        // Sıralama: Əvvəlcə pinned conversations, sonra son aktivliyə görə
        items.Sort((a, b) =>
        {
            // Pinned conversations ən yuxarıda
            if (a.IsPinned && !b.IsPinned) return -1;
            if (!a.IsPinned && b.IsPinned) return 1;

            // Həm pinned və ya heç biri pinned deyilsə, son aktivliyə görə
            return (b.LastActivityTime ?? DateTime.MinValue).CompareTo(a.LastActivityTime ?? DateTime.MinValue);
        });

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
            LastMessageId = conv.LastMessageId,
            IsNotes = conv.IsNotes,
            IsPinned = conv.IsPinned,
            IsMuted = conv.IsMuted,
            IsMarkedReadLater = conv.IsMarkedReadLater,
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
            LastMessageId = channel.LastMessageId,
            LastMessageSenderAvatarUrl = channel.LastMessageSenderAvatarUrl,
            IsPinned = channel.IsPinned,
            IsMuted = channel.IsMuted,
            IsMarkedReadLater = channel.IsMarkedReadLater,
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
        SearchTerm = string.Empty;
    }

    #endregion

    #region Selection Methods

    /// <summary>
    /// Conversation seçir.
    /// Icon təmizləmə işi Messages.SelectDirectConversation metodunda edilir.
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

    private DotNetObjectReference<ConversationList>? _conversationMenuRef;

    /// <summary>
    /// More menu toggle.
    /// </summary>
    private async Task ToggleMoreMenu(Guid itemId)
    {
        if (showMoreMenu && moreMenuItemId == itemId)
        {
            CloseMoreMenu();
        }
        else
        {
            showMoreMenu = true;
            moreMenuItemId = itemId;

            // Setup outside click detection when opening menu
            // JS will automatically close all other open menus (including message menus)
            try
            {
                _conversationMenuRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setupConversationMenuOutsideClickHandler", itemId, _conversationMenuRef);
            }
            catch
            {
                // Silently handle JS interop errors
            }
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
    /// JS callback - called when clicking outside conversation more menu.
    /// </summary>
    [JSInvokable]
    public void OnConversationMenuOutsideClick()
    {
        if (showMoreMenu)
        {
            CloseMoreMenu();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Right-click event handler (context menu).
    /// </summary>
    private async Task HandleContextMenu(Guid itemId)
    {
        await ToggleMoreMenu(itemId);
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
        var localDateTime = dateTime.ToLocalTime();
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1) return "Now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m";
        if (localDateTime.Date == DateTime.Now.Date) return localDateTime.ToString("HH:mm");
        if (diff.TotalDays < 7) return localDateTime.ToString("ddd", CultureInfo.InvariantCulture);
        return localDateTime.ToString("dd/MM/yy");
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

    #region Menu Action Handlers

    /// <summary>
    /// Pin/Unpin channel handler.
    /// PERFORMANCE FIX: Single StateHasChanged, proper error handling.
    /// </summary>
    private async Task TogglePinChannel(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var originalChannel = Channels[index];
        var optimisticValue = !originalChannel.IsPinned;

        // Optimistic UI
        Channels[index] = originalChannel with { IsPinned = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        try
        {
            var result = await ChannelService.TogglePinChannelAsync(channelId);
            if (result.IsSuccess)
            {
                // Sync with backend
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsPinned = result.Value };
                    InvalidateCache();
                }
            }
            else
            {
                // Revert on failure
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsPinned = originalChannel.IsPinned };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Channels.Count && Channels[index].Id == channelId)
            {
                Channels[index] = Channels[index] with { IsPinned = originalChannel.IsPinned };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mute/Unmute channel handler.
    /// PERFORMANCE FIX: Single StateHasChanged, proper error handling.
    /// </summary>
    private async Task ToggleMuteChannel(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var originalChannel = Channels[index];
        var optimisticValue = !originalChannel.IsMuted;

        // Optimistic UI
        Channels[index] = originalChannel with { IsMuted = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        try
        {
            var result = await ChannelService.ToggleMuteChannelAsync(channelId);
            if (result.IsSuccess)
            {
                // Sync with backend
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsMuted = result.Value };
                    InvalidateCache();
                }

                // Notify parent (Messages.razor) to update selectedConversationIsMuted
                await OnConversationMuteToggled.InvokeAsync((channelId, result.Value));
            }
            else
            {
                // Revert on failure
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsMuted = originalChannel.IsMuted };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Channels.Count && Channels[index].Id == channelId)
            {
                Channels[index] = Channels[index] with { IsMuted = originalChannel.IsMuted };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mark channel as read later handler.
    /// PERFORMANCE FIX: Reduced console logging, single StateHasChanged call, proper error handling.
    /// </summary>
    private async Task ToggleMarkChannelAsReadLater(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var originalChannel = Channels[index];
        var optimisticValue = !originalChannel.IsMarkedReadLater;

        // Optimistic UI: Toggle immediately
        Channels[index] = originalChannel with { IsMarkedReadLater = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        // API call
        try
        {
            var result = await ChannelService.ToggleMarkChannelAsReadLaterAsync(channelId);

            if (result.IsSuccess)
            {
                // Sync with backend value (in case of race condition)
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsMarkedReadLater = result.Value };
                    InvalidateCache();
                }
            }
            else
            {
                // Revert on failure
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with { IsMarkedReadLater = originalChannel.IsMarkedReadLater };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Channels.Count && Channels[index].Id == channelId)
            {
                Channels[index] = Channels[index] with { IsMarkedReadLater = originalChannel.IsMarkedReadLater };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Pin/Unpin conversation handler.
    /// PERFORMANCE FIX: Single StateHasChanged, proper error handling.
    /// </summary>
    private async Task TogglePinConversation(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var originalConversation = Conversations[index];
        var optimisticValue = !originalConversation.IsPinned;

        // Optimistic UI
        Conversations[index] = originalConversation with { IsPinned = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        try
        {
            var result = await ConversationService.TogglePinConversationAsync(conversationId);
            if (result.IsSuccess)
            {
                // Sync with backend (race condition protection)
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsPinned = result.Value };
                    InvalidateCache();
                }
            }
            else
            {
                // Revert on failure
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsPinned = originalConversation.IsPinned };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Conversations.Count && Conversations[index].Id == conversationId)
            {
                Conversations[index] = Conversations[index] with { IsPinned = originalConversation.IsPinned };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mute/Unmute conversation handler.
    /// PERFORMANCE FIX: Single StateHasChanged, proper error handling.
    /// </summary>
    private async Task ToggleMuteConversation(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var originalConversation = Conversations[index];
        var optimisticValue = !originalConversation.IsMuted;

        // Optimistic UI
        Conversations[index] = originalConversation with { IsMuted = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        try
        {
            var result = await ConversationService.ToggleMuteConversationAsync(conversationId);
            if (result.IsSuccess)
            {
                // Sync with backend
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsMuted = result.Value };
                    InvalidateCache();
                }

                // Notify parent (Messages.razor) to update selectedConversationIsMuted
                await OnConversationMuteToggled.InvokeAsync((conversationId, result.Value));
            }
            else
            {
                // Revert on failure
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsMuted = originalConversation.IsMuted };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Conversations.Count && Conversations[index].Id == conversationId)
            {
                Conversations[index] = Conversations[index] with { IsMuted = originalConversation.IsMuted };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mark conversation as read later handler.
    /// PERFORMANCE FIX: Removed console logging, single StateHasChanged, proper error handling.
    /// </summary>
    private async Task ToggleMarkConversationAsReadLater(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var originalConversation = Conversations[index];
        var optimisticValue = !originalConversation.IsMarkedReadLater;

        // Optimistic UI
        Conversations[index] = originalConversation with { IsMarkedReadLater = optimisticValue };
        InvalidateCache();
        StateHasChanged();

        try
        {
            var result = await ConversationService.ToggleMarkConversationAsReadLaterAsync(conversationId);
            if (result.IsSuccess)
            {
                // Sync with backend
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsMarkedReadLater = result.Value };
                    InvalidateCache();
                }
            }
            else
            {
                // Revert on failure
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with { IsMarkedReadLater = originalConversation.IsMarkedReadLater };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Conversations.Count && Conversations[index].Id == conversationId)
            {
                Conversations[index] = Conversations[index] with { IsMarkedReadLater = originalConversation.IsMarkedReadLater };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mark all as read handler for channels - marks all unread messages as read and clears ALL read later flags.
    /// Clears both conversation-level (IsMarkedReadLater) and message-level (LastReadLaterMessageId) marks.
    /// PERFORMANCE FIX: Removed console logging, single StateHasChanged.
    /// </summary>
    private async Task MarkChannelAllAsRead(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var originalChannel = Channels[index];

        // Optimistic UI: Clear all flags immediately
        Channels[index] = originalChannel with
        {
            IsMarkedReadLater = false,
            LastReadLaterMessageId = null,
            UnreadCount = 0
        };
        InvalidateCache();
        StateHasChanged();

        // Call backend API to mark all messages as read and clear all flags
        try
        {
            var result = await ChannelService.MarkAllChannelMessagesAsReadAsync(channelId);
            if (!result.IsSuccess)
            {
                // Revert on failure
                if (index < Channels.Count && Channels[index].Id == channelId)
                {
                    Channels[index] = Channels[index] with
                    {
                        IsMarkedReadLater = originalChannel.IsMarkedReadLater,
                        LastReadLaterMessageId = originalChannel.LastReadLaterMessageId,
                        UnreadCount = originalChannel.UnreadCount
                    };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Channels.Count && Channels[index].Id == channelId)
            {
                Channels[index] = Channels[index] with
                {
                    IsMarkedReadLater = originalChannel.IsMarkedReadLater,
                    LastReadLaterMessageId = originalChannel.LastReadLaterMessageId,
                    UnreadCount = originalChannel.UnreadCount
                };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mark all as read handler for conversations - marks all unread messages as read and clears ALL read later flags.
    /// Clears both conversation-level (IsMarkedReadLater) and message-level (LastReadLaterMessageId) marks.
    /// PERFORMANCE FIX: Removed console logging, single StateHasChanged.
    /// </summary>
    private async Task MarkConversationAllAsRead(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var originalConversation = Conversations[index];

        // Optimistic UI: Clear all flags immediately
        Conversations[index] = originalConversation with
        {
            IsMarkedReadLater = false,
            LastReadLaterMessageId = null,
            UnreadCount = 0
        };
        InvalidateCache();
        StateHasChanged();

        // API call
        try
        {
            var result = await ConversationService.MarkAllMessagesAsReadAsync(conversationId);
            if (!result.IsSuccess)
            {
                // Revert on failure
                if (index < Conversations.Count && Conversations[index].Id == conversationId)
                {
                    Conversations[index] = Conversations[index] with
                    {
                        IsMarkedReadLater = originalConversation.IsMarkedReadLater,
                        LastReadLaterMessageId = originalConversation.LastReadLaterMessageId,
                        UnreadCount = originalConversation.UnreadCount
                    };
                    InvalidateCache();
                }
            }
        }
        catch
        {
            // Revert on exception
            if (index < Conversations.Count && Conversations[index].Id == conversationId)
            {
                Conversations[index] = Conversations[index] with
                {
                    IsMarkedReadLater = originalConversation.IsMarkedReadLater,
                    LastReadLaterMessageId = originalConversation.LastReadLaterMessageId,
                    UnreadCount = originalConversation.UnreadCount
                };
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Hide channel handler - removes channel from list until new message arrives.
    /// If currently selected, closes the channel first.
    /// </summary>
    private async Task HideChannel(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var channel = Channels[index];

        // If this channel is currently selected, close it first
        var wasSelected = SelectedChannelId == channelId;

        // Decrement global unread count if channel has unread messages
        if (channel.UnreadCount > 0)
        {
            AppState.DecrementUnreadMessages(channel.UnreadCount);
        }

        // Optimistic UI: Remove from list immediately
        Channels.RemoveAt(index);
        InvalidateCache();

        // Close the channel if it was selected
        if (wasSelected)
        {
            await OnChannelClosed.InvokeAsync();
        }

        StateHasChanged();

        try
        {
            var result = await ChannelService.HideChannelAsync(channelId);
            if (!result.IsSuccess)
            {
                // Reload on failure (backend didn't hide, need to show again)
                await OnInitializedAsync();
            }
        }
        catch
        {
            // Reload on exception
            await OnInitializedAsync();
        }
    }

    /// <summary>
    /// Hide conversation handler - removes conversation from list until new message arrives.
    /// If currently selected, closes the conversation first.
    /// </summary>
    private async Task HideConversation(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var conversation = Conversations[index];

        // If this conversation is currently selected, close it first
        var wasSelected = SelectedConversationId == conversationId;

        // Decrement global unread count if conversation has unread messages
        if (conversation.UnreadCount > 0)
        {
            AppState.DecrementUnreadMessages(conversation.UnreadCount);
        }

        // Optimistic UI: Remove from list immediately
        Conversations.RemoveAt(index);
        InvalidateCache();

        // Close the conversation if it was selected
        if (wasSelected)
        {
            await OnConversationClosed.InvokeAsync();
        }

        StateHasChanged();

        try
        {
            var result = await ConversationService.HideConversationAsync(conversationId);
            if (!result.IsSuccess)
            {
                // Reload on failure (backend didn't hide, need to show again)
                await OnInitializedAsync();
            }
        }
        catch
        {
            // Reload on exception
            await OnInitializedAsync();
        }
    }

    /// <summary>
    /// "Find chats with this user" handler.
    /// </summary>
    private async Task HandleFindChatsWithUser(Guid conversationId)
    {
        CloseMoreMenu();
        await OnFindChatsWithUser.InvokeAsync(conversationId);
    }

    #endregion

    #region IAsyncDisposable

    /// <summary>
    /// Dispose - cleanup resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_conversationMenuRef != null)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("disposeConversationMenuOutsideClickHandler", moreMenuItemId);
                _conversationMenuRef.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}