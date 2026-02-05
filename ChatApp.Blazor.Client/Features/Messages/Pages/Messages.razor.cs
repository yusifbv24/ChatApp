using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Files.Services;
using ChatApp.Blazor.Client.Features.Messages.Components;
using ChatApp.Blazor.Client.Features.Messages.Services;
using ChatApp.Blazor.Client.Infrastructure.SignalR;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Models.Organization;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages : IAsyncDisposable
{
    #region Dependency Injection - Servislərin inject edilməsi

    [Inject] private IConversationService ConversationService { get; set; } = default!;

    [Inject] private IChannelService ChannelService { get; set; } = default!;

    [Inject] private IUserService UserService { get; set; } = default!;

    [Inject] private ISearchService SearchService { get; set; } = default!;

    [Inject] private ISignalRService SignalRService { get; set; } = default!;

    [Inject] private IDepartmentService DepartmentService { get; set; } = default!;

    [Inject] private UserState UserState { get; set; } = default!;

    [Inject] private AppState AppState { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IFileService FileService { get; set; } = default!;

    #endregion

    #region Core State - Əsas state dəyişənləri

    /// <summary>
    /// Cari istifadəçinin ID-si.
    /// Mesajların sahibini müəyyən etmək üçün.
    /// </summary>
    private Guid currentUserId;

    /// <summary>
    /// Direct conversation-ların siyahısı (sol panel).
    /// </summary>
    private List<DirectConversationDto> directConversations = [];

    /// <summary>
    /// Bütün channel-ların siyahısı (sol panel).
    /// </summary>
    private List<ChannelDto> channelConversations = [];

    /// <summary>
    /// Department əməkdaşları siyahısı (conversation olmayanlar).
    /// </summary>
    private List<DepartmentUserDto> departmentUsers = [];

    /// <summary>
    /// Unified conversation list-də daha çox item var?
    /// </summary>
    private bool hasMoreConversationItems = true;

    /// <summary>
    /// Daha çox item yüklənir?
    /// </summary>
    private bool isLoadingMoreItems = false;

    /// <summary>
    /// Unified conversation list səhifə nömrəsi.
    /// </summary>
    private int unifiedPageNumber = 1;

    /// <summary>
    /// Conversation/channel page size.
    /// </summary>
    private const int ConversationListPageSize = 20;

    /// <summary>
    /// Aktiv conversation-un mesajları.
    /// </summary>
    private List<DirectMessageDto> directMessages = [];

    /// <summary>
    /// Aktiv channel-ın mesajları.
    /// </summary>
    private List<ChannelMessageDto> channelMessages = [];

    /// <summary>
    /// Aktiv channel-ın pinlənmiş mesajları.
    /// </summary>
    private List<ChannelMessageDto> pinnedChannelMessages = [];

    /// <summary>
    /// Aktiv conversation-un pinlənmiş mesajları.
    /// </summary>
    private List<DirectMessageDto> pinnedDirectMessages = [];

    /// <summary>
    /// Mesaj cache version - ChatArea cache invalidation üçün.
    /// Mesaj edit/delete/reaction/pin olduqda artırılır.
    /// </summary>
    private int messageCacheVersion = 0;

    #endregion

    #region Selection State - Seçim state-i

    private Guid? selectedConversationId;

    private Guid? selectedChannelId;

    private bool isDirectMessage = true;

    /// <summary>
    /// Seçim rejimindəyik? (bir neçə mesaj seçmək üçün)
    /// </summary>
    private bool isSelectingMessageBuble = false;

    /// <summary>
    /// Seçilmiş mesaj ID-ləri.
    /// </summary>
    private readonly HashSet<Guid> selectedMessageIds = [];

    #endregion

    #region Permissions State - İcazə state-i

    /// <summary>
    /// İstifadəçinin mesaj redaktə etmə icazəsi var?
    /// </summary>
    private bool canEditMessage => UserState.HasPermission(Permissions.MessagesEdit);

    /// <summary>
    /// İstifadəçinin mesaj silmə icazəsi var?
    /// </summary>
    private bool canDeleteMessage => UserState.HasPermission(Permissions.MessagesDelete);

    #endregion

    #region Direct Message State - DM state-i

    /// <summary>
    /// Söhbət edilən şəxsin adı.
    /// </summary>
    private string recipientName = string.Empty;

    /// <summary>
    /// Söhbət edilən şəxsin avatar URL-i.
    /// </summary>
    private string? recipientAvatarUrl;

    /// <summary>
    /// Söhbət edilən şəxsin ID-si.
    /// </summary>
    private Guid recipientUserId;

    /// <summary>
    /// Conversation Notes olduqda true (self-conversation).
    /// </summary>
    private bool isNotesConversation = false;

    /// <summary>
    /// Söhbət edilən şəxs online-dır?
    /// </summary>
    private bool isRecipientOnline;

    /// <summary>
    /// Conversation-un pinlənmiş mesaj sayı.
    /// </summary>
    private int pinnedDirectMessageCount;

    #endregion

    #region Pending Conversation State - Pending conversation state-i

    /// <summary>
    /// Pending conversation modundayıq?
    /// (İstifadəçi seçilib, amma mesaj göndərilməyib)
    /// </summary>
    private bool isPendingConversation;

    /// <summary>
    /// Pending conversation-un istifadəçisi.
    /// </summary>
    private UserSearchResultDto? pendingUser;

    #endregion

    #region Channel State - Channel state-i

    /// <summary>
    /// Seçilmiş channel-ın adı.
    /// </summary>
    private string selectedChannelName = string.Empty;

    /// <summary>
    /// Seçilmiş channel-ın avatar URL-i.
    /// </summary>
    private string? selectedChannelAvatarUrl;

    /// <summary>
    /// Seçilmiş channel-ın təsviri.
    /// </summary>
    private string? selectedChannelDescription;

    /// <summary>
    /// Seçilmiş channel-ın tipi (Public/Private).
    /// </summary>
    private ChannelType selectedChannelType;

    /// <summary>
    /// Seçilmiş channel-ın üzv sayı.
    /// </summary>
    private int selectedChannelMemberCount;

    /// <summary>
    /// Channel-ın pinlənmiş mesaj sayı.
    /// </summary>
    private int pinnedChannelMessageCount;

    /// <summary>
    /// Cari istifadəçi channel admin-dir?
    /// </summary>
    private bool isChannelAdmin;

    /// <summary>
    /// Cari istifadəçinin channel-dakı rolu.
    /// </summary>
    private MemberRole currentUserChannelRole;

    /// <summary>
    /// Cari istifadəçi channel-ın üzvüdür?
    /// Public channel-da false ola bilər (join etməyib).
    /// </summary>
    private bool isCurrentUserChannelMember;

    /// <summary>
    /// Seçilmiş conversation/channel pinlənmişdir?
    /// </summary>
    private bool selectedConversationIsPinned;

    /// <summary>
    /// Seçilmiş conversation/channel mute edilmişdir?
    /// </summary>
    private bool selectedConversationIsMuted;

    /// <summary>
    /// Seçilmiş conversation/channel "read later" işarələnmişdir?
    /// </summary>
    private bool selectedConversationIsMarkedReadLater;

    #endregion

    #region Add Member State - Üzv əlavə etmə state-i

    /// <summary>
    /// Üzv axtarışı nəticələri.
    /// </summary>
    private List<UserSearchResultDto> memberSearchResults = [];

    /// <summary>
    /// Üzv axtarışı davam edir?
    /// </summary>
    private bool isSearchingMembersForAdd;

    #endregion

    #region Mention State - Mention state-i

    /// <summary>
    /// Channel üçün mention user list (hazırki channel-ın member-ləri).
    /// </summary>
    private List<MentionUserDto> currentChannelMembers = [];

    /// <summary>
    /// Direct message üçün conversation partner.
    /// </summary>
    private MentionUserDto? currentConversationPartner;

    #endregion

    #region Loading States - Yükləmə state-ləri

    /// <summary>
    /// List yüklənir? (sol panel)
    /// </summary>
    private bool isLoadingConversationList;

    /// <summary>
    /// Mesajlar yüklənir?
    /// </summary>
    private bool isLoadingMessages;

    /// <summary>
    /// Daha çox mesaj yüklənir? (Load More)
    /// </summary>
    private bool isLoadingMoreMessages;

    /// <summary>
    /// Mesaj göndərilir?
    /// </summary>
    private bool isSendingMessage;

    /// <summary>
    /// İstifadəçi axtarışı davam edir?
    /// </summary>
    private bool isSearchingUsers;

    /// <summary>
    /// Channel yaradılır?
    /// </summary>
    private bool isCreatingChannel;

    /// <summary>
    /// Daha çox mesaj var? (pagination üçün - köhnə mesajlar)
    /// </summary>
    private bool hasMoreMessages = true;

    /// <summary>
    /// Daha yeni mesajlar var? (pagination üçün - around mode-da aşağı scroll)
    /// </summary>
    private bool hasMoreNewerMessages = false;

    /// <summary>
    /// Ən köhnə mesajın tarixi (pagination üçün - yuxarı scroll).
    /// </summary>
    private DateTime? oldestMessageDate;

    /// <summary>
    /// Ən yeni mesajın tarixi (pagination üçün - aşağı scroll, around mode).
    /// </summary>
    private DateTime? newestMessageDate;

    /// <summary>
    /// Context mode-dayıq? (pinned/favorite mesaja jump edəndə)
    /// true: Jump to latest button görünür, bidirectional pagination aktiv
    /// </summary>
    private bool isViewingAroundMessage = false;

    /// <summary>
    /// Səhifə ölçüsü (viewport-based, dynamic).
    /// Bitrix pattern: yalnız görünən mesajlar + buffer.
    /// </summary>
    private int pageSize = 30; // Initial default, will be calculated dynamically

    #endregion

    #region Typing State - Yazır göstəricisi state-i

    /// <summary>
    /// Hazırda yazan istifadəçilər (aktiv conversation/channel üçün).
    /// </summary>
    private List<string> typingUsers = [];

    /// <summary>
    /// Hər conversation üçün typing state (conversation list üçün).
    /// conversationId -> yazırlarmı?
    /// </summary>
    private Dictionary<Guid, bool> conversationTypingState = [];

    /// <summary>
    /// Hər channel üçün yazan istifadəçilər.
    /// channelId -> [fullName1, fullName2, ...]
    /// </summary>
    private Dictionary<Guid, List<string>> channelTypingUsers = [];

    #endregion

    #region Race Condition Prevention - Race condition qarşısını alma

    /// <summary>
    /// Pending read receipts (DM üçün).
    /// Race condition: MessageRead event-i mesajdan tez gələ bilər.
    /// messageId -> readBy
    /// </summary>
    private Dictionary<Guid, Guid> pendingReadReceipts = [];

    /// <summary>
    /// İşlənmiş mesaj ID-ləri.
    /// SignalR dublikatlarının qarşısını alır.
    /// </summary>
    private HashSet<Guid> processedMessageIds = [];

    /// <summary>
    /// Əlavə edilməkdə olan mesaj ID-ləri.
    /// Race condition: SignalR və HTTP eyni anda gələ bilər.
    /// </summary>
    private HashSet<Guid> pendingMessageAdds = [];

    /// <summary>
    /// DUPLICATE FIX: Pending message tracking (TempId → pending message).
    /// Content-based matching ilə duplicate yaranmasının qarşısını alır.
    /// Eyni content-li 2 mesaj tez göndərildikdə düzgün pending mesajı tap.
    /// </summary>
    private Dictionary<Guid, DirectMessageDto> pendingDirectMessages = [];
    private Dictionary<Guid, ChannelMessageDto> pendingChannelMessages = [];

    #endregion

    #region Page Visibility State - Səhifə görünürlüyü state-i

    /// <summary>
    /// Səhifə görünür? (tab fokusda)
    /// Mark as read yalnız görünən tab-da işləyir.
    /// </summary>
    private bool isPageVisible = true;

    /// <summary>
    /// JS visibility subscription reference.
    /// </summary>
    private IJSObjectReference? visibilitySubscription;

    /// <summary>
    /// JS ESC key subscription reference.
    /// </summary>
    private IJSObjectReference? escapeKeySubscription;

    /// <summary>
    /// .NET reference (JS-dən C# metodunu çağırmaq üçün).
    /// </summary>
    private DotNetObjectReference<Messages>? dotNetReference;

    #endregion

    #region Debounce State - Debounce state-i

    /// <summary>
    /// Debounce timer-i.
    /// Typing/online event-lər üçün UI yeniləməni batch edir.
    /// </summary>
    private Timer? _stateChangeDebounceTimer;

    /// <summary>
    /// State yeniləmə planlanıb?
    /// </summary>
    private bool _stateChangeScheduled;

    /// <summary>
    /// State dəyişikliyi üçün lock.
    /// Thread-safe debounce üçün.
    /// </summary>
    private readonly object _stateChangeLock = new();

    #endregion

    #region Disposal State - Disposal state-i

    /// <summary>
    /// Component disposed olub?
    /// Disposed olduqdan sonra state update-lərin qarşısını alır.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Conversation Seçimi davam edir?
    /// </summary>
    private bool _isConversationSelecting;

    #endregion

    #region Dialog State - Dialog state-i

    /// <summary>
    /// Yeni conversation dialog-u açıq?
    /// </summary>
    private bool showNewConversationDialog;

    /// <summary>
    /// Yeni channel dialog-u açıq?
    /// </summary>
    private bool showNewChannelDialog;

    /// <summary>
    /// Create Group panel açıqdır?
    /// ChatArea yerinə Create Group paneli göstərilir.
    /// </summary>
    private bool showCreateGroupPanel;

    /// <summary>
    /// Create Group panel-ə əlavə ediləcək üzvlər.
    /// </summary>
    private List<UserSearchResultDto> createGroupSelectedMembers = [];

    /// <summary>
    /// Create Group panel-də üzv axtarış sorğusu.
    /// </summary>
    private string createGroupMemberSearchQuery = string.Empty;

    /// <summary>
    /// Create Group panel-də üzv axtarılır?
    /// </summary>
    private bool isSearchingCreateGroupMembers;

    /// <summary>
    /// Create Group panel-də üzv axtarış nəticələri.
    /// </summary>
    private List<UserSearchResultDto> createGroupMemberSearchResults = [];

    /// <summary>
    /// Create Group panel-də axtarış cancellation token.
    /// </summary>
    private CancellationTokenSource? _createGroupSearchCts;

    /// <summary>
    /// Create Group panel-də member search dropdown açıq?
    /// </summary>
    private bool showCreateGroupMemberSearch;

    /// <summary>
    /// Create Group panel-də Chat Settings bölməsi açıq?
    /// Default collapsed.
    /// </summary>
    private bool showChatSettings = false;

    /// <summary>
    /// Member picker aktiv tab (recent/departments).
    /// </summary>
    private string memberPickerTab = "recent";

    /// <summary>
    /// Departments siyahısı.
    /// </summary>
    private List<DepartmentDto> createGroupDepartments = [];

    /// <summary>
    /// Departments yüklənir?
    /// </summary>
    private bool isLoadingDepartments;

    /// <summary>
    /// Expanded department IDs.
    /// </summary>
    private HashSet<Guid> expandedDepartmentIds = [];

    /// <summary>
    /// Selected department IDs (bütün department seçildikdə).
    /// </summary>
    private HashSet<Guid> selectedDepartmentIds = [];

    /// <summary>
    /// Department users cache.
    /// </summary>
    private Dictionary<Guid, List<DepartmentUserDto>> departmentUsersCache = [];

    /// <summary>
    /// Create Group avatar URL (preview - base64).
    /// </summary>
    private string? createGroupAvatarUrl;

    /// <summary>
    /// Create Group avatar file data (temporary - backend-ə göndərilməmiş).
    /// Channel yaradıldıqdan sonra bu data istifadə olunub upload edilir.
    /// </summary>
    private byte[]? createGroupAvatarFileData;

    /// <summary>
    /// Create Group avatar file name (temporary).
    /// </summary>
    private string? createGroupAvatarFileName;

    /// <summary>
    /// Create Group avatar content type (temporary).
    /// </summary>
    private string? createGroupAvatarContentType;

    /// <summary>
    /// Create Group form validation:
    /// - Name boşdursa və ya 100-dən çoxdursa
    /// - Description 500-dən çoxdursa
    /// </summary>
    private bool IsCreateGroupFormInvalid =>
        string.IsNullOrWhiteSpace(newChannelRequest.Name) ||
        (newChannelRequest.Name?.Length ?? 0) > 100 ||
        (newChannelRequest.Description?.Length ?? 0) > 500;

    #endregion

    #region Search State - Axtarış state-i

    /// <summary>
    /// İstifadəçi axtarış sorğusu.
    /// </summary>
    private string userSearchQuery = string.Empty;

    /// <summary>
    /// İstifadəçi axtarış nəticələri.
    /// </summary>
    private List<UserSearchResultDto> userSearchResults = [];

    /// <summary>
    /// Axtarış cancellation token source.
    /// Debounce üçün əvvəlki axtarışı ləğv etmək.
    /// </summary>
    private CancellationTokenSource? _searchCts;

    #endregion

    #region New Channel State - Yeni channel state-i

    /// <summary>
    /// Yeni channel yaratmaq üçün request.
    /// </summary>
    private CreateChannelRequest newChannelRequest = new();

    #endregion

    #region Error State - Xəta state-i

    private string? errorMessage;

    #endregion

    #region SignalR State - SignalR state-i

    /// <summary>
    /// SignalR event-lərinə subscribe olunub?
    /// </summary>
    private bool isSubscribedToSignalR;

    #endregion

    #region Reply State - Reply state-i

    /// <summary>
    /// Reply modundayıq?
    /// </summary>
    private bool isReplying;

    /// <summary>
    /// Reply edilən mesajın ID-si.
    /// </summary>
    private Guid? replyToMessageId;

    /// <summary>
    /// Reply edilən mesajın göndərəninin adı.
    /// </summary>
    private string? replyToSenderName;

    /// <summary>
    /// Reply edilən mesajın contenti.
    /// </summary>
    private string? replyToContent;

    /// <summary>
    /// Reply edilən mesajın file ID-si (file varsa).
    /// </summary>
    private string? replyToFileId;

    /// <summary>
    /// Reply edilən mesajın file adı (file varsa).
    /// </summary>
    private string? replyToFileName;

    /// <summary>
    /// Reply edilən mesajın file content type (file varsa).
    /// </summary>
    private string? replyToFileContentType;

    #endregion

    #region Forward State - Forward state-i

    /// <summary>
    /// Forward dialog-u açıq?
    /// </summary>
    private bool showForwardDialog;

    /// <summary>
    /// Forward edilən DM mesajı.
    /// </summary>
    private DirectMessageDto? forwardingDirectMessage;

    /// <summary>
    /// Forward edilən channel mesajı.
    /// </summary>
    private ChannelMessageDto? forwardingChannelMessage;

    /// <summary>
    /// Forward dialog-unda axtarış sorğusu.
    /// </summary>
    private string forwardSearchQuery = string.Empty;

    #endregion

    #region Draft State - Draft state-i

    /// <summary>
    /// Qaralama mesajları.
    /// key: "conv_{id}" / "chan_{id}" / "pending_{userId}"
    /// </summary>
    private readonly Dictionary<string, string> messageDrafts = [];

    /// <summary>
    /// Cari qaralama.
    /// </summary>
    private string currentDraft = string.Empty;

    #endregion

    #region Unread Separator State - Unread separator state-i

    /// <summary>
    /// "New messages" separator-undan sonrakı mesaj ID-si.
    /// </summary>
    private Guid? unreadSeparatorAfterMessageId;

    /// <summary>
    /// Separator hesablanmalıdır?
    /// </summary>
    private bool shouldCalculateUnreadSeparator;

    #endregion

    #region Read Later State - Sonra oxu state-i

    /// <summary>
    /// "Sonra oxu" işarəli mesaj ID-si.
    /// </summary>
    private Guid? lastReadLaterMessageId;

    /// <summary>
    /// Daxil olarkən "sonra oxu" mesaj ID-si.
    /// Çıxarkən avtomatik silmək üçün.
    /// </summary>
    private Guid? lastReadLaterMessageIdOnEntry;

    #endregion

    #region Favorites State - Favori state-i

    /// <summary>
    /// Favori mesaj ID-ləri (star icon göstərmək üçün).
    /// </summary>
    private HashSet<Guid> favoriteMessageIds = [];

    #endregion

    #region Search Panel State - Axtarış paneli state-i

    /// <summary>
    /// Axtarış paneli açıq?
    /// </summary>
    private bool showSearchPanel = false;

    #endregion

    #region Component References - Komponent referansları

    /// <summary>
    /// ConversationList komponenti referansı.
    /// ESC key handler üçün istifadə olunur.
    /// </summary>
    private ConversationList? conversationListRef;

    #endregion

    #region Sidebar State - Sidebar state-i

    /// <summary>
    /// Sidebar açıq?
    /// </summary>
    private bool showSidebar = false;

    /// <summary>
    /// Profile panel açıq?
    /// </summary>
    private bool showProfilePanel = false;

    /// <summary>
    /// Sidebar DM favori mesajları.
    /// </summary>
    private List<FavoriteDirectMessageDto>? sidebarFavoriteDirectMessages;

    /// <summary>
    /// Sidebar channel favori mesajları.
    /// </summary>
    private List<FavoriteChannelMessageDto>? sidebarFavoriteChannelMessages;

    /// <summary>
    /// Shared channels (ortaq channel-lar - "Chats with user" paneli üçün).
    /// </summary>
    private List<ChannelDto>? sharedChannels;

    /// <summary>
    /// Sidebar component referansı.
    /// </summary>
    private Sidebar? sidebarRef;

    #endregion

    #region Computed Properties - Hesablanmış xüsusiyyətlər

    /// <summary>
    /// Heç bir şey seçilməyib?
    /// Boş state göstərmək üçün.
    /// </summary>
    private bool IsEmpty => !selectedConversationId.HasValue && !selectedChannelId.HasValue && !isPendingConversation;

    #endregion

    #region Lifecycle Methods - Yaşam dövrü metodları

    /// <summary>
    /// Component ilk dəfə yaradıldıqda çağrılır.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // Cari istifadəçi ID-sini al
        if (UserState.CurrentUser != null)
        {
            currentUserId = UserState.CurrentUser.Id;
        }

        // UserState dəyişikliklərini dinlə
        UserState.OnChange += HandleUserStateChanged;

        // SignalR event-lərinə subscribe ol
        SubscribeToSignalREvents();

        // Conversation və channel-ları yüklə
        await LoadConversationsAndChannels();
    }

    /// <summary>
    /// Render-dən sonra çağrılır.
    /// İlk render-də page visibility listener qur.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Calculate viewport-based page size (Bitrix pattern)
            try
            {
                var optimalPageSize = await JS.InvokeAsync<int>("chatAppUtils.getViewportBasedPageSize");
                if (optimalPageSize > 0)
                {
                    pageSize = optimalPageSize;
                }
            }
            catch
            {
                // Fallback to default (30)
                pageSize = 30;
            }

            // Page visibility listener
            dotNetReference = DotNetObjectReference.Create(this);
            visibilitySubscription = await JS.InvokeAsync<IJSObjectReference>(
                "chatAppUtils.subscribeToVisibilityChange",
                dotNetReference);

            // Mention click listener (using dotNetReference)
            await JS.InvokeVoidAsync("chatAppUtils.subscribeToMentionClick", dotNetReference);

            // ESC key listener (panel bağlama üçün)
            escapeKeySubscription = await JS.InvokeAsync<IJSObjectReference>(
                "chatAppUtils.subscribeToEscapeKey",
                dotNetReference);

            // İlkin visibility state
            isPageVisible = await JS.InvokeAsync<bool>("chatAppUtils.isPageVisible");
        }
    }

    /// <summary>
    /// </summary>
    [JSInvokable]
    public async Task HandleMentionClick(string fullName)
    {
        try
        {
            // Self-mention check - Əgər öz adına mention edirsə, birbaşa Notes aç
            if (UserState.CurrentUser != null &&
                fullName.Equals(UserState.FullName, StringComparison.OrdinalIgnoreCase))
            {
                var notesConversation = directConversations.FirstOrDefault(c => c.IsNotes);
                if (notesConversation != null)
                {
                    await SelectDirectConversation(notesConversation);
                }
                return;
            }

            // OPTIMIZATION: Əvvəlcə mövcud conversation-larda yoxla (API çağırışı olmadan)
            var existingConversation = directConversations.FirstOrDefault(c =>
                c.OtherUserFullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (existingConversation != null)
            {
                await SelectDirectConversation(existingConversation);
                return;
            }

            // Mövcud conversation yoxdursa, API-dən user axtar (yalnız fullName ilə)
            var searchResult = await UserService.SearchUsersAsync(fullName);

            if (searchResult.IsSuccess && searchResult.Value != null && searchResult.Value.Count > 0)
            {
                // İlk uyğun user-i tap (exact match, sonra contains)
                var user = searchResult.Value.FirstOrDefault(u =>
                    u.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

                // Exact match tapılmadısa, contains ilə yoxla
                user ??= searchResult.Value.FirstOrDefault(u =>
                    u.FullName.Contains(fullName, StringComparison.OrdinalIgnoreCase));

                // Hələ də yoxdursa, ilk nəticəni götür
                user ??= searchResult.Value.FirstOrDefault();

                if (user != null)
                {
                    // userSearchResults-a əlavə et (StartConversationWithUser oradan oxuyur)
                    userSearchResults = searchResult.Value;
                    await StartConversationWithUser(user.Id);
                }
            }
        }
        catch
        {
            // Silently handle errors
        }
    }

    /// <summary>
    /// Page visibility dəyişdikdə JS-dən çağrılır.
    /// Tab görünür olduqda unread mesajları mark-as-read edir.
    /// </summary>
    [JSInvokable]
    public async Task OnVisibilityChanged(bool isVisible)
    {
        var wasHidden = !isPageVisible;
        isPageVisible = isVisible;

        // Tab-a qayıdanda unread mesajları mark-as-read et
        if (isVisible && wasHidden)
        {
            await MarkUnreadMessagesOnVisibilityRestoreAsync();
        }

        // State dəyişikliyini bildir (UI yenilənməsi üçün)
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// ESC düyməsinə basıldıqda JS-dən çağrılır.
    /// Açıq panelləri prioritet sırasına görə bağlayır.
    /// Qeyd: Input focus-dadırsa JS tərəfində blur edilir, bu metod çağrılmır.
    /// </summary>
    [JSInvokable]
    public async Task HandleEscapeKey()
    {
        if (_disposed) return;

        // Priority sırası ilə panelləri bağla (hər ESC basımında yalnız bir panel bağlanır)

        // Priority 1: Forward dialog
        if (showForwardDialog)
        {
            CancelForward();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 2: Profile panel
        if (showProfilePanel)
        {
            showProfilePanel = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 3: Search panel
        if (showSearchPanel)
        {
            CloseSearchPanel();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 4: Sidebar
        if (showSidebar)
        {
            CloseSidebar();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 5: Create Group panel
        if (showCreateGroupPanel)
        {
            CloseCreateGroupPanel();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 6: New conversation dialog
        if (showNewConversationDialog)
        {
            CloseNewConversationDialog();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 7: New channel dialog
        if (showNewChannelDialog)
        {
            CloseNewChannelDialog();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 8: Select mode (isSelectingMessageBuble)
        if (isSelectingMessageBuble)
        {
            ToggleSelectMode();
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Priority 9: ConversationList panelləri (more menu, filter, search mode)
        if (conversationListRef?.TryCloseOpenPanels() == true)
        {
            return;
        }

        // Priority 10: Aktiv conversation/channel-ı bağla
        if (selectedConversationId.HasValue || selectedChannelId.HasValue || isPendingConversation)
        {
            CloseConversation();
            await InvokeAsync(StateHasChanged);
            return;
        }
    }

    /// <summary>
    /// Tab-a qayıdanda conversation/channel-dakı unread mesajları mark-as-read edir.
    /// </summary>
    private async Task MarkUnreadMessagesOnVisibilityRestoreAsync()
    {
        if (_disposed) return;

        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                // DM: Unread mesajları tap və mark-as-read et
                var unreadMessages = directMessages
                    .Where(m => !m.IsRead && m.SenderId != currentUserId)
                    .ToList();

                if (unreadMessages.Count > 0)
                {
                    await MarkDirectMessagesAsReadAsync(unreadMessages);
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                // Channel: Unread mesajları tap və mark-as-read et
                var unreadMessages = channelMessages
                    .Where(m => m.SenderId != currentUserId &&
                                (m.ReadBy == null || !m.ReadBy.Contains(currentUserId)))
                    .ToList();

                if (unreadMessages.Count > 0)
                {
                    await MarkChannelMessagesAsReadAsync(unreadMessages);
                }
            }
        }
        catch
        {
            // Silently handle mark-as-read errors
        }
    }

    /// <summary>
    /// UserState dəyişdikdə çağrılır.
    /// </summary>
    private void HandleUserStateChanged()
    {
        if (UserState.CurrentUser != null && currentUserId == Guid.Empty)
        {
            currentUserId = UserState.CurrentUser.Id;
            InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    #region Dispose - Təmizlik

    /// <summary>
    /// Component disposed olduqda çağrılır.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Disposed flag-i qoy
        _disposed = true;

        // UserState-dən unsubscribe ol
        UserState.OnChange -= HandleUserStateChanged;

        // SignalR-dan unsubscribe ol
        UnsubscribeFromSignalREvents();

        // Debounce timer-ı dispose et
        _stateChangeDebounceTimer?.Dispose();
        _stateChangeDebounceTimer = null; // SAFEGUARD: Prevent dangling reference

        // DEADLOCK FIX: Dispose mark-as-read debounce timers
        _markDMAsReadDebounceTimer?.Dispose();
        _markDMAsReadDebounceTimer = null;
        _markAsReadDebounceTimer?.Dispose();
        _markAsReadDebounceTimer = null;

        // MEMORY LEAK FIX: Dispose search CancellationTokenSource
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null; // SAFEGUARD: Prevent dangling reference

        // Page visibility subscription-ı dispose et
        if (visibilitySubscription != null)
        {
            try
            {
                await visibilitySubscription.InvokeVoidAsync("dispose");
                await visibilitySubscription.DisposeAsync();
            }
            catch
            {
                // Silently handle dispose errors
            }
        }

        // ESC key subscription-ı dispose et
        if (escapeKeySubscription != null)
        {
            try
            {
                await escapeKeySubscription.InvokeVoidAsync("dispose");
                await escapeKeySubscription.DisposeAsync();
            }
            catch
            {
                // Silently handle dispose errors
            }
        }

        dotNetReference?.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}