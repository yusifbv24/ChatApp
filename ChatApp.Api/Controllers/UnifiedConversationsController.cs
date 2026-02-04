using ChatApp.Modules.Channels.Application.DTOs.Responses;
using ChatApp.Modules.Channels.Application.Queries.GetUserChannels;
using ChatApp.Modules.DirectMessages.Application.DTOs.Response;
using ChatApp.Modules.DirectMessages.Application.Queries;
using ChatApp.Modules.Identity.Application.DTOs.Responses;
using ChatApp.Modules.Identity.Application.Queries.GetUsers;
using ChatApp.Shared.Infrastructure.Authorization;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers;

/// <summary>
/// BFF orchestrator: merges DM conversations, channels, and department users
/// into a single paginated list with proper priority ordering.
/// </summary>
[ApiController]
[Route("api/unified-conversations")]
[Authorize]
public class UnifiedConversationsController(IMediator mediator) : ControllerBase
{
    private const int MaxPageSize = 50;

    /// <summary>
    /// Gets unified conversation list.
    /// Page 1: Notes → Pinned → Active conversations/channels → Department users (fill remaining slots).
    /// Page 2+: Only department users (conversations/channels are fully loaded on page 1).
    /// </summary>
    [HttpGet]
    [RequirePermission("Messages.Read")]
    [ProducesResponseType(typeof(UnifiedConversationListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnifiedConversationList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        pageSize = Math.Min(pageSize, MaxPageSize);

        if (pageNumber == 1)
        {
            return await GetFirstPage(userId, pageSize, cancellationToken);
        }
        else
        {
            return await GetSubsequentPage(userId, pageNumber, pageSize, cancellationToken);
        }
    }

    /// <summary>
    /// Page 1: Conversations + Channels + Department users (fill remaining slots).
    /// Conversations və channels paralel yüklənir, sonra merge+sort edilir.
    /// Qalan slotlar department users ilə doldurulur.
    /// </summary>
    private async Task<IActionResult> GetFirstPage(Guid userId, int pageSize, CancellationToken cancellationToken)
    {
        // STEP 1: Load ALL conversations and channels in parallel (typically <50 items each)
        var conversationsTask = mediator.Send(
            new GetConversationsPagedQuery(userId, 1, 100), cancellationToken);
        var channelsTask = mediator.Send(
            new GetUserChannelsPagedQuery(userId, 1, 100), cancellationToken);

        await Task.WhenAll(conversationsTask, channelsTask);

        var conversationsResult = await conversationsTask;
        var channelsResult = await channelsTask;

        var allConversations = conversationsResult.IsSuccess ? conversationsResult.Value?.Items ?? [] : [];
        var allChannels = channelsResult.IsSuccess ? channelsResult.Value?.Items ?? [] : [];

        // STEP 2: Merge and sort by priority
        var mergedItems = new List<UnifiedChatItemDto>();

        // Priority 1: Notes conversation (always first)
        mergedItems.AddRange(allConversations.Where(c => c.IsNotes).Select(MapConversation));

        // Priority 2+3: Pinned then active — interleave conversations and channels by LastMessageAtUtc
        var nonNotes = allConversations
            .Where(c => !c.IsNotes)
            .Select(c => (Item: MapConversation(c), Time: c.LastMessageAtUtc, c.IsPinned));

        var channelItems = allChannels
            .Select(c => (Item: MapChannel(c), Time: c.LastMessageAtUtc ?? DateTime.MinValue, c.IsPinned));

        mergedItems.AddRange(nonNotes.Concat(channelItems)
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.Time)
            .Select(x => x.Item));

        var totalConvAndChannels = mergedItems.Count;

        // STEP 3: Fill remaining slots with department users
        var deptTake = Math.Max(0, pageSize - totalConvAndChannels);

        var conversationUserIds = allConversations
            .Where(c => !c.IsNotes)
            .Select(c => c.OtherUserId)
            .ToList();

        int totalDepartmentUsers = 0;
        var departmentUsers = new List<DepartmentUserDto>();

        // Həmişə department users query göndər (hətta deptTake==0 olsa belə totalCount lazımdır)
        var deptResult = await mediator.Send(
            new GetDepartmentUsersQuery(userId, 1, Math.Max(deptTake, 1), null, conversationUserIds),
            cancellationToken);

        if (deptResult.IsSuccess && deptResult.Value != null)
        {
            totalDepartmentUsers = deptResult.Value.TotalCount;
            if (deptTake > 0)
                departmentUsers = deptResult.Value.Items;
        }

        // STEP 4: Build response
        var pageItems = mergedItems.Take(pageSize).ToList();
        pageItems.AddRange(departmentUsers.Select(MapDepartmentUser));

        var hasNextPage = (totalConvAndChannels + totalDepartmentUsers) > pageSize;

        return Ok(new UnifiedConversationListResponse(
            Items: pageItems,
            PageNumber: 1,
            PageSize: pageSize,
            TotalConversations: allConversations.Count,
            TotalChannels: allChannels.Count,
            TotalDepartmentUsers: totalDepartmentUsers,
            HasNextPage: hasNextPage
        ));
    }

    /// <summary>
    /// Page 2+: Yalnız department users (conversations/channels artıq page 1-də tam yüklənib).
    /// Conversations/channels yenidən yüklənmir — yalnız ExcludeUserIds üçün lazımdır.
    /// </summary>
    private async Task<IActionResult> GetSubsequentPage(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        // Conversations + Channels paralel yüklə (ExcludeUserIds + totalConvAndChannels hesabı üçün)
        var conversationsTask = mediator.Send(
            new GetConversationsPagedQuery(userId, 1, 100), cancellationToken);
        var channelsTask = mediator.Send(
            new GetUserChannelsPagedQuery(userId, 1, 100), cancellationToken);

        await Task.WhenAll(conversationsTask, channelsTask);

        var conversationsResult = await conversationsTask;
        var channelsResult = await channelsTask;

        var allConversations = conversationsResult.IsSuccess ? conversationsResult.Value?.Items ?? [] : [];
        var allChannels = channelsResult.IsSuccess ? channelsResult.Value?.Items ?? [] : [];
        var totalConvAndChannels = allConversations.Count + allChannels.Count;

        var conversationUserIds = allConversations
            .Where(c => !c.IsNotes)
            .Select(c => c.OtherUserId)
            .ToList();

        // Page 1-dəki department user sayını hesabla
        // Məsələn: pageSize=20, totalConvAndChannels=2 → page1-də 18 dept user var
        var deptUsersOnPage1 = Math.Max(0, pageSize - totalConvAndChannels);

        // Page 2+ üçün department users offset:
        // Page 2: skip = 18 (page1-dəkilər)
        // Page 3: skip = 18 + 20 = 38
        // Page N: skip = deptUsersOnPage1 + (pageNumber - 2) * pageSize
        var deptSkip = deptUsersOnPage1 + (pageNumber - 2) * pageSize;

        // SkipOverride ilə birbaşa skip göndəririk
        var deptResult = await mediator.Send(
            new GetDepartmentUsersQuery(userId, 1, pageSize, null, conversationUserIds, deptSkip),
            cancellationToken);

        int totalDepartmentUsers = 0;
        List<UnifiedChatItemDto> pageItems = [];

        if (deptResult.IsSuccess && deptResult.Value != null)
        {
            totalDepartmentUsers = deptResult.Value.TotalCount;
            pageItems = deptResult.Value.Items.Select(MapDepartmentUser).ToList();
        }

        // HasNextPage: daha çox department user varmı?
        var totalShownSoFar = deptSkip + pageItems.Count;
        var hasNextPage = totalShownSoFar < totalDepartmentUsers;

        return Ok(new UnifiedConversationListResponse(
            Items: pageItems,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalConversations: allConversations.Count,
            TotalChannels: allChannels.Count,
            TotalDepartmentUsers: totalDepartmentUsers,
            HasNextPage: hasNextPage
        ));
    }

    #region Mappers

    private static UnifiedChatItemDto MapConversation(DirectConversationDto c) => new(
        Id: c.Id,
        Type: UnifiedChatItemType.Conversation,
        Name: c.IsNotes ? "Notes" : c.OtherUserFullName,
        AvatarUrl: c.OtherUserAvatarUrl,
        LastMessage: c.LastMessageContent,
        LastMessageAtUtc: c.LastMessageAtUtc,
        UnreadCount: c.UnreadCount,
        HasUnreadMentions: c.HasUnreadMentions,
        IsPinned: c.IsPinned,
        IsMuted: c.IsMuted,
        IsMarkedReadLater: c.IsMarkedReadLater,
        IsNotes: c.IsNotes,
        LastReadLaterMessageId: c.LastReadLaterMessageId,
        FirstUnreadMessageId: c.FirstUnreadMessageId,
        LastMessageSenderId: c.LastMessageSenderId,
        LastMessageStatus: c.LastMessageStatus,
        LastMessageId: c.LastMessageId,
        OtherUserId: c.OtherUserId,
        OtherUserEmail: c.OtherUserEmail,
        MemberCount: null,
        ChannelType: null,
        LastMessageSenderAvatarUrl: null,
        CreatedBy: null,
        ChannelDescription: null,
        CreatedAtUtc: null,
        Email: null,
        PositionName: null,
        DepartmentName: null
    );

    private static UnifiedChatItemDto MapChannel(ChannelDto c) => new(
        Id: c.Id,
        Type: UnifiedChatItemType.Channel,
        Name: c.Name,
        AvatarUrl: c.AvatarUrl,
        LastMessage: c.LastMessageContent,
        LastMessageAtUtc: c.LastMessageAtUtc,
        UnreadCount: c.UnreadCount,
        HasUnreadMentions: c.HasUnreadMentions,
        IsPinned: c.IsPinned,
        IsMuted: c.IsMuted,
        IsMarkedReadLater: c.IsMarkedReadLater,
        IsNotes: false,
        LastReadLaterMessageId: c.LastReadLaterMessageId,
        FirstUnreadMessageId: c.FirstUnreadMessageId,
        LastMessageSenderId: c.LastMessageSenderId,
        LastMessageStatus: c.LastMessageStatus,
        LastMessageId: c.LastMessageId,
        OtherUserId: null,
        OtherUserEmail: null,
        MemberCount: c.MemberCount,
        ChannelType: c.Type.ToString(),
        LastMessageSenderAvatarUrl: c.LastMessageSenderAvatarUrl,
        CreatedBy: c.CreatedBy,
        ChannelDescription: c.Description,
        CreatedAtUtc: c.CreatedAtUtc,
        Email: null,
        PositionName: null,
        DepartmentName: null
    );

    private static UnifiedChatItemDto MapDepartmentUser(DepartmentUserDto u) => new(
        Id: u.UserId,
        Type: UnifiedChatItemType.DepartmentUser,
        Name: u.FullName,
        AvatarUrl: u.AvatarUrl,
        LastMessage: null,
        LastMessageAtUtc: null,
        UnreadCount: 0,
        HasUnreadMentions: false,
        IsPinned: false,
        IsMuted: false,
        IsMarkedReadLater: false,
        IsNotes: false,
        LastReadLaterMessageId: null,
        FirstUnreadMessageId: null,
        LastMessageSenderId: null,
        LastMessageStatus: null,
        LastMessageId: null,
        OtherUserId: u.UserId,
        OtherUserEmail: u.Email,
        MemberCount: null,
        ChannelType: null,
        LastMessageSenderAvatarUrl: null,
        CreatedBy: null,
        ChannelDescription: null,
        CreatedAtUtc: null,
        Email: u.Email,
        PositionName: u.PositionName,
        DepartmentName: u.DepartmentName
    );

    #endregion

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Guid.Empty;
        return userId;
    }
}