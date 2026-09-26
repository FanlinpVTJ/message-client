using System;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface INetworkSessionService
    {
        event Action OnClientSessionStopped;

        bool IsHostPortAvailable();

        void NotifyClientSessionStopped();

        void SetNetworkAddress(string networkAddress);

        bool StartHost();

        bool StartClient();

        bool StopClient();

        bool StopHost();
    }
}
