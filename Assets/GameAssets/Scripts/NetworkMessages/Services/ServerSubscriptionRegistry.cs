using System.Collections.Generic;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class ServerSubscriptionRegistry : IServerSubscriptionRegistry
    {
        private readonly Dictionary<NetworkMessageType, HashSet<int>> _connectionIdsByMessageType = new();
        private readonly List<NetworkMessageType> _emptyMessageTypes = new();

        public bool Subscribe(int connectionId, NetworkMessageType messageType)
        {
            if (messageType != NetworkMessageType.Hello)
            {
                return false;
            }

            if (!_connectionIdsByMessageType.TryGetValue(messageType, out HashSet<int> connectionIds))
            {
                connectionIds = new HashSet<int>();
                _connectionIdsByMessageType.Add(messageType, connectionIds);
            }

            return connectionIds.Add(connectionId);
        }

        public bool Unsubscribe(int connectionId, NetworkMessageType messageType)
        {
            if (!_connectionIdsByMessageType.TryGetValue(messageType, out HashSet<int> connectionIds))
            {
                return false;
            }

            bool removed = connectionIds.Remove(connectionId);

            if (connectionIds.Count == 0)
            {
                _connectionIdsByMessageType.Remove(messageType);
            }

            return removed;
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
            _emptyMessageTypes.Clear();

            foreach (KeyValuePair<NetworkMessageType, HashSet<int>> subscription in _connectionIdsByMessageType)
            {
                subscription.Value.Remove(connectionId);

                if (subscription.Value.Count == 0)
                {
                    _emptyMessageTypes.Add(subscription.Key);
                }
            }

            foreach (NetworkMessageType messageType in _emptyMessageTypes)
            {
                _connectionIdsByMessageType.Remove(messageType);
            }
        }

        public void Clear()
        {
            _connectionIdsByMessageType.Clear();
            _emptyMessageTypes.Clear();
        }
    }
}
