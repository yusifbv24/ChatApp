
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
        private readonly ConcurrentDictionary<Guid, List<UserConnection>> _userConnections = new();

        private readonly object _lock = new();

        public Task AddConnectionAsync(Guid userId, string connectionId)
        {
            lock (_lock)
            {
                // Add connectionId => userId mapping
                _connectionToUser[connectionId] = userId;

                // Add to user's connection list
                if(!_userConnections.TryGetValue(userId,out var connections)) // If there are not any connection with this UserId
                {
                    connections = [];
                    _userConnections[userId] = connections;
                }

                var userConnection = new UserConnection(userId, connectionId);
                connections.Add(userConnection);
            }
            return Task.CompletedTask;
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


        public Task RemoveConnectionAsync(string connectionId)
        {
            lock (_lock)
            {
                if(_connectionToUser.TryRemove(connectionId, out var userId))
                {
                    if(_userConnections.TryGetValue(userId,out var connections))
                    {
                        connections.RemoveAll(c => c.ConnectionId == connectionId);

                        // If user has no more connections, remove from dictionary
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(userId, out _);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}