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

    // Typing
    private List<string> typingUsers = [];
    private Dictionary<Guid, string> typingUserNames = [];
    private Dictionary<Guid, bool> conversationTypingState = []; // conversationId -> isTyping
    private Dictionary<Guid, bool> channelTypingState = []; // channelId -> isTyping

    // Pending read receipts (for race condition: MessageRead arrives before message is added)
    private Dictionary<Guid, (Guid readBy, DateTime readAtUtc)> pendingReadReceipts = []; // messageId -> (readBy, readAtUtc)

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

    // Reply state
    private bool isReplying;
    private Guid? replyToMessageId;
    private string? replyToSenderName;
    private string? replyToContent;

    // Forward state
    private bool showForwardDialog;
    private DirectMessageDto? forwardingDirectMessage;
    private ChannelMessageDto? forwardingChannelMessage;

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

                        // Update local state
                        var index = directMessages.IndexOf(message);
                        if (index >= 0)
                        {
                            directMessages[index] = message with { IsRead = true };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to mark message as read: {ex.Message}");
                    }
                }
                StateHasChanged();
            }
        }
    }


    #region SignalR Event Handlers

    private void SubscribeToSignalREvents()
    {
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
    }

    private void HandleNewDirectMessage(DirectMessageDto message)
    {
        InvokeAsync(async () =>
        {
            // Skip our own messages - they're already added via optimistic UI
            if (message.SenderId == currentUserId) return;

            if (message.ConversationId == selectedConversationId)
            {
                // Check if message already exists (prevent duplicates)
                if (!directMessages.Any(m => m.Id == message.Id))
                {
                    // Only mark as read if the page is visible (user is actually viewing the messages)
                    bool shouldMarkAsRead = isPageVisible;

                    directMessages.Add(message with { IsRead = shouldMarkAsRead });
                    StateHasChanged();

                    // Mark as read on the server only if page is visible
                    if (shouldMarkAsRead)
                    {
                        try
                        {
                            await ConversationService.MarkAsReadAsync(message.ConversationId, message.Id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to mark message as read: {ex.Message}");
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

                StateHasChanged();
            }
            else
            {
                // New conversation from someone else - reload the list
                _ = LoadConversationsAndChannels();
            }
        });
    }

    private void HandleNewChannelMessage(ChannelMessageDto message)
    {
        InvokeAsync(() =>
        {
            // Skip our own messages - they're already added via optimistic UI
            if (message.SenderId == currentUserId) return;

            if (message.ChannelId == selectedChannelId)
            {
                // Check if message already exists (prevent duplicates)
                if (!channelMessages.Any(m => m.Id == message.Id))
                {
                    channelMessages.Add(message);
                    StateHasChanged();
                }
            }
        });
    }

    private void HandleDirectMessageEdited(DirectMessageDto editedMessage)
    {
        InvokeAsync(async () =>
        {
            // Update the message in the list if it's in the current conversation
            if (editedMessage.ConversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = editedMessage;
                    StateHasChanged();
                }
            }

            // Update conversation list to show edited content
            await LoadConversationsAndChannels();
        });
    }

    private void HandleDirectMessageDeleted(Guid conversationId, Guid messageId)
    {
        InvokeAsync(() =>
        {
            if (conversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsDeleted = true, Content = "" };
                    StateHasChanged();
                }
            }
        });
    }

    private void HandleChannelMessageEdited(ChannelMessageDto editedMessage)
    {
        InvokeAsync(async () =>
        {
            // Update the message in the list if it's in the current channel
            if (editedMessage.ChannelId == selectedChannelId)
            {
                var message = channelMessages.FirstOrDefault(m => m.Id == editedMessage.Id);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = editedMessage;
                    StateHasChanged();
                }
            }

            // Update channel list to show edited content
            await LoadConversationsAndChannels();
        });
    }

    private void HandleChannelMessageDeleted(Guid channelId, Guid messageId)
    {
        InvokeAsync(() =>
        {
            if (channelId == selectedChannelId)
            {
                var message = channelMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = channelMessages.IndexOf(message);
                    channelMessages[index] = message with { IsDeleted = true, Content = "" };
                    StateHasChanged();
                }
            }
        });
    }

    private void HandleMessageRead(Guid conversationId, Guid messageId, Guid readBy, DateTime readAtUtc)
    {
        InvokeAsync(() =>
        {
            // Only update if we're viewing this conversation
            if (conversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsRead = true, ReadAtUtc = readAtUtc };
                    StateHasChanged();
                }
                else
                {
                    // Store pending read receipt - will be applied when message is added
                    pendingReadReceipts[messageId] = (readBy, readAtUtc);
                }
            }
        });
    }

    private void HandleTypingInConversation(Guid conversationId, Guid userId, bool isTyping)
    {
        // Only track typing state for OTHER users, not yourself
        if (userId != currentUserId)
        {
            // Update typing state for this conversation
            if (isTyping)
            {
                conversationTypingState[conversationId] = true;
            }
            else
            {
                conversationTypingState.Remove(conversationId);
            }

            if (conversationId == selectedConversationId && isDirectMessage && !string.IsNullOrEmpty(recipientName))
            {
                InvokeAsync(() =>
                {
                    if (isTyping)
                    {
                        if (!typingUsers.Contains(recipientName))
                        {
                            typingUsers = new List<string>(typingUsers) { recipientName };
                        }
                    }
                    else
                    {
                        typingUsers = typingUsers.Where(u => u != recipientName).ToList();
                    }

                    StateHasChanged();
                });
            }
        }
    }

    private void HandleTypingInChannel(Guid channelId, Guid userId, bool isTyping)
    {
        // Only track typing state for OTHER users, not yourself
        if (userId != currentUserId)
        {
            // Update typing state for this channel
            if (isTyping)
            {
                channelTypingState[channelId] = true;
            }
            else
            {
                channelTypingState.Remove(channelId);
            }

            if (channelId == selectedChannelId)
            {
                InvokeAsync(() =>
                {
                    var userName = typingUserNames.GetValueOrDefault(userId, "Someone");
                    if (isTyping)
                    {
                        if (!typingUsers.Contains(userName))
                        {
                            typingUsers = new List<string>(typingUsers) { userName };
                        }
                    }
                    else
                    {
                        typingUsers = typingUsers.Where(u => u != userName).ToList();
                    }

                    StateHasChanged();
                });
            }
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

                    // Add message locally (optimistic UI) - don't wait for SignalR
                    var newMessage = new DirectMessageDto(
                        messageId,
                        selectedConversationId.Value,
                        currentUserId,
                        UserState.CurrentUser?.Username ?? "",
                        UserState.CurrentUser?.DisplayName ?? "",
                        UserState.CurrentUser?.AvatarUrl,
                        recipientUserId,
                        content,
                        null,
                        hasReadReceipt, // Apply pending read receipt if exists
                        false,
                        false,
                        0,
                        messageTime,
                        null,
                        hasReadReceipt ? readReceipt.readAtUtc : null,
                        replyToMessageId,
                        replyToContent,
                        replyToSenderName,
                        false);

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
                        DateTime.UtcNow,
                        null,
                        null,
                        replyToMessageId,
                        replyToContent,
                        replyToSenderName,
                        false);

                    channelMessages.Add(newMessage);

                    // Clear reply state after sending
                    CancelReply();
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
                    // Update local message
                    var message = directMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null)
                    {
                        var index = directMessages.IndexOf(message);
                        directMessages[index] = message with { Content = edit.content, IsEdited = true };

                        // Update conversation list to show edited content
                        await LoadConversationsAndChannels();
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
                    var message = channelMessages.FirstOrDefault(m => m.Id == edit.messageId);
                    if (message != null)
                    {
                        var index = channelMessages.IndexOf(message);
                        channelMessages[index] = message with { Content = edit.content, IsEdited = true };

                        // Update channel list to show edited content
                        await LoadConversationsAndChannels();
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
                        directMessages[index] = message with { IsDeleted = true, Content = "" };
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
                        channelMessages[index] = message with { IsDeleted = true, Content = "" };
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
    private async Task LoadDirectMessages()
    {
        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = await ConversationService.GetMessagesAsync(
                selectedConversationId!.Value,
                50,
                oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    directMessages.InsertRange(0, messages.OrderBy(m => m.CreatedAtUtc));
                    oldestMessageDate = messages.Min(m => m.CreatedAtUtc);
                    hasMoreMessages = messages.Count >= 50;
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

        // Leave previous groups
        if (selectedConversationId.HasValue)
        {
            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }
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

        selectedConversationId = conversation.Id;
        selectedChannelId = null;
        isDirectMessage = true;

        recipientName = conversation.OtherUserDisplayName;
        recipientAvatarUrl = conversation.OtherUserAvatarUrl;
        recipientUserId = conversation.OtherUserId;
        isRecipientOnline = conversation.IsOtherUserOnline;

        // Reset messages
        directMessages.Clear();
        channelMessages.Clear();
        hasMoreMessages = true;
        oldestMessageDate = null;
        typingUsers.Clear();
        pendingReadReceipts.Clear(); // Clear pending read receipts when changing conversations

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
                50,
                oldestMessageDate);

            if (result.IsSuccess && result.Value != null)
            {
                var messages = result.Value;
                if (messages.Count != 0)
                {
                    channelMessages.InsertRange(0, messages.OrderBy(m => m.CreatedAtUtc));
                    oldestMessageDate = messages.Min(m => m.CreatedAtUtc);
                    hasMoreMessages = messages.Count >= 50;
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

        // Leave previous groups
        if (selectedConversationId.HasValue)
        {
            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }
        if (selectedChannelId.HasValue)
        {
            await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
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

        selectedChannelId = channel.Id;
        selectedConversationId = null;
        isDirectMessage = false;
        selectedChannelName = channel.Name;
        selectedChannelDescription = channel.Description;
        selectedChannelType = channel.Type;
        selectedChannelMemberCount = channel.MemberCount;

        // Reset messages
        directMessages.Clear();
        channelMessages.Clear();
        hasMoreMessages = true;
        oldestMessageDate = null;
        typingUsers.Clear();
        pendingReadReceipts.Clear(); // Clear pending read receipts when changing channels

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
        StateHasChanged();
    }
    private string GetForwardMessagePreview()
    {
        var content = forwardingDirectMessage?.Content ?? forwardingChannelMessage?.Content ?? "";
        return content.Length > 60 ? content.Substring(0, 60) + "..." : content;
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
                // Update conversation list to show the forwarded message
                await LoadConversationsAndChannels();
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
                // Update channel list to show the forwarded message
                await LoadConversationsAndChannels();
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
        catch (Exception ex)
        {
            Console.WriteLine($"Search error: {ex.Message}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to refresh online status: {ex.Message}");
            // Don't show error to user, this is not critical
        }
    }
    private void UpdateGlobalUnreadCount()
    {
        var totalUnread = conversations.Sum(c => c.UnreadCount) + channels.Sum(c => c.UnreadCount);
        AppState.UnreadMessageCount = totalUnread;
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
    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from SignalR events
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