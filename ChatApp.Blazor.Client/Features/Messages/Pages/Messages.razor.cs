using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Messages.Services;
using ChatApp.Blazor.Client.Infrastructure.SignalR;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;

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
    [Parameter] public Guid? ConversationId { get; set; }
    [Parameter] public Guid? ChannelId { get; set; }

    // State
    private Guid currentUserId;
    private List<DirectConversationDto> conversations = new();
    private List<ChannelDto> channels = new();
    private List<DirectMessageDto> directMessages = new();
    private List<ChannelMessageDto> channelMessages = new();
    private List<ChannelMessageDto> pinnedMessages = new();

    // Selection
    private Guid? selectedConversationId;
    private Guid? selectedChannelId;
    private bool isDirectMessage = true;

    // Direct message state
    private string recipientName = "";
    private string? recipientAvatarUrl;
    private Guid recipientUserId;
    private bool isRecipientOnline;

    // Channel state
    private string selectedChannelName = "";
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
    private List<string> typingUsers = new();
    private Dictionary<Guid, string> typingUserNames = new();

    // Dialogs
    private bool showNewConversationDialog;
    private bool showNewChannelDialog;
    private bool showPinnedMessagesDialog;
    private bool isMobileSidebarOpen;

    // Search
    private string userSearchQuery = "";
    private List<UserDto> userSearchResults = new();

    // New channel
    private CreateChannelRequest newChannelRequest = new();

    // Error handling
    private string? errorMessage;

    private bool IsEmpty => !selectedConversationId.HasValue && !selectedChannelId.HasValue;
    protected override async Task OnInitializedAsync()
    {
        if (UserState.CurrentUser != null)
        {
            currentUserId = UserState.CurrentUser.Id;
        }

        // Initialize SignalR
        await InitializeSignalR();

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
    private async Task InitializeSignalR()
    {
        try
        {
            await SignalRService.InitializeAsync();

            // Subscribe to events
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize SignalR: {ex.Message}");
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
                conversations = conversationsResult.Value ?? new List<DirectConversationDto>();
            }

            if (channelsResult.IsSuccess)
            {
                channels = channelsResult.Value ?? new List<ChannelDto>();
            }
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

    private async Task SelectConversation(DirectConversationDto conversation)
    {
        // Leave previous groups
        if (selectedConversationId.HasValue)
        {
            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }
        if (selectedChannelId.HasValue)
        {
            await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
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

        // Join SignalR group
        await SignalRService.JoinConversationAsync(conversation.Id);

        // Load messages
        await LoadDirectMessages();

        // Update URL
        NavigationManager.NavigateTo($"/messages/conversation/{conversation.Id}", false);

        isMobileSidebarOpen = false;
        StateHasChanged();
    }
    private async Task SelectChannel(ChannelDto channel)
    {
        // Leave previous groups
        if (selectedConversationId.HasValue)
        {
            await SignalRService.LeaveConversationAsync(selectedConversationId.Value);
        }
        if (selectedChannelId.HasValue)
        {
            await SignalRService.LeaveChannelAsync(selectedChannelId.Value);
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

        // Join SignalR group
        await SignalRService.JoinChannelAsync(channel.Id);

        // Load messages and pinned count
        await LoadChannelMessages();
        await LoadPinnedMessageCount();

        // Update URL
        NavigationManager.NavigateTo($"/messages/channel/{channel.Id}", false);

        isMobileSidebarOpen = false;
        StateHasChanged();
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
                if (messages.Any())
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
                if (messages.Any())
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
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                var result = await ConversationService.SendMessageAsync(selectedConversationId.Value, content);
                if (!result.IsSuccess)
                {
                    ShowError(result.Error ?? "Failed to send message");
                }
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                var result = await ChannelService.SendMessageAsync(selectedChannelId.Value, content);
                if (!result.IsSuccess)
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
                await ConversationService.AddReactionAsync(
                    selectedConversationId.Value,
                    reaction.messageId,
                    reaction.emoji);
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
            ShowError("Failed to add reaction: " + ex.Message);
        }
    }

    private async Task HandleTyping(bool isTyping)
    {
        try
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
        catch
        {
            // Ignore typing errors
        }
    }

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
                    pinnedMessages = result.Value ?? new List<ChannelMessageDto>();
                }
            }
        }
        finally
        {
            isLoadingPinnedMessages = false;
            StateHasChanged();
        }
    }

    // SignalR Event Handlers
    private void HandleNewDirectMessage(DirectMessageDto message)
    {
        InvokeAsync(() =>
        {
            if (message.ConversationId == selectedConversationId)
            {
                directMessages.Add(message);
                StateHasChanged();
            }

            // Update conversation list
            var conversation = conversations.FirstOrDefault(c => c.Id == message.ConversationId);
            if (conversation != null)
            {
                var index = conversations.IndexOf(conversation);
                conversations[index] = conversation with
                {
                    LastMessageContent = message.Content,
                    LastMessageAtUtc = message.CreatedAtUtc,
                    UnreadCount = message.ConversationId == selectedConversationId
                        ? 0
                        : conversation.UnreadCount + 1
                };
                StateHasChanged();
            }
        });
    }

    private void HandleNewChannelMessage(ChannelMessageDto message)
    {
        InvokeAsync(() =>
        {
            if (message.ChannelId == selectedChannelId)
            {
                channelMessages.Add(message);
                StateHasChanged();
            }
        });
    }
    private void HandleDirectMessageEdited(Guid conversationId, Guid messageId)
    {
        InvokeAsync(async () =>
        {
            if (conversationId == selectedConversationId)
            {
                // Reload the specific message
                var result = await ConversationService.GetMessagesAsync(conversationId, 1);
                StateHasChanged();
            }
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

    private void HandleChannelMessageEdited(Guid channelId, Guid messageId)
    {
        InvokeAsync(() =>
        {
            if (channelId == selectedChannelId)
            {
                StateHasChanged();
            }
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
            if (conversationId == selectedConversationId)
            {
                var message = directMessages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    var index = directMessages.IndexOf(message);
                    directMessages[index] = message with { IsRead = true, ReadAtUtc = readAtUtc };
                    StateHasChanged();
                }
            }
        });
    }

    private void HandleTypingInConversation(Guid conversationId, Guid userId, bool isTyping)
    {
        InvokeAsync(() =>
        {
            if (conversationId == selectedConversationId && userId != currentUserId)
            {
                if (isTyping)
                {
                    if (!typingUsers.Contains(recipientName))
                    {
                        typingUsers.Add(recipientName);
                    }
                }
                else
                {
                    typingUsers.Remove(recipientName);
                }
                StateHasChanged();
            }
        });
    }

    private void HandleTypingInChannel(Guid channelId, Guid userId, bool isTyping)
    {
        InvokeAsync(() =>
        {
            if (channelId == selectedChannelId && userId != currentUserId)
            {
                var userName = typingUserNames.GetValueOrDefault(userId, "Someone");
                if (isTyping)
                {
                    if (!typingUsers.Contains(userName))
                    {
                        typingUsers.Add(userName);
                    }
                }
                else
                {
                    typingUsers.Remove(userName);
                }
                StateHasChanged();
            }
        });
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

    // Dialog Methods
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
        StateHasChanged();
    }

    private async Task StartConversationWithUser(Guid userId)
    {
        try
        {
            var result = await ConversationService.StartConversationAsync(userId);
            if (result.IsSuccess)
            {
                CloseNewConversationDialog();
                await LoadConversationsAndChannels();

                var conversation = conversations.FirstOrDefault(c => c.Id == result.Value);
                if (conversation != null)
                {
                    await SelectConversation(conversation);
                }
            }
            else
            {
                ShowError(result.Error ?? "Failed to start conversation");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to start conversation: " + ex.Message);
        }
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
    private void ClosePinnedMessagesDialog()
    {
        showPinnedMessagesDialog = false;
        StateHasChanged();
    }

    private void ToggleMobileSidebar()
    {
        isMobileSidebarOpen = !isMobileSidebarOpen;
        StateHasChanged();
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

    private string GetInitials(string name)
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
}