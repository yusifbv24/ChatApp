using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Features.Auth.Services;
using ChatApp.Blazor.Client.Models.Auth;
using ChatApp.Blazor.Client.State;
using Microsoft.AspNetCore.Components;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class Users
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IPermissionService PermissionService { get; set; } = default!;
    [Inject] private UserState UserState { get; set; } = default!;

    // User list state
    private List<UserDto> users = new();
    private List<UserDto> filteredUsers = new();
    private string searchTerm = "";
    private string statusFilter = "all";
    private bool isLoading = true;
    private string? errorMessage;

    // Available roles and permissions
    private List<RoleDto> availableRoles = new();
    private List<PermissionDto> availablePermissions = new();

    // Create User Dialog
    private bool showCreateUserDialog = false;
    private CreateUserRequest createUserModel = new();
    private bool isCreatingUser = false;
    private string? createUserMessage;
    private bool createUserSuccess = false;

    // Edit User Dialog
    private bool showEditUserDialog = false;
    private UserDto? editingUser;
    private UpdateUserRequest editUserModel = new();
    private bool isUpdatingUser = false;
    private string? editUserMessage;
    private bool editUserSuccess = false;

    // Manage Roles Dialog
    private bool showManageRolesDialog = false;
    private UserDto? managingRolesUser;
    private List<Guid> selectedRoleIds = new();
    private bool isUpdatingRoles = false;
    private string? rolesMessage;
    private bool rolesSuccess = false;

    // Manage Permissions Dialog
    private bool showManagePermissionsDialog = false;
    private UserDto? managingPermissionsUser;
    private Dictionary<Guid, bool> permissionStates = new(); // permissionId -> isGranted
    private string selectedModule = "all";
    private List<PermissionDto> filteredPermissions = new();
    private bool isUpdatingPermissions = false;
    private string? permissionsMessage;
    private bool permissionsSuccess = false;

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
            LoadRoles(),
            LoadPermissions()
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

    private async Task LoadPermissions()
    {
        var result = await PermissionService.GetPermissionsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            availablePermissions = result.Value;
            filteredPermissions = availablePermissions;
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

    // Create User Methods
    private void OpenCreateUserDialog()
    {
        createUserModel = new CreateUserRequest();
        createUserMessage = null;
        showCreateUserDialog = true;
    }

    private void CloseCreateUserDialog() => showCreateUserDialog = false;

    private async Task HandleCreateUser()
    {
        isCreatingUser = true;
        createUserMessage = null;

        try
        {
            createUserModel.CreatedBy = UserState.CurrentUser?.Id ?? Guid.Empty;
            var result = await UserService.CreateUserAsync(createUserModel);

            if (result.IsSuccess)
            {
                createUserSuccess = true;
                createUserMessage = "User created successfully!";
                await Task.Delay(1500);
                showCreateUserDialog = false;
                await LoadUsers();
            }
            else
            {
                createUserSuccess = false;
                createUserMessage = result.Error ?? "Failed to create user";
            }
        }
        catch
        {
            createUserSuccess = false;
            createUserMessage = "An error occurred while creating the user";
        }
        finally
        {
            isCreatingUser = false;
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
        showEditUserDialog = true;
    }

    private void CloseEditUserDialog() => showEditUserDialog = false;

    private async Task HandleEditUser()
    {
        if (editingUser == null) return;

        isUpdatingUser = true;
        editUserMessage = null;

        try
        {
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

    // Manage Permissions Methods
    private void OpenManagePermissionsDialog(UserDto user)
    {
        managingPermissionsUser = user;
        permissionStates.Clear();

        foreach (var perm in user.DirectPermissions)
        {
            permissionStates[perm.Id] = true;
        }

        selectedModule = "all";
        FilterPermissionsByModule();
        permissionsMessage = null;
        showManagePermissionsDialog = true;
    }

    private void CloseManagePermissionsDialog() => showManagePermissionsDialog = false;

    private void FilterPermissionsByModule()
    {
        filteredPermissions = selectedModule == "all"
            ? availablePermissions
            : availablePermissions.Where(p => p.Module == selectedModule).ToList();
    }

    private void TogglePermission(Guid permissionId)
    {
        if (permissionStates.ContainsKey(permissionId))
            permissionStates.Remove(permissionId);
        else
            permissionStates[permissionId] = true;
    }

    private async Task SavePermissions()
    {
        if (managingPermissionsUser == null) return;

        isUpdatingPermissions = true;
        permissionsMessage = null;

        try
        {
            var currentPermissionIds = managingPermissionsUser.DirectPermissions.Select(p => p.Id).ToHashSet();
            var permissionsToGrant = permissionStates.Keys.Except(currentPermissionIds).ToList();
            var permissionsToRevoke = currentPermissionIds.Except(permissionStates.Keys).ToList();

            foreach (var permId in permissionsToGrant)
            {
                await UserService.GrantUserPermissionAsync(managingPermissionsUser.Id, permId);
            }

            foreach (var permId in permissionsToRevoke)
            {
                await UserService.RevokeUserPermissionAsync(managingPermissionsUser.Id, permId);
            }

            permissionsSuccess = true;
            permissionsMessage = "Permissions updated successfully!";
            await Task.Delay(1500);
            showManagePermissionsDialog = false;
            await LoadUsers();
        }
        catch
        {
            permissionsSuccess = false;
            permissionsMessage = "An error occurred while updating permissions";
        }
        finally
        {
            isUpdatingPermissions = false;
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
            var result = await UserService.ChangeUserPasswordAsync(changePasswordModel);

            if (result.IsSuccess)
            {
                changePasswordSuccess = true;
                changePasswordMessage = "Password changed successfully!";
                await Task.Delay(1500);
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