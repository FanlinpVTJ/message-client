using System;
using System.Collections.Generic;
using Mirror;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkMessagesService : IClientNetworkMessagesService, IServerNetworkMessagesService, INetworkMessagesLifecycleService
    {
        private const int MESSAGE_BURST_LIMIT = 8;
        private const double MESSAGES_PER_SECOND = 2;

        private readonly IServerSubscriptionRegistry _subscriptionRegistry;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;
        private readonly Dictionary<int, MessageRateLimit> _subscriptionRateLimits = new();
        private readonly MessageRateLimit _helloMessageRateLimit = new();
        private bool _isHelloMessageHandlerRegistered;
        private bool _isSubscribedToHelloMessages;

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
            _subscriptionRegistry.Clear();
            _subscriptionRateLimits.Clear();
            NetworkServer.RegisterHandler<NetworkSubscriptionMessage>(HandleSubscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Server subscription handler registered.");
        }

        public void InitializeClient()
        {
            _isHelloMessageHandlerRegistered = false;
            _isSubscribedToHelloMessages = false;
            _helloMessageRateLimit.Reset();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service initialized.");
        }

        public void SubscribeToHelloMessages()
        {
            if (!NetworkClient.isConnected || _isSubscribedToHelloMessages)
            {
                return;
            }

            RegisterHelloMessageHandler();

            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = NetworkMessageType.Hello,
                OperationType = NetworkSubscriptionOperationType.Subscribe
            };

            _isSubscribedToHelloMessages = true;
            NetworkClient.Send(subscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "HelloMessage subscription sent to server.");
        }

        public void UnsubscribeFromHelloMessages()
        {
            if (!NetworkClient.isConnected || !_isSubscribedToHelloMessages)
            {
                return;
            }

            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = NetworkMessageType.Hello,
                OperationType = NetworkSubscriptionOperationType.Unsubscribe
            };

            _isSubscribedToHelloMessages = false;
            NetworkClient.Send(subscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "HelloMessage unsubscription sent to server.");
        }

        public void SendHelloMessage(NetworkConnectionToClient connection, HelloMessage message)
        {
            if (!NetworkServer.active || !NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) || activeConnection != connection)
            {
                return;
            }

            if (string.IsNullOrEmpty(message.Text) || message.Text.Length > HelloMessage.MAX_TEXT_LENGTH)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "HelloMessage was not sent: invalid text length.");
                return;
            }

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
            _subscriptionRateLimits.Remove(connection.connectionId);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connection {connection.connectionId} subscriptions removed.");
        }

        public void StopClient()
        {
            _isHelloMessageHandlerRegistered = false;
            _isSubscribedToHelloMessages = false;
            _helloMessageRateLimit.Reset();
            NetworkClient.UnregisterHandler<HelloMessage>();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service stopped.");
        }

        public void StopServer()
        {
            NetworkServer.UnregisterHandler<NetworkSubscriptionMessage>();
            _subscriptionRegistry.Clear();
            _subscriptionRateLimits.Clear();
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
            if (!NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) || activeConnection != connection)
            {
                return;
            }

            if (!_subscriptionRateLimits.TryGetValue(connection.connectionId, out MessageRateLimit rateLimit))
            {
                rateLimit = new MessageRateLimit();
                _subscriptionRateLimits.Add(connection.connectionId, rateLimit);
            }

            if (rateLimit.IsRejected)
            {
                return;
            }

            if (message.MessageType != NetworkMessageType.Hello ||
                (message.OperationType != NetworkSubscriptionOperationType.Subscribe && message.OperationType != NetworkSubscriptionOperationType.Unsubscribe))
            {
                RejectSubscription(connection, rateLimit, "unsupported subscription type or operation");
                return;
            }

            if (!rateLimit.TryConsume())
            {
                RejectSubscription(connection, rateLimit, "subscription rate limit exceeded");
                return;
            }

            switch (message.OperationType)
            {
                case NetworkSubscriptionOperationType.Subscribe:
                    if (!_subscriptionRegistry.Subscribe(connection.connectionId, message.MessageType))
                    {
                        return;
                    }

                    _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Connection {connection.connectionId} subscribed to {message.MessageType}.");
                    ClientSubscribed?.Invoke(connection, message.MessageType);
                    break;
                case NetworkSubscriptionOperationType.Unsubscribe:
                    if (!_subscriptionRegistry.Unsubscribe(connection.connectionId, message.MessageType))
                    {
                        return;
                    }

                    _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connection {connection.connectionId} unsubscribed from {message.MessageType}.");
                    break;
            }
        }

        private void HandleHelloMessage(HelloMessage message)
        {
            if (_helloMessageRateLimit.IsRejected)
            {
                return;
            }

            if (string.IsNullOrEmpty(message.Text) || message.Text.Length > HelloMessage.MAX_TEXT_LENGTH || !_helloMessageRateLimit.TryConsume())
            {
                _helloMessageRateLimit.IsRejected = true;
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "Server sent invalid or too many HelloMessages. Disconnecting client.");
                NetworkClient.Disconnect();
                return;
            }

            if (!_isSubscribedToHelloMessages)
            {
                return;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Client received HelloMessage from server: {message.Text}");
            HelloMessageReceived?.Invoke(message);
        }

        private void RejectSubscription(NetworkConnectionToClient connection, MessageRateLimit rateLimit, string reason)
        {
            rateLimit.IsRejected = true;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Disconnecting client {connection.connectionId}: {reason}.");
            connection.Disconnect();
        }

        private sealed class MessageRateLimit
        {
            private double _tokens = MESSAGE_BURST_LIMIT;
            private double _lastUpdateTime = NetworkTime.localTime;

            public bool IsRejected { get; set; }

            public bool TryConsume()
            {
                double currentTime = NetworkTime.localTime;
                _tokens = Math.Min(MESSAGE_BURST_LIMIT, _tokens + Math.Max(0, currentTime - _lastUpdateTime) * MESSAGES_PER_SECOND);
                _lastUpdateTime = currentTime;

                if (_tokens < 1)
                {
                    return false;
                }

                _tokens -= 1;
                return true;
            }

            public void Reset()
            {
                _tokens = MESSAGE_BURST_LIMIT;
                _lastUpdateTime = NetworkTime.localTime;
                IsRejected = false;
            }
        }
    }
}
