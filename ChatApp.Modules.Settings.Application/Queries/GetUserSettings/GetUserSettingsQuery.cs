using ChatApp.Modules.Settings.Application.DTOs;
using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Settings.Application.Queries.GetUserSettings
{
    public record GetUserSettingsQuery(Guid UserId) : IRequest<Result<UserSettingsDto>>;

    public class GetUserSettingsQueryHandler : IRequestHandler<GetUserSettingsQuery, Result<UserSettingsDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUserSettingsQueryHandler> _logger;

        public GetUserSettingsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUserSettingsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserSettingsDto>> Handle(
            GetUserSettingsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _unitOfWork.UserSettings.GetByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (settings == null)
                {
                    settings = new UserSettings(request.UserId);
                    await _unitOfWork.UserSettings.AddAsync(settings, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Created default settings for user {UserId}", request.UserId);
                }

                var dto = new UserSettingsDto(
                    settings.UserId,
                    settings.EmailNotificationsEnabled,
                    settings.PushNotificationsEnabled,
                    settings.NotifyOnChannelMessage,
                    settings.NotifyOnDirectMessage,
                    settings.NotifyOnMention,
                    settings.NotifyOnReaction,
                    settings.ShowOnlineStatus,
                    settings.ShowLastSeen,
                    settings.ShowReadReceipts,
                    settings.AllowDirectMessages,
                    settings.Theme,
                    settings.Language,
                    settings.MessagePageSize,
                    settings.UpdatedAtUtc);

                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user settings");
                return Result.Failure<UserSettingsDto>("An error occurred while retrieving settings");
            }
        }
    }
}