using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Settings.Application.Commands.UpdateDisplaySettings
{
    public record UpdateDisplaySettingsCommand(
        Guid UserId,
        string Theme,
        string Language,
        int MessagePageSize
    ) : IRequest<Result>;

    public class UpdateDisplaySettingsCommandValidator : AbstractValidator<UpdateDisplaySettingsCommand>
    {
        public UpdateDisplaySettingsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Theme)
                .NotEmpty().WithMessage("Theme is required")
                .Must(t => t == "light" || t == "dark")
                .WithMessage("Theme must be 'light' or 'dark'");

            RuleFor(x => x.Language)
                .NotEmpty().WithMessage("Language is required")
                .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters");

            RuleFor(x => x.MessagePageSize)
                .InclusiveBetween(10, 100).WithMessage("Message page size must be between 10 and 100");
        }
    }

    public class UpdateDisplaySettingsCommandHandler : IRequestHandler<UpdateDisplaySettingsCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateDisplaySettingsCommandHandler> _logger;

        public UpdateDisplaySettingsCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateDisplaySettingsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UpdateDisplaySettingsCommand request,
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

                settings.UpdateDisplaySettings(
                    request.Theme,
                    request.Language,
                    request.MessagePageSize);

                await _unitOfWork.UserSettings.UpdateAsync(settings, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Display settings updated for user {UserId}", request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating display settings");
                return Result.Failure(ex.Message);
            }
        }
    }
}