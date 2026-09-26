using Mirror;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkMessagesService : IClientNetworkMessagesService, IServerNetworkMessagesService, INetworkMessagesLifecycleService
    {
        private readonly IServerSubscriptionRegistry _subscriptionRegistry;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;
        private bool _isHelloMessageHandlerRegistered;

        public MirrorNetworkMessagesService(
            IServerSubscriptionRegistry subscriptionRegistry,
            INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _subscriptionRegistry = subscriptionRegistry;
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
        }

        public event System.Action<HelloMessage> HelloMessageReceived;
        public event System.Action<NetworkConnectionToClient, NetworkMessageType> ClientSubscribed;

        public void InitializeServer()
        {
            NetworkServer.RegisterHandler<NetworkSubscriptionMessage>(HandleSubscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Server subscription handler registered.");
        }

        public void InitializeClient()
        {
            _isHelloMessageHandlerRegistered = false;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service initialized.");
        }

        public void SubscribeToHelloMessages()
        {
            RegisterHelloMessageHandler();

            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = NetworkMessageType.Hello,
                OperationType = NetworkSubscriptionOperationType.Subscribe
            };

            NetworkClient.Send(subscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "HelloMessage subscription sent to server.");
        }

        public void UnsubscribeFromHelloMessages()
        {
            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = NetworkMessageType.Hello,
                OperationType = NetworkSubscriptionOperationType.Unsubscribe
            };

            NetworkClient.Send(subscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "HelloMessage unsubscription sent to server.");
        }

        public void SendHelloMessage(NetworkConnectionToClient connection, HelloMessage message)
        {
            if (!_subscriptionRegistry.IsSubscribed(connection.connectionId, NetworkMessageType.Hello))
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"HelloMessage was not sent to connection {connection.connectionId}: no subscription.");
                return;
            }

            connection.Send(message);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Server sent HelloMessage to client connection {connection.connectionId}: {message.Text}");
        }

        public void RemoveClient(NetworkConnectionToClient connection)
        {
            _subscriptionRegistry.RemoveConnection(connection.connectionId);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connection {connection.connectionId} subscriptions removed.");
        }

        public void StopClient()
        {
            _isHelloMessageHandlerRegistered = false;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service stopped.");
        }

        private void RegisterHelloMessageHandler()
        {
            if (_isHelloMessageHandlerRegistered)
            {
                return;
            }

            NetworkClient.RegisterHandler<HelloMessage>(HandleHelloMessage);
            _isHelloMessageHandlerRegistered = true;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "HelloMessage handler registered on client.");
        }

        private void HandleSubscriptionMessage(NetworkConnectionToClient connection, NetworkSubscriptionMessage message)
        {
            switch (message.OperationType)
            {
                case NetworkSubscriptionOperationType.Subscribe:
                    _subscriptionRegistry.Subscribe(connection.connectionId, message.MessageType);
                    _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Connection {connection.connectionId} subscribed to {message.MessageType}.");
                    ClientSubscribed?.Invoke(connection, message.MessageType);
                    break;
                case NetworkSubscriptionOperationType.Unsubscribe:
                    _subscriptionRegistry.Unsubscribe(connection.connectionId, message.MessageType);
                    _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connection {connection.connectionId} unsubscribed from {message.MessageType}.");
                    break;
            }
        }

        private void HandleHelloMessage(HelloMessage message)
        {
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Client received HelloMessage from server: {message.Text}");
            HelloMessageReceived?.Invoke(message);
        }
    }
}
