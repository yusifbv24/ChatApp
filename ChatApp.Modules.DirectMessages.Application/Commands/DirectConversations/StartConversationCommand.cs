using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Shared.Kernel.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Commands.DirectConversations
{
    public record StartConversationCommand
        (
        Guid User1Id,
        Guid User2Id
    ):IRequest<Result<Guid>>;


    public class StartConversationValidator : AbstractValidator<StartConversationCommand>
    {
        public StartConversationValidator()
        {
            RuleFor(x => x.User1Id)
                .NotEmpty().WithMessage("User 1 ID is required");

            RuleFor(x => x.User2Id)
                .NotEmpty().WithMessage("User 2 ID is required");

            RuleFor(x => x)
                .Must(x => x.User1Id != x.User2Id)
                .WithMessage("Cannot start conversation with yourself");
        }
    }


    public class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StartConversationCommandHandler> _logger;

        public StartConversationCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<StartConversationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger=logger;
        }


        public async Task<Result<Guid>> Handle(
            StartConversationCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Starting conversation between {User1Id} and {User2Id}",
                    request.User1Id,
                    request.User2Id);

                // Check if conversation already exists
                var existingConversation = await _unitOfWork.Conversations.GetByParticipantsAsync(
                    request.User1Id,
                    request.User2Id,
                    cancellationToken);

                if (existingConversation != null)
                {
                    _logger?.LogInformation("Conversation already exists: {ConversationId}", existingConversation.Id);
                    return Result.Success(existingConversation.Id);
                }

                // Create new conversation - User1Id is the initiator
                var conversation = new DirectConversation(request.User1Id, request.User2Id, request.User1Id);

                await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation("Conversation created succesfully: {ConversationId}", conversation.Id);
                return Result.Success(conversation.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting conversation");
                return Result.Failure<Guid>("An error occurred while starting the conversation");
            }
        }
    }
}