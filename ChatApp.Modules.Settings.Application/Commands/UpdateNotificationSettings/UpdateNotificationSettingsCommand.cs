using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Settings.Application.Commands.UpdateNotificationSettings
{
    public record UpdateNotificationSettingsCommand(
        Guid UserId,
        bool EmailNotificationsEnabled,
        bool PushNotificationsEnabled,
        bool NotifyOnChannelMessage,
        bool NotifyOnDirectMessage,
        bool NotifyOnMention,
        bool NotifyOnReaction
    ) : IRequest<Result>;

    public class UpdateNotificationSettingsCommandValidator : AbstractValidator<UpdateNotificationSettingsCommand>
    {
        public UpdateNotificationSettingsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class UpdateNotificationSettingsCommandHandler : IRequestHandler<UpdateNotificationSettingsCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateNotificationSettingsCommandHandler> _logger;

        public UpdateNotificationSettingsCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateNotificationSettingsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UpdateNotificationSettingsCommand request,
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
                }

                settings.UpdateNotificationSettings(
                    request.EmailNotificationsEnabled,
                    request.PushNotificationsEnabled,
                    request.NotifyOnChannelMessage,
                    request.NotifyOnDirectMessage,
                    request.NotifyOnMention,
                    request.NotifyOnReaction);

                await _unitOfWork.UserSettings.UpdateAsync(settings, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification settings updated for user {UserId}", request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings");
                return Result.Failure("An error occurred while updating notification settings");
            }
        }
    }
}