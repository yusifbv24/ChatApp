namespace ChatApp.Blazor.Client.Models.Messages;

/// <summary>
/// Department colleague for conversation sidebar listing.
/// </summary>
public record DepartmentUserDto(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? PositionName,
    Guid? DepartmentId,
    string? DepartmentName
);