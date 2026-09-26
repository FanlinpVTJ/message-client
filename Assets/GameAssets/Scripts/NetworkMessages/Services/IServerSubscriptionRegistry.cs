using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IServerSubscriptionRegistry
    {
        bool Subscribe(int connectionId, NetworkMessageType messageType);

        bool Unsubscribe(int connectionId, NetworkMessageType messageType);

        bool IsSubscribed(int connectionId, NetworkMessageType messageType);

        void RemoveConnection(int connectionId);

        void Clear();
    }
}
