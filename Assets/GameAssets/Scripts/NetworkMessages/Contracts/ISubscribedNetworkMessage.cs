using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Contracts
{
    public interface ISubscribedNetworkMessage : NetworkMessage
    {
        bool IsValid { get; }

        string Description { get; }
    }
}
