using Mirror;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class NetworkMessagesNetworkManager : NetworkManager
    {
        [Inject] private INetworkMessagesLifecycleService _networkMessagesLifecycleService;
        [Inject] private IClientNetworkMessagesService _clientNetworkMessagesService;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _networkMessagesLifecycleService.InitializeServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _networkMessagesLifecycleService.InitializeClient();
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            _clientNetworkMessagesService.SubscribeToHelloMessages();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            _networkMessagesLifecycleService.RemoveClient(connection);
            base.OnServerDisconnect(connection);
        }

        public override void OnStopClient()
        {
            _networkMessagesLifecycleService.StopClient();
            base.OnStopClient();
        }
    }
}
