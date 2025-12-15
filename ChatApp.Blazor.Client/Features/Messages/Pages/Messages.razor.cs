using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Messages.Services;
using ChatApp.Blazor.Client.Infrastructure.SignalR;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages : IAsyncDisposable
{
    [Inject] private IConversationService ConversationService { get; set; } = default!;
    [Inject] private IChannelService ChannelService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
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

    // Selection
    private Guid? selectedConversationId;
    private Guid? selectedChannelId;
    private bool isDirectMessage = true;

    // Direct message state
    private string recipientName = string.Empty;
    private string? recipientAvatarUrl;
    private Guid recipientUserId;
    private bool isRecipientOnline;

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
    private bool isLoadingPinnedMessages;
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

    // Pending read receipts (for race condition: MessageRead arrives before message is added)
    private Dictionary<Guid, (Guid readBy, DateTime readAtUtc)> pendingReadReceipts = []; // messageId -> (readBy, readAtUtc)

    // Processed message tracking (to prevent duplicate SignalR notifications)
    private HashSet<Guid> processedMessageIds = [];

    // Page visibility tracking
    private bool isPageVisible = true;
    private IJSObjectReference? visibilitySubscription;
    private DotNetObjectReference<Messages>? dotNetReference;

    // Dialogs
    private bool showNewConversationDialog;
    private bool showNewChannelDialog;
    private bool showPinnedMessagesDialog;

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

    private bool IsEmpty => !selectedConversationId.HasValue && !selectedChannelId.HasValue && !isPendingConversation;

    protected override async Task OnInitializedAsync()
    {
        if (UserState.CurrentUser != null)
        {
            currentUserId = UserState.CurrentUser.Id;
        }

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

    private async Task MarkUnreadMessagesAsRead()
    {
        // Mark unread direct messages as read if viewing a conversation
        if (selectedConversationId.HasValue)
        {
            var unreadMessages = directMessages.Where(m => !m.IsRead && m.SenderId != currentUserId).ToList();
            if (unreadMessages.Count != 0)
            {
                foreach (var message in unreadMessages)
                {
                    try
                    {
                        await ConversationService.MarkAsReadAsync(message.ConversationId, message.Id);

                        var index = directMessages.IndexOf(message);
                        if (index >= 0)
                        {
                            directMessages[index] = message with { IsRead = true };
                        }
                    }
                    catch
                    {
                        // Ignore errors when marking as read
                    }
                }
                StateHasChanged();
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
        SignalRService.OnAddedToChannel += HandleAddedToChannel;
    }

    private void HandleNewDirectMessage(DirectMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Skip our own messages - they're already added via optimistic UI
            if (message.SenderId == currentUserId) return;

            // Skip already processed messages (prevents duplicate SignalR notifications)
            if (!processedMessageIds.Add(message.Id)) return;

            if (message.ConversationId == selectedConversationId)
            {
                // Check if message already exists (prevent duplicates)
                if (!directMessages.Any(m => m.Id == message.Id))
                {
                    // Only mark as read if the page is visible
                    bool shouldMarkAsRead = isPageVisible;

                    directMessages.Add(message with { IsRead = shouldMarkAsRead });

                    // Mark as read on the server only if page is visible
                    if (shouldMarkAsRead)
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

            // Update conversation list for messages from others
            var conversation = conversations.FirstOrDefault(c => c.Id == message.ConversationId);
            if (conversation != null)
            {
                var index = conversations.IndexOf(conversation);
                var isCurrentConversation = message.ConversationId == selectedConversationId;
                conversations[index] = conversation with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    UnreadCount = isCurrentConversation ? 0 : conversation.UnreadCount + 1
                };

                // Increment global unread count if not in this conversation
                if (!isCurrentConversation)
                {
                    AppState.IncrementUnreadMessages();
                }
            }
            else
            {
                // New conversation from someone else - reload the list
                _ = LoadConversationsAndChannels();
            }

            StateHasChanged();
        });
    }

    private void HandleNewChannelMessage(ChannelMessageDto message)
    {
        InvokeAsync(() =>
        {
            // Skip our own messages - they're already added via optimistic UI
            if (message.SenderId == currentUserId) return;

            // Skip already processed messages (prevents duplicate SignalR notifications)
            if (!processedMessageIds.Add(message.Id)) return;

            if (message.ChannelId == selectedChannelId)
            {
                // Check if message already exists (prevent duplicates)
                if (!channelMessages.Any(m => m.Id == message.Id))
                {
                    channelMessages.Add(message);
                }
            }

            // Update channel list for messages from others
            var channel = channels.FirstOrDefault(c => c.Id == message.ChannelId);
            if (channel != null)
            {
                var index = channels.IndexOf(channel);
                var isCurrentChannel = message.ChannelId == selectedChannelId;
                channels[index] = channel with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    LastMessageSenderName = message.SenderDisplayName,
                    UnreadCount = isCurrentChannel ? 0 : channel.UnreadCount + 1
                };

                // Increment global unread count if not in this channel
                if (!isCurrentChannel)
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
            // Update the message in the list if it's in the current conversation
            if (editedMessage.ConversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = editedMessage;

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

                StateHasChanged();
            }

            return Task.CompletedTask;
        });
    }

    private void HandleDirectMessageDeleted(DirectMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            // Update the message in the list if it's in the current conversation
            if (deletedMessage.ConversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = deletedMessage; // Use the deleted DTO from server

                    // Update conversation list if this was the last message
                    if (IsLastMessageInConversation(deletedMessage.ConversationId, deletedMessage))
                    {
                        UpdateConversationLastMessage(deletedMessage.ConversationId, "This message was deleted");
                    }
                }

                // Update reply previews for messages that replied to this deleted message
                for (int i = 0; i < directMessages.Count; i++)
                {
                    var msg = directMessages[i];
                    if (msg.ReplyToMessageId == deletedMessage.Id)
                    {
                        directMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                    }
                }

                StateHasChanged();
            }

            return Task.CompletedTask;
        });
    }

    private void HandleChannelMessageEdited(ChannelMessageDto editedMessage)
    {
        InvokeAsync(() =>
        {
            // Update the message in the list if it's in the current channel
            if (editedMessage.ChannelId == selectedChannelId)
            {
                var message = channelMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = editedMessage;

                    // Update channel list if this was the last message
                    if (IsLastMessageInChannel(editedMessage.ChannelId, editedMessage))
                    {
                        UpdateChannelLastMessage(editedMessage.ChannelId, editedMessage.Content, editedMessage.SenderDisplayName);
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

                StateHasChanged();
            }

            return Task.CompletedTask;
        });
    }

    private void HandleChannelMessageDeleted(ChannelMessageDto deletedMessage)
    {
        InvokeAsync(() =>
        {
            // Update the message in the list if it's in the current channel
            if (deletedMessage.ChannelId == selectedChannelId)
            {
                var message = channelMessages.FirstOrDefault(m => m.Id == deletedMessage.Id);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = deletedMessage; // Use the deleted DTO from server

                    // Update channel list if this was the last message
                    if (IsLastMessageInChannel(deletedMessage.ChannelId, deletedMessage))
                    {
                        UpdateChannelLastMessage(deletedMessage.ChannelId, "This message was deleted", deletedMessage.SenderDisplayName);
                    }
                }

                // Update reply previews for messages that replied to this deleted message
                for (int i = 0; i < channelMessages.Count; i++)
                {
                    var msg = channelMessages[i];
                    if (msg.ReplyToMessageId == deletedMessage.Id)
                    {
                        channelMessages[i] = msg with { ReplyToContent = "This message was deleted" };
                    }
                }

                StateHasChanged();
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
                StateHasChanged();
            }
            else if (conversationId == selectedConversationId)
            {
                // If message not found but we're viewing this conversation,
                // store as pending read receipt (for race condition case)
                pendingReadReceipts[messageId] = (readBy, readAtUtc);
            }
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

                // ALWAYS call StateHasChanged to update conversation list
                StateHasChanged();
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

                // ALWAYS call StateHasChanged to update conversation list
                StateHasChanged();
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

            StateHasChanged();
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

            StateHasChanged();
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
    private void ClosePinnedMessagesDialog()
    {
        showPinnedMessagesDialog = false;
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
                        0,                                              // ReactionCount
                        messageTime,                                    // CreatedAtUtc
                        null,                                           // EditedAtUtc
                        hasReadReceipt ? readReceipt.readAtUtc : null,  // ReadAtUtc
                        replyToMessageId,
                        replyToContent,
                        replyToSenderName,
                        false);                                         // IsForwarded

                    directMessages.Add(newMessage);

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

                    // Add message locally (optimistic UI)
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
                        false);

                    channelMessages.Add(newMessage);

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
                await ChannelService.AddReactionAsync(
                    selectedChannelId.Value,
                    reaction.messageId,
                    reaction.emoji);
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to toggle reaction: " + ex.Message);
        }
    }
    private async Task HandleTyping(bool isTyping)
    {
        if (isDirectMessage && selectedConversationId.HasValue)
        {
            await SignalRService.SendTypingInConversationAsync(selectedConversationId.Value, isTyping);
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

    private void HandleReactionToggled(Guid conversationId, Guid messageId, List<ReactionSummary> reactions)
    {
        if (selectedConversationId.HasValue && selectedConversationId.Value == conversationId)
        {
            UpdateMessageReactions(messageId, reactions);
        }
    }
    #endregion

    #region Conversation
    private void UpdateConversationLocally(Guid conversationId, string lastMessage, DateTime messageTime)
    {
        var conversation = conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            var index = conversations.IndexOf(conversation);

            // Create updated conversation with new last message
            var updatedConversation = conversation with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime
            };

            // Replace in the same position to avoid reordering
            conversations[index] = updatedConversation;

            // Trigger UI update
            StateHasChanged();
        }
    }
    private void UpdateChannelLocally(Guid channelId, string lastMessage, DateTime messageTime, string? senderName = null)
    {
        var channel = channels.FirstOrDefault(c => c.Id == channelId);
        if (channel != null)
        {
            var index = channels.IndexOf(channel);

            // Create updated channel with new last message
            var updatedChannel = channel with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime,
                LastMessageSenderName = senderName ?? channel.LastMessageSenderName
            };

            // Replace in the same position to avoid reordering
            channels[index] = updatedChannel;

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
                    oldestMessageDate = messages.Min(m => m.CreatedAtUtc);
                    hasMoreMessages = messages.Count >= pageSize;
                }
                else
                {
                    hasMoreMessages = false;
                }

                // Mark messages as read
                var unreadMessages = messages.Where(m => !m.IsRead && m.SenderId != currentUserId);
                foreach (var msg in unreadMessages)
                {
                    await ConversationService.MarkAsReadAsync(selectedConversationId!.Value, msg.Id);
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
        // Clear pending conversation state
        isPendingConversation = false;
        pendingUser = null;

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
        pageSize = 50; // Reset page size to 50 for new conversation

        // Set conversation details AFTER clearing
        selectedConversationId = conversation.Id;
        selectedChannelId = null;
        isDirectMessage = true;

        recipientName = conversation.OtherUserDisplayName;
        recipientAvatarUrl = conversation.OtherUserAvatarUrl;
        recipientUserId = conversation.OtherUserId;
        isRecipientOnline = conversation.IsOtherUserOnline;

        // Join SignalR group
        await SignalRService.JoinConversationAsync(conversation.Id);

        // Load messages
        await LoadDirectMessages();

        // Update URL
        NavigationManager.NavigateTo($"/messages/conversation/{conversation.Id}", false);

        StateHasChanged();
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

                // Join all channel SignalR groups to receive notifications for all channels
                foreach (var channel in channels)
                {
                    await SignalRService.JoinChannelAsync(channel.Id);
                }
            }

            // Join all conversation SignalR groups to receive notifications for all conversations
            foreach (var conv in conversations)
            {
                await SignalRService.JoinConversationAsync(conv.Id);
            }

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

        // Clear messages since this is a new conversation
        directMessages.Clear();
        hasMoreMessages = false;
        oldestMessageDate = null;

        CloseNewConversationDialog();
        StateHasChanged();
    }

    #endregion

    #region Channel
    private async Task LoadChannelMessages()
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = await ChannelService.GetMessagesAsync(
                selectedChannelId!.Value,
                pageSize,
                oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    // Filter out duplicates before adding to list
                    var existingIds = channelMessages.Select(m => m.Id).ToHashSet();
                    var newMessages = messages.Where(m => !existingIds.Contains(m.Id)).OrderBy(m => m.CreatedAtUtc);

                    channelMessages.InsertRange(0, newMessages);
                    oldestMessageDate = messages.Min(m => m.CreatedAtUtc);
                    hasMoreMessages = messages.Count >= pageSize;
                }
                else
                {
                    hasMoreMessages = false;
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
    private async Task SelectChannel(ChannelDto channel)
    {
        // Clear pending conversation state
        isPendingConversation = false;
        pendingUser = null;

        // NOTE: We DON'T leave previous conversation/channel groups
        // This allows us to receive typing indicators for conversation list
        // User stays subscribed to all conversation/channel groups they've joined

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

        // Always mark messages as read on the server when entering channel
        _ = ChannelService.MarkAsReadAsync(channel.Id);

        // IMPORTANT: Clear messages BEFORE setting selectedChannelId
        // This prevents race condition where SignalR events arrive between setting ID and clearing messages
        directMessages.Clear();
        channelMessages.Clear();
        hasMoreMessages = true;
        oldestMessageDate = null;
        typingUsers.Clear();
        pendingReadReceipts.Clear(); // Clear pending read receipts when changing channels
        pageSize = 50; // Reset page size to 50 for new channel

        // Set channel details AFTER clearing
        selectedChannelId = channel.Id;
        selectedConversationId = null;
        isDirectMessage = false;
        selectedChannelName = channel.Name;
        selectedChannelDescription = channel.Description;
        selectedChannelType = channel.Type;
        selectedChannelMemberCount = channel.MemberCount;

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
        await LoadChannelMessages();
        await LoadPinnedMessageCount();

        // Update URL
        NavigationManager.NavigateTo($"/messages/channel/{channel.Id}", false);

        StateHasChanged();
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
    private async Task ShowPinnedMessages()
    {
        showPinnedMessagesDialog = true;
        isLoadingPinnedMessages = true;
        StateHasChanged();

        try
        {
            if (selectedChannelId.HasValue)
            {
                var result = await ChannelService.GetPinnedMessagesAsync(selectedChannelId.Value);
                if (result.IsSuccess)
                {
                    pinnedMessages = result.Value ?? [];
                }
            }
        }
        finally
        {
            isLoadingPinnedMessages = false;
            StateHasChanged();
        }
    }
    private async Task LoadPinnedMessageCount()
    {
        try
        {
            var result = await ChannelService.GetPinnedMessagesAsync(selectedChannelId!.Value);
            if (result.IsSuccess && result.Value != null)
            {
                pinnedMessageCount = result.Value.Count;
            }
        }
        catch
        {
            pinnedMessageCount = 0;
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

                // If forwarding to current conversation, add message locally
                if (conversationId == selectedConversationId)
                {
                    var conversation = conversations.FirstOrDefault(c => c.Id == conversationId);
                    var newMessage = new DirectMessageDto(
                        result.Value,
                        conversationId,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        conversation?.OtherUserId ?? Guid.Empty,
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
                        true);

                    directMessages.Add(newMessage);
                }

                // Update conversation list locally
                UpdateConversationLocally(conversationId, content, messageTime);
                CancelForward();
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

                // If forwarding to current channel, add message locally
                if (channelId == selectedChannelId)
                {
                    var newMessage = new ChannelMessageDto(
                        result.Value,
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
                        true);

                    channelMessages.Add(newMessage);
                }

                // Update channel list locally
                UpdateChannelLastMessage(channelId, content, UserState.CurrentUser?.DisplayName);
                CancelForward();
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
        var channelIndex = channels.FindIndex(c => c.Id == channelId);
        if (channelIndex >= 0)
        {
            var channel = channels[channelIndex];
            if (senderName != null)
            {
                channels[channelIndex] = channel with { LastMessageContent = newContent, LastMessageSenderName = senderName };
            }
            else
            {
                channels[channelIndex] = channel with { LastMessageContent = newContent };
            }
        }
    }

    /// <summary>
    /// Checks if the given message is the last message in a conversation
    /// </summary>
    private bool IsLastMessageInConversation(Guid conversationId, DirectMessageDto message)
    {
        var conv = conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conv == null) return false;

        // Check by comparing content or by checking if it's the most recent message in our list
        var lastMessage = directMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
        return lastMessage?.Id == message.Id;
    }

    /// <summary>
    /// Checks if the given message is the last message in a channel
    /// </summary>
    private bool IsLastMessageInChannel(Guid channelId, ChannelMessageDto message)
    {
        var channel = channels.FirstOrDefault(c => c.Id == channelId);
        if (channel == null) return false;

        var lastMessage = channelMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
        return lastMessage?.Id == message.Id;
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
        SignalRService.OnAddedToChannel -= HandleAddedToChannel;
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from SignalR events
        UnsubscribeFromSignalREvents();

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
    #endregion
}