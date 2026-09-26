using R3;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface INetworkSessionService
    {
        ReadOnlyReactiveProperty<NetworkSessionStateType> SessionState { get; }

        ReadOnlyReactiveProperty<bool> IsHostStartAvailable { get; }

        string NetworkAddress { get; }

        void SetNetworkAddress(string networkAddress);

        bool StartHost();

        bool StartClient();

        bool StopClient();

        bool StopHost();
    }
}
