
using ChatApp.Shared.Infrastructure.SignalR.Models;
using System.Collections.Concurrent;

namespace ChatApp.Shared.Infrastructure.SignalR.Services
{
    /// <summary>
    /// In-memory implementation of connection manager
    /// For production with multiple servers, use Redis backplane
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        // connectionId => userId mapping
        private readonly ConcurrentDictionary<string, Guid> _connectionToUser = new();

        // userId => list of connectionIds (one user can have multiple devices)
        // CRITICAL FIX: Removed global lock - use ConcurrentDictionary atomic operations
        private readonly ConcurrentDictionary<Guid, List<UserConnection>> _userConnections = new();

        // Per-user locks for thread-safe list modifications (prevents lock contention)
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();

        public async Task AddConnectionAsync(Guid userId, string connectionId)
        {
            // Add connectionId => userId mapping (thread-safe, no lock needed)
            _connectionToUser[connectionId] = userId;

            // Get or create per-user lock (prevents global lock contention)
            var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await userLock.WaitAsync();
            try
            {
                // Add to user's connection list using atomic AddOrUpdate
                _userConnections.AddOrUpdate(
                    userId,
                    // Factory: create new list if user doesn't exist
                    _ => new List<UserConnection> { new UserConnection(userId, connectionId) },
                    // Update: add to existing list
                    (_, existingList) =>
                    {
                        existingList.Add(new UserConnection(userId, connectionId));
                        return existingList;
                    }
                );
            }
            finally
            {
                userLock.Release();
            }
        }


        public Task<int> GetConnectionCountAsync(Guid userId)
        {
            if(_userConnections.TryGetValue(userId,out var connections))
            {
                return Task.FromResult(connections.Count);
            }

            return Task.FromResult(0);
        }


        public Task<List<string>> GetUserConnectionsAsync(Guid userId)
        {
            if(_userConnections.TryGetValue(userId, out var connections))
            {
                return Task.FromResult(connections.Select(c=>c.ConnectionId).ToList());
            }

            return Task.FromResult(new List<string>());
        }


        public Task<Guid?> GetUserIdByConnectionAsync(string connectionId)
        {
            if(_connectionToUser.TryGetValue(connectionId, out var userId))
            {
                return Task.FromResult<Guid?>(userId);
            }

            return Task.FromResult<Guid?>(null);
        }


        public Task<Dictionary<Guid, List<string>>> GetUsersConnectionsAsync(List<Guid> userIds)
        {
            var result=new Dictionary<Guid, List<string>>();

            foreach(var userId in userIds)
            {
                if(_userConnections.TryGetValue(userId,out var connections))
                {
                    result[userId] = connections.Select(c => c.ConnectionId).ToList();
                }
            }
            return Task.FromResult(result);
        }


        public Task<bool> IsUserOnlineAsync(Guid userId)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId));
        }


        public async Task RemoveConnectionAsync(string connectionId)
        {
            // Remove connectionId => userId mapping (thread-safe, no lock needed)
            if (_connectionToUser.TryRemove(connectionId, out var userId))
            {
                // Get per-user lock (prevents race condition when removing connections)
                var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                await userLock.WaitAsync();
                try
                {
                    if (_userConnections.TryGetValue(userId, out var connections))
                    {
                        connections.RemoveAll(c => c.ConnectionId == connectionId);

                        // If user has no more connections, remove from dictionary AND cleanup lock
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(userId, out _);

                            // Cleanup: remove per-user lock to prevent memory leak
                            _userLocks.TryRemove(userId, out var lockToDispose);
                            lockToDispose?.Dispose();
                        }
                    }
                }
                finally
                {
                    userLock.Release();
                }
            }
        }
    }
}