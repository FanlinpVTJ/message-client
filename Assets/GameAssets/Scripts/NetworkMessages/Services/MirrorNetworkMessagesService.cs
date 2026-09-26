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

        public event Action<NetworkConnectionToClient, NetworkMessageType> OnClientSubscribed;

        private readonly IServerSubscriptionRegistry _subscriptionRegistry;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;
        private readonly Dictionary<ushort, MessageRegistration> _registrationsByMessageId = new();
        private readonly Dictionary<NetworkMessageType, MessageRegistration> _registrationsByMessageType = new();
        private readonly Dictionary<int, MessageRateLimit> _subscriptionRateLimits = new();
        private readonly Dictionary<int, MessageRateLimit> _sendRateLimits = new();
        private bool _isClientMessageRejected;

        public MirrorNetworkMessagesService(
            IServerSubscriptionRegistry subscriptionRegistry,
            INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _subscriptionRegistry = subscriptionRegistry;
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
        }

        public void Register<T>(NetworkMessageType messageType) where T : struct, ISubscribedNetworkMessage
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                throw new InvalidOperationException("Message types must be registered before starting a network session.");
            }

            ushort messageId = Mirror.NetworkMessages.GetId<T>();

            if (_registrationsByMessageId.ContainsKey(messageId) || _registrationsByMessageType.ContainsKey(messageType) ||
                messageId == Mirror.NetworkMessages.GetId<NetworkSubscriptionMessage>())
            {
                throw new InvalidOperationException($"Duplicate message type or Mirror message ID: {messageType}, {messageId}.");
            }

            MessageRegistration registration = new MessageRegistration(messageType, () => NetworkClient.UnregisterHandler<T>());
            _registrationsByMessageId.Add(messageId, registration);
            _registrationsByMessageType.Add(messageType, registration);
            _subscriptionRegistry.RegisterMessageType(messageType);
        }

        public bool Subscribe<T>(Action<T> handler) where T : struct, ISubscribedNetworkMessage
        {
            MessageRegistration registration = GetRegistration<T>();

            if (registration.IsSubscribed)
            {
                return false;
            }

            if (registration.IsHandlerRegistered)
            {
                registration.UnregisterHandler();
                registration.IsHandlerRegistered = false;
            }

            registration.RegisterHandler = () => NetworkClient.ReplaceHandler<T>(message => HandleMessage(message, registration, handler));
            registration.IsSubscribed = true;

            if (NetworkClient.active)
            {
                RegisterClientHandler(registration);
                SynchronizeClientSubscription(registration);
            }

            return true;
        }

        public bool Unsubscribe<T>() where T : struct, ISubscribedNetworkMessage
        {
            MessageRegistration registration = GetRegistration<T>();

            if (!registration.IsSubscribed)
            {
                return false;
            }

            registration.IsSubscribed = false;
            registration.RegisterHandler = null;

            if (NetworkClient.isConnected && registration.IsSubscriptionSent)
            {
                SendSubscription(registration, NetworkSubscriptionOperationType.Unsubscribe);
            }

            registration.IsSubscriptionSent = false;
            return true;
        }

        public bool Send<T>(NetworkConnectionToClient connection, T message) where T : struct, ISubscribedNetworkMessage
        {
            MessageRegistration registration = GetRegistration<T>();

            if (!NetworkServer.active || !NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) ||
                activeConnection != connection || !connection.isAuthenticated)
            {
                return false;
            }

            if (!message.IsValid)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"{registration.MessageType} was not sent: invalid message data.");
                return false;
            }

            if (!_subscriptionRegistry.IsSubscribed(connection.connectionId, registration.MessageType))
            {
                return false;
            }

            if (!_sendRateLimits.TryGetValue(connection.connectionId, out MessageRateLimit rateLimit))
            {
                rateLimit = new MessageRateLimit();
                _sendRateLimits.Add(connection.connectionId, rateLimit);
            }

            if (!rateLimit.TryConsume())
            {
                if (!rateLimit.HasReportedLimit)
                {
                    rateLimit.HasReportedLimit = true;
                    _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Server send limit reached for connection {connection.connectionId}. Excess messages are not sent.");
                }

                return false;
            }

            connection.Send(message);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Server sent {registration.MessageType} to client connection {connection.connectionId}: {message.Description}");
            return true;
        }

        public void InitializeServer()
        {
            _subscriptionRegistry.Clear();
            _subscriptionRateLimits.Clear();
            _sendRateLimits.Clear();
            NetworkServer.RegisterHandler<NetworkSubscriptionMessage>(HandleSubscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, "Server subscription handler registered.");
        }

        public void InitializeClient()
        {
            _isClientMessageRejected = false;

            foreach (MessageRegistration registration in _registrationsByMessageType.Values)
            {
                registration.IsSubscriptionSent = false;
                registration.IsHandlerRegistered = false;

                if (registration.IsSubscribed)
                {
                    RegisterClientHandler(registration);
                }
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service initialized.");
        }

        public void SynchronizeClientSubscriptions()
        {
            foreach (MessageRegistration registration in _registrationsByMessageType.Values)
            {
                SynchronizeClientSubscription(registration);
            }
        }

        public void RemoveClient(NetworkConnectionToClient connection)
        {
            _subscriptionRegistry.RemoveConnection(connection.connectionId);
            _subscriptionRateLimits.Remove(connection.connectionId);
            _sendRateLimits.Remove(connection.connectionId);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connection {connection.connectionId} subscriptions removed.");
        }

        public void StopClient()
        {
            foreach (MessageRegistration registration in _registrationsByMessageType.Values)
            {
                registration.IsSubscriptionSent = false;

                if (registration.IsHandlerRegistered)
                {
                    registration.UnregisterHandler();
                    registration.IsHandlerRegistered = false;
                }
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client network service stopped.");
        }

        public void StopServer()
        {
            NetworkServer.UnregisterHandler<NetworkSubscriptionMessage>();
            _subscriptionRegistry.Clear();
            _subscriptionRateLimits.Clear();
            _sendRateLimits.Clear();
        }

        private MessageRegistration GetRegistration<T>() where T : struct, ISubscribedNetworkMessage
        {
            ushort messageId = Mirror.NetworkMessages.GetId<T>();

            if (!_registrationsByMessageId.TryGetValue(messageId, out MessageRegistration registration))
            {
                throw new InvalidOperationException($"Mirror message {messageId} must be registered in NetworkMessagesInstaller before use.");
            }

            return registration;
        }

        private void RegisterClientHandler(MessageRegistration registration)
        {
            if (registration.IsHandlerRegistered)
            {
                return;
            }

            registration.RegisterHandler();
            registration.IsHandlerRegistered = true;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"{registration.MessageType} handler registered on client.");
        }

        private void SynchronizeClientSubscription(MessageRegistration registration)
        {
            if (!NetworkClient.isConnected || !registration.IsSubscribed || registration.IsSubscriptionSent)
            {
                return;
            }

            RegisterClientHandler(registration);
            registration.IsSubscriptionSent = true;
            SendSubscription(registration, NetworkSubscriptionOperationType.Subscribe);
        }

        private void SendSubscription(MessageRegistration registration, NetworkSubscriptionOperationType operationType)
        {
            NetworkSubscriptionMessage subscriptionMessage = new NetworkSubscriptionMessage
            {
                MessageType = registration.MessageType,
                OperationType = operationType
            };

            NetworkClient.Send(subscriptionMessage);
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"{registration.MessageType}: {operationType} sent to server.");
        }

        private void HandleSubscriptionMessage(NetworkConnectionToClient connection, NetworkSubscriptionMessage message)
        {
            if (!NetworkServer.connections.TryGetValue(connection.connectionId, out NetworkConnectionToClient activeConnection) || activeConnection != connection)
            {
                return;
            }

            if (!_subscriptionRateLimits.TryGetValue(connection.connectionId, out MessageRateLimit rateLimit))
            {
                rateLimit = new MessageRateLimit(Math.Max(MESSAGE_BURST_LIMIT, _registrationsByMessageType.Count));
                _subscriptionRateLimits.Add(connection.connectionId, rateLimit);
            }

            if (rateLimit.IsRejected)
            {
                return;
            }

            if (!_registrationsByMessageType.ContainsKey(message.MessageType) ||
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
                    OnClientSubscribed?.Invoke(connection, message.MessageType);
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

        private void HandleMessage<T>(T message, MessageRegistration registration, Action<T> handler) where T : struct, ISubscribedNetworkMessage
        {
            if (_isClientMessageRejected)
            {
                return;
            }

            if (!message.IsValid)
            {
                _isClientMessageRejected = true;
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Server sent invalid {registration.MessageType} data. Disconnecting client.");
                NetworkClient.Disconnect();
                return;
            }

            if (!registration.IsSubscribed || !registration.IsSubscriptionSent)
            {
                return;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Success, $"Client received {registration.MessageType} from server: {message.Description}");
            handler(message);
        }

        private void RejectSubscription(NetworkConnectionToClient connection, MessageRateLimit rateLimit, string reason)
        {
            rateLimit.IsRejected = true;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Disconnecting client {connection.connectionId}: {reason}.");
            connection.Disconnect();
        }

        private sealed class MessageRegistration
        {
            public NetworkMessageType MessageType { get; }
            public Action UnregisterHandler { get; }
            public Action RegisterHandler { get; set; }
            public bool IsSubscribed { get; set; }
            public bool IsSubscriptionSent { get; set; }
            public bool IsHandlerRegistered { get; set; }

            public MessageRegistration(NetworkMessageType messageType, Action unregisterHandler)
            {
                MessageType = messageType;
                UnregisterHandler = unregisterHandler;
            }
        }

        private sealed class MessageRateLimit
        {
            private readonly int _burstLimit;
            private double _tokens;
            private double _lastUpdateTime = NetworkTime.localTime;

            public bool IsRejected { get; set; }
            public bool HasReportedLimit { get; set; }

            public MessageRateLimit(int burstLimit = MESSAGE_BURST_LIMIT)
            {
                _burstLimit = burstLimit;
                _tokens = burstLimit;
            }

            public bool TryConsume()
            {
                double currentTime = NetworkTime.localTime;
                _tokens = Math.Min(_burstLimit, _tokens + Math.Max(0, currentTime - _lastUpdateTime) * MESSAGES_PER_SECOND);
                _lastUpdateTime = currentTime;

                if (_tokens < 1)
                {
                    return false;
                }

                _tokens -= 1;
                return true;
            }
        }
    }
}
