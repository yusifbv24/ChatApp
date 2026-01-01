namespace ChatApp.Modules.DirectMessages.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDirectConversationRepository Conversations { get; }
        IDirectMessageRepository Messages { get; }
        IDirectMessageReactionRepository Reactions { get; }
        IUserFavoriteMessageRepository Favorites { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}