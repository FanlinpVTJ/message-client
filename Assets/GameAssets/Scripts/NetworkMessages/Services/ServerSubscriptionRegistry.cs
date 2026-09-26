using System.Collections.Generic;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class ServerSubscriptionRegistry : IServerSubscriptionRegistry
    {
        private readonly Dictionary<NetworkMessageType, HashSet<int>> _connectionIdsByMessageType = new();

        public void RegisterMessageType(NetworkMessageType messageType)
        {
            _connectionIdsByMessageType.TryAdd(messageType, new HashSet<int>());
        }

        public bool Subscribe(int connectionId, NetworkMessageType messageType)
        {
            if (!_connectionIdsByMessageType.TryGetValue(messageType, out HashSet<int> connectionIds))
            {
                return false;
            }

            return connectionIds.Add(connectionId);
        }

        public bool Unsubscribe(int connectionId, NetworkMessageType messageType)
        {
            if (!_connectionIdsByMessageType.TryGetValue(messageType, out HashSet<int> connectionIds))
            {
                return false;
            }

            return connectionIds.Remove(connectionId);
        }

        public bool IsSubscribed(int connectionId, NetworkMessageType messageType)
        {
            if (!_connectionIdsByMessageType.TryGetValue(messageType, out HashSet<int> connectionIds))
            {
                return false;
            }

            return connectionIds.Contains(connectionId);
        }

        public void RemoveConnection(int connectionId)
        {
            foreach (HashSet<int> connectionIds in _connectionIdsByMessageType.Values)
            {
                connectionIds.Remove(connectionId);
            }
        }

        public void ClearSubscriptions()
        {
            foreach (HashSet<int> connectionIds in _connectionIdsByMessageType.Values)
            {
                connectionIds.Clear();
            }
        }
    }
}
