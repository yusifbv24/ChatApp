using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Messages.Services;
using ChatApp.Blazor.Client.Infrastructure.SignalR;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Models.Search;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages : IAsyncDisposable
{
    [Inject] private IConversationService ConversationService { get; set; } = default!;
    [Inject] private IChannelService ChannelService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private ISignalRService SignalRService { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Parameter] public Guid? ConversationId { get; set; }
    [Parameter] public Guid? ChannelId { get; set; }

    // State
    private Guid currentUserId;
    private List<DirectConversationDto> conversations = [];
    private List<ChannelDto> channels = [];
    private List<DirectMessageDto> directMessages = [];
    private List<ChannelMessageDto> channelMessages = [];
    private List<ChannelMessageDto> pinnedMessages = [];
    private List<DirectMessageDto> pinnedDirectMessages = [];

    // Selection
    private Guid? selectedConversationId;
    private Guid? selectedChannelId;
    private bool isDirectMessage = true;

    // Direct message state
    private string recipientName = string.Empty;
    private string? recipientAvatarUrl;
    private Guid recipientUserId;
    private bool isRecipientOnline;
    private int pinnedDirectMessageCount;

    // Pending conversation (user selected but conversation not created yet)
    private bool isPendingConversation;
    private UserDto? pendingUser;

    // Channel state
    private string selectedChannelName = string.Empty;
    private string? selectedChannelDescription;
    private ChannelType selectedChannelType;
    private int selectedChannelMemberCount;
    private int pinnedMessageCount;
    private bool isChannelAdmin;
    private ChannelMemberRole currentUserChannelRole;

    // Add member state
    private List<UserDto> memberSearchResults = [];
    private bool isSearchingMembersForAdd;

    // Loading states
    private bool isLoadingList;
    private bool isLoadingMessages;
    private bool isLoadingMoreMessages;
    private bool isSendingMessage;
    private bool isSearchingUsers;
    private bool isCreatingChannel;
    private bool hasMoreMessages = true;
    private DateTime? oldestMessageDate;
    private int pageSize = 50; // İlk yükləmə 50, sonrakılar 100

    // Typing
    private List<string> typingUsers = [];
    private Dictionary<Guid, bool> conversationTypingState = [];
    private Dictionary<Guid, List<string>> channelTypingUsers = [];

    // Pending read receipts for direct messages (for race condition: MessageRead arrives before message is added)
    private Dictionary<Guid, (Guid readBy, DateTime readAtUtc)> pendingReadReceipts = []; // messageId -> (readBy, readAtUtc)

    // Pending read receipts for channel messages (userId -> readAtUtc)
    private Dictionary<Guid, DateTime> pendingChannelReadReceipts = []; // userId -> readAtUtc

    // Processed message tracking (to prevent duplicate SignalR notifications)
    private HashSet<Guid> processedMessageIds = [];

    // Track messages currently being added to prevent race condition
    private HashSet<Guid> pendingMessageAdds = [];

    // Page visibility tracking
    private bool isPageVisible = true;
    private IJSObjectReference? visibilitySubscription;
    private DotNetObjectReference<Messages>? dotNetReference;

    // Debounce state changes to prevent UI freezing
    private Timer? _stateChangeDebounceTimer;
    private bool _stateChangeScheduled;
    private readonly object _stateChangeLock = new();

    // Disposal tracking to prevent updates after component disposed
    private bool _disposed;

    // Selection tracking to prevent concurrent SelectConversation/SelectChannel calls
    private bool _isSelecting;

    // Dialogs
    private bool showNewConversationDialog;
    private bool showNewChannelDialog;

    // Search
    private string userSearchQuery = string.Empty;
    private List<UserDto> userSearchResults = [];
    private CancellationTokenSource? _searchCts;

    // New channel
    private CreateChannelRequest newChannelRequest = new();

    // Error handling
    private string? errorMessage;

    // Subscription tracking
    private bool isSubscribedToSignalR;

    // Reply state
    private bool isReplying;
    private Guid? replyToMessageId;
    private string? replyToSenderName;
    private string? replyToContent;

    // Forward state
    private bool showForwardDialog;
    private DirectMessageDto? forwardingDirectMessage;
    private ChannelMessageDto? forwardingChannelMessage;
    private string forwardSearchQuery = string.Empty;

    // Draft messages - stores unsent message text for each conversation/channel
    private Dictionary<string, string> messageDrafts = [];
    private string currentDraft = string.Empty;

    // Unread separator state
    private Guid? unreadSeparatorAfterMessageId;
    private bool shouldCalculateUnreadSeparator;
    private DateTime? currentMemberLastReadAtUtc;

    // Read Later separator state
    private Guid? lastReadLaterMessageId;
    private Guid? lastReadLaterMessageIdOnEntry; // Track value when entering channel

    // Selection mode state
    private bool isSelectMode = false;
    private HashSet<Guid> selectedMessageIds = new HashSet<Guid>();

    // Favorites state
    private HashSet<Guid> favoriteMessageIds = new HashSet<Guid>();

    // Search panel state
    private bool showSearchPanel = false;

    private bool IsEmpty => !selectedConversationId.HasValue && !selectedChannelId.HasValue && !isPendingConversation;

    protected override async Task OnInitializedAsync()
    {
        // Set current user ID (with fallback if UserState not yet populated)
        if (UserState.CurrentUser != null)
        {
            currentUserId = UserState.CurrentUser.Id;
        }

        // Subscribe to UserState changes to handle race condition
        // (App.razor loads user async, might not be ready when this component initializes)
        UserState.OnChange += HandleUserStateChanged;

        // Subscribe to SignalR events (SignalR is already initialized in MainLayout)
        SubscribeToSignalREvents();

        // Load conversations and channels
        await LoadConversationsAndChannels();

        // Handle route parameters
        if (ConversationId.HasValue)
        {
            var conversation = conversations.FirstOrDefault(c => c.Id == ConversationId.Value);
            if (conversation != null)
            {
                await SelectConversation(conversation);
            }
        }
        else if (ChannelId.HasValue)
        {
            var channel = channels.FirstOrDefault(c => c.Id == ChannelId.Value);
            if (channel != null)
            {
                await SelectChannel(channel);
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // Handle navigation changes
        if (ConversationId.HasValue && ConversationId != selectedConversationId)
        {
            var conversation = conversations.FirstOrDefault(c => c.Id == ConversationId.Value);
            if (conversation != null)
            {
                await SelectConversation(conversation);
            }
        }
        else if (ChannelId.HasValue && ChannelId != selectedChannelId)
        {
            var channel = channels.FirstOrDefault(c => c.Id == ChannelId.Value);
            if (channel != null)
            {
                await SelectChannel(channel);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Subscribe to page visibility changes
            dotNetReference = DotNetObjectReference.Create(this);
            visibilitySubscription = await JS.InvokeAsync<IJSObjectReference>(
                "chatAppUtils.subscribeToVisibilityChange",
                dotNetReference);

            // Get initial visibility state
            isPageVisible = await JS.InvokeAsync<bool>("chatAppUtils.isPageVisible");
        }
    }

    [JSInvokable]
    public void OnVisibilityChanged(bool isVisible)
    {
        var wasHidden = !isPageVisible;
        isPageVisible = isVisible;

        // When user returns to the page (becomes visible), mark unread messages as read
        if (wasHidden && isVisible)
        {
            InvokeAsync(async () =>
            {
                await MarkUnreadMessagesAsRead();
            });
        }
    }

    /// <summary>
    /// Handles UserState changes to fix race condition where Messages component initializes
    /// before App.razor finishes loading CurrentUser
    /// </summary>
    private void HandleUserStateChanged()
    {
        // Update currentUserId when UserState changes (handles race condition)
        if (UserState.CurrentUser != null && currentUserId == Guid.Empty)
        {
            currentUserId = UserState.CurrentUser.Id;
            // Trigger UI refresh to re-render messages with correct ownership
            InvokeAsync(StateHasChanged);
        }
    }

    private async Task MarkUnreadMessagesAsRead()
    {
        // Mark unread direct messages as read if viewing a conversation using smart threshold
        // Use smart threshold: 5+ messages = bulk, <5 messages = individual
        if (selectedConversationId.HasValue)
        {
            var unreadMessages = directMessages.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList();
            if (unreadMessages.Count != 0)
            {
                try
                {
                    if (unreadMessages.Count >= 5)
                    {
                        // Bulk operation for 5+ unread messages
                        await ConversationService.MarkAllAsReadAsync(selectedConversationId.Value);
                    }
                    else
                    {
                        // Individual operations for 1-4 unread messages (parallel)
                        var markTasks = unreadMessages.Select(message =>
                            ConversationService.MarkAsReadAsync(message.ConversationId, message.Id)
                        );
                        await Task.WhenAll(markTasks);
                    }

                    // Update UI
                    foreach (var message in unreadMessages)
                    {
                        var index = directMessages.IndexOf(message);
                        if (index >= 0)
                        {
                            directMessages[index] = message with { IsRead = true };
                        }
                    }
                    StateHasChanged();
                }
                catch
                {
                    // Ignore errors when marking as read
                }
            }
        }
        else if (selectedChannelId.HasValue)
        {
            // Mark all channel messages as read using smart threshold
            // Use smart threshold: 5+ messages = bulk, <5 messages = individual
            try
            {
                var unreadMessages = channelMessages.Where(m =>
                    m.SenderId != currentUserId &&
                    (m.ReadBy == null || !m.ReadBy.Contains(currentUserId))
                ).ToList();

                if (unreadMessages.Count >= 5)
                {
                    // Bulk operation for 5+ unread messages
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                else if (unreadMessages.Count > 0)
                {
                    // Individual operations for 1-4 unread messages
                    foreach (var msg in unreadMessages)
                    {
                        await ChannelService.MarkSingleMessageAsReadAsync(selectedChannelId.Value, msg.Id);
                    }
                }
                // SignalR event will update the UI automatically
            }
            catch
            {
                // Ignore errors when marking as read
            }
        }
    }


    #region SignalR Event Handlers

    private void SubscribeToSignalREvents()
    {
        // Prevent multiple subscriptions
        if (isSubscribedToSignalR) return;
        isSubscribedToSignalR = true;

        // SignalR is already initialized in MainLayout, just subscribe to events here
        SignalRService.OnNewDirectMessage += HandleNewDirectMessage;
        SignalRService.OnNewChannelMessage += HandleNewChannelMessage;
        SignalRService.OnDirectMessageEdited += HandleDirectMessageEdited;
        SignalRService.OnDirectMessageDeleted += HandleDirectMessageDeleted;
        SignalRService.OnChannelMessageEdited += HandleChannelMessageEdited;
        SignalRService.OnChannelMessageDeleted += HandleChannelMessageDeleted;
        SignalRService.OnMessageRead += HandleMessageRead;
        SignalRService.OnUserTypingInConversation += HandleTypingInConversation;
        SignalRService.OnUserTypingInChannel += HandleTypingInChannel;
        SignalRService.OnUserOnline += HandleUserOnline;
        SignalRService.OnUserOffline += HandleUserOffline;
        SignalRService.OnDirectMessageReactionToggled += HandleReactionToggled;
        SignalRService.OnChannelMessageReactionsUpdated += HandleChannelMessageReactionsUpdated;
        SignalRService.OnChannelMessagesRead += HandleChannelMessagesRead;
        SignalRService.OnAddedToChannel += HandleAddedToChannel;

        // CRITICAL: Rejoin groups after reconnection
        SignalRService.OnReconnected += HandleSignalRReconnected;
    }

    private void HandleNewDirectMessage(DirectMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Skip already processed messages (prevents duplicate SignalR notifications)
            if (!processedMessageIds.Add(message.Id)) return;

            if (message.ConversationId == selectedConversationId)
            {
                // Check if this is our own message (already added optimistically)
                var existingIndex = directMessages.FindIndex(m => m.Id == message.Id);

                if (existingIndex >= 0)
                {
                    // REPLACE the optimistic message with the real one from the backend
                    // This ensures IsRead, ReadAtUtc, and other fields are up-to-date
                    // Create a new list instance to trigger Blazor parameter change detection
                    var updatedList = new List<DirectMessageDto>(directMessages);
                    updatedList[existingIndex] = message;
                    directMessages = updatedList;
                }
                else
                {
                    // Message from another user - add it
                    directMessages.Add(message);

                    // Only mark as read if: message is from another user AND page is visible
                    if (message.SenderId != currentUserId && isPageVisible)
                    {
                        try
                        {
                            await ConversationService.MarkAsReadAsync(message.ConversationId, message.Id);
                        }
                        catch
                        {
                            // Ignore errors when marking as read
                        }
                    }
                }
            }

            // Update conversation list (for both sent and received messages)
            var conversation = conversations.FirstOrDefault(c => c.Id == message.ConversationId);
            if (conversation != null)
            {
                var isCurrentConversation = message.ConversationId == selectedConversationId;
                var isMyMessage = message.SenderId == currentUserId;

                // Create updated conversation with new message info
                var updatedConversation = conversation with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageSenderId = message.SenderId,
                    LastMessageStatus = isMyMessage ? (message.IsRead ? "Read" : "Sent") : null,
                    UnreadCount = isCurrentConversation ? 0 : (isMyMessage ? conversation.UnreadCount : conversation.UnreadCount + 1)
                };

                // Remove from current position and add to top (most recent)
                conversations.Remove(conversation);
                conversations.Insert(0, updatedConversation);

                // Increment global unread count if not in this conversation and message from others
                if (!isCurrentConversation && !isMyMessage)
                {
                    AppState.IncrementUnreadMessages();
                }
            }
            else if (message.SenderId != currentUserId)
            {
                // New conversation from someone else - reload the list
                _ = LoadConversationsAndChannels();
            }

            StateHasChanged();
        });
    }

    private void HandleNewChannelMessage(ChannelMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Skip already processed messages (prevents duplicate SignalR notifications)
            if (!processedMessageIds.Add(message.Id))
                return;

            if (message.ChannelId == selectedChannelId)
            {
                // Check if message is already being added by another handler (race condition protection)
                if (pendingMessageAdds.Contains(message.Id))
                    return;

                // Check if this is our own message (already added optimistically)
                var existingIndex = channelMessages.FindIndex(m => m.Id == message.Id);

                if (existingIndex >= 0)
                {
                    // REPLACE the optimistic message with the real one from the backend
                    // Apply pending read receipts (race condition: mark-as-read arrived before HTTP response)
                    var readByList = new List<Guid>(message.ReadBy ?? []);
                    var appliedReceipts = new List<Guid>();

                    foreach (var (userId, readAtUtc) in pendingChannelReadReceipts)
                    {
                        // CRITICAL: Never add sender to their own message's ReadBy list
                        if (userId != message.SenderId && readAtUtc >= message.CreatedAtUtc && !readByList.Contains(userId))
                        {
                            readByList.Add(userId);
                            appliedReceipts.Add(userId);
                        }
                    }

                    // Update message with applied receipts
                    var updatedMessage = readByList.Count > (message.ReadBy?.Count ?? 0)
                        ? message with { ReadBy = readByList, ReadByCount = readByList.Count }
                        : message;

                    // Create a new list instance to trigger Blazor parameter change detection
                    var updatedList = new List<ChannelMessageDto>(channelMessages);
                    updatedList[existingIndex] = updatedMessage;
                    channelMessages = updatedList;

                    // Remove applied pending receipts
                    foreach (var userId in appliedReceipts)
                        pendingChannelReadReceipts.Remove(userId);
                }
                else
                {
                    // Mark as pending to prevent HTTP handler from adding it
                    pendingMessageAdds.Add(message.Id);

                    // Message from another user - mark as read if page is visible
                    // ONLY mark if: message is from another user AND page is visible
                    // Use single message endpoint for individual new messages
                    if (message.SenderId != currentUserId && isPageVisible)
                    {
                        try
                        {
                            await ChannelService.MarkSingleMessageAsReadAsync(message.ChannelId, message.Id);
                        }
                        catch
                        {
                            // Ignore mark-as-read errors
                        }
                    }

                    // Apply pending read receipts before adding
                    var readByList = new List<Guid>(message.ReadBy ?? []);
                    var appliedReceipts = new List<Guid>();

                    foreach (var (userId, readAtUtc) in pendingChannelReadReceipts)
                    {
                        // CRITICAL: Never add sender to their own message's ReadBy list
                        if (userId != message.SenderId && readAtUtc >= message.CreatedAtUtc && !readByList.Contains(userId))
                        {
                            readByList.Add(userId);
                            appliedReceipts.Add(userId);
                        }
                    }

                    // Update message with applied receipts before adding
                    var messageWithReceipts = readByList.Count > (message.ReadBy?.Count ?? 0)
                        ? message with { ReadBy = readByList, ReadByCount = readByList.Count }
                        : message;

                    channelMessages.Add(messageWithReceipts);

                    // Remove applied pending receipts
                    foreach (var userId in appliedReceipts)
                        pendingChannelReadReceipts.Remove(userId);

                    // Remove from pending after adding
                    pendingMessageAdds.Remove(message.Id);
                }
            }

            // Update channel list (for both sent and received messages)
            var channel = channels.FirstOrDefault(c => c.Id == message.ChannelId);
            if (channel != null)
            {
                var isCurrentChannel = message.ChannelId == selectedChannelId;
                var isMyMessage = message.SenderId == currentUserId;

                // Calculate status for own messages
                string? status = null;
                if (isMyMessage)
                {
                    var totalMembers = channel.MemberCount - 1; // Exclude sender
                    if (totalMembers == 0)
                    {
                        status = "Sent";
                    }
                    else if (message.ReadByCount >= totalMembers)
                    {
                        status = "Read";
                    }
                    else if (message.ReadByCount > 0)
                    {
                        status = "Delivered";
                    }
                    else
                    {
                        status = "Sent";
                    }
                }

                // Create updated channel with new message info
                var updatedChannel = channel with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageId = message.Id,
                    LastMessageSenderId = message.SenderId,
                    LastMessageSenderAvatarUrl = message.SenderAvatarUrl,
                    LastMessageStatus = status,
                    UnreadCount = isCurrentChannel ? 0 : (isMyMessage ? channel.UnreadCount : channel.UnreadCount + 1)
                };

                // Remove from current position and add to top (most recent)
                channels.Remove(channel);
                channels.Insert(0, updatedChannel);

                // Increment global unread count if not in this channel and message from others
                if (!isCurrentChannel && !isMyMessage)
                {
                    AppState.IncrementUnreadMessages();
                }
            }

            StateHasChanged();
        });
    }

    private void HandleDirectMessageEdited(DirectMessageDto editedMessage)
    {
        InvokeAsync(() =>
        {
            // Guard: Don't process if component is disposed
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                // Update the message in the list if it's in the current conversation
                if (editedMessage.ConversationId == selectedConversationId)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = editedMessage;
                        needsStateUpdate = true;

                        // Update conversation list if this was the last message
                        if (IsLastMessageInConversation(editedMessage.ConversationId, editedMessage))
                        {
                            UpdateConversationLastMessage(editedMessage.ConversationId, editedMessage.Content);
                        }
                    }

                    // Update reply previews for messages that replied to this edited message
                    for (int i = 0; i < directMessages.Count; i++)
                    {
                        var msg = directMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            directMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                        }
                    }
                }

                // ALWAYS update conversation list if this edited message is the last message
                // (even if user is not currently viewing the conversation)
                var conversation = conversations.FirstOrDefault(c => c.Id == editedMessage.ConversationId);
                if (conversation != null && IsLastMessageInConversation(editedMessage.ConversationId, editedMessage))
                {
                    UpdateConversationLastMessage(editedMessage.ConversationId, editedMessage.Content);
                    needsStateUpdate = true;
                }

                // Consolidated state update (only once at the end)
                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently ignore errors to prevent runtime crash
                // In production, consider logging this error
            }

            return Task.CompletedTask;
        });
    }

    private void HandleDirectMessageDeleted(DirectMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            // Guard: Don't process if component is disposed
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                // Update the message in the list if it's in the current conversation
                if (deletedMessage.ConversationId == selectedConversationId)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = deletedMessage; // Use the deleted DTO from server
                        needsStateUpdate = true;

                        // Update reply previews for messages that replied to this deleted message
                        for (int i = 0; i < directMessages.Count; i++)
                        {
                            var msg = directMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                directMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                            }
                        }
                    }
                }

                // ALWAYS update conversation list if this deleted message is the last message
                // (even if user is not currently viewing the conversation)
                var conversation = conversations.FirstOrDefault(c => c.Id == deletedMessage.ConversationId);
                if (conversation != null && IsLastMessageInConversation(deletedMessage.ConversationId, deletedMessage))
                {
                    UpdateConversationLastMessage(deletedMessage.ConversationId, "This message was deleted");
                    needsStateUpdate = true;
                }

                // Consolidated state update (only once at the end)
                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently ignore errors to prevent runtime crash
                // In production, consider logging this error
            }

            return Task.CompletedTask;
        });
    }

    private void HandleChannelMessageEdited(ChannelMessageDto editedMessage)
    {
        InvokeAsync(() =>
        {
            // Guard: Don't process if component is disposed
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                // Update the message in the list if it's in the current channel
                if (editedMessage.ChannelId == selectedChannelId)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);

                        // IMPORTANT: Only update content and IsEdited field, preserve ReadByCount/TotalMemberCount
                        // Backend GetByIdAsDtoAsync doesn't populate these fields, so we must preserve them
                        var updatedMessage = message with
                        {
                            Content = editedMessage.Content,
                            IsEdited = editedMessage.IsEdited,
                            EditedAtUtc = editedMessage.EditedAtUtc
                        };

                        channelMessages[index] = updatedMessage;
                        needsStateUpdate = true;

                        // Update channel list if this was the last message
                        if (IsLastMessageInChannel(editedMessage.ChannelId, updatedMessage))
                        {
                            UpdateChannelLastMessage(editedMessage.ChannelId, updatedMessage.Content, message.SenderDisplayName);
                        }
                    }

                    // Update reply previews for messages that replied to this edited message
                    for (int i = 0; i < channelMessages.Count; i++)
                    {
                        var msg = channelMessages[i];
                        if (msg.ReplyToMessageId == editedMessage.Id && msg.ReplyToContent != editedMessage.Content)
                        {
                            channelMessages[i] = msg with { ReplyToContent = editedMessage.Content };
                        }
                    }
                }

                // ALWAYS update channel list if this edited message is the last message
                // (even if user is not currently viewing the channel)
                var channel = channels.FirstOrDefault(c => c.Id == editedMessage.ChannelId);
                if (channel != null && IsLastMessageInChannel(editedMessage.ChannelId, editedMessage))
                {
                    UpdateChannelLastMessage(editedMessage.ChannelId, editedMessage.Content, editedMessage.SenderDisplayName);
                    needsStateUpdate = true;
                }

                // Consolidated state update (only once at the end)
                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently ignore errors to prevent runtime crash
                // In production, consider logging this error
            }

            return Task.CompletedTask;
        });
    }

    private void HandleChannelMessageDeleted(ChannelMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            // Guard: Don't process if component is disposed
            if (_disposed) return Task.CompletedTask;

            try
            {
                var needsStateUpdate = false;

                // Update the message in the list if it's in the current channel
                if (deletedMessage.ChannelId == selectedChannelId)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);
                        channelMessages[index] = deletedMessage; // Use the deleted DTO from server
                        needsStateUpdate = true;

                        // Update reply previews for messages that replied to this deleted message
                        for (int i = 0; i < channelMessages.Count; i++)
                        {
                            var msg = channelMessages[i];
                            if (msg.ReplyToMessageId == deletedMessage.Id)
                            {
                                channelMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                            }
                        }
                    }
                }

                // ALWAYS update channel list if this deleted message is the last message
                // (even if user is not currently viewing the channel)
                var channel = channels.FirstOrDefault(c => c.Id == deletedMessage.ChannelId);
                if (channel != null && IsLastMessageInChannel(deletedMessage.ChannelId, deletedMessage))
                {
                    UpdateChannelLastMessage(deletedMessage.ChannelId, "This message was deleted", deletedMessage.SenderDisplayName);
                    needsStateUpdate = true;
                }

                // Consolidated state update (only once at the end)
                if (needsStateUpdate && !_disposed)
                {
                    StateHasChanged();
                }
            }
            catch (Exception)
            {
                // Silently ignore errors to prevent runtime crash
                // In production, consider logging this error
            }

            return Task.CompletedTask;
        });
    }

    private void HandleMessageRead(Guid conversationId, Guid messageId, Guid readBy, DateTime readAtUtc)
    {
        InvokeAsync(() =>
        {
            // Update message in SENDER's view (regardless of which conversation is selected)
            // This allows sender to see read receipt even if they switched to another conversation
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                var index = directMessages.IndexOf(message);
                directMessages[index] = message with { IsRead = true, ReadAtUtc = readAtUtc };
            }
            else if (conversationId == selectedConversationId)
            {
                // If message not found but we're viewing this conversation,
                // store as pending read receipt (for race condition case)
                pendingReadReceipts[messageId] = (readBy, readAtUtc);
            }

            // Update conversation list status if this is the last message
            var conversation = conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conversation != null && conversation.LastMessageSenderId == currentUserId)
            {
                // Update status to "Read" for sender's last message
                var updatedConversation = conversation with
                {
                    LastMessageStatus = "Read"
                };

                var index = conversations.IndexOf(conversation);
                if (index >= 0)
                {
                    conversations[index] = updatedConversation;
                }
            }

            StateHasChanged();
        });
    }

    private void HandleTypingInConversation(Guid conversationId, Guid userId, bool isTyping)
    {
        // Only track typing state for OTHER users, not yourself
        if (userId != currentUserId)
        {
            InvokeAsync(() =>
            {
                // Update typing state for this conversation (for conversation list)
                if (isTyping)
                {
                    conversationTypingState[conversationId] = true;
                }
                else
                {
                    conversationTypingState.Remove(conversationId);
                }

                // ALSO update typingUsers list if we're currently viewing this conversation (for chat header)
                // For direct messages, we just add a placeholder since the header shows "typing..." without username
                if (conversationId == selectedConversationId)
                {
                    if (isTyping)
                    {
                        if (!typingUsers.Contains("typing"))
                        {
                            typingUsers.Add("typing");
                        }
                    }
                    else
                    {
                        typingUsers.Clear();
                    }
                }

                // Use debounced update for typing events (very frequent)
                ScheduleStateUpdate();
            });
        }
    }

    private void HandleTypingInChannel(Guid channelId, Guid userId, string username, bool isTyping)
    {
        // Only track typing state for OTHER users, not yourself
        if (userId != currentUserId)
        {
            InvokeAsync(() =>
            {
                // Update typing state for conversation list (just bool, no username)
                if (isTyping)
                {
                    // Track usernames for chat header
                    if (!channelTypingUsers.TryGetValue(channelId, out List<string>? value))
                    {
                        value = [];
                        channelTypingUsers[channelId] = value;
                    }
                    if (!value.Contains(username))
                    {
                        value.Add(username);
                    }
                }
                else
                {
                    // Remove from username list
                    if (channelTypingUsers.TryGetValue(channelId, out List<string>? value))
                    {
                        value.Remove(username);
                        if (channelTypingUsers[channelId].Count == 0)
                        {
                            channelTypingUsers.Remove(channelId);
                        }
                    }
                }

                // ALSO update typingUsers list if we're currently viewing this channel (for chat header)
                if (channelId == selectedChannelId)
                {
                    if (isTyping)
                    {
                        if (!typingUsers.Contains(username))
                        {
                            typingUsers.Add(username);
                        }
                    }
                    else
                    {
                        typingUsers = typingUsers.Where(u => u != username).ToList();
                    }
                }

                // Use debounced update for typing events (very frequent)
                ScheduleStateUpdate();
            });
        }
    }

    private void HandleUserOnline(Guid userId)
    {
        InvokeAsync(() =>
        {
            if (userId == recipientUserId)
            {
                isRecipientOnline = true;
            }

            var conversation = conversations.FirstOrDefault(c => c.OtherUserId == userId);
            if (conversation != null)
            {
                var index = conversations.IndexOf(conversation);
                conversations[index] = conversation with { IsOtherUserOnline = true };
            }

            // Use debounced update for online status
            ScheduleStateUpdate();
        });
    }

    private void HandleUserOffline(Guid userId)
    {
        InvokeAsync(() =>
        {
            if (userId == recipientUserId)
            {
                isRecipientOnline = false;
            }

            var conversation = conversations.FirstOrDefault(c => c.OtherUserId == userId);
            if (conversation != null)
            {
                var index = conversations.IndexOf(conversation);
                conversations[index] = conversation with { IsOtherUserOnline = false };
            }

            // Use debounced update for offline status
            ScheduleStateUpdate();
        });
    }

    #endregion

    #region Dialog Mehtods
    private void OpenNewConversationDialog()
    {
        showNewConversationDialog = true;
        userSearchQuery = "";
        userSearchResults.Clear();
        StateHasChanged();
    }
    private void CloseNewConversationDialog()
    {
        showNewConversationDialog = false;
        _searchCts?.Cancel();
        StateHasChanged();
    }
    private void OpenNewChannelDialog()
    {
        showNewChannelDialog = true;
        newChannelRequest = new CreateChannelRequest();
        StateHasChanged();
    }
    private void CloseNewChannelDialog()
    {
        showNewChannelDialog = false;
        StateHasChanged();
    }

    #endregion

    #region Messages
    private async Task LoadMoreMessages()
    {
        if (isLoadingMoreMessages || !hasMoreMessages) return;

        isLoadingMoreMessages = true;
        StateHasChanged();

        try
        {
            // İlk load more-dan sonra pageSize-i 100-ə çevir
            pageSize = 100;

            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await LoadDirectMessages();
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await LoadChannelMessages();
            }
        }
        finally
        {
            isLoadingMoreMessages = false;
            StateHasChanged();
        }
    }
    private async Task SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        isSendingMessage = true;
        StateHasChanged();

        try
        {
            // Handle pending conversation - create it first
            if (isPendingConversation && pendingUser != null)
            {
                var createResult = await ConversationService.StartConversationAsync(pendingUser.Id);
                if (!createResult.IsSuccess)
                {
                    ShowError(createResult.Error ?? "Failed to create conversation");
                    return;
                }

                // Set the conversation ID and clear pending state
                selectedConversationId = createResult.Value;
                isPendingConversation = false;
                pendingUser = null;

                // Join the SignalR group
                await SignalRService.JoinConversationAsync(selectedConversationId.Value);

                // Reload conversations to include the new one in the list
                await LoadConversationsAndChannels();
            }

            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.SendMessageAsync(
                    selectedConversationId.Value,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
                    isForwarded: false);

                if (result.IsSuccess)
                {
                    var messageTime = DateTime.UtcNow;
                    var messageId = result.Value;

                    // Check if there's a pending read receipt for this message
                    bool hasReadReceipt = pendingReadReceipts.TryGetValue(messageId, out var readReceipt);

                    var newMessage = new DirectMessageDto(
                        messageId,
                        selectedConversationId.Value,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        recipientUserId,
                        content,
                        null,                                           // FileId
                        false,                                          // IsEdited - new message is never edited
                        false,                                          // IsDeleted
                        hasReadReceipt,                                 // IsRead - apply pending read receipt if exists
                        false,                                          // IsPinned - new message is never pinned
                        0,                                              // ReactionCount
                        messageTime,                                    // CreatedAtUtc
                        null,                                           // EditedAtUtc
                        hasReadReceipt ? readReceipt.readAtUtc : null,  // ReadAtUtc
                        null,                                           // PinnedAtUtc - new message is never pinned
                        replyToMessageId,
                        replyToContent,
                        replyToSenderName,
                        false);                                         // IsForwarded

                    // Before adding optimistic message, check if it already exists
                    // (in case SignalR arrived faster than HTTP response)
                    if (!directMessages.Any(m => m.Id == messageId))
                    {
                        directMessages.Add(newMessage);
                    }

                    // Remove from pending if applied
                    if (hasReadReceipt)
                    {
                        pendingReadReceipts.Remove(messageId);
                    }

                    // Clear reply state after sending
                    CancelReply();

                    // Update conversation locally without reloading
                    UpdateConversationLocally(selectedConversationId.Value, content, messageTime);
                }
                else
                {
                    ShowError(result.Error ?? "Failed to send message");
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.SendMessageAsync(
                    selectedChannelId.Value,
                    content,
                    fileId: null,
                    replyToMessageId: replyToMessageId,
                    isForwarded: false);

                if (result.IsSuccess)
                {
                    var messageTime = DateTime.UtcNow;

                    // Check if message is already being added by SignalR handler (race condition protection)
                    if (pendingMessageAdds.Contains(result.Value))
                    {
                        // SignalR already adding it - skip
                    }
                    // Check if message already exists in the list
                    else if (channelMessages.Any(m => m.Id == result.Value))
                    {
                        // Already added by SignalR - skip
                    }
                    else
                    {
                        // Mark as pending to prevent SignalR handler from adding it
                        pendingMessageAdds.Add(result.Value);

                        // Add message locally (optimistic UI)
                        // TotalMemberCount = all active members except sender
                        var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

                        // Check for pending read receipts (race condition: mark-as-read arrived before HTTP completed)
                        var readByList = new List<Guid>();
                        var appliedReceipts = new List<Guid>(); // Track which receipts we applied

                        foreach (var (userId, readAtUtc) in pendingChannelReadReceipts)
                        {
                            // CRITICAL: Never add sender to their own message's ReadBy list
                            if (userId != currentUserId && readAtUtc >= messageTime)
                            {
                                readByList.Add(userId);
                                appliedReceipts.Add(userId);
                            }
                        }

                        var newMessage = new ChannelMessageDto(
                            result.Value,
                            selectedChannelId.Value,
                            currentUserId,
                            UserState.CurrentUser?.Username ?? "",
                            UserState.CurrentUser?.DisplayName ?? "",
                            UserState.CurrentUser?.AvatarUrl,
                            content,
                            null,
                            false,
                            false,
                            false,
                            0,
                            messageTime,
                            null,
                            null,
                            replyToMessageId,
                            replyToContent,
                            replyToSenderName,
                            false,
                            ReadByCount: readByList.Count,
                            TotalMemberCount: totalMembers,
                            ReadBy: readByList,
                            Reactions: []);

                        channelMessages.Add(newMessage);

                        // Remove applied pending receipts
                        foreach (var userId in appliedReceipts)
                        {
                            pendingChannelReadReceipts.Remove(userId);
                        }

                        // Remove from pending after adding
                        pendingMessageAdds.Remove(result.Value);
                    }

                    // Clear reply state after sending
                    CancelReply();

                    // Update channel list locally without reloading
                    UpdateChannelLocally(selectedChannelId.Value, content, messageTime, UserState.CurrentUser?.DisplayName);
                }
                else
                {
                    ShowError(result.Error ?? "Failed to send message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to send message: " + ex.Message);
        }
        finally
        {
            isSendingMessage = false;
            StateHasChanged();
        }
    }
    private async Task EditMessage((Guid messageId, string content) edit)
    {
        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.EditMessageAsync(
                    selectedConversationId.Value,
                    edit.messageId,
                    edit.content);

                if (result.IsSuccess)
                {
                    // Update local message only if content actually changed
                    var message = directMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null && message.Content != edit.content)
                    {
                        var index = directMessages.IndexOf(message);
                        var updatedMessage = message with { Content = edit.content, IsEdited = true };
                        directMessages[index] = updatedMessage;

                        // Update conversation list locally if this is the last message
                        if (IsLastMessageInConversation(selectedConversationId.Value, updatedMessage))
                        {
                            UpdateConversationLastMessage(selectedConversationId.Value, edit.content);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to edit message");
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.EditMessageAsync(
                    selectedChannelId.Value,
                    edit.messageId,
                    edit.content);

                if (result.IsSuccess)
                {
                    // Update local message only if content actually changed
                    var message = channelMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null && message.Content != edit.content)
                    {
                        var index = channelMessages.IndexOf(message);
                        var updatedMessage = message with { Content = edit.content, IsEdited = true };
                        channelMessages[index] = updatedMessage;

                        // Update channel list locally if this is the last message
                        if (IsLastMessageInChannel(selectedChannelId.Value, updatedMessage))
                        {
                            UpdateChannelLastMessage(selectedChannelId.Value, edit.content, message.SenderDisplayName);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to edit message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to edit message: " + ex.Message);
        }
    }
    private async Task DeleteMessage(Guid messageId)
    {
        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.DeleteMessageAsync(
                    selectedConversationId.Value,
                    messageId);

                if (result.IsSuccess)
                {
                    var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        var deletedMessage = message with { IsDeleted = true, Content = "" };
                        directMessages[index] = deletedMessage;

                        // Update conversation list locally if this was the last message
                        if (IsLastMessageInConversation(selectedConversationId.Value, deletedMessage))
                        {
                            UpdateConversationLastMessage(selectedConversationId.Value, "This message was deleted");
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to delete message");
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.DeleteMessageAsync(
                    selectedChannelId.Value,
                    messageId);

                if (result.IsSuccess)
                {
                    var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);
                        var deletedMessage = message with { IsDeleted = true, Content = "" };
                        channelMessages[index] = deletedMessage;

                        // Update channel list locally if this was the last message
                        if (IsLastMessageInChannel(selectedChannelId.Value, deletedMessage))
                        {
                            UpdateChannelLastMessage(selectedChannelId.Value, "This message was deleted", message.SenderDisplayName);
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    ShowError(result.Error ?? "Failed to delete message");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to delete message: " + ex.Message);
        }
    }
    private async Task AddReaction((Guid messageId, string emoji) reaction)
    {
        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.ToggleReactionAsync(
                    selectedConversationId.Value,
                    reaction.messageId,
                    reaction.emoji);

                if (result.IsSuccess && result.Value != null)
                {
                    // Update the message reactions in the local list
                    UpdateMessageReactions(reaction.messageId, result.Value.Reactions);
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.ToggleReactionAsync(
                    selectedChannelId.Value,
                    reaction.messageId,
                    reaction.emoji);

                if (result.IsSuccess && result.Value != null)
                {
                    // Update the message reactions in the local list
                    UpdateChannelMessageReactions(reaction.messageId, result.Value);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to toggle reaction: " + ex.Message);
        }
    }
    private async Task HandleTyping(bool isTyping)
    {
        if (isDirectMessage && selectedConversationId.HasValue && recipientUserId != Guid.Empty)
        {
            await SignalRService.SendTypingInConversationAsync(selectedConversationId.Value, recipientUserId, isTyping);
        }
        else if (!isDirectMessage && selectedChannelId.HasValue)
        {
            await SignalRService.SendTypingInChannelAsync(selectedChannelId.Value, isTyping);
        }
    }

    private void UpdateMessageReactions(Guid messageId, List<ReactionSummary> reactions)
    {
        var message = directMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            var totalCount = reactions.Sum(r => r.Count);
            var index = directMessages.IndexOf(message);

            // Update the message with new reaction data
            var updatedMessage = message with
            {
                ReactionCount = totalCount,
                Reactions = reactions.Select(r => new MessageReactionDto(r.Emoji, r.Count, r.UserIds)).ToList()
            };

            directMessages[index] = updatedMessage;
            StateHasChanged();
        }
    }

    private void UpdateChannelMessageReactions(Guid messageId, List<ChannelMessageReactionDto> reactions)
    {
        var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            var totalCount = reactions.Sum(r => r.Count);
            var index = channelMessages.IndexOf(message);

            // Update the message with new reaction data
            var updatedMessage = message with
            {
                ReactionCount = totalCount,
                Reactions = reactions
            };

            channelMessages[index] = updatedMessage;
            StateHasChanged();
        }
    }

    private void HandleReactionToggled(Guid conversationId, Guid messageId, List<ReactionSummary> reactions)
    {
        InvokeAsync(() =>
        {
            try
            {
                // Guard: Don't process if component is disposed
                if (_disposed) return;

                if (selectedConversationId.HasValue && selectedConversationId.Value == conversationId)
                {
                    UpdateMessageReactions(messageId, reactions);
                }
            }
            catch (Exception ex)
            {
                // Silently handle exceptions to prevent runtime crash
                Console.WriteLine($"Error handling reaction toggled: {ex.Message}");
            }
        });
    }

    private void HandleChannelMessageReactionsUpdated(Guid messageId, List<ChannelMessageReactionDto> reactions)
    {
        InvokeAsync(() =>
        {
            try
            {
                // Guard: Don't process if component is disposed
                if (_disposed) return;

                if (!selectedChannelId.HasValue)
                    return;

                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);

                    // Simply replace all reactions (no complex logic, just direct update)
                    var updatedMessage = message with
                    {
                        ReactionCount = reactions.Sum(r => r.Count),
                        Reactions = reactions
                    };

                    channelMessages[index] = updatedMessage;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Silently handle exceptions to prevent runtime crash
                Console.WriteLine($"Error handling channel message reactions updated: {ex.Message}");
            }
        });
    }

    private void HandleChannelMessagesRead(Guid channelId, Guid userId, List<Guid> messageIds)
    {
        InvokeAsync(() =>
        {
            bool updated = false;

            // Update messages in current view if viewing this channel
            if (selectedChannelId.HasValue && selectedChannelId.Value == channelId)
            {
                var updatedList = new List<ChannelMessageDto>(channelMessages);

                // Update read status for the specified messages without reloading
                for (int i = 0; i < updatedList.Count; i++)
                {
                    var message = updatedList[i];
                    if (messageIds.Contains(message.Id))
                    {
                        // Don't add sender to their own ReadBy list
                        if (message.SenderId == userId)
                            continue;

                        // Create new ReadBy list with the userId added
                        var newReadBy = message.ReadBy != null
                            ? new List<Guid>(message.ReadBy)
                            : new List<Guid>();

                        // Add userId if not already present
                        if (!newReadBy.Contains(userId))
                        {
                            newReadBy.Add(userId);

                            // Replace the message with updated ReadBy and ReadByCount
                            // This creates a new object reference, triggering Blazor's change detection
                            updatedList[i] = message with
                            {
                                ReadBy = newReadBy,
                                ReadByCount = newReadBy.Count
                            };

                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    // Replace the entire list to trigger Blazor's change detection on the parent component
                    channelMessages = updatedList;
                }
            }

            // Update channel list status if last message was sent by current user and was read
            var channel = channels.FirstOrDefault(c => c.Id == channelId);
            if (channel != null &&
                channel.LastMessageSenderId == currentUserId &&
                channel.LastMessageId.HasValue &&
                messageIds.Contains(channel.LastMessageId.Value))
            {
                // Last message was read by someone
                // Find the message in current view to get exact ReadByCount
                var lastMessageInView = channelMessages.FirstOrDefault(m => m.Id == channel.LastMessageId.Value);

                string newStatus;
                if (lastMessageInView != null)
                {
                    // We have the message in view - use its ReadByCount
                    var totalMembers = channel.MemberCount - 1;
                    if (totalMembers == 0)
                    {
                        newStatus = "Sent";
                    }
                    else if (lastMessageInView.ReadByCount >= totalMembers)
                    {
                        newStatus = "Read";
                    }
                    else if (lastMessageInView.ReadByCount > 0)
                    {
                        newStatus = "Delivered";
                    }
                    else
                    {
                        newStatus = "Sent";
                    }
                }
                else
                {
                    // Message not in view - at least one person read it
                    newStatus = "Delivered";
                }

                var updatedChannel = channel with
                {
                    LastMessageStatus = newStatus
                };

                var index = channels.IndexOf(channel);
                if (index >= 0)
                {
                    channels[index] = updatedChannel;
                    updated = true;
                }
            }

            if (updated)
            {
                StateHasChanged();
            }
        });
    }
    #endregion

    #region Conversation
    private void UpdateConversationLocally(Guid conversationId, string lastMessage, DateTime messageTime)
    {
        var conversation = conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            // Create updated conversation with new last message
            var updatedConversation = conversation with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime
            };

            // Remove from current position
            conversations.Remove(conversation);

            // Add to the top of the list (most recent)
            conversations.Insert(0, updatedConversation);

            // Trigger UI update
            StateHasChanged();
        }
    }
    private void UpdateChannelLocally(Guid channelId, string lastMessage, DateTime messageTime, string? senderName = null)
    {
        var channel = channels.FirstOrDefault(c => c.Id == channelId);
        if (channel != null)
        {
            // Create updated channel with new last message
            var updatedChannel = channel with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime
            };

            // Remove from current position
            channels.Remove(channel);

            // Add to the top of the list (most recent)
            channels.Insert(0, updatedChannel);

            // Trigger UI update
            StateHasChanged();
        }
    }
    private async Task LoadDirectMessages()
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = await ConversationService.GetMessagesAsync(
                selectedConversationId!.Value,
                pageSize,
                oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // Filter out duplicates before adding to list
                    var existingIds = directMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc);

                    directMessages.InsertRange(0, newMessages);
                    oldestMessageDate = DateTime.SpecifyKind(messages.Min(m => m.CreatedAtUtc), DateTimeKind.Utc);
                    hasMoreMessages = messages.Count >= pageSize;
                }
                else
                {
                    hasMoreMessages = false;
                }

                // Calculate unread separator position
                CalculateUnreadSeparatorPosition(
                    messages,
                    m => !m.IsRead && m.SenderId != currentUserId,
                    m => m.Id,
                    m => m.CreatedAtUtc
                );

                // Mark messages as read using smart threshold
                // Use smart threshold: 5+ messages = bulk, <5 messages = individual
                var unreadMessages = messages.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList();
                if (unreadMessages.Count > 0)
                {
                    if (unreadMessages.Count >= 5)
                    {
                        // Bulk operation for 5+ unread messages
                        await ConversationService.MarkAllAsReadAsync(selectedConversationId!.Value);
                    }
                    else
                    {
                        // Individual operations for 1-4 unread messages (parallel)
                        var markTasks = unreadMessages.Select(msg =>
                            ConversationService.MarkAsReadAsync(selectedConversationId!.Value, msg.Id)
                        );
                        await Task.WhenAll(markTasks);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load messages: " + ex.Message);
        }
        finally
        {
            isLoadingMessages = false;
            StateHasChanged();
        }
    }
    private async Task SelectConversation(DirectConversationDto conversation)
    {
        // Exit selection mode when switching conversations
        if (isSelectMode)
        {
            ToggleSelectMode();
        }

        // Guard: Prevent concurrent selection operations (race condition)
        if (_isSelecting || _disposed)
        {
            return;
        }

        // Guard: Null check
        if (conversation == null)
        {
            return;
        }

        // Guard: Already selected
        if (selectedConversationId.HasValue && selectedConversationId.Value == conversation.Id)
        {
            return;
        }

        _isSelecting = true;

        try
        {
            // Mark previous channel as read before switching to conversation
            if (selectedChannelId.HasValue)
            {
                try
                {
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                catch
                {
                    // Ignore errors when marking as read
                }
            }

            // Save current draft before switching (currentDraft is updated by MessageInput)
            // Note: currentDraft is synced with MessageInput via OnDraftChanged callback

            // Clear pending conversation state
            isPendingConversation = false;
            pendingUser = null;

            // LAZY LOADING: Leave previous conversation group before joining new one
            // This reduces active group memberships and improves scalability
            if (selectedConversationId.HasValue && selectedConversationId.Value != conversation.Id)
            {
                await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
            }

            // Leave previous channel group if switching from channel to conversation
            if (selectedChannelId.HasValue)
            {
                await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
            }

            // Mark conversation as read and update global unread count
            if (conversation.UnreadCount > 0)
            {
                AppState.DecrementUnreadMessages(conversation.UnreadCount);

                // Update local conversation list to show 0 unread
                var index = conversations.IndexOf(conversation);
                if (index >= 0)
                {
                    conversations[index] = conversation with { UnreadCount = 0 };
                }
            }

            directMessages.Clear();
            channelMessages.Clear();
            hasMoreMessages = true;
            oldestMessageDate = null;
            typingUsers.Clear();
            pendingReadReceipts.Clear(); // Clear pending read receipts when changing conversations
            pendingChannelReadReceipts.Clear(); // Clear pending channel read receipts when changing conversations
            pendingMessageAdds.Clear(); // Clear pending message adds when changing conversations
            favoriteMessageIds.Clear(); // Clear favorites when changing conversations
            showSearchPanel = false; // Close search panel when changing conversations
            pageSize = 50; // Reset page size to 50 for new conversation

            // Auto-unmark read later if user saw separator (channel)
            if (selectedChannelId.HasValue && lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
            {
                try
                {
                    await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, lastReadLaterMessageId.Value);

                    // Update channels list
                    var channelIndex = channels.FindIndex(c => c.Id == selectedChannelId.Value);
                    if (channelIndex >= 0)
                    {
                        channels[channelIndex] = channels[channelIndex] with { LastReadLaterMessageId = null };
                        channels = channels.ToList();
                    }
                }
                catch { }
            }

            // Auto-unmark read later if user saw separator (conversation)
            if (selectedConversationId.HasValue && lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
            {
                try
                {
                    await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, lastReadLaterMessageId.Value);

                    // Update conversations list
                    var conversationIndex = conversations.FindIndex(c => c.Id == selectedConversationId.Value);
                    if (conversationIndex >= 0)
                    {
                        conversations[conversationIndex] = conversations[conversationIndex] with { LastReadLaterMessageId = null };
                        conversations = conversations.ToList();
                    }
                }
                catch { }
            }

            // Reset unread separator
            unreadSeparatorAfterMessageId = null;
            shouldCalculateUnreadSeparator = conversation.UnreadCount > 0;
            currentMemberLastReadAtUtc = null;

            // Set read later marker from conversation DTO
            lastReadLaterMessageId = conversation.LastReadLaterMessageId;
            lastReadLaterMessageIdOnEntry = conversation.LastReadLaterMessageId; // Track on entry

            // Set conversation details AFTER clearing
            selectedConversationId = conversation.Id;
            selectedChannelId = null;
            isDirectMessage = true;

            recipientName = conversation.OtherUserDisplayName;
            recipientAvatarUrl = conversation.OtherUserAvatarUrl;
            recipientUserId = conversation.OtherUserId;
            isRecipientOnline = conversation.IsOtherUserOnline;

            // Load draft for this conversation
            currentDraft = LoadDraft(conversation.Id, null);

            // Join SignalR group
            await SignalRService.JoinConversationAsync(conversation.Id);

            // Load messages
            await LoadDirectMessages();

            // Load pinned message count
            await LoadPinnedDirectMessageCount();

            // Update URL
            NavigationManager.NavigateTo($"/messages/conversation/{conversation.Id}", false);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to select conversation: {ex.Message}");
        }
        finally
        {
            _isSelecting = false;
        }
    }
    private async Task LoadConversationsAndChannels()
    {
        isLoadingList = true;
        StateHasChanged();
        try
        {
            var conversationsTask = ConversationService.GetConversationsAsync();
            var channelsTask = ChannelService.GetMyChannelsAsync();

            await Task.WhenAll(conversationsTask, channelsTask);

            var conversationsResult = await conversationsTask;
            var channelsResult = await channelsTask;
            if (conversationsResult.IsSuccess)
            {
                conversations = conversationsResult.Value ?? [];
            }

            if (channelsResult.IsSuccess)
            {
                channels = channelsResult.Value ?? [];
            }

            // NO LONGER NEEDED: Bulk join removed
            // Hybrid notification pattern (group + direct connections) handles notifications
            // without requiring users to join all channel/conversation groups on page load
            // Groups are now joined only when user actively views a conversation/channel (lazy loading)

            // Refresh online status for all conversation participants
            await RefreshOnlineStatus();

            // Update global unread message count
            UpdateGlobalUnreadCount();
        }
        catch (Exception ex)
        {
            ShowError("Failed to load conversations: " + ex.Message);
        }
        finally
        {
            isLoadingList = false;
            StateHasChanged();
        }
    }
    private async Task StartConversationWithUser(Guid userId)
    {
        // Check if conversation already exists
        var existingConversation = conversations.FirstOrDefault(c => c.OtherUserId == userId);
        if (existingConversation != null)
        {
            // Conversation exists, just select it
            CloseNewConversationDialog();
            await SelectConversation(existingConversation);
            return;
        }

        // Get user info from search results
        var user = userSearchResults.FirstOrDefault(u => u.Id == userId);
        if (user == null) return;

        // Set up pending conversation (don't create yet)
        isPendingConversation = true;
        pendingUser = user;
        selectedConversationId = null;
        selectedChannelId = null;
        isDirectMessage = true;

        // Set up chat area UI
        recipientUserId = user.Id;
        recipientName = user.DisplayName;
        recipientAvatarUrl = user.AvatarUrl;
        isRecipientOnline = false; // Will be updated via SignalR

        // Clear ALL messages and state since this is a new conversation
        directMessages.Clear();
        channelMessages.Clear();
        typingUsers.Clear();
        pendingReadReceipts.Clear();
        pendingChannelReadReceipts.Clear();
        pendingMessageAdds.Clear();
        hasMoreMessages = false;
        oldestMessageDate = null;
        pageSize = 50;

        // Load draft for this pending user (if any)
        currentDraft = LoadDraft(null, null, user.Id);

        CloseNewConversationDialog();
        StateHasChanged();
    }

    #endregion

    #region Channel
    private async Task LoadChannelMessages(bool reload = false)
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            // If reload, fetch latest messages (not paginated)
            var result = await ChannelService.GetMessagesAsync(
                selectedChannelId!.Value,
                reload ? 50 : pageSize,  // Reload: get latest 50 messages
                reload ? null : oldestMessageDate);  // Reload: no beforeUtc filter

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    if (reload)
                    {
                        // Replace all messages with fresh data from backend
                        channelMessages.Clear();
                        channelMessages.AddRange(messages.OrderBy(m => m.CreatedAtUtc));
                        oldestMessageDate = DateTime.SpecifyKind(messages.Min(m => m.CreatedAtUtc), DateTimeKind.Utc);
                        hasMoreMessages = messages.Count >= 50;
                    }
                    else
                    {
                        // Pagination: filter out duplicates before adding to list
                        var existingIds = channelMessages.Select(m => m.Id).ToHashSet();
                        var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc);

                        channelMessages.InsertRange(0, newMessages);
                        oldestMessageDate = DateTime.SpecifyKind(messages.Min(m => m.CreatedAtUtc), DateTimeKind.Utc);
                        hasMoreMessages = messages.Count >= pageSize;
                    }
                }
                else
                {
                    hasMoreMessages = false;
                }

                // Calculate unread separator position (for channels)
                // Use ReadBy property instead of timestamp (same pattern as direct messages)
                // NOTE: Mark-as-read happens when LEAVING channel, not when loading messages
                CalculateUnreadSeparatorPosition(
                    messages,
                    m => m.SenderId != currentUserId && (m.ReadBy == null || !m.ReadBy.Contains(currentUserId)),
                    m => m.Id,
                    m => m.CreatedAtUtc
                );
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to load messages: " + ex.Message);
        }
        finally
        {
            isLoadingMessages = false;
            StateHasChanged();
        }
    }
    private async Task SelectChannel(ChannelDto channel)
    {
        // Exit selection mode when switching channels
        if (isSelectMode)
        {
            ToggleSelectMode();
        }

        // Guard: Prevent concurrent selection operations (race condition)
        if (_isSelecting || _disposed)
        {
            return;
        }

        // Guard: Null check
        if (channel == null)
        {
            return;
        }

        // Guard: Already selected
        if (selectedChannelId.HasValue && selectedChannelId.Value == channel.Id)
        {
            return;
        }

        _isSelecting = true;

        try
        {
            // Mark previous channel as read before switching
            if (selectedChannelId.HasValue && selectedChannelId.Value != channel.Id)
            {
                try
                {
                    await ChannelService.MarkAsReadAsync(selectedChannelId.Value);
                }
                catch
                {
                    // Ignore errors when marking as read
                }

                // Auto-unmark read later if user saw separator
                if (lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
                {
                    try
                    {
                        await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, lastReadLaterMessageId.Value);

                        // Update channels list
                        var channelIndex = channels.FindIndex(c => c.Id == selectedChannelId.Value);
                        if (channelIndex >= 0)
                        {
                            channels[channelIndex] = channels[channelIndex] with { LastReadLaterMessageId = null };
                            channels = channels.ToList();
                        }
                    }
                    catch { }
                }
            }

            // Clear pending conversation state
            isPendingConversation = false;
            pendingUser = null;

        // LAZY LOADING: Leave previous channel group before joining new one
        // This reduces active group memberships and improves scalability
        if (selectedChannelId.HasValue && selectedChannelId.Value != channel.Id)
        {
            await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
        }

        // Leave previous conversation group if switching from conversation to channel
        if (selectedConversationId.HasValue)
        {
            // Auto-unmark read later if user saw separator (conversation)
            if (lastReadLaterMessageId.HasValue && lastReadLaterMessageIdOnEntry.HasValue)
            {
                try
                {
                    await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, lastReadLaterMessageId.Value);

                    // Update conversations list
                    var conversationIndex = conversations.FindIndex(c => c.Id == selectedConversationId.Value);
                    if (conversationIndex >= 0)
                    {
                        conversations[conversationIndex] = conversations[conversationIndex] with { LastReadLaterMessageId = null };
                        conversations = conversations.ToList();
                    }
                }
                catch { }
            }

            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }

        // Mark channel as read and update global unread count
        if (channel.UnreadCount > 0)
        {
            AppState.DecrementUnreadMessages(channel.UnreadCount);

            // Update local channel list to show 0 unread
            var index = channels.IndexOf(channel);
            if (index >= 0)
            {
                channels[index] = channel with { UnreadCount = 0 };
            }
        }

        // IMPORTANT: Clear messages BEFORE setting selectedChannelId
        // This prevents race condition where SignalR events arrive between setting ID and clearing messages
        directMessages.Clear();
        channelMessages.Clear();
        hasMoreMessages = true;
        oldestMessageDate = null;
        typingUsers.Clear();
        pendingReadReceipts.Clear(); // Clear pending read receipts when changing channels
        pendingChannelReadReceipts.Clear(); // Clear pending channel read receipts when changing channels
        pendingMessageAdds.Clear(); // Clear pending message adds when changing channels
        favoriteMessageIds.Clear(); // Clear favorites when changing channels
        showSearchPanel = false; // Close search panel when changing channels
        pageSize = 50; // Reset page size to 50 for new channel

        // Reset unread separator
        unreadSeparatorAfterMessageId = null;
        shouldCalculateUnreadSeparator = channel.UnreadCount > 0;
        currentMemberLastReadAtUtc = channel.CurrentMemberLastReadAtUtc;

        // Set read later marker from channel DTO
        lastReadLaterMessageId = channel.LastReadLaterMessageId;
        lastReadLaterMessageIdOnEntry = channel.LastReadLaterMessageId; // Track on entry

        // Set channel details AFTER clearing
        selectedChannelId = channel.Id;
        selectedConversationId = null;
        isDirectMessage = false;
        selectedChannelName = channel.Name;
        selectedChannelDescription = channel.Description;
        selectedChannelType = channel.Type;
        selectedChannelMemberCount = channel.MemberCount;

        // Load draft for this channel
        currentDraft = LoadDraft(null, channel.Id);

        // Check if current user is admin/owner of this channel
        isChannelAdmin = channel.CreatedBy == currentUserId;
        currentUserChannelRole = isChannelAdmin ? ChannelMemberRole.Owner : ChannelMemberRole.Member;

        // If not owner, check if admin through channel details
        if (!isChannelAdmin)
        {
            var channelDetails = await ChannelService.GetChannelAsync(channel.Id);
            if (channelDetails.IsSuccess && channelDetails.Value != null)
            {
                var currentMember = channelDetails.Value.Members.FirstOrDefault(m => m.UserId == currentUserId);
                if (currentMember != null)
                {
                    currentUserChannelRole = currentMember.Role;
                    isChannelAdmin = currentMember.Role == ChannelMemberRole.Admin || currentMember.Role == ChannelMemberRole.Owner;
                }
            }
        }

            // Join SignalR group
            await SignalRService.JoinChannelAsync(channel.Id);

            // Load messages and pinned count
            // NOTE: LoadChannelMessages now handles mark-as-read (same pattern as LoadDirectMessages)
            await LoadChannelMessages();
            await LoadPinnedMessageCount();

            // Update URL
            NavigationManager.NavigateTo($"/messages/channel/{channel.Id}", false);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to select channel: {ex.Message}");
        }
        finally
        {
            _isSelecting = false;
        }
    }
    private async Task CreateChannel()
    {
        isCreatingChannel = true;
        StateHasChanged();

        try
        {
            var result = await ChannelService.CreateChannelAsync(newChannelRequest);
            if (result.IsSuccess)
            {
                CloseNewChannelDialog();
                await LoadConversationsAndChannels();

                var channel = channels.FirstOrDefault(c => c.Id == result.Value);
                if (channel != null)
                {
                    await SelectChannel(channel);
                }
            }
            else
            {
                ShowError(result.Error ?? "Failed to create channel");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to create channel: " + ex.Message);
        }
        finally
        {
            isCreatingChannel = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Pinned Messages
    private async Task NavigateToPinnedMessage(Guid messageId)
    {
        try
        {
            // Check if message is already loaded
            bool messageExists = isDirectMessage
                ? directMessages.Any(m => m.Id == messageId)
                : channelMessages.Any(m => m.Id == messageId);

            // Keep loading more messages until we find the target message
            int maxAttempts = 20; // Prevent infinite loop (20 * 50 = 1000 messages max)
            int attempts = 0;

            while (!messageExists && hasMoreMessages && attempts < maxAttempts)
            {
                await LoadMoreMessages();
                attempts++;

                messageExists = isDirectMessage
                    ? directMessages.Any(m => m.Id == messageId)
                    : channelMessages.Any(m => m.Id == messageId);

                StateHasChanged();
                await Task.Delay(50); // Small delay for DOM update
            }

            if (messageExists)
            {
                // Wait for DOM to fully render
                await Task.Delay(100);
                // Scroll to the message and highlight it
                await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", $"message-{messageId}");
            }
        }
        catch
        {
            // Message might not be loaded yet or element not found
        }
    }

    private async Task LoadPinnedMessageCount()
    {
        try
        {
            var result = await ChannelService.GetPinnedMessagesAsync(selectedChannelId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                pinnedMessages = result.Value;
                pinnedMessageCount = result.Value.Count;
            }
        }
        catch
        {
            pinnedMessages = [];
            pinnedMessageCount = 0;
        }
    }

    private async Task LoadPinnedDirectMessageCount()
    {
        try
        {
            var result = await ConversationService.GetPinnedMessagesAsync(selectedConversationId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                pinnedDirectMessages = result.Value;
                pinnedDirectMessageCount = result.Value.Count;
            }
        }
        catch
        {
            pinnedDirectMessages = [];
            pinnedDirectMessageCount = 0;
        }
    }

    private async Task HandlePinDirectMessage(Guid messageId)
    {
        if (!selectedConversationId.HasValue) return;

        try
        {
            var result = await ConversationService.PinMessageAsync(selectedConversationId.Value, messageId);
            if (result.IsSuccess)
            {
                // Update local message state
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsPinned = true, PinnedAtUtc = DateTime.UtcNow };
                }

                // Update pinned count
                await LoadPinnedDirectMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to pin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to pin message: " + ex.Message);
        }
    }

    private async Task HandleUnpinDirectMessage(Guid messageId)
    {
        if (!selectedConversationId.HasValue) return;

        try
        {
            var result = await ConversationService.UnpinMessageAsync(selectedConversationId.Value, messageId);
            if (result.IsSuccess)
            {
                // Update local message state
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsPinned = false, PinnedAtUtc = null };
                }

                // Update pinned count
                await LoadPinnedDirectMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to unpin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to unpin message: " + ex.Message);
        }
    }

    private async Task HandlePinChannelMessage(Guid messageId)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            var result = await ChannelService.PinMessageAsync(selectedChannelId.Value, messageId);
            if (result.IsSuccess)
            {
                // Update local message state
                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = message with { IsPinned = true, PinnedAtUtc = DateTime.UtcNow };
                }

                // Update pinned count and list
                await LoadPinnedMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to pin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to pin message: " + ex.Message);
        }
    }

    private async Task HandleUnpinChannelMessage(Guid messageId)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            var result = await ChannelService.UnPinMessageAsync(selectedChannelId.Value, messageId);
            if (result.IsSuccess)
            {
                // Update local message state
                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = message with { IsPinned = false, PinnedAtUtc = null };
                }

                // Update pinned count and list
                await LoadPinnedMessageCount();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to unpin message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to unpin message: " + ex.Message);
        }
    }
    #endregion

    #region Forward Feature
    private void HandleForward(Guid messageId)
    {
        // Find the message to forward
        if (isDirectMessage)
        {
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                forwardingDirectMessage = message;
                forwardingChannelMessage = null;
                showForwardDialog = true;
                StateHasChanged();
            }
        }
        else
        {
            var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                forwardingChannelMessage = message;
                forwardingDirectMessage = null;
                showForwardDialog = true;
                StateHasChanged();
            }
        }
    }
    private void CancelForward()
    {
        showForwardDialog = false;
        forwardingDirectMessage = null;
        forwardingChannelMessage = null;
        forwardSearchQuery = string.Empty;
        StateHasChanged();
    }

    private async Task ForwardToConversation(Guid conversationId)
    {
        try
        {
            var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (string.IsNullOrEmpty(content)) return;

            var result = await ConversationService.SendMessageAsync(
                conversationId,
                content,
                fileId: null,
                replyToMessageId: null,
                isForwarded: true);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

                // If forwarding to current conversation, add message locally
                if (conversationId == selectedConversationId)
                {
                    var conversation = conversations.FirstOrDefault(c => c.Id == conversationId);
                    var newMessage = new DirectMessageDto(
                        messageId,
                        conversationId,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        conversation?.OtherUserId ?? Guid.Empty,
                        content,
                        null,                   // FileId
                        false,                  // IsEdited
                        false,                  // IsDeleted
                        false,                  // IsRead
                        false,                  // IsPinned
                        0,                      // ReactionCount
                        messageTime,            // CreatedAtUtc
                        null,                   // EditedAtUtc
                        null,                   // ReadAtUtc
                        null,                   // PinnedAtUtc
                        null,                   // ReplyToMessageId
                        null,                   // ReplyToContent
                        null,                   // ReplyToSenderName
                        true);                  // IsForwarded

                    // Before adding optimistic message, check if it already exists
                    // (in case SignalR arrived faster than HTTP response)
                    if (!directMessages.Any(m => m.Id == messageId))
                    {
                        directMessages.Add(newMessage);
                    }

                    // Add to processed messages to prevent SignalR duplicate
                    processedMessageIds.Add(messageId);
                }

                // Update conversation list locally
                UpdateConversationLocally(conversationId, content, messageTime);

                // Exit selection mode after successful forward
                if (isSelectMode)
                {
                    ToggleSelectMode();
                }

                CancelForward();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to forward message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to forward message: " + ex.Message);
        }
    }

    private async Task ForwardToChannel(Guid channelId)
    {
        try
        {
            var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content;
            if (string.IsNullOrEmpty(content)) return;

            var result = await ChannelService.SendMessageAsync(
                channelId,
                content,
                fileId: null,
                replyToMessageId: null,
                isForwarded: true);

            if (result.IsSuccess)
            {
                var messageTime = DateTime.UtcNow;
                var messageId = result.Value;

                // If forwarding to current channel, add message locally
                if (channelId == selectedChannelId)
                {
                    // TotalMemberCount = all active members except sender
                    var totalMembers = Math.Max(0, selectedChannelMemberCount - 1);

                    var newMessage = new ChannelMessageDto(
                        messageId,
                        channelId,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        content,
                        null,
                        false,
                        false,
                        false,
                        0,
                        messageTime,
                        null,
                        null,
                        null,
                        null,
                        null,
                        true,
                        ReadByCount: 0,
                        TotalMemberCount: totalMembers,
                        ReadBy: new List<Guid>(),
                        Reactions: new List<ChannelMessageReactionDto>());

                    // Before adding optimistic message, check if it already exists
                    // (in case SignalR arrived faster than HTTP response)
                    if (!channelMessages.Any(m => m.Id == messageId))
                    {
                        channelMessages.Add(newMessage);
                    }

                    // Add to processed messages to prevent SignalR duplicate
                    processedMessageIds.Add(messageId);
                }

                // Update channel list locally with time (to sort to top)
                UpdateChannelLocally(channelId, content, messageTime, UserState.CurrentUser?.DisplayName);

                // Exit selection mode after successful forward
                if (isSelectMode)
                {
                    ToggleSelectMode();
                }

                CancelForward();
                StateHasChanged();
            }
            else
            {
                ShowError(result.Error ?? "Failed to forward message");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to forward message: " + ex.Message);
        }
    }

    private record ForwardItem(Guid Id, string Name, string? AvatarUrl, bool IsChannel, bool IsPrivate, DateTime? LastMessageAt);

    private IEnumerable<ForwardItem> GetFilteredForwardItems()
    {
        var items = new List<ForwardItem>();

        // Add conversations
        foreach (var conv in conversations)
        {
            items.Add(new ForwardItem(
                conv.Id,
                conv.OtherUserDisplayName,
                conv.OtherUserAvatarUrl,
                IsChannel: false,
                IsPrivate: false,
                conv.LastMessageAtUtc));
        }

        // Add channels
        foreach (var channel in channels)
        {
            items.Add(new ForwardItem(
                channel.Id,
                channel.Name,
                null,
                IsChannel: true,
                IsPrivate: channel.Type == ChannelType.Private,
                channel.LastMessageAtUtc));
        }

        // Sort by last message date (most recent first)
        items = items.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue).ToList();

        // Filter by search query
        if (!string.IsNullOrWhiteSpace(forwardSearchQuery))
        {
            items = items.Where(x => x.Name.Contains(forwardSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items;
    }

    private async Task HandleForwardItemClick(ForwardItem item)
    {
        if (item.IsChannel)
        {
            await ForwardToChannel(item.Id);
        }
        else
        {
            await ForwardToConversation(item.Id);
        }
    }
    #endregion

    #region Reply Feature
    private void HandleReply(Guid messageId)
    {
        // Find the message to reply to
        if (isDirectMessage)
        {
            var message = directMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                isReplying = true;
                replyToMessageId = messageId;
                replyToSenderName = message.SenderDisplayName;
                replyToContent = message.Content;
                StateHasChanged();
            }
        }
        else
        {
            var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                isReplying = true;
                replyToMessageId = messageId;
                replyToSenderName = message.SenderDisplayName;
                replyToContent = message.Content;
                StateHasChanged();
            }
        }
    }
    private void CancelReply()
    {
        isReplying = false;
        replyToMessageId = null;
        replyToSenderName = null;
        replyToContent = null;
        StateHasChanged();
    }

    #endregion

    #region Searching 
    private async Task OnUserSearchInput(ChangeEventArgs e)
    {
        userSearchQuery = e.Value?.ToString() ?? "";

        // Cancel previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Clear results if query is too short
        if (string.IsNullOrWhiteSpace(userSearchQuery) || userSearchQuery.Length < 2)
        {
            userSearchResults.Clear();
            isSearchingUsers = false;
            StateHasChanged();
            return;
        }

        // Debounce - wait 300ms before searching
        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SearchUsers(token);
    }
    private async Task SearchUsers(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        isSearchingUsers = true;
        StateHasChanged();

        try
        {
            var result = await UserService.SearchUsersAsync(userSearchQuery);

            if (token.IsCancellationRequested) return;

            if (result.IsSuccess)
            {
                userSearchResults = result.Value ?? [];
            }
            else
            {
                userSearchResults.Clear();
            }
        }
        catch
        {
            userSearchResults.Clear();
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isSearchingUsers = false;
                StateHasChanged();
            }
        }
    }
    #endregion

    #region Debounced State Updates

    /// <summary>
    /// Schedules a debounced StateHasChanged call.
    /// Multiple calls within 50ms will be batched into a single UI update.
    /// </summary>
    private void ScheduleStateUpdate()
    {
        lock (_stateChangeLock)
        {
            if (_stateChangeScheduled) return;
            _stateChangeScheduled = true;

            _stateChangeDebounceTimer?.Dispose();
            _stateChangeDebounceTimer = new Timer(_ =>
            {
                InvokeAsync(() =>
                {
                    lock (_stateChangeLock)
                    {
                        _stateChangeScheduled = false;
                    }
                    StateHasChanged();
                });
            }, null, 50, Timeout.Infinite);
        }
    }

    /// <summary>
    /// For critical updates that need immediate UI refresh (user actions)
    /// </summary>
    private void ImmediateStateUpdate()
    {
        lock (_stateChangeLock)
        {
            _stateChangeDebounceTimer?.Dispose();
            _stateChangeScheduled = false;
        }
        StateHasChanged();
    }

    #endregion

    #region Helpers
    private async Task RefreshOnlineStatus()
    {
        try
        {
            // Get all unique user IDs from conversations
            var userIds = conversations
                .Select(c => c.OtherUserId)
                .Distinct()
                .ToList();

            if (userIds.Count != 0)
            {
                // Query online status from SignalR hub
                var onlineStatus = await SignalRService.GetOnlineStatusAsync(userIds);

                // Update conversation list with current online status
                for (int i = 0; i < conversations.Count; i++)
                {
                    var conversation = conversations[i];
                    if (onlineStatus.TryGetValue(conversation.OtherUserId, out var isOnline))
                    {
                        conversations[i] = conversation with { IsOtherUserOnline = isOnline };
                    }
                }

                // Update current recipient online status if viewing a conversation
                if (recipientUserId != Guid.Empty && onlineStatus.TryGetValue(recipientUserId, out var recipientOnline))
                {
                    isRecipientOnline = recipientOnline;
                }
            }
        }
        catch
        {
            // Don't show error to user, this is not critical
        }
    }
    private void UpdateGlobalUnreadCount()
    {
        var totalUnread = conversations.Sum(c => c.UnreadCount) + channels.Sum(c => c.UnreadCount);
        AppState.UnreadMessageCount = totalUnread;
    }

    /// <summary>
    /// Updates the conversation's LastMessageContent locally without reloading from server
    /// </summary>
    private void UpdateConversationLastMessage(Guid conversationId, string newContent)
    {
        var convIndex = conversations.FindIndex(c => c.Id == conversationId);
        if (convIndex >= 0)
        {
            var conv = conversations[convIndex];
            conversations[convIndex] = conv with { LastMessageContent = newContent };
        }
    }

    /// <summary>
    /// Updates the channel's LastMessageContent locally without reloading from server
    /// </summary>
    private void UpdateChannelLastMessage(Guid channelId, string newContent, string? senderName = null)
    {
        var channel = channels.FirstOrDefault(c => c.Id == channelId);
        if (channel != null)
        {
            // Create updated channel with new last message
            var updatedChannel = channel with { LastMessageContent = newContent };

            // Remove from current position
            channels.Remove(channel);

            // Add to the top of the list (most recent)
            channels.Insert(0, updatedChannel);

            // Trigger UI update
            StateHasChanged();
        }
    }

    /// <summary>
    /// Checks if the given message is the last message in a conversation
    /// </summary>
    private bool IsLastMessageInConversation(Guid conversationId, DirectMessageDto message)
    {
        var conv = conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conv == null) return false;

        // If we're viewing this conversation, check the loaded messages
        if (conversationId == selectedConversationId && directMessages.Any())
        {
            var lastMessage = directMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == message.Id;
        }

        // Otherwise, compare with conversation's last message timestamp
        // If message timestamp matches conversation's last message time, it's the last message
        if (conv.LastMessageAtUtc == default(DateTime))
            return false;

        return Math.Abs((message.CreatedAtUtc - conv.LastMessageAtUtc).TotalSeconds) < 1;
    }

    /// <summary>
    /// Checks if the given message is the last message in a channel
    /// </summary>
    private bool IsLastMessageInChannel(Guid channelId, ChannelMessageDto message)
    {
        var channel = channels.FirstOrDefault(c => c.Id == channelId);
        if (channel == null) return false;

        // If we're viewing this channel, check the loaded messages
        if (channelId == selectedChannelId && channelMessages.Any())
        {
            var lastMessage = channelMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == message.Id;
        }

        // Otherwise, compare with channel's last message timestamp
        // If message timestamp matches channel's last message time, it's the last message
        if (!channel.LastMessageAtUtc.HasValue)
            return false;

        return Math.Abs((message.CreatedAtUtc - channel.LastMessageAtUtc.Value).TotalSeconds) < 1;
    }

    private void CalculateUnreadSeparatorPosition<T>(List<T> messages, Func<T, bool> isUnreadPredicate, Func<T, Guid> getIdFunc, Func<T, DateTime> getCreatedAtFunc)
    {
        if (!shouldCalculateUnreadSeparator || messages.Count == 0)
            return;

        var orderedMessages = messages.OrderBy(getCreatedAtFunc).ToList();
        var unreadMessages = orderedMessages.Where(isUnreadPredicate).ToList();

        if (unreadMessages.Count > 0)
        {
            var firstUnread = unreadMessages.First();
            var firstUnreadIndex = orderedMessages.FindIndex(m => getIdFunc(m).Equals(getIdFunc(firstUnread)));

            if (firstUnreadIndex > 0)
            {
                unreadSeparatorAfterMessageId = getIdFunc(orderedMessages[firstUnreadIndex - 1]);
            }
        }

        shouldCalculateUnreadSeparator = false;
    }

    private void ShowError(string message)
    {
        errorMessage = message;
        StateHasChanged();

        // Auto-hide after 5 seconds
        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                if (errorMessage == message)
                {
                    errorMessage = null;
                    StateHasChanged();
                }
            });
        });
    }
    private void ClearError()
    {
        errorMessage = null;
        StateHasChanged();
    }
    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : name[0].ToString().ToUpper();
    }

    #region Draft Management

    /// <summary>
    /// Saves the current draft for the given conversation or channel
    /// </summary>
    private void SaveCurrentDraft(string draft)
    {
        // Get the current key (conversation or channel)
        string? key = null;
        if (selectedConversationId.HasValue)
        {
            key = $"conv_{selectedConversationId.Value}";
        }
        else if (selectedChannelId.HasValue)
        {
            key = $"chan_{selectedChannelId.Value}";
        }
        else if (isPendingConversation && pendingUser != null)
        {
            key = $"pending_{pendingUser.Id}";
        }

        if (key == null) return;

        if (string.IsNullOrWhiteSpace(draft))
        {
            // Remove draft if empty
            messageDrafts.Remove(key);
        }
        else
        {
            // Save draft
            messageDrafts[key] = draft;
        }
    }

    /// <summary>
    /// Loads the draft for the given conversation or channel
    /// </summary>
    private string LoadDraft(Guid? conversationId, Guid? channelId, Guid? pendingUserId = null)
    {
        string? key = null;
        if (conversationId.HasValue)
        {
            key = $"conv_{conversationId.Value}";
        }
        else if (channelId.HasValue)
        {
            key = $"chan_{channelId.Value}";
        }
        else if (pendingUserId.HasValue)
        {
            key = $"pending_{pendingUserId.Value}";
        }

        if (key == null) return string.Empty;

        return messageDrafts.TryGetValue(key, out var draft) ? draft : string.Empty;
    }

    /// <summary>
    /// Called when user types in the message input - saves draft
    /// </summary>
    private void HandleDraftChanged(string draft)
    {
        currentDraft = draft;
        SaveCurrentDraft(draft);
    }

    #endregion

    private void HandleAddedToChannel(ChannelDto channel)
    {
        InvokeAsync(() =>
        {
            // Check if channel already exists
            if (!channels.Any(c => c.Id == channel.Id))
            {
                // Add channel to the list
                channels.Insert(0, channel);
                StateHasChanged();
            }
        });
    }

    /// <summary>
    /// Handles SignalR reconnection - rejoins current channel/conversation group
    /// CRITICAL for maintaining real-time updates after connection loss
    /// </summary>
    private void HandleSignalRReconnected()
    {
        InvokeAsync(async () =>
        {
            try
            {
                // Rejoin current channel group if channel is selected
                if (selectedChannelId.HasValue)
                {
                    await SignalRService.JoinChannelAsync(selectedChannelId.Value);
                }
                // Rejoin current conversation group if conversation is selected
                else if (selectedConversationId.HasValue)
                {
                    await SignalRService.JoinConversationAsync(selectedConversationId.Value);
                }

                StateHasChanged();
            }
            catch
            {
                // Silently handle reconnection errors - connection will retry automatically
            }
        });
    }

    #region Add Member Methods

    private async Task SearchUsersForAddMember(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            memberSearchResults.Clear();
            isSearchingMembersForAdd = false;
            StateHasChanged();
            return;
        }

        isSearchingMembersForAdd = true;
        StateHasChanged();

        try
        {
            var result = await UserService.SearchUsersAsync(query);
            if (result.IsSuccess && result.Value != null)
            {
                // Get current channel members to exclude them from search results
                var channelDetails = selectedChannelId.HasValue
                    ? await ChannelService.GetChannelAsync(selectedChannelId.Value)
                    : null;

                var existingMemberIds = channelDetails?.Value?.Members
                    .Select(m => m.UserId)
                    .ToHashSet() ?? [];

                // Filter out current user and existing members
                memberSearchResults = result.Value
                    .Where(u => u.Id != currentUserId && !existingMemberIds.Contains(u.Id))
                    .ToList();
            }
            else
            {
                memberSearchResults.Clear();
            }
        }
        catch
        {
            memberSearchResults.Clear();
        }
        finally
        {
            isSearchingMembersForAdd = false;
            StateHasChanged();
        }
    }

    private async Task AddMemberToChannel((Guid userId, ChannelMemberRole role) memberData)
    {
        if (!selectedChannelId.HasValue) return;

        try
        {
            // First add the member
            var addResult = await ChannelService.AddMemberAsync(selectedChannelId.Value, memberData.userId);
            if (addResult.IsFailure)
            {
                throw new Exception(addResult.Error ?? "Failed to add member");
            }

            // If role is Admin, update the role
            if (memberData.role == ChannelMemberRole.Admin)
            {
                var roleResult = await ChannelService.UpdateMemberRoleAsync(
                    selectedChannelId.Value,
                    memberData.userId,
                    memberData.role);

                if (roleResult.IsFailure)
                {
                    // Member added but role update failed - log but don't throw
                    Console.WriteLine($"Member added but role update failed: {roleResult.Error}");
                }
            }

            // Update member count
            selectedChannelMemberCount++;

            // Update channel in list
            var channelIndex = channels.FindIndex(c => c.Id == selectedChannelId.Value);
            if (channelIndex >= 0)
            {
                channels[channelIndex] = channels[channelIndex] with
                {
                    MemberCount = selectedChannelMemberCount
                };
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    #endregion

    private void UnsubscribeFromSignalREvents()
    {
        if (!isSubscribedToSignalR) return;
        isSubscribedToSignalR = false;

        SignalRService.OnNewDirectMessage -= HandleNewDirectMessage;
        SignalRService.OnNewChannelMessage -= HandleNewChannelMessage;
        SignalRService.OnDirectMessageEdited -= HandleDirectMessageEdited;
        SignalRService.OnDirectMessageDeleted -= HandleDirectMessageDeleted;
        SignalRService.OnChannelMessageEdited -= HandleChannelMessageEdited;
        SignalRService.OnChannelMessageDeleted -= HandleChannelMessageDeleted;
        SignalRService.OnMessageRead -= HandleMessageRead;
        SignalRService.OnUserTypingInConversation -= HandleTypingInConversation;
        SignalRService.OnUserTypingInChannel -= HandleTypingInChannel;
        SignalRService.OnUserOnline -= HandleUserOnline;
        SignalRService.OnUserOffline -= HandleUserOffline;
        SignalRService.OnDirectMessageReactionToggled -= HandleReactionToggled;
        SignalRService.OnChannelMessageReactionsUpdated -= HandleChannelMessageReactionsUpdated;
        SignalRService.OnChannelMessagesRead -= HandleChannelMessagesRead;
        SignalRService.OnAddedToChannel -= HandleAddedToChannel;
    }

    public async ValueTask DisposeAsync()
    {
        // Mark as disposed to prevent further state updates
        _disposed = true;

        // Unsubscribe from UserState changes
        UserState.OnChange -= HandleUserStateChanged;

        // Unsubscribe from SignalR events
        UnsubscribeFromSignalREvents();

        // Dispose debounce timer
        _stateChangeDebounceTimer?.Dispose();

        // Dispose page visibility subscription
        if (visibilitySubscription != null)
        {
            try
            {
                await visibilitySubscription.InvokeVoidAsync("dispose");
                await visibilitySubscription.DisposeAsync();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        dotNetReference?.Dispose();

        // Leave groups
        if (selectedConversationId.HasValue)
        {
            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }
        if (selectedChannelId.HasValue)
        {
            await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
        }
    }

    private async Task HandleToggleMarkAsLaterClick(Guid messageId)
    {
        try
        {
            Result result;

            // Check if we're in a channel or conversation
            if (selectedChannelId.HasValue)
            {
                result = await ChannelService.ToggleMessageAsLaterAsync(selectedChannelId.Value, messageId);
            }
            else if (selectedConversationId.HasValue)
            {
                result = await ConversationService.ToggleMessageAsLaterAsync(selectedConversationId.Value, messageId);
            }
            else
            {
                return; // Neither channel nor conversation selected
            }

            if (result.IsSuccess)
            {
                // Toggle state
                if (lastReadLaterMessageId.HasValue && lastReadLaterMessageId.Value == messageId)
                {
                    lastReadLaterMessageId = null;
                    lastReadLaterMessageIdOnEntry = null; // Clear tracking
                }
                else
                {
                    lastReadLaterMessageId = messageId;
                    lastReadLaterMessageIdOnEntry = null; // Reset - user just marked, hasn't left yet

                    // "New messages" separator-unu gizlət
                    unreadSeparatorAfterMessageId = null;
                }

                // Update channels or conversations list
                if (selectedChannelId.HasValue)
                {
                    var channelIndex = channels.FindIndex(c => c.Id == selectedChannelId.Value);
                    if (channelIndex >= 0)
                    {
                        var currentChannel = channels[channelIndex];
                        channels[channelIndex] = currentChannel with { LastReadLaterMessageId = lastReadLaterMessageId };
                        // Force new reference for Blazor change detection
                        channels = new List<ChannelDto>(channels);
                    }
                }
                else if (selectedConversationId.HasValue)
                {
                    var conversationIndex = conversations.FindIndex(c => c.Id == selectedConversationId.Value);
                    if (conversationIndex >= 0)
                    {
                        conversations[conversationIndex] = conversations[conversationIndex] with { LastReadLaterMessageId = lastReadLaterMessageId };
                        // Force new reference for Blazor change detection
                        conversations = new List<DirectConversationDto>(conversations);
                    }
                }

                StateHasChanged();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to toggle read later";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error toggling read later: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task HandleToggleFavorite(Guid messageId)
    {
        try
        {
            Result<bool> result;

            // Check if we're in a channel or conversation
            if (selectedChannelId.HasValue)
            {
                result = await ChannelService.ToggleFavoriteAsync(selectedChannelId.Value, messageId);
            }
            else if (selectedConversationId.HasValue)
            {
                result = await ConversationService.ToggleFavoriteAsync(selectedConversationId.Value, messageId);
            }
            else
            {
                return; // Neither channel nor conversation selected
            }

            if (result.IsSuccess)
            {
                // Update local state based on returned value
                if (result.Value)
                {
                    favoriteMessageIds.Add(messageId);
                }
                else
                {
                    favoriteMessageIds.Remove(messageId);
                }

                StateHasChanged();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to toggle favorite";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error toggling favorite: {ex.Message}";
            StateHasChanged();
        }
    }

    #endregion

    #region Search Panel

    private void ToggleSearchPanel()
    {
        showSearchPanel = !showSearchPanel;
    }

    private void CloseSearchPanel()
    {
        showSearchPanel = false;
    }

    private async Task<SearchResultsDto?> SearchMessagesAsync(Guid targetId, string searchTerm, int page, int pageSize)
    {
        try
        {
            Result<SearchResultsDto> result;

            if (isDirectMessage)
            {
                result = await SearchService.SearchInConversationAsync(targetId, searchTerm, page, pageSize);
            }
            else
            {
                result = await SearchService.SearchInChannelAsync(targetId, searchTerm, page, pageSize);
            }

            return result.IsSuccess ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task NavigateToSearchResult(Guid messageId)
    {
        // Close search panel
        showSearchPanel = false;
        StateHasChanged();

        try
        {
            // Check if message is already loaded
            bool messageExists = isDirectMessage
                ? directMessages.Any(m => m.Id == messageId)
                : channelMessages.Any(m => m.Id == messageId);

            // Keep loading more messages until we find the target message
            int maxAttempts = 20; // Prevent infinite loop (20 * 50 = 1000 messages max)
            int attempts = 0;

            while (!messageExists && hasMoreMessages && attempts < maxAttempts)
            {
                await LoadMoreMessages();
                attempts++;

                messageExists = isDirectMessage
                    ? directMessages.Any(m => m.Id == messageId)
                    : channelMessages.Any(m => m.Id == messageId);

                StateHasChanged();
                await Task.Delay(50); // Small delay for DOM update
            }

            if (messageExists)
            {
                // Wait for DOM to fully render
                await Task.Delay(100);
                // Scroll to the message
                await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageById", $"message-{messageId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating to search result: {ex.Message}");
        }

        StateHasChanged();
    }

    #endregion

    #region Selection Mode

    private void HandleSelectToggle(Guid messageId)
    {
        if (!isSelectMode)
        {
            // Enter select mode and select this message
            isSelectMode = true;
            selectedMessageIds.Add(messageId);
        }
        else
        {
            // Already in select mode - toggle this message
            ToggleMessageSelection(messageId);
        }
        StateHasChanged();
    }

    private void ToggleSelectMode()
    {
        isSelectMode = !isSelectMode;
        if (!isSelectMode)
        {
            // Exit select mode - clear selections
            selectedMessageIds.Clear();
        }
        StateHasChanged();
    }

    private void ToggleMessageSelection(Guid messageId)
    {
        if (selectedMessageIds.Contains(messageId))
            selectedMessageIds.Remove(messageId);
        else
            selectedMessageIds.Add(messageId);

        StateHasChanged();
    }

    private bool CanDeleteSelected()
    {
        if (!selectedMessageIds.Any())
            return false;

        // Get all selected messages
        var selectedMessages = directMessages
            .Where(m => selectedMessageIds.Contains(m.Id))
            .Cast<dynamic>()
            .Concat(channelMessages.Where(m => selectedMessageIds.Contains(m.Id)).Cast<dynamic>());

        // Can only delete if ALL selected messages are owned by current user
        return selectedMessages.All(m => (Guid)m.SenderId == currentUserId);
    }

    private async Task DeleteSelectedMessages()
    {
        if (!CanDeleteSelected())
            return;

        var messagesToDelete = selectedMessageIds.ToList();

        foreach (var messageId in messagesToDelete)
        {
            await DeleteMessage(messageId);
        }

        // Exit select mode after deletion
        ToggleSelectMode();
    }

    private void ForwardSelectedMessages()
    {
        if (!selectedMessageIds.Any())
            return;

        // Open forward dialog with selected messages
        // Use existing forward logic but with multiple messages
        // For now, just forward the first one (will be enhanced)
        var firstMessageId = selectedMessageIds.First();
        HandleForward(firstMessageId);

        // Note: Don't exit select mode here - only exit after successful forward
        // If user cancels the forward dialog, selection mode should remain active
    }

    #endregion
}