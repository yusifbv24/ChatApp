using ChatApp.Modules.DirectMessages.Domain.Entities;
using ChatApp.Modules.DirectMessages.Infrastructure.Persistence;
using ChatApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Seeder
{
    public static class DefaultSeeder
    {
        public static async Task CreateConversationForDefaultUsers(
			IdentityDbContext contextID, 
			DirectMessagesDbContext contextDM,
			ILogger logger)
        {
			try
			{
				if (!await contextID.Users.AnyAsync())
				{
					return;
				}
				if (!await contextDM.DirectConversations.AnyAsync())
				{
                    var users = await contextID.Users.ToListAsync();
					foreach (var user in users)
					{
						var hasNotesConversation = await contextDM.DirectConversations
							.AnyAsync(c => c.IsNotes && c.User1Id == user.Id && c.User2Id == user.Id);

						if (!hasNotesConversation)
						{
							var notesConversation = new DirectConversation(
								user1Id: user.Id,
								user2Id: user.Id,
								initiatedByUserId: user.Id,
								isNotes: true
							);
							await contextDM.DirectConversations.AddAsync(notesConversation);
                            logger?.LogInformation("Created Notes conversation for user {UserId}", user.Id);
                        }
                    }
					await contextDM.SaveChangesAsync();
                }
            }
			catch (Exception)
			{

				throw;
			}
        }
    }
}