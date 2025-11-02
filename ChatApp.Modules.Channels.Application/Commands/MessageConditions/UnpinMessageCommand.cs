using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.MessageConditions
{
    public record UnpinMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;

    public class UnpinMessageCommandValidator : AbstractValidator<UnpinMessageCommand>
    {
        public UnpinMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }

    public class UnpinMessageCommandHandler : IRequestHandler<UnpinMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UnpinMessageCommandHandler> _logger;

        public UnpinMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UnpinMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            UnpinMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Unpinning message {MessageId}", request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // Only admin/owner can unpin messages
                var userRole = await _unitOfWork.ChannelMembers.GetUserRoleAsync(
                    message.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (userRole != MemberRole.Admin && userRole != MemberRole.Owner)
                {
                    return Result.Failure("Only admins and owners can unpin messages");
                }

                if (!message.IsPinned)
                {
                    return Result.Failure("Message is not pinned");
                }

                message.Unpin();

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Message {MessageId} unpinned successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpinning message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}