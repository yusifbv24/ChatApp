using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Modules.Channels.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.ChannelMembers
{
    public record AddMemberCommand(
        Guid ChannelId,
        Guid UserId,
        Guid AddedBy
    ) : IRequest<Result>;



    public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
    {
        public AddMemberCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.AddedBy)
                .NotEmpty().WithMessage("Added by user ID is required");
        }
    }



    public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<AddMemberCommandHandler> _logger;

        public AddMemberCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<AddMemberCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<Result> Handle(
            AddMemberCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Adding user {UserId} to channel {ChannelId}",
                    request.UserId,
                    request.ChannelId);

                var channel = await _unitOfWork.Channels.GetByIdWithMembersAsync(
                    request.ChannelId,
                    cancellationToken);

                if (channel == null)
                    throw new NotFoundException($"Channel with ID {request.ChannelId} not found");

                // Check if requester has permission (Admin or Owner for private channels)
                if (channel.Type == ChannelType.Private)
                {
                    var requesterRole = await _unitOfWork.ChannelMembers.GetUserRoleAsync(
                        request.ChannelId,
                        request.AddedBy,
                        cancellationToken);

                    if (requesterRole == null || (requesterRole != MemberRole.Admin && requesterRole != MemberRole.Owner))
                    {
                        return Result.Failure("You don't have permission to add members to this channel");
                    }
                }


                // Check if user is already a member
                var existingMember = await _unitOfWork.ChannelMembers.GetMemberAsync(
                    request.ChannelId,
                    request.UserId,
                    cancellationToken);

                if (existingMember != null && existingMember.IsActive)
                {
                    return Result.Failure("User is already a member of this channel");
                }

                // If member existed but left, reactivate
                if (existingMember != null && !existingMember.IsActive)
                {
                    existingMember.Rejoin();
                    await _unitOfWork.ChannelMembers.UpdateAsync(existingMember, cancellationToken);
                }
                else
                {
                    // Add new member
                    var newMember = new ChannelMember(request.ChannelId, request.UserId, MemberRole.Member);
                    await _unitOfWork.ChannelMembers.AddAsync(newMember, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish event
                await _eventBus.PublishAsync(
                    new MemberAddedEvent(request.ChannelId, request.UserId, request.AddedBy),
                    cancellationToken);

                _logger.LogInformation(
                    "User {UserId} added to channel {ChannelId} successfully",
                    request.UserId,
                    request.ChannelId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding user {UserId} to channel {ChannelId}",
                    request.UserId,
                    request.ChannelId);
                return Result.Failure(ex.Message);
            }
        }
    }
}