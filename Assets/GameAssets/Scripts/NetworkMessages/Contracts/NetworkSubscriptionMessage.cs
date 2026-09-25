using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Contracts
{
    public struct NetworkSubscriptionMessage : NetworkMessage
    {
        public NetworkMessageType MessageType;
        public NetworkSubscriptionOperationType OperationType;
    }
}
