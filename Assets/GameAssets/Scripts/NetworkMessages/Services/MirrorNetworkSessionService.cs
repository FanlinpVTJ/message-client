using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkSessionService : INetworkSessionService
    {
        public void StartHost()
        {
            NetworkManager.singleton.StartHost();
        }

        public void StartClient()
        {
            NetworkManager.singleton.StartClient();
        }
    }
}
