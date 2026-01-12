using ChatApp.Blazor.Client.Models.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChatApp.Blazor.Client.Features.Messages.Components;

public partial class MentionPanel : IAsyncDisposable
{
    #region Parameters

    /// <summary>
    /// Bütün istifadəçi siyahısı (DM: 1 user, Channel: All + members).
    /// </summary>
    [Parameter]
    public List<MentionUserDto> Users { get; set; } = [];

    /// <summary>
    /// Axtarış sorğusu (@ sonra yazdığı mətn).
    /// Parent-dən gəlir (MessageInput mentionSearchQuery).
    /// </summary>
    [Parameter]
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    /// İstifadəçi seçildikdə trigger olunan callback.
    /// </summary>
    [Parameter]
    public EventCallback<MentionUserDto> OnUserSelected { get; set; }

    /// <summary>
    /// Mention panel bağlandıqda trigger olunan callback (Esc basıldıqda).
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    #endregion

    #region State

    private ElementReference mentionPanelRef;
    private ElementReference mentionListRef;

    private List<MentionUserDto> filteredUsers = [];
    private List<MentionUserDto> displayedUsers = [];
    private int selectedIndex = 0;
    private int currentPage = 0;
    private const int PageSize = 10;
    private bool hasMoreUsers => (currentPage + 1) * PageSize < filteredUsers.Count;

    private DotNetObjectReference<MentionPanel>? dotNetRef;
    private IJSObjectReference? jsModule;

    // Track previous parameter values to detect actual changes
    private List<MentionUserDto> _previousUsers = [];
    private string _previousSearchQuery = string.Empty;

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        // İlk initialization (Users hələ boşdur)
        FilterUsers();
        LoadPage(0);
    }

    protected override void OnParametersSet()
    {
        // Only re-filter if Users list or SearchQuery actually changed
        bool usersChanged = !_previousUsers.SequenceEqual(Users);
        bool searchChanged = _previousSearchQuery != SearchQuery;

        if (usersChanged || searchChanged)
        {
            FilterUsers();
            LoadPage(0);

            _previousUsers = Users.ToList();
            _previousSearchQuery = SearchQuery;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/mention.js");
                dotNetRef = DotNetObjectReference.Create(this);
                await jsModule.InvokeVoidAsync("initializeMentionPanel", dotNetRef, mentionPanelRef);
            }
            catch (Exception)
            {
                // JS module yüklənmə xətası (ignore)
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("disposeMentionPanel");
                await jsModule.DisposeAsync();
            }

            dotNetRef?.Dispose();
        }
        catch
        {
            // Dispose xətası (ignore)
        }
    }

    #endregion

    #region Search & Filter

    private void FilterUsers()
    {
        // Parent-dən gələn SearchQuery istifadə et (@ sonra yazdığı mətn)
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            filteredUsers = Users.ToList();
        }
        else
        {
            var query = SearchQuery.Trim().ToLower();
            filteredUsers = Users
                .Where(u => u.Name.ToLower().Contains(query))
                .ToList();
        }

        // Reset pagination
        currentPage = 0;
        selectedIndex = 0;
        LoadPage(0);
    }

    private void LoadPage(int page)
    {
        currentPage = page;
        var skip = page * PageSize;
        displayedUsers = filteredUsers.Skip(skip).Take(PageSize).ToList();
        StateHasChanged();
    }

    private void LoadMoreUsers()
    {
        if (hasMoreUsers)
        {
            currentPage++;
            var skip = currentPage * PageSize;
            var nextBatch = filteredUsers.Skip(skip).Take(PageSize).ToList();
            displayedUsers.AddRange(nextBatch);
            StateHasChanged();
        }
    }

    #endregion

    #region Selection

    private async Task SelectUser(MentionUserDto user)
    {
        await OnUserSelected.InvokeAsync(user);
    }

    [JSInvokable]
    public async Task HandleKeyDown(string key)
    {
        switch (key)
        {
            case "ArrowDown":
                MoveSelectionDown();
                break;

            case "ArrowUp":
                MoveSelectionUp();
                break;

            case "Enter":
                if (displayedUsers.Count > 0 && selectedIndex >= 0 && selectedIndex < displayedUsers.Count)
                {
                    await SelectUser(displayedUsers[selectedIndex]);
                }
                break;

            case "Escape":
                await OnCancel.InvokeAsync();
                break;
        }
    }

    private async void MoveSelectionDown()
    {
        if (displayedUsers.Count == 0) return;

        selectedIndex++;
        if (selectedIndex >= displayedUsers.Count)
        {
            selectedIndex = 0; // Wrap to top
        }

        // Auto-load more if close to bottom
        if (selectedIndex == displayedUsers.Count - 1 && hasMoreUsers)
        {
            LoadMoreUsers();
        }

        StateHasChanged();
        await ScrollToSelectedItem();
    }

    private async void MoveSelectionUp()
    {
        if (displayedUsers.Count == 0) return;

        selectedIndex--;
        if (selectedIndex < 0)
        {
            selectedIndex = displayedUsers.Count - 1; // Wrap to bottom
        }

        StateHasChanged();
        await ScrollToSelectedItem();
    }

    private async Task ScrollToSelectedItem()
    {
        try
        {
            if (jsModule != null)
            {
                await jsModule.InvokeVoidAsync("scrollToSelectedMentionItem", mentionListRef, selectedIndex);
            }
        }
        catch
        {
            // JS scroll xətası (ignore)
        }
    }

    #endregion
}