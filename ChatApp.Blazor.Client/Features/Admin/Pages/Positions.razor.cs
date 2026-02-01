using ChatApp.Blazor.Client.Features.Admin.Services;
using ChatApp.Blazor.Client.Models.Organization;
using Microsoft.AspNetCore.Components;

namespace ChatApp.Blazor.Client.Features.Admin.Pages;

public partial class Positions
{
    // Data
    private List<PositionDto> allPositions = [];
    private List<DepartmentDto> departments = [];
    private List<PositionGroup> filteredGroups = [];

    // State
    private bool isLoading = true;
    private string? errorMessage;
    private string searchTerm = "";
    private string? _lastSearchTerm;

    // Create dialog
    private bool showCreateDialog;
    private CreatePositionRequest createModel = new();
    private string createDepartmentId = "";

    // Edit dialog
    private bool showEditDialog;
    private UpdatePositionRequest editModel = new();
    private string editDepartmentId = "";
    private Guid editPositionId;

    // Delete dialog
    private bool showDeleteDialog;
    private PositionDto? deleteTarget;

    // Shared dialog state
    private bool isSaving;
    private string? dialogMessage;
    private bool dialogSuccess;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (_lastSearchTerm != searchTerm)
        {
            _lastSearchTerm = searchTerm;
            BuildFilteredGroups();
            StateHasChanged();
        }
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var posResult = await PositionService.GetAllPositionsAsync();
            var deptResult = await DepartmentService.GetAllDepartmentsAsync();

            if (!posResult.IsSuccess)
            {
                errorMessage = posResult.Error ?? "Failed to load positions.";
                return;
            }

            allPositions = posResult.Value ?? [];
            departments = deptResult.IsSuccess ? (deptResult.Value ?? []) : [];
            departments = departments.OrderBy(d => d.Name).ToList();

            BuildFilteredGroups();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void BuildFilteredGroups()
    {
        var filtered = string.IsNullOrWhiteSpace(searchTerm)
            ? allPositions
            : allPositions.Where(p =>
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.DepartmentName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

        filteredGroups = filtered
            .GroupBy(p => new { p.DepartmentId, DeptName = p.DepartmentName ?? "No Department" })
            .OrderBy(g => g.Key.DeptName == "No Department" ? 1 : 0)
            .ThenBy(g => g.Key.DeptName)
            .Select(g => new PositionGroup
            {
                DepartmentName = g.Key.DeptName,
                Positions = g.OrderBy(p => p.Name).ToList()
            })
            .ToList();
    }

    // === Create ===
    private void OpenCreateDialog()
    {
        createModel = new CreatePositionRequest();
        createDepartmentId = "";
        dialogMessage = null;
        showCreateDialog = true;
    }

    private void CloseCreateDialog()
    {
        showCreateDialog = false;
        dialogMessage = null;
    }

    private async Task HandleCreate()
    {
        isSaving = true;
        dialogMessage = null;

        try
        {
            if (Guid.TryParse(createDepartmentId, out var deptId) && deptId != Guid.Empty)
                createModel.DepartmentId = deptId;
            else
                createModel.DepartmentId = null;

            var result = await PositionService.CreatePositionAsync(createModel);

            if (result.IsSuccess)
            {
                dialogSuccess = true;
                dialogMessage = "Position created successfully.";
                await LoadDataAsync();
                StateHasChanged();
                await Task.Delay(800);
                CloseCreateDialog();
            }
            else
            {
                dialogSuccess = false;
                dialogMessage = result.Error ?? "Failed to create position.";
            }
        }
        catch (Exception ex)
        {
            dialogSuccess = false;
            dialogMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    // === Edit ===
    private void OpenEditDialog(PositionDto position)
    {
        editPositionId = position.Id;
        editModel = new UpdatePositionRequest
        {
            Name = position.Name,
            Description = position.Description,
            DepartmentId = position.DepartmentId
        };
        editDepartmentId = position.DepartmentId?.ToString() ?? "";
        dialogMessage = null;
        showEditDialog = true;
    }

    private void CloseEditDialog()
    {
        showEditDialog = false;
        dialogMessage = null;
    }

    private async Task HandleEdit()
    {
        isSaving = true;
        dialogMessage = null;

        try
        {
            if (Guid.TryParse(editDepartmentId, out var deptId) && deptId != Guid.Empty)
                editModel.DepartmentId = deptId;
            else
                editModel.DepartmentId = null;

            var result = await PositionService.UpdatePositionAsync(editPositionId, editModel);

            if (result.IsSuccess)
            {
                dialogSuccess = true;
                dialogMessage = "Position updated successfully.";
                await LoadDataAsync();
                StateHasChanged();
                await Task.Delay(800);
                CloseEditDialog();
            }
            else
            {
                dialogSuccess = false;
                dialogMessage = result.Error ?? "Failed to update position.";
            }
        }
        catch (Exception ex)
        {
            dialogSuccess = false;
            dialogMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    // === Delete ===
    private void OpenDeleteDialog(PositionDto position)
    {
        deleteTarget = position;
        dialogMessage = null;
        showDeleteDialog = true;
    }

    private void CloseDeleteDialog()
    {
        showDeleteDialog = false;
        dialogMessage = null;
    }

    private async Task HandleDelete()
    {
        if (deleteTarget == null) return;

        isSaving = true;
        dialogMessage = null;

        try
        {
            var result = await PositionService.DeletePositionAsync(deleteTarget.Id);

            if (result.IsSuccess)
            {
                showDeleteDialog = false;
                await LoadDataAsync();
            }
            else
            {
                dialogSuccess = false;
                dialogMessage = result.Error ?? "Failed to delete position.";
            }
        }
        catch (Exception ex)
        {
            dialogSuccess = false;
            dialogMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void NavigateBack() => Navigation.NavigateTo("/admin/organization");

    // Helper class
    private class PositionGroup
    {
        public string DepartmentName { get; set; } = "";
        public List<PositionDto> Positions { get; set; } = [];
    }
}
