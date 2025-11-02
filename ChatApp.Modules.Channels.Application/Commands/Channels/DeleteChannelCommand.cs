using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.Channels
{
    public record DeleteChannelCommand(
        Guid ChannelId,
        Guid RequestedBy
    ) : IRequest<Result>;



    public class DeleteChannelCommandValidator : AbstractValidator<DeleteChannelCommand>
    {
        public DeleteChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }



    public class DeleteChannelCommandHandler : IRequestHandler<DeleteChannelCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteChannelCommandHandler> _logger;

        public DeleteChannelCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteChannelCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteChannelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Deleting channel: {ChannelId}", request.ChannelId);

                var channel = await _unitOfWork.Channels.GetByIdAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                    throw new NotFoundException($"Channel with ID {request.ChannelId} not found");

                // Only owner can delete
                var userRole = await _unitOfWork.ChannelMembers.GetUserRoleAsync(
                    request.ChannelId,
                    request.RequestedBy,
                    cancellationToken);

                if (userRole != MemberRole.Owner)
                {
                    return Result.Failure("Only the channel owner can delete the channel");
                }

                // Soft delete - archive the channel
                channel.Archive();

                await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Channel {ChannelId} deleted successfully", request.ChannelId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting channel {ChannelId}", request.ChannelId);
                return Result.Failure(ex.Message);
            }
        }
    }
}