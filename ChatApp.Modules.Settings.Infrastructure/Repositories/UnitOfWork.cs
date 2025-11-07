using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Infrastructure.Persistence;

namespace ChatApp.Modules.Settings.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SettingsDbContext _context;

        public IUserSettingsRepository UserSettings { get; }

        public UnitOfWork(SettingsDbContext context)
        {
            _context = context;
            UserSettings = new UserSettingsRepository(context);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}