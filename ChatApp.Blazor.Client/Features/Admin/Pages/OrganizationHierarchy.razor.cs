using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Files.Services;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Organization;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class OrganizationHierarchy
{
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IDepartmentService DepartmentService { get; set; } = default!;
    [Inject] private IPositionService PositionService { get; set; } = default!;
    [Inject] private IFileService FileService { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Hierarchy state
    private List<OrganizationHierarchyNode> hierarchyRoots = [];
    private List<OrganizationHierarchyNode> allNodes = [];
    private List<DepartmentDto> allDepartments = [];
    private List<PositionDto> allPositions = [];
    private string searchTerm = "";
    private bool isLoading = true;
    private string? errorMessage;
    private int totalDepartments = 0;
    private int totalUsers = 0;
    private int activeUsers = 0;
    private int maxDepth = 0;

    // Create User Dialog (context-aware)
    private bool showCreateUserDialog = false;
    private Guid? createUserInDepartmentId;
    private string? createUserInDepartmentName;
    private CreateUserRequest createUserModel = new();
    private bool isCreatingUser = false;
    private string? createUserMessage;
    private bool createUserSuccess = false;
    private byte[]? selectedAvatarFileData;
    private string? selectedAvatarFileName;
    private string? selectedAvatarContentType;
    private long selectedAvatarFileSize;
    private bool isUploadingAvatar = false;
    private string? avatarPreviewUrl;

    // Create Department Dialog (context-aware)
    private bool showCreateDepartmentDialog = false;
    private Guid? createDepartmentParentId;
    private string? createDepartmentParentName;
    private CreateDepartmentRequest createDepartmentModel = new();
    private bool isCreatingDepartment = false;
    private string? createDepartmentMessage;
    private bool createDepartmentSuccess = false;

    // Edit User Dialog
    private bool showEditUserDialog = false;
    private OrganizationHierarchyNode? editingUserNode;
    private UpdateUserRequest editUserModel = new();
    private bool isUpdatingUser = false;
    private string? editUserMessage;
    private bool editUserSuccess = false;
    private byte[]? editAvatarFileData;
    private string? editAvatarFileName;
    private string? editAvatarContentType;
    private long editAvatarFileSize;
    private string? editAvatarPreviewUrl;

    // Edit Department Dialog
    private bool showEditDepartmentDialog = false;
    private OrganizationHierarchyNode? editingDepartmentNode;
    private UpdateDepartmentRequest editDepartmentModel = new();
    private bool isUpdatingDepartment = false;
    private string? editDepartmentMessage;
    private bool editDepartmentSuccess = false;

    // Delete User Dialog
    private bool showDeleteUserDialog = false;
    private OrganizationHierarchyNode? deletingUserNode;
    private bool isDeletingUser = false;
    private string? deleteUserMessage;

    // Delete Department Dialog
    private bool showDeleteDepartmentDialog = false;
    private OrganizationHierarchyNode? deletingDepartmentNode;
    private bool isDeletingDepartment = false;
    private string? deleteDepartmentMessage;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadHierarchy(),
            LoadDepartments(),
            LoadPositions()
        );
    }

    private async Task LoadHierarchy()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await OrganizationService.GetOrganizationHierarchyAsync();

            if (result.IsSuccess && result.Value != null)
            {
                hierarchyRoots = result.Value;
                FlattenNodes(hierarchyRoots);
                CalculateStatistics();
                ApplySearch();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to load organization hierarchy";
            }
        }
        catch
        {
            errorMessage = "An error occurred while loading organization hierarchy";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void FlattenNodes(List<OrganizationHierarchyNode> roots)
    {
        allNodes = [];
        foreach (var root in roots)
        {
            FlattenNode(root);
        }
    }

    private void FlattenNode(OrganizationHierarchyNode node)
    {
        allNodes.Add(node);
        foreach (var child in node.Children)
        {
            FlattenNode(child);
        }
    }

    private void CalculateStatistics()
    {
        totalDepartments = allNodes.Count(n => n.Type == NodeType.Department);
        totalUsers = allNodes.Count(n => n.Type == NodeType.User);
        activeUsers = allNodes.Count(n => n.Type == NodeType.User && n.IsActive);
        maxDepth = allNodes.Any() ? allNodes.Max(n => n.Level) : 0;
    }

    private async Task LoadDepartments()
    {
        var result = await DepartmentService.GetAllDepartmentsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            allDepartments = result.Value;
        }
    }

    private async Task LoadPositions()
    {
        var result = await PositionService.GetAllPositionsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            allPositions = result.Value;
        }
    }

    private void ToggleNode(OrganizationHierarchyNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        StateHasChanged();
    }

    private void ExpandAll()
    {
        foreach (var node in allNodes.Where(n => n.Type == NodeType.Department))
        {
            node.IsExpanded = true;
        }
        StateHasChanged();
    }

    private void CollapseAll()
    {
        foreach (var node in allNodes.Where(n => n.Type == NodeType.Department))
        {
            node.IsExpanded = false;
        }
        StateHasChanged();
    }

    private void HandleSearch()
    {
        ApplySearch();
        StateHasChanged();
    }

    private void ClearSearch()
    {
        searchTerm = "";
        ApplySearch();
        StateHasChanged();
    }

    private void ApplySearch()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            foreach (var node in allNodes)
            {
                node.IsVisible = true;
                node.MatchesSearch = true;
            }
            return;
        }

        var search = searchTerm.ToLower();

        foreach (var node in allNodes)
        {
            bool matches = node.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                          (node.Email != null && node.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                          (node.PositionName != null && node.PositionName.Contains(search, StringComparison.OrdinalIgnoreCase));

            node.MatchesSearch = matches;
        }

        // Show parents of matching nodes
        foreach (var node in allNodes.Where(n => n.MatchesSearch))
        {
            ShowParentChain(node);
        }

        // Set visibility
        foreach (var node in allNodes)
        {
            node.IsVisible = node.MatchesSearch || HasMatchingDescendant(node);
        }

        // Auto-expand nodes with matches
        foreach (var node in allNodes.Where(n => n.Type == NodeType.Department && HasMatchingDescendant(n)))
        {
            node.IsExpanded = true;
        }
    }

    private void ShowParentChain(OrganizationHierarchyNode node)
    {
        if (node.Type == NodeType.User && node.DepartmentId.HasValue)
        {
            var parent = allNodes.FirstOrDefault(n => n.Type == NodeType.Department && n.Id == node.DepartmentId.Value);
            if (parent != null)
            {
                parent.MatchesSearch = true;
                ShowParentChain(parent);
            }
        }
        else if (node.Type == NodeType.Department && node.ParentDepartmentId.HasValue)
        {
            var parent = allNodes.FirstOrDefault(n => n.Type == NodeType.Department && n.Id == node.ParentDepartmentId.Value);
            if (parent != null)
            {
                parent.MatchesSearch = true;
                ShowParentChain(parent);
            }
        }
    }

    private bool HasMatchingDescendant(OrganizationHierarchyNode node)
    {
        return node.Children.Any(c => c.MatchesSearch || HasMatchingDescendant(c));
    }

    // Create User Methods
    private void OpenCreateUserDialog(Guid? departmentId = null, string? departmentName = null)
    {
        createUserInDepartmentId = departmentId;
        createUserInDepartmentName = departmentName;
        createUserModel = new CreateUserRequest
        {
            DepartmentId = departmentId ?? Guid.Empty
        };
        createUserMessage = null;
        createUserSuccess = false;
        selectedAvatarFileData = null;
        selectedAvatarFileName = null;
        selectedAvatarContentType = null;
        selectedAvatarFileSize = 0;
        avatarPreviewUrl = null;
        isUploadingAvatar = false;
        showCreateUserDialog = true;
    }

    private void CloseCreateUserDialog()
    {
        showCreateUserDialog = false;
        createUserMessage = null;
        createUserSuccess = false;
        selectedAvatarFileData = null;
        selectedAvatarFileName = null;
        selectedAvatarContentType = null;
        selectedAvatarFileSize = 0;
        avatarPreviewUrl = null;
        isUploadingAvatar = false;
    }

    private async Task OnAvatarFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        createUserMessage = null;
        createUserSuccess = false;

        if (file != null)
        {
            if (!file.ContentType.StartsWith("image/"))
            {
                createUserMessage = "Only image files are allowed for profile pictures";
                selectedAvatarFileData = null;
                avatarPreviewUrl = null;
                return;
            }

            if (file.Size > 10 * 1024 * 1024)
            {
                createUserMessage = "File size must be less than 10 MB";
                selectedAvatarFileData = null;
                avatarPreviewUrl = null;
                return;
            }

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var buffer = new byte[file.Size];
                await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

                selectedAvatarFileData = buffer;
                selectedAvatarFileName = file.Name;
                selectedAvatarContentType = file.ContentType;
                selectedAvatarFileSize = file.Size;
                avatarPreviewUrl = file.Name;

                StateHasChanged();
            }
            catch (Exception ex)
            {
                createUserMessage = $"Error reading file: {ex.Message}";
                selectedAvatarFileData = null;
                avatarPreviewUrl = null;
            }
        }
        else
        {
            selectedAvatarFileData = null;
            avatarPreviewUrl = null;
        }
    }

    private void RemoveAvatar()
    {
        selectedAvatarFileData = null;
        selectedAvatarFileName = null;
        selectedAvatarContentType = null;
        selectedAvatarFileSize = 0;
        avatarPreviewUrl = null;
        createUserModel.AvatarUrl = null;
        StateHasChanged();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private async Task HandleCreateUser()
    {
        isCreatingUser = true;
        createUserMessage = null;
        createUserSuccess = false;

        try
        {
            createUserModel.AvatarUrl = null;

            var result = await UserService.CreateUserAsync(createUserModel);

            if (!result.IsSuccess || result.Value == null)
            {
                createUserSuccess = false;
                createUserMessage = !string.IsNullOrEmpty(result.Error)
                    ? result.Error
                    : "Failed to create user. Please check your input and try again.";
                return;
            }

            var newUserId = result.Value.UserId;

            // Upload avatar if selected
            if (selectedAvatarFileData != null && !string.IsNullOrEmpty(selectedAvatarFileName) && !string.IsNullOrEmpty(selectedAvatarContentType))
            {
                isUploadingAvatar = true;
                StateHasChanged();

                var uploadResult = await FileService.UploadProfilePictureAsync(
                    selectedAvatarFileData,
                    selectedAvatarFileName,
                    selectedAvatarContentType,
                    newUserId);

                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    var avatarUrl = uploadResult.Value.ThumbnailUrl ?? uploadResult.Value.DownloadUrl;
                    var updateRequest = new UpdateUserRequest
                    {
                        FirstName = createUserModel.FirstName,
                        LastName = createUserModel.LastName,
                        Email = createUserModel.Email,
                        AvatarUrl = avatarUrl
                    };

                    await UserService.UpdateUserAsync(newUserId, updateRequest);
                }

                isUploadingAvatar = false;
            }

            createUserSuccess = true;
            createUserMessage = result.Value.Message ?? "User created successfully!";

            await LoadHierarchy();

            StateHasChanged();
            await Task.Delay(1500);
            showCreateUserDialog = false;
            createUserMessage = null;
        }
        catch (Exception ex)
        {
            createUserSuccess = false;
            createUserMessage = !string.IsNullOrEmpty(ex.Message)
                ? ex.Message
                : "An unexpected error occurred. Please try again.";
        }
        finally
        {
            isCreatingUser = false;
            isUploadingAvatar = false;
        }
    }

    // Create Department Methods
    private void OpenCreateDepartmentDialog(Guid? parentId = null, string? parentName = null)
    {
        createDepartmentParentId = parentId;
        createDepartmentParentName = parentName;
        createDepartmentModel = new CreateDepartmentRequest { ParentDepartmentId = parentId };
        createDepartmentMessage = null;
        createDepartmentSuccess = false;
        showCreateDepartmentDialog = true;
    }

    private void CloseCreateDepartmentDialog()
    {
        showCreateDepartmentDialog = false;
        createDepartmentMessage = null;
        createDepartmentSuccess = false;
    }

    private async Task HandleCreateDepartment()
    {
        isCreatingDepartment = true;
        createDepartmentMessage = null;
        createDepartmentSuccess = false;

        try
        {
            var result = await DepartmentService.CreateDepartmentAsync(createDepartmentModel);

            if (result.IsSuccess)
            {
                createDepartmentSuccess = true;
                createDepartmentMessage = "Department created successfully!";
                StateHasChanged();
                await Task.Delay(1500);
                showCreateDepartmentDialog = false;
                await LoadHierarchy();
                await LoadDepartments();
            }
            else
            {
                createDepartmentSuccess = false;
                createDepartmentMessage = result.Error ?? "Failed to create department";
            }
        }
        catch
        {
            createDepartmentSuccess = false;
            createDepartmentMessage = "An error occurred while creating the department";
        }
        finally
        {
            isCreatingDepartment = false;
            StateHasChanged();
        }
    }

    // Edit Department Methods
    private void OpenEditDepartmentDialog(OrganizationHierarchyNode node)
    {
        editingDepartmentNode = node;
        editDepartmentModel = new UpdateDepartmentRequest
        {
            Name = node.Name,
            ParentDepartmentId = node.ParentDepartmentId
        };
        editDepartmentMessage = null;
        editDepartmentSuccess = false;
        showEditDepartmentDialog = true;
    }

    private void CloseEditDepartmentDialog()
    {
        showEditDepartmentDialog = false;
        editDepartmentMessage = null;
        editDepartmentSuccess = false;
    }

    private async Task HandleEditDepartment()
    {
        if (editingDepartmentNode == null) return;

        isUpdatingDepartment = true;
        editDepartmentMessage = null;
        editDepartmentSuccess = false;

        try
        {
            var result = await DepartmentService.UpdateDepartmentAsync(editingDepartmentNode.Id, editDepartmentModel);

            if (result.IsSuccess)
            {
                editDepartmentSuccess = true;
                editDepartmentMessage = "Department updated successfully!";
                StateHasChanged();
                await Task.Delay(1500);
                showEditDepartmentDialog = false;
                await LoadHierarchy();
                await LoadDepartments();
            }
            else
            {
                editDepartmentSuccess = false;
                editDepartmentMessage = result.Error ?? "Failed to update department";
            }
        }
        catch
        {
            editDepartmentSuccess = false;
            editDepartmentMessage = "An error occurred while updating the department";
        }
        finally
        {
            isUpdatingDepartment = false;
            StateHasChanged();
        }
    }

    // Delete Department Methods
    private void OpenDeleteDepartmentDialog(OrganizationHierarchyNode node)
    {
        deletingDepartmentNode = node;
        deleteDepartmentMessage = null;
        showDeleteDepartmentDialog = true;
    }

    private void CloseDeleteDepartmentDialog()
    {
        showDeleteDepartmentDialog = false;
        deleteDepartmentMessage = null;
    }

    private async Task HandleDeleteDepartment()
    {
        if (deletingDepartmentNode == null) return;

        isDeletingDepartment = true;
        deleteDepartmentMessage = null;

        try
        {
            var result = await DepartmentService.DeleteDepartmentAsync(deletingDepartmentNode.Id);

            if (result.IsSuccess)
            {
                showDeleteDepartmentDialog = false;
                await LoadHierarchy();
                await LoadDepartments();
            }
            else
            {
                deleteDepartmentMessage = result.Error ?? "Failed to delete department";
            }
        }
        catch
        {
            deleteDepartmentMessage = "An error occurred while deleting the department";
        }
        finally
        {
            isDeletingDepartment = false;
        }
    }

    // Edit User Methods
    private async Task OpenEditUserDialog(OrganizationHierarchyNode node)
    {
        try
        {
            var result = await UserService.GetUserByIdAsync(node.Id);
            if (result.IsSuccess && result.Value != null)
            {
                editingUserNode = node;
                var user = result.Value;
                editUserModel = new UpdateUserRequest
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = Enum.Parse<Role>(user.Role),
                    PositionId = user.PositionId,
                    AvatarUrl = user.AvatarUrl,
                    AboutMe = user.AboutMe,
                    DateOfBirth = user.DateOfBirth,
                    WorkPhone = user.WorkPhone,
                    HiringDate = user.HiringDate
                };
                editUserMessage = null;
                editUserSuccess = false;
                editAvatarFileData = null;
                editAvatarFileName = null;
                editAvatarContentType = null;
                editAvatarFileSize = 0;
                editAvatarPreviewUrl = null;
                showEditUserDialog = true;
            }
        }
        catch
        {
            // Handle error
        }
    }

    private void CloseEditUserDialog()
    {
        showEditUserDialog = false;
        editUserMessage = null;
        editUserSuccess = false;
    }

    private async Task HandleEditUser()
    {
        if (editingUserNode == null) return;

        isUpdatingUser = true;
        editUserMessage = null;
        editUserSuccess = false;

        try
        {
            if (editAvatarFileData != null && !string.IsNullOrEmpty(editAvatarFileName) && !string.IsNullOrEmpty(editAvatarContentType))
            {
                var uploadResult = await FileService.UploadProfilePictureAsync(
                    editAvatarFileData,
                    editAvatarFileName,
                    editAvatarContentType,
                    editingUserNode.Id);

                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    editUserModel.AvatarUrl = uploadResult.Value.ThumbnailUrl ?? uploadResult.Value.DownloadUrl;
                }
                else
                {
                    editUserSuccess = false;
                    editUserMessage = !string.IsNullOrEmpty(uploadResult.Error)
                        ? uploadResult.Error
                        : "Failed to upload avatar. Please try again.";
                    isUpdatingUser = false;
                    return;
                }
            }

            var result = await UserService.UpdateUserAsync(editingUserNode.Id, editUserModel);

            if (result.IsSuccess)
            {
                editUserSuccess = true;
                editUserMessage = "User updated successfully!";
                StateHasChanged();
                await Task.Delay(1500);
                showEditUserDialog = false;
                await LoadHierarchy();
            }
            else
            {
                editUserSuccess = false;
                editUserMessage = result.Error ?? "Failed to update user";
                StateHasChanged();
            }
        }
        catch
        {
            editUserSuccess = false;
            editUserMessage = "An error occurred while updating the user";
            StateHasChanged();
        }
        finally
        {
            isUpdatingUser = false;
            StateHasChanged();
        }
    }

    // Delete User Methods
    private void OpenDeleteUserDialog(OrganizationHierarchyNode node)
    {
        deletingUserNode = node;
        deleteUserMessage = null;
        showDeleteUserDialog = true;
    }

    private void CloseDeleteUserDialog()
    {
        showDeleteUserDialog = false;
        deleteUserMessage = null;
    }

    private async Task HandleDeleteUser()
    {
        if (deletingUserNode == null) return;

        isDeletingUser = true;
        deleteUserMessage = null;

        try
        {
            var result = await UserService.DeleteUserAsync(deletingUserNode.Id);

            if (result.IsSuccess)
            {
                showDeleteUserDialog = false;
                await LoadHierarchy();
            }
            else
            {
                deleteUserMessage = result.Error ?? "Failed to delete user";
            }
        }
        catch
        {
            deleteUserMessage = "An error occurred while deleting the user";
        }
        finally
        {
            isDeletingUser = false;
        }
    }

    // Edit Avatar Methods (for edit user dialog)
    private async Task OnEditAvatarFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        editUserMessage = null;

        if (file != null)
        {
            if (!file.ContentType.StartsWith("image/"))
            {
                editUserMessage = "Only image files are allowed for profile pictures";
                editAvatarFileData = null;
                editAvatarPreviewUrl = null;
                return;
            }

            if (file.Size > 10 * 1024 * 1024)
            {
                editUserMessage = "File size must be less than 10 MB";
                editAvatarFileData = null;
                editAvatarPreviewUrl = null;
                return;
            }

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var buffer = new byte[file.Size];
                await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

                editAvatarFileData = buffer;
                editAvatarFileName = file.Name;
                editAvatarContentType = file.ContentType;
                editAvatarFileSize = file.Size;
                editAvatarPreviewUrl = file.Name;

                StateHasChanged();
            }
            catch (Exception ex)
            {
                editUserMessage = $"Error reading file: {ex.Message}";
                editAvatarFileData = null;
                editAvatarPreviewUrl = null;
            }
        }
    }

    private void RemoveEditAvatar()
    {
        editAvatarFileData = null;
        editAvatarFileName = null;
        editAvatarContentType = null;
        editAvatarFileSize = 0;
        editAvatarPreviewUrl = null;
        editUserModel.AvatarUrl = null;
        StateHasChanged();
    }

    // Navigation helpers
    private void NavigateToDepartment(Guid departmentId) => Navigation.NavigateTo($"/admin/department/{departmentId}");
    private void NavigateToUser(Guid userId) => Navigation.NavigateTo($"/admin/user/{userId}");
}
