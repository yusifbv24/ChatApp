using ChatApp.Modules.Channels.Application.Interfaces;
using ChatApp.Modules.Channels.Domain.Entities;
using ChatApp.Modules.Channels.Domain.Enums;
using ChatApp.Modules.Channels.Domain.Events;
using ChatApp.Modules.Channels.Domain.ValueObjects;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Channels.Application.Commands.Channels
{
    public record CreateChannelCommand(
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy
    ) : IRequest<Result<Guid>>;



    public class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
    {
        public CreateChannelCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Channel name is required")
                .MaximumLength(100).WithMessage("Channel name must not exceed 100 characters");

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
            });

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid channel type");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("Creator ID is required");
        }
    }



    public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;
        private readonly ILogger<CreateChannelCommandHandler> _logger;

        public CreateChannelCommandHandler(
            IUnitOfWork unitOfWork,
            IEventBus eventBus,
            ILogger<CreateChannelCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateChannelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Creating channel: {ChannelName}", request.Name);
                var channelName = ChannelName.Create(request.Name);

                // Check if channel name already exists
                var existingChannel = await _unitOfWork.Channels.GetByNameAsync(
                    channelName,
                    cancellationToken);

                if (existingChannel != null)
                {
                    _logger?.LogWarning("Channel name {ChannelName} already exists", request.Name);
                    return Result.Failure<Guid>("A channel with this name already exists");
                }

                var channel = new Channel(
                    channelName,
                    request.Description,
                    request.Type,
                    request.CreatedBy);

                await _unitOfWork.Channels.AddAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish event
                await _eventBus.PublishAsync(
                    new ChannelCreatedEvent(channel.Id, channelName, request.CreatedBy),
                    cancellationToken);

                _logger?.LogInformation(
                    "Channel {ChannelName} created successfully with ID {ChannelId}",
                    channelName,
                    channel.Id);

                return Result.Success(channel.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating channel {ChannelName}", request.Name);
                return Result.Failure<Guid>("An error occurred while creating the channel");
            }
        }
    }
}