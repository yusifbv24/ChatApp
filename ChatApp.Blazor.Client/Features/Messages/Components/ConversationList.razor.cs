using Microsoft.AspNetCore.Components;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using ChatApp.Blazor.Client.Features.Messages.Services;
using MudBlazor;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
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

    /// <summary>
    /// Department əməkdaşları siyahısı (conversation olmayan istifadəçilər).
    /// </summary>
    [Parameter] public List<DepartmentUserDto> DepartmentUsers { get; set; } = [];

    /// <summary>
    /// Daha çox item var? (infinite scroll üçün)
    /// </summary>
    [Parameter] public bool HasMoreItems { get; set; }

    /// <summary>
    /// Növbəti səhifə yüklənir?
    /// </summary>
    [Parameter] public bool IsLoadingMore { get; set; }

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

    /// <summary>
    /// Department istifadəçisi seçildiyi zaman callback (conversation yaratmaq üçün).
    /// </summary>
    [Parameter] public EventCallback<DepartmentUserDto> OnDepartmentUserSelected { get; set; }

    /// <summary>
    /// Daha çox item yüklə callback (infinite scroll).
    /// </summary>
    [Parameter] public EventCallback OnLoadMore { get; set; }

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Search mode aktivdir? (input-a focus olduqda true).
    /// </summary>
    private bool isSearchMode = false;

    /// <summary>
    /// User search nəticələri.
    /// </summary>
    private List<UserSearchResultDto> userSearchResults = [];

    /// <summary>
    /// Channel search nəticələri.
    /// </summary>
    private List<ChannelDto> channelSearchResults = [];

    /// <summary>
    /// Search yüklənir?
    /// </summary>
    private bool isSearchingUsers = false;

    /// <summary>
    /// User search üçün debounce timer.
    /// </summary>
    private CancellationTokenSource? _userSearchCts;

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
                // Search mode-da API-dən user axtar
                if (isSearchMode)
                {
                    _ = SearchUsersAsync(value);
                }
                else
                {
                    InvalidateCache();
                }
            }
        }
    }

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

    /// <summary>
    /// Filter panel açıqdır?
    /// </summary>
    private bool showFilterPanel = false;

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

    /// <summary>
    /// Əvvəlki DepartmentUsers reference-i.
    /// </summary>
    private List<DepartmentUserDto>? _previousDepartmentUsers;

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

        public DepartmentUserDto? DepartmentUser { get; set; }

        public bool IsDepartmentUser { get; set; }
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
            !ReferenceEquals(DepartmentUsers, _previousDepartmentUsers) ||
            SelectedConversationId != _previousSelectedConversationId ||
            SelectedChannelId != _previousSelectedChannelId)
        {
            InvalidateCache();
            _previousConversations = Conversations;
            _previousChannels = Channels;
            _previousDepartmentUsers = DepartmentUsers;
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
        var items = new List<UnifiedChatItem>(Conversations.Count + Channels.Count + DepartmentUsers.Count);

        // Conversation-lardakı istifadəçi ID-lərini topla (department users ilə overlap olmasın)
        var conversationUserIds = new HashSet<Guid>(Conversations.Select(c => c.OtherUserId));

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

        // Department istifadəçilərini əlavə et (yalnız conversation olmayanlari)
        foreach (var user in DepartmentUsers)
        {
            if (!conversationUserIds.Contains(user.UserId))
            {
                items.Add(CreateDepartmentUserItem(user));
            }
        }

        // Axtarış filtri
        if (!string.IsNullOrEmpty(_searchTerm))
        {
            items = items.Where(i =>
                i.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // Sıralama: Pinned → aktiv conversations/channels → department users
        items.Sort((a, b) =>
        {
            // Department users həmişə sonuncudur
            if (a.IsDepartmentUser && !b.IsDepartmentUser) return 1;
            if (!a.IsDepartmentUser && b.IsDepartmentUser) return -1;
            if (a.IsDepartmentUser && b.IsDepartmentUser)
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

            // Pinned conversations ən yuxarıda
            if (a.IsPinned && !b.IsPinned) return -1;
            if (!a.IsPinned && b.IsPinned) return 1;

            // Son aktivliyə görə
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
            Name = conv.IsNotes ? "Notes" : conv.OtherUserFullName,
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
            AvatarUrl = channel.AvatarUrl,
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

    /// <summary>
    /// DepartmentUserDto-dan UnifiedChatItem yaradır.
    /// </summary>
    private UnifiedChatItem CreateDepartmentUserItem(DepartmentUserDto user)
    {
        return new UnifiedChatItem
        {
            Id = user.UserId,
            OtherUserId = user.UserId,
            Name = user.FullName,
            AvatarUrl = user.AvatarUrl,
            LastMessage = user.PositionName, // Vəzifə adı göstərilir
            IsDepartmentUser = true,
            DepartmentUser = user
        };
    }

    #endregion

    #region Search Methods

    /// <summary>
    /// Search input-a focus olduqda - search mode aktivləşir.
    /// </summary>
    private void EnterSearchMode()
    {
        isSearchMode = true;
    }

    /// <summary>
    /// Search input-dan blur olduqda - search mode bağlanır (əgər boşdursa).
    /// </summary>
    private async Task HandleSearchBlur()
    {
        // Kiçik delay - user search result-a klik edə bilsin
        await Task.Delay(200);

        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            ExitSearchMode();
        }
    }

    /// <summary>
    /// Search mode-dan çıxır.
    /// </summary>
    private void ExitSearchMode()
    {
        isSearchMode = false;
        SearchTerm = string.Empty;
        userSearchResults.Clear();
        channelSearchResults.Clear();
        _userSearchCts?.Cancel();
    }

    /// <summary>
    /// Axtarışı təmizləyir.
    /// </summary>
    private void ClearSearch()
    {
        ExitSearchMode();
    }

    /// <summary>
    /// Unified search - həm users, həm channels (API-dən).
    /// </summary>
    private async Task SearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            userSearchResults.Clear();
            channelSearchResults.Clear();
            return;
        }

        // Əvvəlki sorğunu cancel et
        _userSearchCts?.Cancel();
        _userSearchCts = new CancellationTokenSource();
        var token = _userSearchCts.Token;

        try
        {
            // Debounce - 300ms gözlə
            await Task.Delay(300, token);

            isSearchingUsers = true;
            StateHasChanged();

            // Paralel olaraq həm users, həm channels axtar
            var userTask = ConversationService.SearchUsersAsync(query);
            var channelTask = ChannelService.SearchChannelsAsync(query);

            await Task.WhenAll(userTask, channelTask);

            if (token.IsCancellationRequested) return;

            var userResult = await userTask;
            var channelResult = await channelTask;

            userSearchResults = userResult.IsSuccess && userResult.Value != null
                ? userResult.Value
                : [];

            channelSearchResults = channelResult.IsSuccess && channelResult.Value != null
                ? channelResult.Value
                : [];
        }
        catch (TaskCanceledException)
        {
            // Debounce cancel - normal
        }
        catch
        {
            userSearchResults.Clear();
            channelSearchResults.Clear();
        }
        finally
        {
            isSearchingUsers = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Search result-dan user seçildi.
    /// </summary>
    private async Task SelectSearchedUser(UserSearchResultDto user)
    {
        // DepartmentUserDto-ya çevir (record constructor istifadə et)
        var deptUser = new DepartmentUserDto(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            AvatarUrl: user.AvatarUrl,
            PositionName: user.Position,
            DepartmentId: null,
            DepartmentName: null
        );

        // Search mode-dan çıx
        ExitSearchMode();

        // Parent-ə bildir
        await OnDepartmentUserSelected.InvokeAsync(deptUser);
    }

    /// <summary>
    /// Search result-dan channel seçildi.
    /// </summary>
    private async Task SelectSearchedChannel(ChannelDto channel)
    {
        // Search mode-dan çıx
        ExitSearchMode();

        // Parent-ə bildir
        await OnChannelSelected.InvokeAsync(channel);
    }

    #endregion

    #region Filter Panel Methods

    private DotNetObjectReference<ConversationList>? _filterMenuRef;

    /// <summary>
    /// Filter paneli açıb-bağlayır.
    /// JS _allMenuHandlers pattern digər açıq panelləri avtomatik bağlayır.
    /// </summary>
    private async Task ToggleFilterPanel()
    {
        if (showFilterPanel)
        {
            showFilterPanel = false;
        }
        else
        {
            showFilterPanel = true;

            // JS handler digər menu-ları avtomatik bağlayır (_allMenuHandlers pattern)
            try
            {
                _filterMenuRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setupFilterMenuOutsideClickHandler", _filterMenuRef);
            }
            catch
            {
                // Silently handle JS interop errors
            }
        }
    }

    /// <summary>
    /// JS callback - filter panel dışına klik edildikdə və ya digər menu açıldıqda.
    /// </summary>
    [JSInvokable]
    public void OnFilterMenuOutsideClick()
    {
        if (showFilterPanel)
        {
            showFilterPanel = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Bütün oxunmamış mesajları oxunmuş kimi işarələyir.
    /// </summary>
    private async Task MarkAllAsRead()
    {
        showFilterPanel = false;

        // Bütün conversation-ları mark et
        foreach (var conv in Conversations.Where(c => c.UnreadCount > 0).ToList())
        {
            var result = await ConversationService.MarkAllMessagesAsReadAsync(conv.Id);
            if (result.IsSuccess)
            {
                var index = Conversations.FindIndex(c => c.Id == conv.Id);
                if (index >= 0)
                {
                    Conversations[index] = Conversations[index] with { UnreadCount = 0, IsMarkedReadLater = false, LastReadLaterMessageId = null };
                }
            }
        }

        // Bütün channel-ları mark et
        foreach (var channel in Channels.Where(c => c.UnreadCount > 0).ToList())
        {
            var result = await ChannelService.MarkAllChannelMessagesAsReadAsync(channel.Id);
            if (result.IsSuccess)
            {
                var index = Channels.FindIndex(c => c.Id == channel.Id);
                if (index >= 0)
                {
                    Channels[index] = Channels[index] with { UnreadCount = 0, IsMarkedReadLater = false, LastReadLaterMessageId = null };
                }
            }
        }

        // Global unread badge-i yenilə
        AppState.UnreadMessageCount = 0;

        InvalidateCache();
        StateHasChanged();
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
    /// Department istifadəçisi seçir - conversation yaradılır.
    /// </summary>
    private async Task SelectDepartmentUserItem(UnifiedChatItem item)
    {
        if (item.DepartmentUser != null)
        {
            await OnDepartmentUserSelected.InvokeAsync(item.DepartmentUser);
        }
    }

    /// <summary>
    /// New channel click handler.
    /// </summary>
    private async Task OnNewChannelClick()
    {
        await OnNewChannel.InvokeAsync();
    }

    #endregion

    #region More Menu Methods

    private DotNetObjectReference<ConversationList>? _conversationMenuRef;

    /// <summary>
    /// More menu toggle.
    /// JS _allMenuHandlers pattern digər açıq panelləri avtomatik bağlayır.
    /// </summary>
    private async Task ToggleMoreMenu(Guid itemId)
    {
        if (showMoreMenu && moreMenuItemId == itemId)
        {
            showMoreMenu = false;
            moreMenuItemId = null;
        }
        else
        {
            showMoreMenu = true;
            moreMenuItemId = itemId;

            // JS handler digər menu-ları avtomatik bağlayır (_allMenuHandlers pattern)
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
    /// JS callback - more menu dışına klik edildikdə və ya digər menu açıldıqda.
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
    /// Scroll event handler - infinite scroll + menu bağlama.
    /// </summary>
    private async Task HandleScroll(EventArgs e)
    {
        if (showMoreMenu)
        {
            CloseMoreMenu();
        }

        // Infinite scroll: aşağıya çatanda daha çox yüklə
        if (HasMoreItems && !IsLoadingMore)
        {
            try
            {
                var scrollInfo = await JSRuntime.InvokeAsync<ScrollInfo>("chatAppUtils.getScrollInfo", ".conversation-items");
                if (scrollInfo != null && scrollInfo.ScrollTop + scrollInfo.ClientHeight >= scrollInfo.ScrollHeight - 100)
                {
                    await OnLoadMore.InvokeAsync();
                }
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
    }

    private record ScrollInfo(double ScrollTop, double ClientHeight, double ScrollHeight);

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
            "Pending" => Icons.Material.Filled.Schedule,     // Saat ikonu (göndərilir...)
            "Sent" => Icons.Material.Filled.Check,           // Tək checkmark
            "Delivered" => Icons.Material.Filled.DoneAll,    // İkiqat checkmark (boz)
            "Read" => Icons.Material.Filled.DoneAll,         // İkiqat checkmark (mavi - CSS ilə)
            "Failed" => Icons.Material.Filled.ErrorOutline,  // Xəta ikonu
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
    /// Also decrements global unread message count in AppState.
    /// </summary>
    private async Task MarkChannelAllAsRead(Guid channelId)
    {
        var index = Channels.FindIndex(c => c.Id == channelId);
        if (index < 0) return;

        var originalChannel = Channels[index];
        var unreadCountToDecrement = originalChannel.UnreadCount;

        // Optimistic UI: Clear all flags immediately
        Channels[index] = originalChannel with
        {
            IsMarkedReadLater = false,
            LastReadLaterMessageId = null,
            UnreadCount = 0
        };

        // Decrement global unread count
        if (unreadCountToDecrement > 0)
        {
            AppState.DecrementUnreadMessages(unreadCountToDecrement);
        }

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

                    // Revert global unread count
                    if (unreadCountToDecrement > 0)
                    {
                        AppState.UnreadMessageCount += unreadCountToDecrement;
                    }

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

                // Revert global unread count
                if (unreadCountToDecrement > 0)
                {
                    AppState.UnreadMessageCount += unreadCountToDecrement;
                }

                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Mark all as read handler for conversations - marks all unread messages as read and clears ALL read later flags.
    /// Clears both conversation-level (IsMarkedReadLater) and message-level (LastReadLaterMessageId) marks.
    /// Also decrements global unread message count in AppState.
    /// </summary>
    private async Task MarkConversationAllAsRead(Guid conversationId)
    {
        var index = Conversations.FindIndex(c => c.Id == conversationId);
        if (index < 0) return;

        var originalConversation = Conversations[index];
        var unreadCountToDecrement = originalConversation.UnreadCount;

        // Optimistic UI: Clear all flags immediately
        Conversations[index] = originalConversation with
        {
            IsMarkedReadLater = false,
            LastReadLaterMessageId = null,
            UnreadCount = 0
        };

        // Decrement global unread count
        if (unreadCountToDecrement > 0)
        {
            AppState.DecrementUnreadMessages(unreadCountToDecrement);
        }

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

                    // Revert global unread count
                    if (unreadCountToDecrement > 0)
                    {
                        AppState.UnreadMessageCount += unreadCountToDecrement;
                    }

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

                // Revert global unread count
                if (unreadCountToDecrement > 0)
                {
                    AppState.UnreadMessageCount += unreadCountToDecrement;
                }

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