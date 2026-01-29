using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Files.Services;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.Models.Organization;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class Users
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IDepartmentService DepartmentService { get; set; } = default!;
    [Inject] private IPositionService PositionService { get; set; } = default!;
    [Inject] private IFileService FileService { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;

    // User list state
    private List<UserListItemDto> users = [];
    private List<UserListItemDto> filteredUsers = [];
    private string searchTerm = "";
    private string statusFilter = "all";
    private bool isLoading = true;
    private string? errorMessage;

    // Available departments and positions
    private List<DepartmentDto> availableDepartments = [];
    private List<PositionDto> availablePositions = [];

    // Create User Dialog
    private bool showCreateUserDialog = false;
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

    // Edit User Dialog
    private bool showEditUserDialog = false;
    private UserDetailDto? editingUser;
    private UpdateUserRequest editUserModel = new();
    private bool isUpdatingUser = false;
    private string? editUserMessage;
    private bool editUserSuccess = false;
    private byte[]? editAvatarFileData;
    private string? editAvatarFileName;
    private string? editAvatarContentType;
    private long editAvatarFileSize;
    private string? editAvatarPreviewUrl;

    // Change Password Dialog
    private bool showChangePasswordDialog = false;
    private UserDetailDto? changingPasswordUser;
    private AdminChangePasswordRequest changePasswordModel = new();
    private bool isChangingPassword = false;
    private string? changePasswordMessage;
    private bool changePasswordSuccess = false;

    // Delete User Dialog
    private bool showDeleteUserDialog = false;
    private UserDetailDto? deletingUser;
    private bool isDeletingUser = false;
    private string? deleteUserMessage;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadUsers(),
            LoadDepartments(),
            LoadPositions()
        );
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await UserService.GetUsersAsync(1, 1000);

            if (result.IsSuccess && result.Value != null)
            {
                users = result.Value;
                ApplyFilters();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to load users";
            }
        }
        catch
        {
            errorMessage = "An error occurred while loading users";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadDepartments()
    {
        var result = await DepartmentService.GetAllDepartmentsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            availableDepartments = result.Value;
        }
    }

    private async Task LoadPositions()
    {
        var result = await PositionService.GetAllPositionsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            availablePositions = result.Value;
        }
    }

    private void ApplyFilters()
    {
        filteredUsers = users.Where(u =>
        {
            bool matchesSearch = string.IsNullOrEmpty(searchTerm) ||
                u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

            bool matchesStatus = statusFilter == "all" ||
                (statusFilter == "active" && u.IsActive) ||
                (statusFilter == "inactive" && !u.IsActive);

            return matchesSearch && matchesStatus;
        }).ToList();
    }

    private void HandleSearch() => ApplyFilters();

    private void ClearSearch()
    {
        searchTerm = "";
        ApplyFilters();
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

    // Create User Methods
    private void OpenCreateUserDialog()
    {
        createUserModel = new CreateUserRequest();
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
            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
            {
                createUserMessage = "Only image files are allowed for profile pictures";
                selectedAvatarFileData = null;
                avatarPreviewUrl = null;
                return;
            }

            // Validate file size (10MB)
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

            await LoadUsers();

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

    // Edit User Methods
    private async Task OpenEditUserDialog(Guid userId)
    {
        try
        {
            var result = await UserService.GetUserByIdAsync(userId);
            if (result.IsSuccess && result.Value != null)
            {
                editingUser = result.Value;
                editUserModel = new UpdateUserRequest
                {
                    FirstName = editingUser.FirstName,
                    LastName = editingUser.LastName,
                    Email = editingUser.Email,
                    Role = Enum.Parse<Role>(editingUser.Role),
                    PositionId = editingUser.DepartmentId,
                    AvatarUrl = editingUser.AvatarUrl,
                    AboutMe = editingUser.AboutMe,
                    DateOfBirth = editingUser.DateOfBirth,
                    WorkPhone = editingUser.WorkPhone,
                    HiringDate = editingUser.HiringDate
                };
                editUserMessage = null;
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

    private void CloseEditUserDialog() => showEditUserDialog = false;

    private async Task OnEditAvatarFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        editUserMessage = null;
        editUserSuccess = false;

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
        else
        {
            editAvatarFileData = null;
            editAvatarPreviewUrl = null;
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

    private async Task HandleEditUser()
    {
        if (editingUser == null) return;

        isUpdatingUser = true;
        editUserMessage = null;

        try
        {
            if (editAvatarFileData != null && !string.IsNullOrEmpty(editAvatarFileName) && !string.IsNullOrEmpty(editAvatarContentType))
            {
                var uploadResult = await FileService.UploadProfilePictureAsync(
                    editAvatarFileData,
                    editAvatarFileName,
                    editAvatarContentType,
                    editingUser.Id);

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

            var result = await UserService.UpdateUserAsync(editingUser.Id, editUserModel);

            if (result.IsSuccess)
            {
                editUserSuccess = true;
                editUserMessage = "User updated successfully!";
                StateHasChanged();
                await Task.Delay(1500);
                showEditUserDialog = false;
                await LoadUsers();
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

    // Change Password Methods
    private async Task OpenChangePasswordDialog(Guid userId)
    {
        try
        {
            var result = await UserService.GetUserByIdAsync(userId);
            if (result.IsSuccess && result.Value != null)
            {
                changingPasswordUser = result.Value;
                changePasswordModel = new AdminChangePasswordRequest();
                changePasswordMessage = null;
                showChangePasswordDialog = true;
            }
        }
        catch
        {
            // Handle error
        }
    }

    private void CloseChangePasswordDialog() => showChangePasswordDialog = false;

    private async Task HandleChangePassword()
    {
        if (changingPasswordUser == null) return;

        isChangingPassword = true;
        changePasswordMessage = null;

        try
        {
            changePasswordModel.Id = changingPasswordUser.Id;

            var result = await UserService.ChangeUserPasswordAsync(changePasswordModel);

            if (result.IsSuccess)
            {
                changePasswordSuccess = true;
                changePasswordMessage = "Password changed successfully!";
                StateHasChanged();
                await Task.Delay(2500);
                showChangePasswordDialog = false;
            }
            else
            {
                changePasswordSuccess = false;
                changePasswordMessage = result.Error ?? "Failed to change password";
            }
        }
        catch
        {
            changePasswordSuccess = false;
            changePasswordMessage = "An error occurred while changing password";
        }
        finally
        {
            isChangingPassword = false;
            StateHasChanged();
        }
    }

    // Delete User Methods
    private async Task OpenDeleteUserDialog(Guid userId)
    {
        try
        {
            var result = await UserService.GetUserByIdAsync(userId);
            if (result.IsSuccess && result.Value != null)
            {
                deletingUser = result.Value;
                deleteUserMessage = null;
                showDeleteUserDialog = true;
            }
        }
        catch
        {
            // Handle error
        }
    }

    private void CloseDeleteUserDialog() => showDeleteUserDialog = false;

    private async Task HandleDeleteUser()
    {
        if (deletingUser == null) return;

        isDeletingUser = true;
        deleteUserMessage = null;

        try
        {
            var result = await UserService.DeleteUserAsync(deletingUser.Id);

            if (result.IsSuccess)
            {
                showDeleteUserDialog = false;
                await LoadUsers();
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

    // Activate/Deactivate Methods
    private async Task ActivateUser(Guid userId)
    {
        var result = await UserService.ActivateUserAsync(userId);
        if (result.IsSuccess)
        {
            await LoadUsers();
        }
    }

    private async Task DeactivateUser(Guid userId)
    {
        var result = await UserService.DeactivateUserAsync(userId);
        if (result.IsSuccess)
        {
            await LoadUsers();
        }
    }
}
