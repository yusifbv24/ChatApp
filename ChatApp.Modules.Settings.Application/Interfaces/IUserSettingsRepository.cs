using ChatApp.Modules.Settings.Domain.Entities;

namespace ChatApp.Modules.Settings.Application.Interfaces
{
    public interface IUserSettingsRepository
    {
        Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default);
        Task UpdateAsync(UserSettings settings, CancellationToken cancellationToken = default);
    }
}