using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.MessageConditions
{
    public record PinMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class PinMessageCommandValidator : AbstractValidator<PinMessageCommand>
    {
        public PinMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class PinMessageCommandHandler : IRequestHandler<PinMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PinMessageCommandHandler> _logger;

        public PinMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<PinMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            PinMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Pinning message {MessageId}", request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Only admin/owner can pin messages
                var userRole = await _unitOfWork.ChannelMembers.GetUserRoleAsync(
                    message.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (userRole != MemberRole.Admin && userRole != MemberRole.Owner)
                {
                    return Result.Failure("Only admins and owners can pin messages");
                }

                if (message.IsPinned)
                {
                    return Result.Failure("Message is already pinned");
                }

                message.Pin(request.RequestedBy);

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Message {MessageId} pinned successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinning message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}