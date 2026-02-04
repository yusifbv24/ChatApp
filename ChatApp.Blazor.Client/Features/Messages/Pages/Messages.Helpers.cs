using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Common;
using ChatApp.Blazor.Client.Models.Messages;
using ChatApp.Blazor.Client.Models.Organization;
using ChatApp.Blazor.Client.Models.Search;
using ChatApp.Shared.Kernel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Dialog Methods - Dialog metodları

    /// <summary>
    /// Yeni conversation dialog-unu aç.
    /// </summary>
    private void OpenNewConversationDialog()
    {
        showNewConversationDialog = true;
        userSearchQuery = string.Empty;
        userSearchResults.Clear();
        StateHasChanged();
    }

    /// <summary>
    /// Yeni conversation dialog-unu bağla.
    /// </summary>
    private void CloseNewConversationDialog()
    {
        showNewConversationDialog = false;
        _searchCts?.Cancel();
        _searchCts?.Dispose(); // MEMORY LEAK FIX: Dispose after cancel
        StateHasChanged();
    }

    /// <summary>
    /// Yeni channel dialog-unu aç.
    /// İndi Create Group panelini açır (Bitrix24 style).
    /// </summary>
    private void OpenNewChannelDialog()
    {
        OpenCreateGroupPanel();
    }

    /// <summary>
    /// Yeni channel dialog-unu bağla.
    /// </summary>
    private void CloseNewChannelDialog()
    {
        showNewChannelDialog = false;
        StateHasChanged();
    }

    /// <summary>
    /// Create Group panelini aç.
    /// Aktiv conversation bağlanır və panel göstərilir.
    /// </summary>
    private void OpenCreateGroupPanel()
    {
        // Aktiv conversation-u bağla
        selectedConversationId = null;
        selectedChannelId = null;
        directMessages.Clear();
        channelMessages.Clear();

        // Panel state-ini sıfırla
        showCreateGroupPanel = true;
        newChannelRequest = new Models.Messages.CreateChannelRequest { Type = ChannelType.Private };
        createGroupSelectedMembers.Clear();
        createGroupMemberSearchQuery = string.Empty;
        createGroupMemberSearchResults.Clear();
        showCreateGroupMemberSearch = false;
        showChatSettings = false; // Default collapsed

        // Member picker state-ini sıfırla
        memberPickerTab = "recent";
        expandedDepartmentIds.Clear();
        selectedDepartmentIds.Clear();

        // Dialog-u bağla (əgər açıqdırsa)
        showNewChannelDialog = false;

        StateHasChanged();
    }

    /// <summary>
    /// Create Group panelini bağla.
    /// </summary>
    private void CloseCreateGroupPanel()
    {
        showCreateGroupPanel = false;
        _createGroupSearchCts?.Cancel();
        _createGroupSearchCts?.Dispose();
        _createGroupSearchCts = null;

        // Avatar state-ini sıfırla (bütün temporary data)
        createGroupAvatarUrl = null;
        createGroupAvatarFileData = null;
        createGroupAvatarFileName = null;
        createGroupAvatarContentType = null;

        StateHasChanged();
    }

    /// <summary>
    /// Create Group panelini ləğv et (Cancel butonu).
    /// Avatar artıq backend-ə yüklənmir (temporary saxlanılır), buna görə silməyə ehtiyac yoxdur.
    /// </summary>
    private void CancelCreateGroupPanel()
    {
        // Temporary avatar data CloseCreateGroupPanel-da təmizlənir
        CloseCreateGroupPanel();
    }

    /// <summary>
    /// Create Group avatar seçildikdə.
    /// Fayl backend-ə göndərilmir, sadəcə temporary olaraq saxlanılır.
    /// Channel yaradıldıqdan sonra CreateGroupChannel() metodunda upload olunur.
    /// </summary>
    private async Task OnGroupAvatarSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        // Yalnız şəkil faylları qəbul et
        if (!file.ContentType.StartsWith("image/"))
        {
            ShowError("Please select an image file");
            return;
        }

        // Max 5MB
        if (file.Size > 5 * 1024 * 1024)
        {
            ShowError("Image size must be less than 5MB");
            return;
        }

        try
        {
            // Faylı oxu və temporary saxla
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(memoryStream);
            var buffer = memoryStream.ToArray();

            // Preview üçün base64 URL yarat
            createGroupAvatarUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";

            // Temporary file data saxla (backend-ə sonra göndəriləcək)
            createGroupAvatarFileData = buffer;
            createGroupAvatarFileName = file.Name;
            createGroupAvatarContentType = file.ContentType;

        }
        catch (Exception ex)
        {
            ShowError("Failed to process avatar: " + ex.Message);
            createGroupAvatarUrl = null;
            createGroupAvatarFileData = null;
            createGroupAvatarFileName = null;
            createGroupAvatarContentType = null;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Create Group panel-də member search dropdown-u toggle et.
    /// </summary>
    private void ToggleCreateGroupMemberSearch()
    {
        showCreateGroupMemberSearch = !showCreateGroupMemberSearch;
        if (!showCreateGroupMemberSearch)
        {
            createGroupMemberSearchQuery = string.Empty;
            createGroupMemberSearchResults.Clear();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Member picker dropdown-u bağla.
    /// </summary>
    private void CloseMemberPicker()
    {
        showCreateGroupMemberSearch = false;
        createGroupMemberSearchQuery = string.Empty;
        createGroupMemberSearchResults.Clear();
        StateHasChanged();
    }

    /// <summary>
    /// Create Group panel-də Chat Settings bölməsini toggle et.
    /// </summary>
    private void ToggleChatSettings()
    {
        showChatSettings = !showChatSettings;
        StateHasChanged();
    }

    /// <summary>
    /// Create Group panel-də üzv axtarışı.
    /// </summary>
    private async Task OnCreateGroupMemberSearchInput(ChangeEventArgs e)
    {
        createGroupMemberSearchQuery = e.Value?.ToString() ?? string.Empty;

        _createGroupSearchCts?.Cancel();
        _createGroupSearchCts?.Dispose();
        _createGroupSearchCts = new CancellationTokenSource();
        var token = _createGroupSearchCts.Token;

        if (string.IsNullOrWhiteSpace(createGroupMemberSearchQuery) || createGroupMemberSearchQuery.Length < 2)
        {
            createGroupMemberSearchResults.Clear();
            isSearchingCreateGroupMembers = false;
            StateHasChanged();
            return;
        }

        try
        {
            await Task.Delay(300, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SearchCreateGroupMembers(token);
    }

    /// <summary>
    /// Create Group panel-də üzvləri axtar.
    /// </summary>
    private async Task SearchCreateGroupMembers(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        isSearchingCreateGroupMembers = true;
        StateHasChanged();

        try
        {
            var result = await UserService.SearchUsersAsync(createGroupMemberSearchQuery);

            if (token.IsCancellationRequested) return;

            if (result.IsSuccess)
            {
                // Artıq əlavə edilmiş üzvləri və özümü çıxar
                var selectedIds = createGroupSelectedMembers.Select(m => m.Id).ToHashSet();
                selectedIds.Add(currentUserId);

                createGroupMemberSearchResults = result.Value?
                    .Where(u => !selectedIds.Contains(u.Id))
                    .ToList() ?? [];
            }
            else
            {
                createGroupMemberSearchResults.Clear();
            }
        }
        catch
        {
            createGroupMemberSearchResults.Clear();
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                isSearchingCreateGroupMembers = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Create Group panel-ə üzv əlavə et.
    /// </summary>
    private void AddCreateGroupMember(UserSearchResultDto user)
    {
        if (!createGroupSelectedMembers.Any(m => m.Id == user.Id))
        {
            createGroupSelectedMembers.Add(user);
        }
        // Axtarış nəticələrindən çıxar
        createGroupMemberSearchResults.RemoveAll(u => u.Id == user.Id);
        StateHasChanged();
    }

    /// <summary>
    /// Create Group panel-dən üzv sil.
    /// </summary>
    private void RemoveCreateGroupMember(Guid userId)
    {
        createGroupSelectedMembers.RemoveAll(m => m.Id == userId);
        // Department seçimini də sil
        selectedDepartmentIds.Remove(userId);
        StateHasChanged();
    }

    /// <summary>
    /// Create Group panel-də üzv toggle et (seçili isə sil, deyilsə əlavə et).
    /// </summary>
    private async Task ToggleCreateGroupMember(UserSearchResultDto user)
    {
        var existing = createGroupSelectedMembers.FirstOrDefault(m => m.Id == user.Id);
        if (existing != null)
        {
            createGroupSelectedMembers.Remove(existing);
        }
        else
        {
            createGroupSelectedMembers.Add(user);
        }

        // Axtarış yazısını sil və inputa fokuslan
        createGroupMemberSearchQuery = string.Empty;
        createGroupMemberSearchResults.Clear();
        StateHasChanged();

        // Input-a fokuslan
        await JS.InvokeVoidAsync("chatAppUtils.focusElement", ".inline-member-search");
    }

    /// <summary>
    /// Member picker tab dəyişdir.
    /// </summary>
    private async Task SetMemberPickerTab(string tab)
    {
        memberPickerTab = tab;

        if (tab == "departments" && !createGroupDepartments.Any())
        {
            await LoadDepartmentsForPicker();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Departments yüklə.
    /// </summary>
    private async Task LoadDepartmentsForPicker()
    {
        isLoadingDepartments = true;
        StateHasChanged();

        try
        {
            var result = await DepartmentService.GetAllDepartmentsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                createGroupDepartments = result.Value;
            }
        }
        catch
        {
            createGroupDepartments = [];
        }
        finally
        {
            isLoadingDepartments = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Department expand/collapse toggle.
    /// </summary>
    private async Task ToggleDepartmentExpand(Guid departmentId)
    {
        if (expandedDepartmentIds.Contains(departmentId))
        {
            expandedDepartmentIds.Remove(departmentId);
        }
        else
        {
            expandedDepartmentIds.Add(departmentId);

            // Department users yüklə (əgər cache-də yoxdursa)
            if (!departmentUsersCache.ContainsKey(departmentId))
            {
                await LoadDepartmentUsers(departmentId);
            }
        }
        StateHasChanged();
    }

    /// <summary>
    /// Department users yüklə.
    /// </summary>
    private async Task LoadDepartmentUsers(Guid departmentId)
    {
        try
        {
            // GetDepartmentUsersAsync-dan istifadə et
            var result = await UserService.GetDepartmentUsersAsync(1, 100);
            if (result.IsSuccess && result.Value?.Items != null)
            {
                // Filter by departmentId
                var users = result.Value.Items
                    .Where(u => u.DepartmentId == departmentId)
                    .ToList();
                departmentUsersCache[departmentId] = users;
            }
            else
            {
                departmentUsersCache[departmentId] = [];
            }
        }
        catch
        {
            departmentUsersCache[departmentId] = [];
        }
        StateHasChanged();
    }

    /// <summary>
    /// Department seçimi toggle (bütün department).
    /// Department seçildikdə onun bütün istifadəçiləri əlavə olunur.
    /// </summary>
    private async Task ToggleDepartmentSelection(DepartmentDto dept)
    {
        if (selectedDepartmentIds.Contains(dept.Id))
        {
            // Deselect department
            selectedDepartmentIds.Remove(dept.Id);

            // Department-in istifadəçilərini sil (əgər cache-də varsa)
            if (departmentUsersCache.TryGetValue(dept.Id, out var cachedUsers))
            {
                var userIds = cachedUsers.Select(u => u.UserId).ToHashSet();
                createGroupSelectedMembers.RemoveAll(m => userIds.Contains(m.Id));
            }
        }
        else
        {
            // Select department
            selectedDepartmentIds.Add(dept.Id);

            // Department istifadəçilərini yüklə (əgər cache-də yoxdursa)
            if (!departmentUsersCache.ContainsKey(dept.Id))
            {
                await LoadDepartmentUsers(dept.Id);
            }

            // Department-in bütün istifadəçilərini əlavə et
            if (departmentUsersCache.TryGetValue(dept.Id, out var users))
            {
                foreach (var user in users)
                {
                    // Artıq əlavə olunmamış və cari user olmayan istifadəçiləri əlavə et
                    if (user.UserId != currentUserId &&
                        !createGroupSelectedMembers.Any(m => m.Id == user.UserId))
                    {
                        createGroupSelectedMembers.Add(new UserSearchResultDto(
                            user.UserId,
                            user.FullName,
                            string.Empty,
                            user.Email,
                            user.AvatarUrl,
                            user.PositionName
                        ));
                    }
                }
            }
        }
        StateHasChanged();
    }

    /// <summary>
    /// Department user toggle.
    /// </summary>
    private void ToggleDepartmentUser(DepartmentUserDto user)
    {
        var existing = createGroupSelectedMembers.FirstOrDefault(m => m.Id == user.UserId);
        if (existing != null)
        {
            createGroupSelectedMembers.Remove(existing);
        }
        else
        {
            createGroupSelectedMembers.Add(new UserSearchResultDto(
                user.UserId,
                user.FullName,
                string.Empty,
                user.Email,
                user.AvatarUrl,
                user.PositionName
            ));
        }
        StateHasChanged();
    }

    /// <summary>
    /// Bu ID department-dir?
    /// </summary>
    private bool IsSelectedDepartment(Guid id)
    {
        return selectedDepartmentIds.Contains(id);
    }

    /// <summary>
    /// Filter departments (axtarışa görə).
    /// </summary>
    private List<DepartmentDto> GetFilteredDepartments()
    {
        if (string.IsNullOrWhiteSpace(createGroupMemberSearchQuery))
            return createGroupDepartments;

        return createGroupDepartments
            .Where(d => d.Name.Contains(createGroupMemberSearchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Create Group panelindən channel yarat.
    /// Avatar channel yaradıldıqdan sonra upload olunur.
    /// </summary>
    private async Task CreateGroupChannel()
    {
        if (string.IsNullOrWhiteSpace(newChannelRequest.Name))
        {
            ShowError("Please enter a chat name");
            return;
        }

        isCreatingChannel = true;
        StateHasChanged();

        try
        {
            var result = await ChannelService.CreateChannelAsync(newChannelRequest);
            if (result.IsSuccess)
            {
                var channelId = result.Value;

                // Seçilmiş üzvləri əlavə et
                foreach (var member in createGroupSelectedMembers)
                {
                    await ChannelService.AddMemberAsync(channelId, member.Id);
                }

                // Avatar upload et (əgər seçilibsə)
                if (createGroupAvatarFileData != null &&
                    !string.IsNullOrEmpty(createGroupAvatarFileName) &&
                    !string.IsNullOrEmpty(createGroupAvatarContentType))
                {
                    var avatarResult = await FileService.UploadChannelAvatarAsync(
                        createGroupAvatarFileData,
                        createGroupAvatarFileName,
                        createGroupAvatarContentType,
                        channelId);

                    if (avatarResult.IsSuccess && avatarResult.Value != null)
                    {
                        // Channel-ın AvatarUrl-unu yenilə
                        var updateResult = await ChannelService.UpdateChannelAsync(channelId, new UpdateChannelRequest
                        {
                            AvatarUrl = avatarResult.Value.DownloadUrl
                        });

                        if (updateResult.IsFailure)
                        {
                            ShowError("Channel created but avatar URL update failed");
                        }
                    }
                    else
                    {
                        // Avatar upload uğursuz olsa da channel yaradılıb, sadəcə xəbərdarlıq göstər
                        ShowError("Channel created but avatar upload failed: " + avatarResult.Error);
                    }
                }

                CloseCreateGroupPanel();
                await LoadConversationsAndChannels();

                // Yaradılan channel-ı seç
                var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
                if (channel != null)
                {
                    await SelectChannel(channel);
                }
            }
            else
            {
                ShowError(result.Error ?? "Failed to create chat");
            }
        }
        catch (Exception ex)
        {
            ShowError("Failed to create chat: " + ex.Message);
        }
        finally
        {
            isCreatingChannel = false;
            StateHasChanged();
        }
    }

    #endregion

    #region User Search - İstifadəçi axtarışı

    /// <summary>
    /// İstifadəçi axtarış input-u dəyişdikdə.
    /// DEBOUNCE pattern: 300ms gözləyir, sonra axtarır.
    /// Bu sayədə hər keystroke-da API çağrılmır.
    /// </summary>
    private async Task OnUserSearchInput(ChangeEventArgs e)
    {
        userSearchQuery = e.Value?.ToString() ?? string.Empty;

        // PERFORMANCE: Dispose old CancellationTokenSource to prevent memory leak
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Sorğu çox qısadırsa - nəticələri təmizlə
        if (string.IsNullOrWhiteSpace(userSearchQuery) || userSearchQuery.Length < 2)
        {
            userSearchResults.Clear();
            isSearchingUsers = false;
            StateHasChanged();
            return;
        }

        // DEBOUNCE - 300ms gözlə
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

    /// <summary>
    /// İstifadəçiləri axtar.
    /// </summary>
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

    #region Message Search Panel - Mesaj axtarış paneli

    /// <summary>
    /// Search panel-i toggle et.
    /// </summary>
    private void ToggleSearchPanel()
    {
        showSearchPanel = !showSearchPanel;
    }

    /// <summary>
    /// Search panel-i bağla.
    /// </summary>
    private void CloseSearchPanel()
    {
        showSearchPanel = false;
    }

    /// <summary>
    /// Mesajları axtar.
    /// SearchPanel component-dən çağrılır.
    /// </summary>
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

    /// <summary>
    /// Axtarış nəticəsinə naviqasiya et.
    /// Mesaj yüklənməyibsə GetMessagesAround ilə yüklənir.
    /// </summary>
    private async Task NavigateToSearchResult(Guid messageId)
    {
        await NavigateToMessageAsync(messageId);
    }

    /// <summary>
    /// PERFORMANCE: Mesaja scroll et və highlight et.
    /// OPTIMIZED: Artıq LoadMore loop yoxdur - NavigateToMessageAsync əvvəlcə GetMessagesAround ilə yükləyir.
    /// Bu metod yalnız scroll və highlight edir.
    ///
    /// DEPRECATED OLD LOGIC (Removed):
    /// - LoadMore loop (inefficient: 20*50=1000 mesaj yüklə və axtar)
    ///
    /// NEW LOGIC:
    /// - Sadəcə scroll və highlight (NavigateToMessageAsync artıq mesajı yüklədiyindən)
    /// </summary>
    private async Task ScrollToAndHighlightMessage(Guid messageId)
    {
        try
        {
            // DOM tam render olana qədər gözlə
            await Task.Delay(100);

            // JS ilə scroll və highlight
            await JS.InvokeVoidAsync("chatAppUtils.scrollToMessageAndHighlight", $"message-{messageId}");
        }
        catch
        {
            // Scroll error silently ignore (element not found, etc.)
        }
    }

    #endregion

    #region Draft Management - Qaralama idarəetməsi

    /// <summary>
    /// Cari draft-ı saxla.
    /// Conversation/channel dəyişdikdə çağrılır.
    ///
    /// DRAFT KEY FORMAT:
    /// - conv_{conversationId} - Conversation üçün
    /// - chan_{channelId} - Channel üçün
    /// - pending_{userId} - Pending conversation üçün
    /// </summary>
    private void SaveCurrentDraft(string draft)
    {
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
            // Boş draft - sil
            messageDrafts.Remove(key);
        }
        else
        {
            // Draft saxla
            messageDrafts[key] = draft;
        }
    }

    /// <summary>
    /// Draft yüklə.
    /// Conversation/channel seçildikdə çağrılır.
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
    /// Draft dəyişdikdə çağrılır.
    /// MessageInput component-dən.
    /// </summary>
    private void HandleDraftChanged(string draft)
    {
        currentDraft = draft;
        SaveCurrentDraft(draft);
    }

    #endregion

    #region Update Methods - Yeniləmə metodları

    /// <summary>
    /// Conversation-u local olaraq yenilə.
    /// Son mesaj göndərildikdə çağrılır.
    /// PERFORMANCE: Using helper method (eliminated duplicate pattern)
    /// </summary>
    private void UpdateConversationLocally(Guid conversationId, string lastMessage, DateTime messageTime)
    {
        var conversation = directConversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            var updatedConversation = conversation with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime,
                LastMessageSenderId = currentUserId,
                // OPTIMISTIC UI: Status "Pending" ilə başlayır, backend cavabından sonra "Sent"/"Read" olur
                LastMessageStatus = "Pending"
            };

            MoveItemToTop(ref directConversations, updatedConversation, c => c.Id == conversationId);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Channel-ı local olaraq yenilə.
    /// PERFORMANCE: Using helper method (eliminated duplicate pattern)
    /// </summary>
    private void UpdateChannelLocally(Guid channelId, string lastMessage, DateTime messageTime, string? senderName = null)
    {
        var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
        if (channel != null)
        {
            var updatedChannel = channel with
            {
                LastMessageContent = lastMessage,
                LastMessageAtUtc = messageTime,
                LastMessageSenderId = currentUserId,
                // OPTIMISTIC UI: Status "Pending" ilə başlayır, backend cavabından sonra "Sent"/"Delivered"/"Read" olur
                LastMessageStatus = "Pending"
            };

            MoveItemToTop(ref channelConversations, updatedChannel, c => c.Id == channelId);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Conversation-un son mesaj content-ini yenilə.
    /// Edit/delete zamanı çağrılır.
    /// PERFORMANCE: Using helper method (eliminated duplicate pattern)
    /// </summary>
    private void UpdateConversationLastMessage(Guid conversationId, string newContent)
    {
        UpdateListItemWhere(
            ref directConversations,
            c => c.Id == conversationId,
            c => c with { LastMessageContent = newContent }
        );
    }

    /// <summary>
    /// Channel-ın son mesaj content-ini yenilə.
    /// PERFORMANCE: Using helper method (eliminated duplicate pattern)
    /// </summary>
    private void UpdateChannelLastMessage(Guid channelId, string newContent, string? senderName = null)
    {
        UpdateListItemWhere(
            ref channelConversations,
            c => c.Id == channelId,
            c => c with { LastMessageContent = newContent }
        );
    }

    /// <summary>
    /// Mesajdan file preview string-ini çıxarır (conversation list üçün).
    /// Sadə format: [Image], [File]
    /// PERFORMANCE: Merged duplicate methods (DirectMessageDto overload)
    /// </summary>
    private static string GetFilePreview(DirectMessageDto message)
    {
        return GetFilePreviewInternal(message.FileId, message.FileContentType, message.Content);
    }

    /// <summary>
    /// Mesajdan file preview string-ini çıxarır (conversation list üçün).
    /// Sadə format: [Image], [File]
    /// PERFORMANCE: Merged duplicate methods (ChannelMessageDto overload)
    /// </summary>
    private string GetFilePreview(ChannelMessageDto message)
    {
        return GetFilePreviewInternal(message.FileId, message.FileContentType, message.Content);
    }

    /// <summary>
    /// PERFORMANCE: Internal shared implementation for GetFilePreview (eliminates code duplication)
    /// </summary>
    private static string GetFilePreviewInternal(string? fileId, string? fileContentType, string content)
    {
        if (fileId != null)
        {
            if (fileContentType != null && fileContentType.StartsWith("image/"))
            {
                return string.IsNullOrWhiteSpace(content) ? "[Image]" : $"[Image] {content}";
            }

            return string.IsNullOrWhiteSpace(content) ? "[File]" : $"[File] {content}";
        }
        return content;
    }

    /// <summary>
    /// Global unread count-u yenilə.
    /// </summary>
    private void UpdateGlobalUnreadCount()
    {
        var totalUnread = directConversations.Sum(c => c.UnreadCount) + channelConversations.Sum(c => c.UnreadCount);
        AppState.UnreadMessageCount = totalUnread;
    }

    /// <summary>
    /// Mesaj cache version-u artır.
    /// ChatArea cache-i invalidate etmək üçün çağrılır.
    /// In-place mesaj dəyişikliklərindən sonra (edit/delete/reaction/pin/read) çağrılmalıdır.
    /// </summary>
    private void InvalidateMessageCache()
    {
        messageCacheVersion++;
    }

    #endregion

    #region Helper Methods - Yardımçı metodlar

    /// <summary>
    /// Bu mesaj conversation-un son mesajıdır?
    /// Edit/delete zamanı conversation list-i yeniləmək üçün.
    /// </summary>
    private bool IsLastMessageInConversation(Guid conversationId, Guid messageId)
    {
        var conv = directConversations.FirstOrDefault(c => c.Id == conversationId);
        if (conv == null) return false;

        // Aktiv conversation-dayıqsa, yüklənmiş mesajları yoxla
        if (conversationId == selectedConversationId && directMessages.Count != 0)
        {
            var lastMessage = directMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == messageId;
        }

        // Başqa conversation - LastMessageId ilə müqayisə
        return conv.LastMessageId == messageId;
    }

    /// <summary>
    /// Bu mesaj channel-ın son mesajıdır?
    /// </summary>
    private bool IsLastMessageInChannel(Guid channelId, Guid messageId)
    {
        var channel = channelConversations.FirstOrDefault(c => c.Id == channelId);
        if (channel == null) return false;

        if (channelId == selectedChannelId && channelMessages.Count != 0)
        {
            var lastMessage = channelMessages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
            return lastMessage?.Id == messageId;
        }

        // Başqa channel - LastMessageId ilə müqayisə
        return channel.LastMessageId == messageId;
    }

    /// <summary>
    /// Unread separator pozisiyasını hesabla.
    /// İlk oxunmamış mesajın əvvəlinə "New messages" separator qoyuruq.
    /// </summary>
    private void CalculateUnreadSeparatorPosition<T>(
        List<T> messages,
        Func<T, bool> isUnreadPredicate,
        Func<T, Guid> getIdFunc,
        Func<T, DateTime> getCreatedAtFunc)
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
                // Separator əvvəlki mesajdan sonra qoyulur
                unreadSeparatorAfterMessageId = getIdFunc(orderedMessages[firstUnreadIndex - 1]);
            }
            else if (firstUnreadIndex == 0)
            {
                // CRITICAL FIX: İlk mesaj unread-dirsə (30+ unread mesaj senariusu)
                // Separator Guid.Empty ilə işarələnir və ən yuxarıda göstərilir
                unreadSeparatorAfterMessageId = Guid.Empty;
            }
        }

        shouldCalculateUnreadSeparator = false;
    }

    #endregion

    #region Debounced State Updates - Gecikmeli UI yeniləmə

    /// <summary>
    /// Debounce edilmiş StateHasChanged.
    /// 50ms ərzində bir neçə çağırış bir UI yeniləməyə birləşdirilir.
    ///
    /// NİYƏ LAZIMDIR?
    /// Typing/online event-ləri çox tez-tez gəlir.
    /// Hər birində StateHasChanged çağırsaq, UI freeze olur.
    /// Debounce ilə batch edirik.
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
                // MEMORY LEAK FIX: Don't invoke if component is disposed
                if (_disposed) return;

                InvokeAsync(() =>
                {
                    lock (_stateChangeLock)
                    {
                        _stateChangeScheduled = false;
                    }
                    // RACE CONDITION FIX: Check disposed again after lock
                    if (!_disposed)
                    {
                        StateHasChanged();
                    }
                });
            }, null, 50, Timeout.Infinite);
        }
    }

    #endregion

    #region Mention Support - Mention dəstəyi

    /// <summary>
    /// Mention üçün user axtarışı.
    /// MessageInput component-dən çağrılır (@ simvolu trigger edir).
    /// </summary>
    private async Task<List<MentionUserDto>> SearchUsersForMention(string searchTerm)
    {
        try
        {
            var result = await UserService.SearchUsersAsync(searchTerm);

            if (result.IsSuccess && result.Value != null)
            {
                // UserDto-nu MentionUserDto-ya map et
                return result.Value.Select(u => new MentionUserDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    AvatarUrl = u.AvatarUrl,
                    IsMember = false, // User search-də member yoxdur
                    IsAll = false
                }).ToList();
            }

            return [];
        }
        catch
        {
            return [];
        }
    }

    #endregion

    #region Message Status Calculation - Mesaj status hesablama

    /// <summary>
    /// Direct message üçün status hesabla.
    /// CRITICAL FIX: Status initial load-da hesablanmalıdır.
    /// </summary>
    private MessageStatus CalculateDirectMessageStatus(DirectMessageDto message)
    {
        // Başqası göndəribsə - status göstərilmir
        if (message.SenderId != currentUserId)
            return MessageStatus.Sent;

        // Mən göndərmişəm
        return message.IsRead ? MessageStatus.Read : MessageStatus.Sent;
    }

    /// <summary>
    /// Channel message üçün status hesabla.
    /// CRITICAL FIX: Status initial load-da hesablanmalıdır.
    /// CRITICAL FIX: Real-time channel member count istifadə edilir (TotalMemberCount snapshot-dır və dəyişməz).
    /// </summary>
    private MessageStatus CalculateChannelMessageStatus(ChannelMessageDto message, int? realTimeMemberCount = null)
    {
        // Başqası göndəribsə - status göstərilmir
        if (message.SenderId != currentUserId)
            return MessageStatus.Sent;

        // Mən göndərmişəm
        if (message.ReadByCount == 0)
            return MessageStatus.Sent;

        // CRITICAL FIX: Use real-time member count if available (fixes "Read → Delivered" bug when members leave)
        // TotalMemberCount is a snapshot from when message was sent and doesn't update if members leave
        // Real-time count from channelConversations reflects current member count
        var totalMembers = realTimeMemberCount ?? message.TotalMemberCount;
        var otherMembersCount = totalMembers > 0 ? totalMembers - 1 : 0;

        if (message.ReadByCount >= otherMembersCount && otherMembersCount > 0)
            return MessageStatus.Read;

        // CRITICAL FIX: Only show Delivered if there are other members to deliver to
        // If channel has only 1 member (sender), keep status as Sent
        if (message.ReadByCount > 0 && otherMembersCount > 0)
            return MessageStatus.Delivered;

        return MessageStatus.Sent;
    }

    /// <summary>
    /// Direct messages list-də hər bir mesajın status-unu hesabla və təyin et.
    /// CRITICAL FIX: API-dən gələn mesajlarda status olmur, hesablamalıyıq.
    /// </summary>
    private void CalculateDirectMessageStatuses(List<DirectMessageDto> messages)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            var calculatedStatus = CalculateDirectMessageStatus(message);

            // Əgər status default value-dan fərqlidirsə, yenilə
            if (message.Status != calculatedStatus)
            {
                messages[i] = message with { Status = calculatedStatus };
            }
        }
    }

    /// <summary>
    /// Channel messages list-də hər bir mesajın status-unu hesabla və təyin et.
    /// CRITICAL FIX: API-dən gələn mesajlarda status olmur, hesablamalıyıq.
    /// CRITICAL FIX: Real-time channel member count istifadə edilir (member leave problemi).
    /// </summary>
    private void CalculateChannelMessageStatuses(List<ChannelMessageDto> messages)
    {
        // CRITICAL FIX: Get real-time member count for selected channel
        // This ensures status is calculated correctly even if members have left since message was sent
        int? realTimeMemberCount = null;
        if (selectedChannelId.HasValue)
        {
            var currentChannel = channelConversations.FirstOrDefault(c => c.Id == selectedChannelId.Value);
            if (currentChannel != null)
            {
                realTimeMemberCount = currentChannel.MemberCount;
            }
        }

        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            var calculatedStatus = CalculateChannelMessageStatus(message, realTimeMemberCount);

            // Əgər status default value-dan fərqlidirsə, yenilə
            if (message.Status != calculatedStatus)
            {
                messages[i].Status = calculatedStatus; // Mutable property
            }
        }
    }

    #endregion

    #region List Update Helpers - List yeniləmə helper-ləri

    /// <summary>
    /// PERFORMANCE: Generic list update helper - move item to top.
    /// Creates new list with updated item at position 0.
    /// Used for conversation/channel list sorting (most recent first).
    ///
    /// NOTE: Message list-lər IN-PLACE mutation istifadə edir (directMessages[i] = ...) + InvalidateMessageCache()
    /// Bu helper yalnız conversation/channel list-lər üçündür (ReferenceEquals pattern)
    /// </summary>
    private static void MoveItemToTop<T>(ref List<T> list, T updatedItem, Func<T, bool> predicate)
    {
        var newList = new List<T>(list.Count) { updatedItem };
        newList.AddRange(list.Where(item => !predicate(item)));
        list = newList;
    }

    /// <summary>
    /// PERFORMANCE: Generic list update helper - update by predicate.
    /// Creates NEW list with updated item to ensure Blazor change detection works.
    /// CRITICAL FIX: Reverted from in-place mutation to immutable pattern.
    /// Reason: Blazor's change detection relies on reference equality for [Parameter] List bindings.
    /// When list reference doesn't change, ConversationList component doesn't detect updates.
    /// </summary>
    private static void UpdateListItemWhere<T>(ref List<T> list, Func<T, bool> predicate, Func<T, T> updateFunc)
    {
        var index = list.FindIndex(item => predicate(item));
        if (index >= 0)
        {
            var newList = new List<T>(list);
            newList[index] = updateFunc(list[index]);
            list = newList;
        }
    }

    #endregion

    #region Error Handling - Xəta idarəetməsi

    /// <summary>
    /// Error mesajını göstər.
    /// 5 saniyə sonra avtomatik gizlənir.
    /// </summary>
    private void ShowError(string message)
    {
        errorMessage = message;
        StateHasChanged();

        // Auto-hide: 5 saniyə sonra
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

    /// <summary>
    /// Error mesajını təmizlə.
    /// </summary>
    private void ClearError()
    {
        errorMessage = null;
        StateHasChanged();
    }

    #endregion
}