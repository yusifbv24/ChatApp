using ChatApp.Modules.Settings.Application.Interfaces;
using ChatApp.Modules.Settings.Domain.Entities;
using ChatApp.Modules.Settings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Settings.Infrastructure.Repositories
{
    public class UserSettingsRepository : IUserSettingsRepository
    {
        private readonly SettingsDbContext _context;

        public UserSettingsRepository(SettingsDbContext context)
        {
            _context = context;
        }

        public async Task<UserSettings?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            await _context.UserSettings.AddAsync(settings, cancellationToken);
        }

        public Task UpdateAsync(UserSettings settings, CancellationToken cancellationToken = default)
        {
            _context.UserSettings.Update(settings);
            return Task.CompletedTask;
        }
    }
}