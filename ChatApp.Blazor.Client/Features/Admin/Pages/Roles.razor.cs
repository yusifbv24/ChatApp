using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Models.Auth;
using Microsoft.AspNetCore.Components;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class Roles
{
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IPermissionService PermissionService { get; set; } = default!;

    // Role list state
    private List<RoleDto> roles = [];
    private List<RoleDto> filteredRoles = [];
    private string searchTerm = "";
    private bool isLoading = true;
    private string? errorMessage;

    // Available permissions
    private List<PermissionDto> availablePermissions = [];

    // Create Role Dialog
    private bool showCreateRoleDialog = false;
    private CreateRoleRequest createRoleModel = new();
    private bool isCreatingRole = false;
    private string? createRoleMessage;
    private bool createRoleSuccess = false;

    // Edit Role Dialog
    private bool showEditRoleDialog = false;
    private RoleDto? editingRole;
    private UpdateRoleRequest editRoleModel = new();
    private bool isUpdatingRole = false;
    private string? editRoleMessage;
    private bool editRoleSuccess = false;

    // Manage Permissions Dialog
    private bool showManagePermissionsDialog = false;
    private RoleDto? managingPermissionsRole;
    private List<Guid> selectedPermissionIds = [];
    private string selectedModule = "all";
    private List<PermissionDto> filteredPermissions = [];
    private bool isUpdatingPermissions = false;
    private string? permissionsMessage;
    private bool permissionsSuccess = false;

    // Delete Role Dialog
    private bool showDeleteRoleDialog = false;
    private RoleDto? deletingRole;
    private bool isDeletingRole = false;
    private string? deleteRoleMessage;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadRoles(),
            LoadPermissions()
        );
    }

    private async Task LoadRoles()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var result = await RoleService.GetRolesAsync();

            if (result.IsSuccess && result.Value != null)
            {
                roles = result.Value;
                ApplyFilters();
            }
            else
            {
                errorMessage = result.Error ?? "Failed to load roles";
            }
        }
        catch
        {
            errorMessage = "An error occurred while loading roles";
        }
        finally
        {
            isLoading = false;
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
        filteredRoles = [.. roles.Where(r =>
            string.IsNullOrEmpty(searchTerm) ||
            r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            (r.Description != null && r.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        )];
    }

    private void HandleSearch() => ApplyFilters();

    private void ClearSearch()
    {
        searchTerm = "";
        ApplyFilters();
    }

    // Create Role Methods
    private void OpenCreateRoleDialog()
    {
        createRoleModel = new CreateRoleRequest();
        createRoleMessage = null;
        showCreateRoleDialog = true;
    }

    private void CloseCreateRoleDialog() => showCreateRoleDialog = false;

    private async Task HandleCreateRole()
    {
        isCreatingRole = true;
        createRoleMessage = null;

        try
        {
            var result = await RoleService.CreateRoleAsync(createRoleModel);

            if (result.IsSuccess)
            {
                createRoleSuccess = true;
                createRoleMessage = "Role created successfully!";
                StateHasChanged(); // Force UI update to show success message
                await Task.Delay(1500);
                showCreateRoleDialog = false;
                await LoadRoles();
            }
            else
            {
                createRoleSuccess = false;
                createRoleMessage = result.Error ?? "Failed to create role";
                StateHasChanged(); // Force UI update to show error message
            }
        }
        catch
        {
            createRoleSuccess = false;
            createRoleMessage = "An error occurred while creating the role";
            StateHasChanged(); // Force UI update to show error message
        }
        finally
        {
            isCreatingRole = false;
            StateHasChanged(); // Force UI update to re-enable button
        }
    }

    // Edit Role Methods
    private void OpenEditRoleDialog(RoleDto role)
    {
        editingRole = role;
        editRoleModel = new UpdateRoleRequest
        {
            Name = role.Name,
            Description = role.Description
        };
        editRoleMessage = null;
        showEditRoleDialog = true;
    }

    private void CloseEditRoleDialog() => showEditRoleDialog = false;

    private async Task HandleEditRole()
    {
        if (editingRole == null) return;

        isUpdatingRole = true;
        editRoleMessage = null;

        try
        {
            var result = await RoleService.UpdateRoleAsync(editingRole.Id, editRoleModel);

            if (result.IsSuccess)
            {
                editRoleSuccess = true;
                editRoleMessage = "Role updated successfully!";
                StateHasChanged(); // Force UI update to show success message
                await Task.Delay(1500);
                showEditRoleDialog = false;
                await LoadRoles();
            }
            else
            {
                editRoleSuccess = false;
                editRoleMessage = result.Error ?? "Failed to update role";
                StateHasChanged(); // Force UI update to show error message
            }
        }
        catch
        {
            editRoleSuccess = false;
            editRoleMessage = "An error occurred while updating the role";
            StateHasChanged(); // Force UI update to show error message
        }
        finally
        {
            isUpdatingRole = false;
            StateHasChanged(); // Force UI update to re-enable button
        }
    }

    // Manage Permissions Methods
    private void OpenManagePermissionsDialog(RoleDto role)
    {
        managingPermissionsRole = role;
        selectedPermissionIds = [.. role.Permissions.Select(p => p.Id)];
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
            : [.. availablePermissions.Where(p => p.Module == selectedModule)];
    }

    private void TogglePermission(Guid permissionId)
    {
        if (!selectedPermissionIds.Remove(permissionId))
            selectedPermissionIds.Add(permissionId);
    }

    private async Task SavePermissions()
    {
        if (managingPermissionsRole == null) return;

        isUpdatingPermissions = true;
        permissionsMessage = null;
        StateHasChanged(); // Immediate UI feedback

        try
        {
            // Capture current state to avoid potential race conditions
            var roleId = managingPermissionsRole.Id;
            var currentPermissionIds = managingPermissionsRole.Permissions.Select(p => p.Id).ToHashSet();
            var selectedIds = selectedPermissionIds?.ToList() ?? [];

            var permissionsToAdd = selectedIds.Except(currentPermissionIds).ToList();
            var permissionsToRemove = currentPermissionIds.Except(selectedIds).ToList();

            // Use Task.WhenAll for parallel execution
            var addTasks = permissionsToAdd.Select(permId =>
                PermissionService.AssignPermissionToRoleAsync(roleId, permId));
            var removeTasks = permissionsToRemove.Select(permId =>
                PermissionService.RemovePermissionFromRoleAsync(roleId, permId));

            await Task.WhenAll(addTasks.Concat(removeTasks));

            permissionsSuccess = true;
            permissionsMessage = $"Permissions updated successfully! " +
                               $"(Added: {permissionsToAdd.Count}, Removed: {permissionsToRemove.Count})";

            StateHasChanged();

            // Optional: Auto-close after delay only if successful
            await Task.Delay(2000); // Slightly longer for better UX
            showManagePermissionsDialog = false;

            await LoadRoles(); // Refresh the data
        }
        catch (Exception ex)
        {
            permissionsSuccess = false;
            permissionsMessage = $"Failed to update permissions: {GetUserFriendlyErrorMessage(ex)}";
        }
        finally
        {
            isUpdatingPermissions = false;
            StateHasChanged();
        }
    }

    // Helper method for user-friendly error messages
    private string GetUserFriendlyErrorMessage(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "Network error occurred. Please check your connection.",
            TimeoutException => "Request timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    // Delete Role Methods
    private void OpenDeleteRoleDialog(RoleDto role)
    {
        deletingRole = role;
        deleteRoleMessage = null;
        showDeleteRoleDialog = true;
    }

    private void CloseDeleteRoleDialog() => showDeleteRoleDialog = false;

    private async Task HandleDeleteRole()
    {
        if (deletingRole == null) return;

        isDeletingRole = true;
        deleteRoleMessage = null;

        try
        {
            var result = await RoleService.DeleteRoleAsync(deletingRole.Id);

            if (result.IsSuccess)
            {
                showDeleteRoleDialog = false;
                await LoadRoles();
            }
            else
            {
                deleteRoleMessage = result.Error ?? "Failed to delete role";
                StateHasChanged(); // Force UI update to show error message
            }
        }
        catch
        {
            deleteRoleMessage = "An error occurred while deleting the role";
            StateHasChanged(); // Force UI update to show error message
        }
        finally
        {
            isDeletingRole = false;
            StateHasChanged(); // Force UI update to re-enable button
        }
    }
}
