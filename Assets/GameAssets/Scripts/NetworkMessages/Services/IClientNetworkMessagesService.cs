using System;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IClientNetworkMessagesService
    {
        event Action<HelloMessage> HelloMessageReceived;

        void SubscribeToHelloMessages();

        void UnsubscribeFromHelloMessages();
    }
}
