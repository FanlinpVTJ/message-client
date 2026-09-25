using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IServerSubscriptionRegistry
    {
        void Subscribe(int connectionId, NetworkMessageType messageType);

        void Unsubscribe(int connectionId, NetworkMessageType messageType);

        bool IsSubscribed(int connectionId, NetworkMessageType messageType);

        void RemoveConnection(int connectionId);
    }
}
