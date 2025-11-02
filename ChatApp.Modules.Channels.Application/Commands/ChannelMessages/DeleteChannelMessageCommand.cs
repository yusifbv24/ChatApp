using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMessages
{
    public record DeleteChannelMessageCommand(
        Guid MessageId,
        Guid RequestedBy
    ) : IRequest<Result>;



    public class DeleteChannelMessageCommandValidator : AbstractValidator<DeleteChannelMessageCommand>
    {
        public DeleteChannelMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("Message ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }



    public class DeleteChannelMessageCommandHandler : IRequestHandler<DeleteChannelMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteChannelMessageCommandHandler> _logger;

        public DeleteChannelMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteChannelMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteChannelMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting message {MessageId}", request.MessageId);

                var message = await _unitOfWork.ChannelMessages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if (message == null)
                    throw new NotFoundException($"Message with ID {request.MessageId} not found");

                // User can delete their own message, or admin/owner can delete any message
                bool canDelete = message.SenderId == request.RequestedBy;

                if (!canDelete)
                {
                    var userRole = await _unitOfWork.ChannelMembers.GetUserRoleAsync(
                        message.ChannelId,
                        request.RequestedBy,
                        cancellationToken);

                    canDelete = userRole == MemberRole.Admin || userRole == MemberRole.Owner;
                }

                if (!canDelete)
                {
                    return Result.Failure("You don't have permission to delete this message");
                }

                message.Delete();

                await _unitOfWork.ChannelMessages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Message {MessageId} deleted successfully", request.MessageId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", request.MessageId);
                return Result.Failure(ex.Message);
            }
        }
    }
}