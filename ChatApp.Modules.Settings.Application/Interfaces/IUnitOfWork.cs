namespace ChatApp.Modules.Settings.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserSettingsRepository UserSettings { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}