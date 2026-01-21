using ChatApp.Blazor.Client.Models.Common;

namespace ChatApp.Blazor.Client.Features.Messages.Pages;

public partial class Messages
{
    #region Load Favorites - Favori mesajları yüklə

    /// <summary>
    /// DM favori mesajlarını yükləyir.
    /// </summary>
    private async Task LoadFavoriteDirectMessages()
    {
        try
        {
            if (!selectedConversationId.HasValue) return;

            var result = await ConversationService.GetFavoriteMessagesAsync(selectedConversationId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                sidebarFavoriteDirectMessages = result.Value;
                // MessageBubble-da star icon göstərmək üçün ID-ləri saxla
                favoriteMessageIds = new HashSet<Guid>(result.Value.Select(f => f.Id));
            }
        }
        catch
        {
            sidebarFavoriteDirectMessages = null;
            favoriteMessageIds.Clear();
        }
    }

    /// <summary>
    /// Channel favori mesajlarını yükləyir.
    /// SelectChannel zamanı çağrılır.
    /// </summary>
    private async Task LoadFavoriteChannelMessages()
    {
        try
        {
            if (!selectedChannelId.HasValue) return;

            var result = await ChannelService.GetFavoriteMessagesAsync(selectedChannelId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                sidebarFavoriteChannelMessages = result.Value;
                favoriteMessageIds = new HashSet<Guid>(result.Value.Select(f => f.Id));
            }
        }
        catch
        {
            sidebarFavoriteChannelMessages = null;
            favoriteMessageIds.Clear();
        }
    }

    #endregion

    #region Toggle Favorite - Favori toggle et

    /// <summary>
    /// Mesajı favorilərə əlavə et / favorilərden sil.
    /// MessageBubble component-dən çağrılır.
    ///
    /// TOGGLE PATTERN:
    /// Eyni mesaja 2-ci dəfə click = favorilərden sil.
    /// </summary>
    private async Task HandleToggleFavorite(Guid messageId)
    {
        try
        {
            Result<bool> result;

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
                return;
            }

            if (result.IsSuccess)
            {
                if (result.Value)
                {
                    favoriteMessageIds.Add(messageId);
                }
                else
                {
                    favoriteMessageIds.Remove(messageId);
                }

                // Sidebar list-i yenilə
                if (isDirectMessage && selectedConversationId.HasValue)
                {
                    await LoadFavoriteDirectMessages();
                }
                else if (!isDirectMessage && selectedChannelId.HasValue)
                {
                    await LoadFavoriteChannelMessages();
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

    #region Sidebar - Sağ panel

    /// <summary>
    /// Sidebar-ı toggle et.
    /// Header-dakı sidebar button-dan çağrılır.
    ///
    /// LOGIC:
    /// - Search panel açıqdırsa, əvvəlcə onu bağla
    /// - Sonra sidebar-ı toggle et
    /// </summary>
    private void ToggleSidebar()
    {
        // Search panel açıqdırsa, əvvəlcə onu bağla
        if (showSearchPanel)
        {
            showSearchPanel = false;
            return;
        }

        showSidebar = !showSidebar;
    }

    /// <summary>
    /// Sidebar-ı bağla.
    /// </summary>
    private void CloseSidebar()
    {
        showSidebar = false;
    }

    /// <summary>
    /// Profile panel-ı aç.
    /// Sidebar "View profile" button-dan çağrılır.
    /// Sidebar açıq qalır.
    /// </summary>
    private void OpenProfilePanel()
    {
        // Search panel bağla (profile panel ilə sidebar eyni anda açıq ola bilər)
        showSearchPanel = false;

        // Profile panel aç
        showProfilePanel = true;
    }

    /// <summary>
    /// Sidebar üçün favori mesajları yüklə.
    /// Sidebar açıldıqda çağrılır.
    /// </summary>
    private async Task LoadSidebarFavorites()
    {
        try
        {
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                await LoadFavoriteDirectMessages();
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                await LoadFavoriteChannelMessages();
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Favori mesaja naviqasiya et.
    /// Mesaj yüklənməyibsə GetMessagesAround ilə yüklənir.
    /// </summary>
    private async Task NavigateToFavoriteMessage(Guid messageId)
    {
        await NavigateToMessageAsync(messageId);
    }

    /// <summary>
    /// Sidebar-dan favoridən sil.
    /// Mesajın yanındaki X button-dan çağrılır.
    /// </summary>
    private async Task HandleRemoveFromFavoritesInSidebar(Guid messageId)
    {
        try
        {
            Result<bool> result;

            // Toggle API çağır (artıq favoridədir, silmək üçün)
            if (isDirectMessage && selectedConversationId.HasValue)
            {
                result = await ConversationService.ToggleFavoriteAsync(selectedConversationId.Value, messageId);
            }
            else if (!isDirectMessage && selectedChannelId.HasValue)
            {
                result = await ChannelService.ToggleFavoriteAsync(selectedChannelId.Value, messageId);
            }
            else
            {
                return;
            }

            if (result.IsSuccess)
            {
                // Local state yenilə
                favoriteMessageIds.Remove(messageId);

                // Sidebar list-dən sil
                if (isDirectMessage && sidebarFavoriteDirectMessages != null)
                {
                    sidebarFavoriteDirectMessages = sidebarFavoriteDirectMessages
                        .Where(m => m.Id != messageId)
                        .ToList();
                }
                else if (!isDirectMessage && sidebarFavoriteChannelMessages != null)
                {
                    sidebarFavoriteChannelMessages = sidebarFavoriteChannelMessages
                        .Where(m => m.Id != messageId)
                        .ToList();
                }

                StateHasChanged();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to remove from favorites";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error removing from favorites: {ex.Message}";
            StateHasChanged();
        }
    }

    #endregion
}