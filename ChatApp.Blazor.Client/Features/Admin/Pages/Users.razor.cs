using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Features.Files.Services;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class Users
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IFileService FileService { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;

    // User list state
    private List<UserDto> users = [];
    private List<UserDto> filteredUsers = [];
    private string searchTerm = "";
    private string statusFilter = "all";
    private bool isLoading = true;
    private string? errorMessage;

    // Available roles
    private List<RoleDto> availableRoles = new();

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
    private UserDto? editingUser;
    private UpdateUserRequest editUserModel = new();
    private bool isUpdatingUser = false;
    private string? editUserMessage;
    private bool editUserSuccess = false;
    private byte[]? editAvatarFileData;
    private string? editAvatarFileName;
    private string? editAvatarContentType;
    private long editAvatarFileSize;
    private string? editAvatarPreviewUrl;

    // Manage Roles Dialog
    private bool showManageRolesDialog = false;
    private UserDto? managingRolesUser;
    private List<Guid> selectedRoleIds = new();
    private bool isUpdatingRoles = false;
    private string? rolesMessage;
    private bool rolesSuccess = false;

    // Change Password Dialog
    private bool showChangePasswordDialog = false;
    private UserDto? changingPasswordUser;
    private AdminChangePasswordRequest changePasswordModel = new();
    private bool isChangingPassword = false;
    private string? changePasswordMessage;
    private bool changePasswordSuccess = false;

    // Delete User Dialog
    private bool showDeleteUserDialog = false;
    private UserDto? deletingUser;
    private bool isDeletingUser = false;
    private string? deleteUserMessage;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadUsers(),
            LoadRoles()
        );
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await UserService.GetUsersAsync();

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

    private async Task LoadRoles()
    {
        var result = await RoleService.GetRolesAsync();
        if (result.IsSuccess && result.Value != null)
        {
            availableRoles = result.Value;
        }
    }


    private void ApplyFilters()
    {
        filteredUsers = users.Where(u =>
        {
            bool matchesSearch = string.IsNullOrEmpty(searchTerm) ||
                u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
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

    private string GetInitials(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "?";
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : displayName[0].ToString().ToUpper();
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
                // Read file into memory immediately to avoid Blazor reference issues
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var buffer = new byte[file.Size];
                await stream.ReadAsync(buffer);

                // Store file data in memory
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
            // Upload avatar first if selected
            if (selectedAvatarFileData != null && !string.IsNullOrEmpty(selectedAvatarFileName) && !string.IsNullOrEmpty(selectedAvatarContentType))
            {
                isUploadingAvatar = true;
                StateHasChanged();

                // Note: For new user creation, we don't have a userId yet, so upload to current admin's folder
                // The avatar will be moved to the new user's folder after user creation if needed
                var uploadResult = await FileService.UploadProfilePictureAsync(
                    selectedAvatarFileData,
                    selectedAvatarFileName,
                    selectedAvatarContentType,
                    null); // Upload to current user's folder for now

                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    // Use thumbnail URL for performance, fall back to download URL if no thumbnail
                    createUserModel.AvatarUrl = uploadResult.Value.ThumbnailUrl ?? uploadResult.Value.DownloadUrl;
                }
                else
                {
                    createUserSuccess = false;
                    createUserMessage = !string.IsNullOrEmpty(uploadResult.Error)
                        ? uploadResult.Error
                        : "Failed to upload avatar. Please try again.";
                    isUploadingAvatar = false;
                    isCreatingUser = false;
                    return;
                }

                isUploadingAvatar = false;
            }

            // Create user
            createUserModel.CreatedBy = UserState.CurrentUser?.Id ?? Guid.Empty;
            var result = await UserService.CreateUserAsync(createUserModel);

            if (result.IsSuccess && result.Value != null)
            {
                createUserSuccess = true;
                createUserMessage = result.Value.Message ?? "User created successfully!";

                // Load users first
                await LoadUsers();

                // Then show success message and close dialog
                StateHasChanged();
                await Task.Delay(1500);
                showCreateUserDialog = false;
                createUserMessage = null;
            }
            else
            {
                createUserSuccess = false;
                // Show detailed error message from API
                createUserMessage = !string.IsNullOrEmpty(result.Error)
                    ? result.Error
                    : "Failed to create user. Please check your input and try again.";
            }
        }
        catch (Exception ex)
        {
            createUserSuccess = false;
            // Log the full exception but show user-friendly message
            Console.WriteLine($"Error creating user: {ex}");
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
    private void OpenEditUserDialog(UserDto user)
    {
        editingUser = user;
        editUserModel = new UpdateUserRequest
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Notes = user.Notes
        };
        editUserMessage = null;
        editAvatarFileData = null;
        editAvatarFileName = null;
        editAvatarContentType = null;
        editAvatarFileSize = 0;
        editAvatarPreviewUrl = null;
        showEditUserDialog = true;
    }

    private void CloseEditUserDialog() => showEditUserDialog = false;

    private async Task OnEditAvatarFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        editUserMessage = null;
        editUserSuccess = false;

        if (file != null)
        {
            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
            {
                editUserMessage = "Only image files are allowed for profile pictures";
                editAvatarFileData = null;
                editAvatarPreviewUrl = null;
                return;
            }

            // Validate file size (10MB)
            if (file.Size > 10 * 1024 * 1024)
            {
                editUserMessage = "File size must be less than 10 MB";
                editAvatarFileData = null;
                editAvatarPreviewUrl = null;
                return;
            }

            try
            {
                // Read file into memory immediately to avoid Blazor reference issues
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var buffer = new byte[file.Size];
                await stream.ReadAsync(buffer);

                // Store file data in memory
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
            // Upload avatar first if a new one is selected
            if (editAvatarFileData != null && !string.IsNullOrEmpty(editAvatarFileName) && !string.IsNullOrEmpty(editAvatarContentType))
            {
                // Upload to the target user's folder (editingUser.Id)
                var uploadResult = await FileService.UploadProfilePictureAsync(
                    editAvatarFileData,
                    editAvatarFileName,
                    editAvatarContentType,
                    editingUser.Id); // Pass target user ID to upload to their folder

                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    // Use thumbnail URL for performance, fall back to download URL if no thumbnail
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
                await Task.Delay(1500);
                showEditUserDialog = false;
                await LoadUsers();
            }
            else
            {
                editUserSuccess = false;
                editUserMessage = result.Error ?? "Failed to update user";
            }
        }
        catch
        {
            editUserSuccess = false;
            editUserMessage = "An error occurred while updating the user";
        }
        finally
        {
            isUpdatingUser = false;
        }
    }

    // Manage Roles Methods
    private void OpenManageRolesDialog(UserDto user)
    {
        managingRolesUser = user;
        selectedRoleIds = user.Roles.Select(r => r.Id).ToList();
        rolesMessage = null;
        showManageRolesDialog = true;
    }

    private void CloseManageRolesDialog() => showManageRolesDialog = false;

    private void ToggleRole(Guid roleId)
    {
        if (selectedRoleIds.Contains(roleId))
            selectedRoleIds.Remove(roleId);
        else
            selectedRoleIds.Add(roleId);
    }

    private async Task SaveRoles()
    {
        if (managingRolesUser == null) return;

        isUpdatingRoles = true;
        rolesMessage = null;

        try
        {
            var currentRoleIds = managingRolesUser.Roles.Select(r => r.Id).ToHashSet();
            var rolesToAdd = selectedRoleIds.Except(currentRoleIds).ToList();
            var rolesToRemove = currentRoleIds.Except(selectedRoleIds).ToList();

            foreach (var roleId in rolesToAdd)
            {
                await UserService.AssignRoleAsync(managingRolesUser.Id, roleId);
            }

            foreach (var roleId in rolesToRemove)
            {
                await UserService.RemoveRoleAsync(managingRolesUser.Id, roleId);
            }

            rolesSuccess = true;
            rolesMessage = "Roles updated successfully!";
            await Task.Delay(1500);
            showManageRolesDialog = false;
            await LoadUsers();
        }
        catch
        {
            rolesSuccess = false;
            rolesMessage = "An error occurred while updating roles";
        }
        finally
        {
            isUpdatingRoles = false;
        }
    }

    // Change Password Methods
    private void OpenChangePasswordDialog(UserDto user)
    {
        changingPasswordUser = user;
        changePasswordModel = new AdminChangePasswordRequest();
        changePasswordMessage = null;
        showChangePasswordDialog = true;
    }

    private void CloseChangePasswordDialog() => showChangePasswordDialog = false;

    private async Task HandleChangePassword()
    {
        if (changingPasswordUser == null) return;

        isChangingPassword = true;
        changePasswordMessage = null;

        try
        {
            // Set the user ID before sending the request
            changePasswordModel.Id = changingPasswordUser.Id;

            var result = await UserService.ChangeUserPasswordAsync(changePasswordModel);

            if (result.IsSuccess)
            {
                changePasswordSuccess = true;
                changePasswordMessage = "Password changed successfully!";

                // Force UI update to show success message immediately
                StateHasChanged();

                // Wait 2.5 seconds so user can see the success message
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
    private void OpenDeleteUserDialog(UserDto user)
    {
        deletingUser = user;
        deleteUserMessage = null;
        showDeleteUserDialog = true;
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
    private async Task ActivateUser(UserDto user)
    {
        var result = await UserService.ActivateUserAsync(user.Id);
        if (result.IsSuccess)
        {
            await LoadUsers();
        }
    }

    private async Task DeactivateUser(UserDto user)
    {
        var result = await UserService.DeactivateUserAsync(user.Id);
        if (result.IsSuccess)
        {
            await LoadUsers();
        }
    }
}