using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Settings.Application.Commands.UpdatePrivacySettings
{
    public record UpdatePrivacySettingsCommand(
        Guid UserId,
        bool ShowOnlineStatus,
        bool ShowLastSeen,
        bool ShowReadReceipts,
        bool AllowDirectMessages
    ) : IRequest<Result>;

    public class UpdatePrivacySettingsCommandValidator : AbstractValidator<UpdatePrivacySettingsCommand>
    {
        public UpdatePrivacySettingsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }

    public class UpdatePrivacySettingsCommandHandler : IRequestHandler<UpdatePrivacySettingsCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdatePrivacySettingsCommandHandler> _logger;

        public UpdatePrivacySettingsCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdatePrivacySettingsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UpdatePrivacySettingsCommand request,
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

                settings.UpdatePrivacySettings(
                    request.ShowOnlineStatus,
                    request.ShowLastSeen,
                    request.ShowReadReceipts,
                    request.AllowDirectMessages);

                await _unitOfWork.UserSettings.UpdateAsync(settings, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Privacy settings updated for user {UserId}", request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings");
                return Result.Failure("An error occurred while updating privacy settings");
            }
        }
    }
}