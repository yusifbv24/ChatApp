using ChatApp.Modules.DirectMessages.Application.Interfaces;
using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.Identity.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.DirectMessages.Application.Events
{
    /// <summary>
    /// Creates Notes conversation (self-conversation) when user is created
    /// </summary>
    public class UserCreatedEventHandler(
        IDirectConversationRepository conversationRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserCreatedEventHandler> logger)
    {
        public async Task HandleAsync(UserCreatedEvent @event)
        {
            try
            {
                logger.LogInformation("Creating Notes conversation for user {UserId}", @event.UserId);

                // Create Notes conversation (self-conversation)
                var notesConversation = new DirectConversation(
                    user1Id: @event.UserId,
                    user2Id: @event.UserId,
                    initiatedByUserId: @event.UserId,
                    isNotes: true
                );

                await conversationRepository.AddAsync(notesConversation);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Notes conversation created successfully for user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Notes conversation for user {UserId}", @event.UserId);
                // Don't throw - this is a non-critical operation
            }
        }
    }
}