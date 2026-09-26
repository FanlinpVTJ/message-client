using System;
using Mirror;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IServerNetworkMessagesService
    {
        event Action<NetworkConnectionToClient, NetworkMessageType> OnClientSubscribed;

        bool Send<T>(NetworkConnectionToClient connection, T message) where T : struct, ISubscribedNetworkMessage;
    }
}
