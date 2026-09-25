using Mirror;
using UnityEngine;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkMessagesService : IClientNetworkMessagesService, IServerNetworkMessagesService, INetworkMessagesLifecycleService
    {
        private readonly IServerSubscriptionRegistry _subscriptionRegistry;
        private bool _isHelloMessageHandlerRegistered;

        public MirrorNetworkMessagesService(IServerSubscriptionRegistry subscriptionRegistry)
        {
            _subscriptionRegistry = subscriptionRegistry;
        }

        public event System.Action<HelloMessage> HelloMessageReceived;
        public event System.Action<NetworkConnectionToClient, NetworkMessageType> ClientSubscribed;

        public void InitializeServer()
        {
            NetworkServer.RegisterHandler<NetworkSubscriptionMessage>(HandleSubscriptionMessage);
        }

        public void InitializeClient()
        {
            _isHelloMessageHandlerRegistered = false;
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
        }

        public void UnsubscribeFromHelloMessages()
        {
            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = NetworkMessageType.Hello,
                OperationType = NetworkSubscriptionOperationType.Unsubscribe
            };

            NetworkClient.Send(subscriptionMessage);
        }

        public void SendHelloMessage(NetworkConnectionToClient connection, HelloMessage message)
        {
            if (!_subscriptionRegistry.IsSubscribed(connection.connectionId, NetworkMessageType.Hello))
            {
                return;
            }

            connection.Send(message);
        }

        public void RemoveClient(NetworkConnectionToClient connection)
        {
            _subscriptionRegistry.RemoveConnection(connection.connectionId);
        }

        public void StopClient()
        {
            _isHelloMessageHandlerRegistered = false;
        }

        private void RegisterHelloMessageHandler()
        {
            if (_isHelloMessageHandlerRegistered)
            {
                return;
            }

            NetworkClient.RegisterHandler<HelloMessage>(HandleHelloMessage);
            _isHelloMessageHandlerRegistered = true;
        }

        private void HandleSubscriptionMessage(NetworkConnectionToClient connection, NetworkSubscriptionMessage message)
        {
            switch (message.OperationType)
            {
                case NetworkSubscriptionOperationType.Subscribe:
                    _subscriptionRegistry.Subscribe(connection.connectionId, message.MessageType);
                    ClientSubscribed?.Invoke(connection, message.MessageType);
                    break;
                case NetworkSubscriptionOperationType.Unsubscribe:
                    _subscriptionRegistry.Unsubscribe(connection.connectionId, message.MessageType);
                    break;
            }
        }

        private void HandleHelloMessage(HelloMessage message)
        {
            Debug.Log(message.Text);
            HelloMessageReceived?.Invoke(message);
        }
    }
}
