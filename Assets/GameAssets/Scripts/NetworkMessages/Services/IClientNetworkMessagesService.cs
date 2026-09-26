using System;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface IClientNetworkMessagesService
    {
        bool Subscribe<T>(Action<T> handler) where T : struct, ISubscribedNetworkMessage;

        bool Unsubscribe<T>() where T : struct, ISubscribedNetworkMessage;
    }
}
