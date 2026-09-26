using Mirror;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class NetworkMessagesNetworkManager : NetworkManager
    {
        [Inject] private INetworkMessagesLifecycleService _networkMessagesLifecycleService;
        [Inject] private IClientNetworkMessagesService _clientNetworkMessagesService;
        [Inject] private INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;

        public override void OnStartHost()
        {
            base.OnStartHost();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Host started.");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _networkMessagesLifecycleService.InitializeServer();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _networkMessagesLifecycleService.InitializeClient();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client started. Waiting for transport connection.");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Client connected to server.");
            _clientNetworkMessagesService.SubscribeToHelloMessages();
        }

        public override void OnServerConnect(NetworkConnectionToClient connection)
        {
            base.OnServerConnect(connection);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Client connected to server with connection ID {connection.connectionId}.");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            _networkMessagesLifecycleService.RemoveClient(connection);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Server disconnected connection {connection.connectionId}.");
            base.OnServerDisconnect(connection);
        }

        public override void OnClientDisconnect()
        {
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client disconnected from server.");
            base.OnClientDisconnect();
        }

        public override void OnClientError(TransportError error, string reason)
        {
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Client transport error {error}: {reason}");
            base.OnClientError(error, reason);
        }

        public override void OnStopClient()
        {
            _networkMessagesLifecycleService.StopClient();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client stopped.");
            base.OnStopClient();
        }

        public override void OnApplicationQuit()
        {
            try
            {
                if (NetworkClient.active)
                {
                    StopClient();
                }
            }
            finally
            {
                base.OnApplicationQuit();
            }
        }

        public override void OnStopServer()
        {
            _networkMessagesLifecycleService.StopServer();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Server stopped.");
            base.OnStopServer();
        }

        public override void OnServerError(NetworkConnectionToClient connection, TransportError error, string reason)
        {
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Server transport error {error}: {reason}");
            base.OnServerError(connection, error, reason);
        }
    }
}
