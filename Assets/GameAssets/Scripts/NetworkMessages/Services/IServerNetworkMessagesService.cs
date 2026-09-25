using System;
using Mirror;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IServerNetworkMessagesService
    {
        event Action<NetworkConnectionToClient, NetworkMessageType> ClientSubscribed;

        void SendHelloMessage(NetworkConnectionToClient connection, HelloMessage message);
    }
}
