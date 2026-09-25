using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface INetworkMessagesLifecycleService
    {
        void InitializeServer();

        void InitializeClient();

        void RemoveClient(NetworkConnectionToClient connection);

        void StopClient();
    }
}
