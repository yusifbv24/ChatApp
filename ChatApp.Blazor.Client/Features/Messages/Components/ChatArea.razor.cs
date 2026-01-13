using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Files;
using System.Globalization;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class ChatArea : IAsyncDisposable
{
    #region Injected Services

    [Inject] private IJSRuntime JS { get; set; } = default!;

    #endregion

    #region Parameters - Core State

    /// <summary>
    /// Chat sahəsinin boş olub-olmadığını göstərir.
    /// </summary>
    [Parameter] public bool IsEmpty { get; set; } = true;

    /// <summary>
    /// Direct message və ya channel olduğunu ayırd edir.
    /// </summary>
    [Parameter] public bool IsDirectMessage { get; set; } = true;

    /// <summary>
    /// Hazırki istifadəçinin ID-si.
    /// Mesajların "own" və ya "other" olduğunu müəyyən etmək üçün istifadə olunur.
    /// </summary>
    [Parameter] public Guid CurrentUserId { get; set; }

    /// <summary>
    /// Direct conversation ID-si (DM üçün).
    /// </summary>
    [Parameter] public Guid? ConversationId { get; set; }

    /// <summary>
    /// Channel ID-si (channel üçün).
    /// </summary>
    [Parameter] public Guid? ChannelId { get; set; }

    /// <summary>
    /// Mesaj cache version - mesaj edit/delete/reaction/pin olduqda artır.
    /// Cache invalidation üçün istifadə olunur.
    /// </summary>
    [Parameter] public int CacheVersion { get; set; }

    #endregion

    #region Parameters - Direct Message

    /// <summary>
    /// Direct Message siyahısı - tarixə görə sıralanır və qruplaşdırılır.
    /// </summary>
    [Parameter] public List<DirectMessageDto> DirectMessages { get; set; } = [];

    /// <summary>
    /// DM-də qarşı tərəfin adı - header-da göstərilir.
    /// </summary>
    [Parameter] public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// DM-də qarşı tərəfin avatar URL-i.
    /// </summary>
    [Parameter] public string? RecipientAvatarUrl { get; set; }

    /// <summary>
    /// Qarşı tərəfin online statusu - header-da göstərilir.
    /// </summary>
    [Parameter] public bool IsRecipientOnline { get; set; }

    /// <summary>
    /// Notes conversation olduqda true (self-conversation).
    /// Online status göstərilməməlidir.
    /// </summary>
    [Parameter] public bool IsNotesConversation { get; set; }

    #endregion

    #region Parameters - Channel

    /// <summary>
    /// Channel mesaj siyahısı - tarixə görə sıralanır və qruplaşdırılır.
    /// </summary>
    [Parameter] public List<ChannelMessageDto> ChannelMessages { get; set; } = [];

    /// <summary>
    /// Channel adı - header-da göstərilir.
    /// </summary>
    [Parameter] public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Channel təsviri - sidebar-da istifadə olunur.
    /// </summary>
    [Parameter] public string? ChannelDescription { get; set; }

    /// <summary>
    /// Channel növü (Public, Private, Direct).
    /// </summary>
    [Parameter] public ChannelType ChannelType { get; set; }

    /// <summary>
    /// Channel üzv sayı - header-da göstərilir.
    /// </summary>
    [Parameter] public int MemberCount { get; set; }

    /// <summary>
    /// Hazırki istifadəçinin channel admin olub-olmadığı.
    /// Admin olduqda Add Member butonu görünür.
    /// </summary>
    [Parameter] public bool IsChannelAdmin { get; set; }

    #endregion

    #region Parameters - Pinned Messages

    /// <summary>
    /// Channel-da pinlənmiş mesaj sayı.
    /// </summary>
    [Parameter] public int PinnedCount { get; set; }

    /// <summary>
    /// DM-də pinlənmiş mesaj sayı.
    /// </summary>
    [Parameter] public int PinnedDirectMessageCount { get; set; }

    /// <summary>
    /// Channel-da pinlənmiş mesajların tam siyahısı.
    /// Cycling və dropdown panel üçün istifadə olunur.
    /// </summary>
    [Parameter] public List<ChannelMessageDto> PinnedChannelMessages { get; set; } = [];

    /// <summary>
    /// DM-də pinlənmiş mesajların tam siyahısı.
    /// Cycling və dropdown panel üçün istifadə olunur.
    /// </summary>
    [Parameter] public List<DirectMessageDto> PinnedDirectMessages { get; set; } = [];

    /// <summary>
    /// Pinlənmiş mesaja naviqasiya callback-i.
    /// Həm header click, həm dropdown item click üçün istifadə olunur.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnNavigateToPinnedMessage { get; set; }

    #endregion

    #region Parameters - Loading & Status

    /// <summary>
    /// İlkin yükləmə statusu - spinner göstərilir.
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// GetAroundMessage ilə mesajlar yükləndiyini göstərir.
    /// Bu halda bi-directional loading (scroll-to-top) disable olunur.
    /// </summary>
    [Parameter] public bool IsViewingAroundMessage { get; set; }

    /// <summary>
    /// Daha çox mesaj yükləmə statusu (backend HTTP request).
    /// </summary>
    [Parameter] public bool IsLoadingMoreMessages { get; set; }

    /// <summary>
    /// Daha çox mesaj olub-olmadığı (pagination - yuxarı scroll).
    /// </summary>
    [Parameter] public bool HasMoreMessages { get; set; }

    /// <summary>
    /// Daha yeni mesajlar olub-olmadığı (around mode - aşağı scroll).
    /// </summary>
    [Parameter] public bool HasMoreNewerMessages { get; set; }

    /// <summary>
    /// Mesaj göndərmə statusu - input disabled olur.
    /// </summary>
    [Parameter] public bool IsSending { get; set; }

    /// <summary>
    /// Typing indicator - hazırda yazan istifadəçilərin adları.
    /// </summary>
    [Parameter] public List<string> TypingUsers { get; set; } = [];

    #endregion

    #region Parameters - Event Callbacks (Message Operations)

    /// <summary>
    /// Yeni mesaj göndərmə callback-i (message content + mention edilmiş istifadəçilər).
    /// </summary>
    [Parameter] public EventCallback<(string Message, Dictionary<string, Guid> MentionedUsers)> OnSendMessage { get; set; }

    /// <summary>
    /// Fayllarla mesaj göndərmə callback-i - (Files, Message) tuple.
    /// </summary>
    [Parameter] public EventCallback<(List<SelectedFile> Files, string Message)> OnSendWithFiles { get; set; }

    /// <summary>
    /// Mesaj redaktə callback-i - (messageId, newContent) tuple.
    /// </summary>
    [Parameter] public EventCallback<(Guid messageId, string content)> OnEditMessage { get; set; }

    /// <summary>
    /// Mesaj silmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnDeleteMessage { get; set; }

    /// <summary>
    /// Mention-a klik callback-i - userId ötürülür.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnMentionClick { get; set; }

    /// <summary>
    /// Reaction əlavə etmə callback-i - (messageId, emoji) tuple.
    /// </summary>
    [Parameter] public EventCallback<(Guid messageId, string emoji)> OnAddReaction { get; set; }

    /// <summary>
    /// Typing status dəyişikliyi callback-i.
    /// True = typing başladı, False = typing bitdi.
    /// </summary>
    [Parameter] public EventCallback<bool> OnTyping { get; set; }

    /// <summary>
    /// Daha çox mesaj yükləmə callback-i (pagination - yuxarı scroll).
    /// </summary>
    [Parameter] public EventCallback OnLoadMore { get; set; }

    /// <summary>
    /// Yeni mesajlar yükləmə callback-i (around mode - aşağı scroll).
    /// </summary>
    [Parameter] public EventCallback OnLoadNewer { get; set; }

    #endregion

    #region Parameters - Event Callbacks (Pin Operations)

    /// <summary>
    /// DM mesajını pinləmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnPinDirectMessage { get; set; }

    /// <summary>
    /// DM mesajını unpin etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnUnpinDirectMessage { get; set; }

    /// <summary>
    /// Channel mesajını pinləmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnPinChannelMessage { get; set; }

    /// <summary>
    /// Channel mesajını unpin etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnUnpinChannelMessage { get; set; }

    #endregion

    #region Parameters - Event Callbacks (Message Actions)

    /// <summary>
    /// Reply callback-i - mesaja cavab vermək.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnReply { get; set; }

    /// <summary>
    /// Forward callback-i - mesajı başqasına göndərmək.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnForward { get; set; }

    /// <summary>
    /// Reply ləğv etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnCancelReply { get; set; }

    /// <summary>
    /// Favorites toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnToggleFavorite { get; set; }

    /// <summary>
    /// Read Later işarələmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnMarkAsLater { get; set; }

    #endregion

    #region Parameters - Event Callbacks (Channel Admin)

    /// <summary>
    /// Channel-a üzv əlavə etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback<(Guid userId, ChannelMemberRole role)> OnAddMember { get; set; }

    /// <summary>
    /// İstifadəçi axtarışı callback-i (Add Member dialog üçün).
    /// </summary>
    [Parameter] public EventCallback<string> OnSearchUsers { get; set; }

    /// <summary>
    /// İstifadəçi axtarış nəticələri (parent-dən gəlir).
    /// </summary>
    [Parameter] public List<UserDto> UserSearchResults { get; set; } = [];

    /// <summary>
    /// İstifadəçi axtarış statusu.
    /// </summary>
    [Parameter] public bool IsSearchingUsers { get; set; }

    #endregion

    #region Parameters - Mention Support

    /// <summary>
    /// Channel-dadırsa true, DM-dədirsə false.
    /// </summary>
    [Parameter] public bool IsChannel { get; set; }

    /// <summary>
    /// Channel member-ləri (mention panel üçün).
    /// </summary>
    [Parameter] public List<MentionUserDto> ChannelMembers { get; set; } = [];

    /// <summary>
    /// Conversation partner (DM-də mention üçün).
    /// </summary>
    [Parameter] public MentionUserDto? ConversationPartner { get; set; }

    /// <summary>
    /// Mention üçün user axtarışı callback-i.
    /// </summary>
    [Parameter] public Func<string, Task<List<MentionUserDto>>>? OnSearchMentionUsers { get; set; }

    #endregion

    #region Parameters - Reply State

    /// <summary>
    /// Reply modunda olub-olmadığı.
    /// </summary>
    [Parameter] public bool IsReplying { get; set; }

    /// <summary>
    /// Reply edilən mesajın ID-si.
    /// </summary>
    [Parameter] public Guid? ReplyToMessageId { get; set; }

    /// <summary>
    /// Reply edilən mesajın göndərəninin adı.
    /// </summary>
    [Parameter] public string? ReplyToSenderName { get; set; }

    /// <summary>
    /// Reply edilən mesajın məzmunu.
    /// </summary>
    [Parameter] public string? ReplyToContent { get; set; }

    /// <summary>
    /// Reply edilən mesajın fayl ID-si.
    /// </summary>
    [Parameter] public string? ReplyToFileId { get; set; }

    /// <summary>
    /// Reply edilən mesajın fayl adı.
    /// </summary>
    [Parameter] public string? ReplyToFileName { get; set; }

    /// <summary>
    /// Reply edilən mesajın fayl content type-ı.
    /// </summary>
    [Parameter] public string? ReplyToFileContentType { get; set; }

    #endregion

    #region Parameters - Unread & Read Later

    /// <summary>
    /// Unread separator-dan sonrakı mesajın ID-si.
    /// Bu mesajdan sonra "New messages" separator göstərilir.
    /// </summary>
    [Parameter] public Guid? UnreadSeparatorAfterMessageId { get; set; }

    /// <summary>
    /// Read Later işarəli son mesajın ID-si.
    /// Bu mesajdan əvvəl "Read Later" separator göstərilir.
    /// </summary>
    [Parameter] public Guid? LastReadLaterMessageId { get; set; }

    #endregion

    #region Parameters - Draft Support

    /// <summary>
    /// İlkin draft məzmunu - conversation dəyişdikdə restore edilir.
    /// </summary>
    [Parameter] public string? InitialDraft { get; set; }

    /// <summary>
    /// Draft dəyişikliyi callback-i - parent-ə saxlamaq üçün.
    /// </summary>
    [Parameter] public EventCallback<string> OnDraftChanged { get; set; }

    #endregion

    #region Parameters - Selection Mode

    /// <summary>
    /// Selection modunda olub-olmadığı (multi-select).
    /// </summary>
    [Parameter] public bool IsSelectMode { get; set; }

    /// <summary>
    /// Seçilmiş mesajların ID-ləri.
    /// </summary>
    [Parameter] public HashSet<Guid> SelectedMessageIds { get; set; } = [];

    /// <summary>
    /// Mesaj seçimi toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnSelectToggle { get; set; }

    /// <summary>
    /// Selection modu ləğv etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnCancelSelection { get; set; }

    /// <summary>
    /// Seçilmiş mesajları silmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnDeleteSelected { get; set; }

    /// <summary>
    /// Seçilmiş mesajları forward etmə callback-i.
    /// </summary>
    [Parameter] public EventCallback OnForwardSelected { get; set; }

    /// <summary>
    /// Seçilmiş mesajları silmək mümkündürmü?
    /// Yalnız öz mesajları silmək olar.
    /// </summary>
    [Parameter] public bool CanDeleteSelected { get; set; }

    #endregion

    #region Parameters - Favorites

    /// <summary>
    /// Favorite mesajların ID-ləri.
    /// Star icon rənglənməsi üçün istifadə olunur.
    /// </summary>
    [Parameter] public HashSet<Guid> FavoriteMessageIds { get; set; } = [];

    #endregion

    #region Parameters - Search & Sidebar Panels

    /// <summary>
    /// Search panel toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback OnToggleSearchPanel { get; set; }

    /// <summary>
    /// Search panelin açıq olub-olmadığı.
    /// </summary>
    [Parameter] public bool IsSearchPanelOpen { get; set; }

    /// <summary>
    /// Sidebar toggle callback-i.
    /// </summary>
    [Parameter] public EventCallback OnToggleSidebar { get; set; }

    /// <summary>
    /// Sidebar-ın açıq olub-olmadığı.
    /// </summary>
    [Parameter] public bool IsSidebarOpen { get; set; }

    #endregion

    #region Parameters - Around Mode State

    /// <summary>
    /// Scroll to bottom callback - parent clear+reload edir.
    /// </summary>
    [Parameter] public EventCallback OnScrollToBottom { get; set; }

    #endregion

    #region Private Fields - Element References

    /// <summary>
    /// Mesaj konteynerinin DOM reference-i.
    /// Scroll əməliyyatları üçün istifadə olunur.
    /// </summary>
    private ElementReference messagesContainerRef;

    /// <summary>
    /// MessageInput komponentinin reference-i.
    /// Focus əməliyyatları üçün istifadə olunur.
    /// </summary>
    private MessageInput? messageInputRef;

    #endregion

    #region Private Fields - UI State

    /// <summary>
    /// Add Member dialog-un açıq olub-olmadığı.
    /// </summary>
    private bool showAddMemberPanel = false;

    /// <summary>
    /// Scroll to bottom butonunun görünüb-görünmədiyi.
    /// İstifadəçi yuxarı scroll etdikdə görünür.
    /// </summary>
    private bool showScrollToBottom = false;

    /// <summary>
    /// Yeni gələn mesaj sayı (istifadəçi yuxarıda olduqda).
    /// Scroll butonu üzərində badge kimi göstərilir.
    /// </summary>
    private int newMessagesCount = 0;

    /// <summary>
    /// Mesaj redaktə modunda olub-olmadığı.
    /// </summary>
    private bool isEditing = false;

    /// <summary>
    /// Redaktə edilən mesajın ID-si.
    /// </summary>
    private Guid editingMessageId;

    /// <summary>
    /// Redaktə edilən mesajın orijinal məzmunu.
    /// Dəyişiklik olub-olmadığını yoxlamaq üçün.
    /// </summary>
    private string editingContent = string.Empty;

    #endregion

    #region Private Fields - Scroll Management

    /// <summary>
    /// Əvvəlki mesaj sayı - yeni mesaj əlavə olunduğunu müəyyən etmək üçün.
    /// </summary>
    private int _previousMessageCount;

    /// <summary>
    /// Aşağı scroll etmək lazımdırmı?
    /// OnAfterRenderAsync-də yoxlanılır.
    /// </summary>
    private bool _shouldScrollToBottom;

    /// <summary>
    /// Scroll position restore əməliyyatı davam edirmi?
    /// Load more sonrası DOM manipulyasiyası üçün istifadə olunur.
    /// </summary>
    private bool _isRestoringScrollPosition = false;

    /// <summary>
    /// Load more əvvəl scroll position-un saxlanması.
    /// </summary>
    private object? _savedScrollPosition = null;

    /// <summary>
    /// Unread separator-a scroll edilib-edilmədiyini izləyir.
    /// Eyni separator-a təkrar scroll etməmək üçün.
    /// </summary>
    private bool _hasScrolledToSeparator = false;

    /// <summary>
    /// Əvvəlki unread separator ID-si.
    /// Conversation dəyişdikdə reset üçün.
    /// </summary>
    private Guid? _previousUnreadSeparatorId = null;

    /// <summary>
    /// Read Later separator-a scroll edilib-edilmədiyini izləyir.
    /// </summary>
    private bool _hasScrolledToReadLaterSeparator = false;

    /// <summary>
    /// Əvvəlki Read Later mesaj ID-si.
    /// Conversation dəyişdikdə reset üçün.
    /// </summary>
    private Guid? _previousReadLaterMessageId = null;

    #endregion

    #region Private Fields - Add Member Panel

    /// <summary>
    /// Üzv axtarış sorğusu.
    /// </summary>
    private string memberSearchQuery = string.Empty;

    /// <summary>
    /// Üzv axtarış nəticələri (local copy).
    /// </summary>
    private List<UserDto> memberSearchResults = [];

    /// <summary>
    /// Üzv axtarışı davam edirmi?
    /// </summary>
    private bool isSearchingMembers = false;

    /// <summary>
    /// Üzv əlavə etmə davam edirmi?
    /// </summary>
    private bool isAddingMember = false;

    /// <summary>
    /// Hazırda əlavə edilən istifadəçinin ID-si.
    /// Loading spinner göstərmək üçün.
    /// </summary>
    private Guid? addingUserId = null;

    /// <summary>
    /// Üzv əlavə etmə xətası mesajı.
    /// </summary>
    private string? addMemberError = null;

    /// <summary>
    /// Üzv əlavə etmə uğur mesajı.
    /// </summary>
    private string? addMemberSuccess = null;

    /// <summary>
    /// Hər istifadəçi üçün seçilmiş rol.
    /// </summary>
    private readonly Dictionary<Guid, ChannelMemberRole> selectedRoleForUser = [];

    /// <summary>
    /// Axtarış əməliyyatını ləğv etmək üçün CancellationToken.
    /// Debouncing üçün istifadə olunur.
    /// </summary>
    private CancellationTokenSource? _memberSearchCts;

    /// <summary>
    /// Komponent dispose olunub?
    /// </summary>
    private bool _disposed = false;

    #endregion

    #region Private Fields - Pinned Messages

    /// <summary>
    /// Hazırki pinlənmiş mesajın indexi (cycling üçün).
    /// </summary>
    private int currentPinnedIndex = 0;

    /// <summary>
    /// Pinned dropdown panelinin açıq olub-olmadığı.
    /// </summary>
    private bool showPinnedDropdown = false;

    /// <summary>
    /// Əvvəlki conversation ID - conversation dəyişdikdə pin index reset üçün.
    /// </summary>
    private Guid? _previousConversationId;

    /// <summary>
    /// Əvvəlki channel ID - channel dəyişdikdə pin index reset üçün.
    /// </summary>
    private Guid? _previousChannelId;

    #endregion

    #region Private Fields - Performance & Cache

    /// <summary>
    /// ChannelMessages list-in hash-i - reference dəyişikliyini track etmək üçün.
    /// ReadByCount kimi field-lərin yenilənməsini təmin edir.
    /// </summary>
    private int _previousChannelMessagesHash = 0;

    /// <summary>
    /// Cache-lənmiş Direct Messages qrupları.
    /// </summary>
    private List<IGrouping<DateTime, DirectMessageDto>>? _cachedGroupedDirectMessages;

    /// <summary>
    /// Cache-lənmiş Channel Messages qrupları.
    /// </summary>
    private List<IGrouping<DateTime, ChannelMessageDto>>? _cachedGroupedChannelMessages;

    /// <summary>
    /// Əvvəlki DirectMessages reference.
    /// </summary>
    private List<DirectMessageDto>? _previousDirectMessages;

    /// <summary>
    /// Əvvəlki DirectMessages sayı.
    /// </summary>
    private int _previousDirectMessagesCount;

    /// <summary>
    /// Əvvəlki ChannelMessages reference.
    /// </summary>
    private List<ChannelMessageDto>? _previousChannelMessages;

    /// <summary>
    /// Əvvəlki ChannelMessages sayı.
    /// </summary>
    private int _previousChannelMessagesCount;

    /// <summary>
    /// Əvvəlki CacheVersion - mesaj edit/delete/reaction/pin detection üçün.
    /// </summary>
    private int _previousCacheVersion;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Parameter dəyişiklikləri olduqda çağırılır.
    /// Conversation/channel dəyişikliyi, yeni mesaj əlavəsi və s. burada handle edilir.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Conversation/channel dəyişdikdə pinned index-i sıfırla
        ResetPinnedIndexIfConversationChanged();

        // Separator position dəyişdikdə scroll flag-ləri sıfırla
        ResetScrollFlagsIfSeparatorChanged();

        // Yeni mesaj əlavə olunduğunu detect et və scroll behavior-u müəyyən et
        HandleNewMessageDetection();

        // ChannelMessages reference dəyişikliyini track et
        HandleChannelMessagesChange();

        // Message cache invalidation - yalnız dəyişiklik olduqda
        InvalidateMessageCacheIfNeeded();
    }

    /// <summary>
    /// Mesaj cache-ini yalnız lazım olduqda yeniləyir.
    /// </summary>
    private void InvalidateMessageCacheIfNeeded()
    {
        // CacheVersion dəyişibsə (edit/delete/reaction/pin) - hər iki cache-i invalidate et
        if (CacheVersion != _previousCacheVersion)
        {
            _cachedGroupedDirectMessages = null;
            _cachedGroupedChannelMessages = null;
            _previousCacheVersion = CacheVersion;
        }

        // DirectMessages dəyişibsə cache-i invalidate et
        if (!ReferenceEquals(DirectMessages, _previousDirectMessages) ||
            DirectMessages.Count != _previousDirectMessagesCount)
        {
            _cachedGroupedDirectMessages = null;
            _previousDirectMessages = DirectMessages;
            _previousDirectMessagesCount = DirectMessages.Count;
        }

        // ChannelMessages dəyişibsə cache-i invalidate et
        if (!ReferenceEquals(ChannelMessages, _previousChannelMessages) ||
            ChannelMessages.Count != _previousChannelMessagesCount)
        {
            _cachedGroupedChannelMessages = null;
            _previousChannelMessages = ChannelMessages;
            _previousChannelMessagesCount = ChannelMessages.Count;
        }
    }

    /// <summary>
    /// Render-dən sonra çağırılır.
    /// Scroll əməliyyatları burada icra edilir.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Load more sonrası scroll position restore
        if (_isRestoringScrollPosition && _savedScrollPosition != null)
        {
            await RestoreScrollPositionAfterLoadMore();
        }
        // Unread separator-a scroll (PRIORITY 1 - həmişə əvvəl yoxla!)
        else if (UnreadSeparatorAfterMessageId.HasValue && !_hasScrolledToSeparator && HasAnyMessages())
        {
            _hasScrolledToSeparator = true;
            await Task.Delay(150); // Image load üçün daha uzun delay
            await ScrollToUnreadSeparatorAsync();
        }
        // Read Later separator-a scroll (PRIORITY 2)
        else if (LastReadLaterMessageId.HasValue && !_hasScrolledToReadLaterSeparator && HasAnyMessages())
        {
            _hasScrolledToReadLaterSeparator = true;
            await Task.Delay(150); // Image load üçün daha uzun delay
            await ScrollToReadLaterSeparatorAsync();
        }
        // Yeni mesaj üçün scroll to bottom (PRIORITY 3)
        // Bu yalnız real-time yeni mesaj gəldikdə işləyir (HandleNewMessageDetection-da set olunur)
        else if (_shouldScrollToBottom)
        {
            _shouldScrollToBottom = false;
            await Task.Delay(50); // Pinned header render-dən sonra
            await ScrollToBottomAsync();
        }
        // NOT: firstRender halında scroll ETMƏ!
        // Parent (Messages.Selection.cs) scrollToBottomAndShow ilə scroll edir.
        // Burada scroll etmək double scroll və flash effekti yaradır.
    }

    #endregion

    #region Lifecycle Helper Methods

    /// <summary>
    /// Conversation/channel dəyişdikdə pinned index-i sıfırlayır.
    /// </summary>
    private void ResetPinnedIndexIfConversationChanged()
    {
        if (ConversationId != _previousConversationId || ChannelId != _previousChannelId)
        {
            currentPinnedIndex = 0;
            showPinnedDropdown = false;
            _previousConversationId = ConversationId;
            _previousChannelId = ChannelId;

            // Conversation dəyişdikdə message count-u sıfırla
            // Beləliklə HandleNewMessageDetection() işləyir və separator yoxdursa scroll to bottom edir
            _previousMessageCount = 0;
        }
    }

    /// <summary>
    /// Separator position dəyişdikdə scroll flag-ləri sıfırlayır.
    /// </summary>
    private void ResetScrollFlagsIfSeparatorChanged()
    {
        // Unread separator
        if (UnreadSeparatorAfterMessageId != _previousUnreadSeparatorId)
        {
            _hasScrolledToSeparator = false;
            _previousUnreadSeparatorId = UnreadSeparatorAfterMessageId;
        }

        // Read Later separator
        if (LastReadLaterMessageId != _previousReadLaterMessageId)
        {
            _hasScrolledToReadLaterSeparator = false;
            _previousReadLaterMessageId = LastReadLaterMessageId;
        }
    }

    /// <summary>
    /// Yeni mesaj əlavə olunduğunu detect edir və scroll behavior-u müəyyən edir.
    /// </summary>
    private void HandleNewMessageDetection()
    {
        var currentCount = DirectMessages.Count + ChannelMessages.Count;

        // İlk dəfə mesaj yükləndikdə - SCROLL ETMƏ!
        // Parent (Messages.Selection.cs) artıq scroll edir, burada etmək lazım deyil.
        // Bu _shouldScrollToBottom = true etməsək, OnAfterRenderAsync-də scroll olmayacaq.
        if (_previousMessageCount == 0 && currentCount > 0)
        {
            // Heç nə etmə - parent scroll edəcək
            // Separator varsa OnAfterRenderAsync-də separator-a scroll olacaq
        }
        else
        {
            var messageIncrease = currentCount - _previousMessageCount;
            var isNewIncomingMessage = messageIncrease > 0 && messageIncrease <= 5;

            // CRITICAL: Load more və restore zamanı badge counter artırma!
            if (isNewIncomingMessage && !_isRestoringScrollPosition && !IsLoadingMoreMessages && !isEditing)
            {
                if (showScrollToBottom)
                {
                    // İstifadəçi yuxarıda - badge counter-i artır
                    newMessagesCount += messageIncrease;
                    StateHasChanged();
                }
                else
                {
                    // İstifadəçi aşağıda - auto scroll
                    _shouldScrollToBottom = true;
                }
            }
        }

        _previousMessageCount = currentCount;
    }

    /// <summary>
    /// ChannelMessages list reference dəyişikliyini track edir.
    /// ReadByCount kimi field-lərin yenilənməsini təmin edir.
    /// </summary>
    private void HandleChannelMessagesChange()
    {
        var currentHash = ChannelMessages.GetHashCode();
        if (currentHash != _previousChannelMessagesHash)
        {
            _previousChannelMessagesHash = currentHash;
            InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Hər hansı mesaj olub-olmadığını yoxlayır.
    /// </summary>
    private bool HasAnyMessages() => DirectMessages.Count != 0 || ChannelMessages.Count != 0;

    /// <summary>
    /// Load more sonrası scroll position-u restore edir.
    /// INSTANT: Height-difference method + double rAF pattern.
    /// </summary>
    private async Task RestoreScrollPositionAfterLoadMore()
    {
        // Kiçik delay - Blazor component render cycle tamamlansın
        await Task.Delay(16); // 1 frame (16ms at 60fps)

        // Double rAF pattern JS-də işləyir (32ms daha)
        await JS.InvokeVoidAsync("chatAppUtils.restoreScrollPosition", messagesContainerRef, _savedScrollPosition);

        _savedScrollPosition = null;

        // Minimal cooldown - infinite loop qarşısını alır
        await Task.Delay(100);

        // Re-enable loading
        _isRestoringScrollPosition = false;
    }

    #endregion

    #region Scroll Management

    /// <summary>
    /// Mesaj konteynerin ən aşağısına scroll edir.
    /// </summary>
    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("chatAppUtils.scrollToBottom", messagesContainerRef);
        }
        catch
        {
            // Component disposed ola bilər - ignore
        }
    }

    /// <summary>
    /// Aşağı scroll edir və input-a focus edir.
    /// Float button-a click olunduqda çağırılır.
    /// Parent-də clear+reload edir (conversation switch kimi).
    /// </summary>
    private async Task ScrollToBottomAndFocusAsync()
    {
        // Button-u gizlət və counter-i sıfırla
        showScrollToBottom = false;
        newMessagesCount = 0;
        StateHasChanged();

        // Parent-də clear+reload et (ən son 50 mesaj yüklənir)
        await OnScrollToBottom.InvokeAsync();

        // Focus input
        if (messageInputRef != null)
        {
            await messageInputRef.FocusAsync();
        }
    }

    /// <summary>
    /// Unread separator-a scroll edir.
    /// </summary>
    private async Task ScrollToUnreadSeparatorAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("chatAppUtils.scrollToElement", "unread-separator");
        }
        catch
        {
            // Element tapılmaya bilər - ignore
        }
    }

    /// <summary>
    /// Read Later mesajına scroll edir və highlight edir.
    /// </summary>
    private async Task ScrollToReadLaterSeparatorAsync()
    {
        if (!LastReadLaterMessageId.HasValue) return;

        try
        {
            // Mesajın özünə scroll et və highlight et (separator-a deyil!)
            await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", LastReadLaterMessageId.Value.ToString());
        }
        catch
        {
            // Element tapılmaya bilər - ignore
        }
    }

    /// <summary>
    /// Scroll event handler.
    /// Scroll to bottom button visibility və load more trigger edir.
    /// Around mode-da bi-directional loading (yuxarı və aşağı).
    /// </summary>
    private async Task HandleScroll()
    {
        // Restore və loading zamanı scroll handle etmə
        if (_isRestoringScrollPosition || IsLoadingMoreMessages) return;

        try
        {
            var scrollTop = await JS.InvokeAsync<int>("chatAppUtils.getScrollTop", messagesContainerRef);

            // Scroll button visibility
            await UpdateScrollButtonVisibility();

            // User manual scroll etdikdə auto-scroll-u ləğv et
            if (_shouldScrollToBottom && scrollTop > 0)
            {
                _shouldScrollToBottom = false;
            }

            // Load more trigger (yuxarı scroll - köhnə mesajlar)
            await TriggerLoadMoreIfNeeded(scrollTop);

            // Load newer trigger (aşağı scroll - yeni mesajlar, around mode)
            if (IsViewingAroundMessage)
            {
                await TriggerLoadNewerIfNeeded();
            }
        }
        catch
        {
            // Scroll errors - ignore
        }
    }

    /// <summary>
    /// Scroll to bottom button visibility-ni yeniləyir.
    /// </summary>
    private async Task UpdateScrollButtonVisibility()
    {
        var isNearBottom = await JS.InvokeAsync<bool>("chatAppUtils.isNearBottom", messagesContainerRef, 300);
        var shouldShow = !isNearBottom;

        if (showScrollToBottom != shouldShow)
        {
            showScrollToBottom = shouldShow;
            if (!shouldShow) newMessagesCount = 0;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Lazım olduqda load more trigger edir (pagination).
    /// Strategiya: Yuxarıda 1 viewport qalandan yüklə.
    /// </summary>
    private async Task TriggerLoadMoreIfNeeded(int scrollTop)
    {
        // GetAroundMessage ilə yüklənmiş mesajlar üçün bi-directional loading-i disable et
        if (IsViewingAroundMessage) return;

        if (!HasMoreMessages || IsLoadingMoreMessages) return;

        try
        {
            // Fixed threshold - 1 viewport from top
            var element = messagesContainerRef;
            var clientHeight = await JS.InvokeAsync<double>("chatAppUtils.getClientHeight", element);
            var threshold = clientHeight * 1; // 1 viewport - simple and reliable

            // Load when user scrolls close to top
            if (scrollTop < threshold)
            {
                await LoadMoreMessages();
            }
        }
        catch
        {
            // Error - ignore
        }
    }

    /// <summary>
    /// Daha çox mesaj yükləyir (pagination - yuxarı scroll).
    /// Scroll position saxlanılır və restore edilir.
    /// </summary>
    private async Task LoadMoreMessages()
    {
        // Position save et
        _savedScrollPosition = await JS.InvokeAsync<object>("chatAppUtils.saveScrollPosition", messagesContainerRef);

        // Backend request (IsLoadingMoreMessages flag parent-də idarə olunur)
        await OnLoadMore.InvokeAsync();

        // Mesajlar yükləndi, indi restore et
        _isRestoringScrollPosition = true;
    }

    /// <summary>
    /// Around mode-da aşağıya scroll edildikdə yeni mesajlar yükləyir (APPEND).
    /// Strategiya: Aşağıda 1 viewport qalandan yüklə.
    /// </summary>
    private async Task TriggerLoadNewerIfNeeded()
    {
        if (!HasMoreNewerMessages) return;

        try
        {
            // Viewport height və scroll metrics
            var element = messagesContainerRef;
            var clientHeight = await JS.InvokeAsync<double>("chatAppUtils.getClientHeight", element);
            var scrollHeight = await JS.InvokeAsync<double>("chatAppUtils.getScrollHeight", element);
            var scrollTop = await JS.InvokeAsync<double>("chatAppUtils.getScrollTop", element);

            var threshold = clientHeight * 1; // 1 viewport məsafə
            var distanceFromBottom = scrollHeight - scrollTop - clientHeight;

            // Əgər scroll bottom-dan 1 viewport məsafədə qalıbsa → yüklə
            if (distanceFromBottom < threshold)
            {
                await OnLoadNewer.InvokeAsync();
            }
        }
        catch
        {
            // Error - ignore
        }
    }

    #endregion

    #region Message Grouping & Formatting

    /// <summary>
    /// Direct mesajları tarixə görə qruplaşdırır.
    /// Cache-lənmiş - yalnız mesajlar dəyişdikdə yenidən hesablanır.
    /// </summary>
    private List<IGrouping<DateTime, DirectMessageDto>> GroupedDirectMessages
    {
        get
        {
            _cachedGroupedDirectMessages ??= DirectMessages
                    .OrderBy(m => m.CreatedAtUtc)
                    .GroupBy(m => m.CreatedAtUtc.Date)
                    .ToList();
            return _cachedGroupedDirectMessages;
        }
    }

    /// <summary>
    /// Channel mesajlarını tarixə görə qruplaşdırır.
    /// Cache-lənmiş - yalnız mesajlar dəyişdikdə yenidən hesablanır.
    /// </summary>
    private List<IGrouping<DateTime, ChannelMessageDto>> GroupedChannelMessages
    {
        get
        {
            _cachedGroupedChannelMessages ??= ChannelMessages
                    .OrderBy(m => m.CreatedAtUtc)
                    .GroupBy(m => m.CreatedAtUtc.Date)
                    .ToList();
            return _cachedGroupedChannelMessages;
        }
    }

    /// <summary>
    /// Tarix divider-i formatlanır.
    /// Bu gün: "today", Bu il: "Tuesday, December 23", Keçən il: "Tuesday, September 17, 2019"
    /// </summary>
    private static string FormatDateDivider(DateTime date)
    {
        var now = DateTime.Now;
        var today = now.Date;

        if (date == today)
            return "today";

        if (date.Year == now.Year)
            return date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);

        return date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Input placeholder text-i qaytarır.
    /// </summary>
    private static string GetInputPlaceholder() => "Enter @ to mention a person or chat";

    /// <summary>
    /// Channel-da avatar göstərilməli olub-olmadığını müəyyən edir.
    /// </summary>
    private static bool ShouldShowAvatarInChannel(ChannelMessageDto message, IEnumerable<ChannelMessageDto> messages)
    {
        var list = messages.ToList();
        var index = list.IndexOf(message);

        // Son mesajda göstər
        if (index == list.Count - 1) return true;

        // Növbəti mesaj fərqli sender-dən olduqda göstər
        return list[index + 1].SenderId != message.SenderId;
    }

    /// <summary>
    /// Channel-da sender adı göstərilməli olub-olmadığını müəyyən edir.
    /// Qrupun İLK mesajında göstərilir.
    /// Öz mesajlarında heç vaxt göstərilmir.
    /// </summary>
    private bool ShouldShowSenderName(ChannelMessageDto message, IEnumerable<ChannelMessageDto> messages)
    {
        // Öz mesajlarında göstərmə
        if (message.SenderId == CurrentUserId) return false;

        var list = messages.ToList();
        var index = list.IndexOf(message);

        // İlk mesajda göstər
        if (index == 0) return true;

        // Əvvəlki mesaj fərqli sender-dən olduqda göstər
        return list[index - 1].SenderId != message.SenderId;
    }

    #endregion

    #region Pinned Messages

    /// <summary>
    /// Hazırki pinlənmiş DM mesajını qaytarır (cycling üçün).
    /// Ən sonuncu pinlənmiş mesaj ilk göstərilir (reverse order).
    /// </summary>
    private DirectMessageDto? GetCurrentPinnedDirectMessage()
    {
        if (PinnedDirectMessages == null || PinnedDirectMessages.Count == 0)
            return null;

        // Reverse order: most recently pinned shows first
        var safeIndex = PinnedDirectMessages.Count - 1 - (currentPinnedIndex % PinnedDirectMessages.Count);
        return PinnedDirectMessages[safeIndex];
    }

    /// <summary>
    /// Hazırki pinlənmiş channel mesajını qaytarır (cycling üçün).
    /// Ən sonuncu pinlənmiş mesaj ilk göstərilir (reverse order).
    /// </summary>
    private ChannelMessageDto? GetCurrentPinnedChannelMessage()
    {
        if (PinnedChannelMessages == null || PinnedChannelMessages.Count == 0)
            return null;

        // Reverse order: most recently pinned shows first
        var safeIndex = PinnedChannelMessages.Count - 1 - (currentPinnedIndex % PinnedChannelMessages.Count);
        return PinnedChannelMessages[safeIndex];
    }

    /// <summary>
    /// Pinned header click handler.
    /// Dropdown bağlı olduqda mesaja naviqasiya edir və növbəti pin-ə keçir.
    /// </summary>
    private async Task HandlePinnedHeaderClick()
    {
        if (showPinnedDropdown) return;

        Guid? messageIdToNavigate = null;

        if (IsDirectMessage)
        {
            var currentPinned = GetCurrentPinnedDirectMessage();
            if (currentPinned != null)
            {
                messageIdToNavigate = currentPinned.Id;
                currentPinnedIndex = (currentPinnedIndex + 1) % PinnedDirectMessageCount;
            }
        }
        else
        {
            var currentPinned = GetCurrentPinnedChannelMessage();
            if (currentPinned != null)
            {
                messageIdToNavigate = currentPinned.Id;
                currentPinnedIndex = (currentPinnedIndex + 1) % PinnedCount;
            }
        }

        if (messageIdToNavigate.HasValue)
        {
            await OnNavigateToPinnedMessage.InvokeAsync(messageIdToNavigate.Value);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Pinned dropdown toggle.
    /// </summary>
    private void TogglePinnedDropdown() => showPinnedDropdown = !showPinnedDropdown;

    /// <summary>
    /// Pinned dropdown bağla.
    /// </summary>
    private void ClosePinnedDropdown() => showPinnedDropdown = false;

    /// <summary>
    /// Dropdown-dan pin mesaja naviqasiya.
    /// </summary>
    private async Task NavigateToPinnedMessage(Guid messageId)
    {
        await OnNavigateToPinnedMessage.InvokeAsync(messageId);
        showPinnedDropdown = false;
        StateHasChanged();
    }

    /// <summary>
    /// DM mesajını unpin etmə (dropdown-dan).
    /// </summary>
    private async Task HandleUnpinMessage(Guid messageId)
    {
        await OnUnpinDirectMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// Channel mesajını unpin etmə (dropdown-dan).
    /// </summary>
    private async Task HandleUnpinChannelMessage(Guid messageId)
    {
        await OnUnpinChannelMessage.InvokeAsync(messageId);
    }

    #endregion

    #region Message Actions

    /// <summary>
    /// Mention-a klik - həmin şəxslə conversation aç.
    /// </summary>
    private async Task HandleMentionClick(Guid userId)
    {
        Console.WriteLine($"[DEBUG] HandleMentionClick - userId: {userId}");
        await OnMentionClick.InvokeAsync(userId);
    }

    /// <summary>
    /// Mesaj göndərmə (content + mention edilmiş istifadəçi ID-ləri).
    /// </summary>
    private async Task SendMessage((string Message, Dictionary<string, Guid> MentionedUsers) data)
    {
        await OnSendMessage.InvokeAsync(data);
    }

    /// <summary>
    /// Mesaj redaktə modunu başladır.
    /// </summary>
    private void StartEditMessage(Guid messageId, string content)
    {
        isEditing = true;
        editingMessageId = messageId;
        editingContent = content;
        StateHasChanged();
    }

    /// <summary>
    /// Mesajı redaktə edir.
    /// Content dəyişməyibsə API call etmir.
    /// </summary>
    private async Task EditMessage(string newContent)
    {
        if (newContent == editingContent)
        {
            CancelEdit();
            return;
        }

        await OnEditMessage.InvokeAsync((editingMessageId, newContent));
        CancelEdit();
    }

    /// <summary>
    /// Redaktə modunu ləğv edir.
    /// </summary>
    private void CancelEdit()
    {
        isEditing = false;
        editingMessageId = Guid.Empty;
        editingContent = string.Empty;
    }

    /// <summary>
    /// Mesajı silir.
    /// </summary>
    private async Task DeleteMessage(Guid messageId)
    {
        await OnDeleteMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// Mesaja reaction əlavə edir.
    /// </summary>
    private async Task AddReaction(Guid messageId, string emoji)
    {
        await OnAddReaction.InvokeAsync((messageId, emoji));
    }

    /// <summary>
    /// Channel mesajını pin/unpin edir.
    /// </summary>
    private async Task PinChannelMessage(Guid messageId, bool isPinned)
    {
        if (isPinned)
            await OnUnpinChannelMessage.InvokeAsync(messageId);
        else
            await OnPinChannelMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// DM mesajını pin/unpin edir.
    /// </summary>
    private async Task PinDirectMessage(Guid messageId, bool isPinned)
    {
        if (isPinned)
            await OnUnpinDirectMessage.InvokeAsync(messageId);
        else
            await OnPinDirectMessage.InvokeAsync(messageId);
    }

    /// <summary>
    /// Reply click handler - replied mesaja naviqasiya edir.
    /// </summary>
    private async Task HandleReplyClick(Guid repliedMessageId)
    {
        await OnNavigateToPinnedMessage.InvokeAsync(repliedMessageId);
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Typing status handler.
    /// </summary>
    private async Task HandleTyping(bool isTyping)
    {
        await OnTyping.InvokeAsync(isTyping);
    }

    /// <summary>
    /// File attachment handler (implement edilməyib).
    /// </summary>
    private void HandleAttach()
    {
        // File attachment feature not implemented yet
    }

    /// <summary>
    /// Input-a yenidən focus edir.
    /// Hər mesaj aksiyasından sonra çağırılır.
    /// </summary>
    private async Task RefocusInput()
    {
        if (messageInputRef != null)
        {
            await messageInputRef.FocusAsync();
        }
    }

    #endregion

    #region Add Member Panel

    /// <summary>
    /// Add member panel toggle.
    /// </summary>
    private void ToggleAddMemberPanel()
    {
        showAddMemberPanel = !showAddMemberPanel;
        if (!showAddMemberPanel)
        {
            ResetAddMemberPanel();
        }
    }

    /// <summary>
    /// Add member panel bağlama.
    /// </summary>
    private void CloseAddMemberPanel()
    {
        showAddMemberPanel = false;
        ResetAddMemberPanel();
    }

    /// <summary>
    /// Add member panel state-ini sıfırlayır.
    /// </summary>
    private void ResetAddMemberPanel()
    {
        memberSearchQuery = string.Empty;
        memberSearchResults.Clear();
        selectedRoleForUser.Clear();
        addMemberError = null;
        addMemberSuccess = null;
        _memberSearchCts?.Cancel();
    }

    /// <summary>
    /// Üzv axtarış input handler (debounced).
    /// </summary>
    private async Task OnMemberSearchInput(ChangeEventArgs e)
    {
        memberSearchQuery = e.Value?.ToString() ?? string.Empty;
        addMemberError = null;
        addMemberSuccess = null;

        // Əvvəlki axtarışı ləğv et
        _memberSearchCts?.Cancel();
        _memberSearchCts = new CancellationTokenSource();
        var token = _memberSearchCts.Token;

        // Sorğu çox qısadır
        if (string.IsNullOrWhiteSpace(memberSearchQuery) || memberSearchQuery.Length < 2)
        {
            memberSearchResults.Clear();
            isSearchingMembers = false;
            StateHasChanged();
            return;
        }

        // Debounce - 300ms gözlə
        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SearchMembers(token);
    }

    /// <summary>
    /// Üzv axtarışı icra edir.
    /// </summary>
    private async Task SearchMembers(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        isSearchingMembers = true;
        StateHasChanged();

        try
        {
            await OnSearchUsers.InvokeAsync(memberSearchQuery);

            if (token.IsCancellationRequested) return;

            // Nəticələri kopyala və rol selections-ı initialize et
            memberSearchResults = UserSearchResults.ToList();
            foreach (var user in memberSearchResults)
            {
                if (!selectedRoleForUser.ContainsKey(user.Id))
                {
                    selectedRoleForUser[user.Id] = ChannelMemberRole.Member;
                }
            }
        }
        catch
        {
            memberSearchResults.Clear();
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isSearchingMembers = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Channel-a üzv əlavə edir.
    /// </summary>
    private async Task AddMemberToChannel(Guid userId)
    {
        if (isAddingMember) return;

        isAddingMember = true;
        addingUserId = userId;
        addMemberError = null;
        addMemberSuccess = null;
        StateHasChanged();

        try
        {
            var role = selectedRoleForUser.GetValueOrDefault(userId, ChannelMemberRole.Member);
            await OnAddMember.InvokeAsync((userId, role));

            // Uğurlu əlavədən sonra nəticələrdən çıxar
            var user = memberSearchResults.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                addMemberSuccess = $"{user.DisplayName} added successfully";
                memberSearchResults.Remove(user);
                selectedRoleForUser.Remove(userId);
            }
        }
        catch (Exception ex)
        {
            addMemberError = ex.Message;
        }
        finally
        {
            isAddingMember = false;
            addingUserId = null;
            StateHasChanged();

            // Uğur halında 1 saniyə sonra panel-i bağla
            if (!string.IsNullOrEmpty(addMemberSuccess))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    if (!_disposed)
                    {
                        await InvokeAsync(CloseAddMemberPanel);
                    }
                });
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// DirectMessage-dən file preview string-ini çıxarır (pinned messages üçün).
    /// </summary>
    private string GetFilePreview(DirectMessageDto message)
    {
        if (message.FileId != null)
        {
            if (message.FileContentType != null && message.FileContentType.StartsWith("image/"))
            {
                return string.IsNullOrWhiteSpace(message.Content) ? "[Image]" : $"[Image] {message.Content}";
            }

            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    /// <summary>
    /// ChannelMessage-dən file preview string-ini çıxarır (pinned messages üçün).
    /// </summary>
    private string GetFilePreview(ChannelMessageDto message)
    {
        if (message.FileId != null)
        {
            if (message.FileContentType != null && message.FileContentType.StartsWith("image/"))
            {
                return string.IsNullOrWhiteSpace(message.Content) ? "[Image]" : $"[Image] {message.Content}";
            }

            return string.IsNullOrWhiteSpace(message.Content) ? "[File]" : $"[File] {message.Content}";
        }
        return message.Content;
    }

    #endregion

    #region IAsyncDisposable

    /// <summary>
    /// Resurları təmizləyir.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _memberSearchCts?.Cancel();
        _memberSearchCts?.Dispose();
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    #endregion
}