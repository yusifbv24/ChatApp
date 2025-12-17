namespace ChatApp.Modules.Channels.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IChannelRepository Channels { get; }
        IChannelMemberRepository ChannelMembers { get; }
        IChannelMessageRepository ChannelMessages { get; }
        IChannelMessageReadRepository ChannelMessageReads { get; }
        IChannelMessageReactionRepository ChannelMessageReactions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}