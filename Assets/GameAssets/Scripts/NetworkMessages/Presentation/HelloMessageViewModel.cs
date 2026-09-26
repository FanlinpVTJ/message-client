using System;
using R3;
using Yuriy.MatchThree.NetworkMessages.Contracts;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class HelloMessageViewModel : IDisposable
    {
        private readonly IClientNetworkMessagesService _clientNetworkMessagesService;
        private readonly INetworkSessionService _networkSessionService;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;

        public ReactiveProperty<string> HelloText { get; }
        public ReactiveProperty<string> NetworkAddress { get; }
        public ReactiveProperty<string> ConnectionLog { get; }
        public ReactiveProperty<NetworkSessionStateType> SessionState { get; }
        public ReactiveProperty<bool> IsHostStartAvailable { get; }

        public HelloMessageViewModel(
            IClientNetworkMessagesService clientNetworkMessagesService,
            INetworkSessionService networkSessionService,
            INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _clientNetworkMessagesService = clientNetworkMessagesService;
            _networkSessionService = networkSessionService;
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
            HelloText = new ReactiveProperty<string>(string.Empty);
            NetworkAddress = new ReactiveProperty<string>("localhost");
            ConnectionLog = new ReactiveProperty<string>(string.Empty);
            SessionState = new ReactiveProperty<NetworkSessionStateType>(NetworkSessionStateType.Offline);
            IsHostStartAvailable = new ReactiveProperty<bool>(_networkSessionService.IsHostPortAvailable());

            _clientNetworkMessagesService.HelloMessageReceived += HandleHelloMessageReceived;
            _networkSessionService.OnClientSessionStopped += HandleClientSessionStopped;
            _networkMessagesDiagnosticsService.MessageReported += HandleDiagnosticsMessageReported;
        }

        public void StartHost()
        {
            if (SessionState.Value != NetworkSessionStateType.Offline)
            {
                return;
            }

            SessionState.Value = NetworkSessionStateType.Host;

            if (!_networkSessionService.StartHost())
            {
                SessionState.Value = NetworkSessionStateType.Offline;
                RefreshHostStartAvailability();
            }
        }

        public void SetNetworkAddress(string networkAddress)
        {
            NetworkAddress.Value = networkAddress;
            _networkSessionService.SetNetworkAddress(networkAddress);
        }

        public void StartClient()
        {
            if (SessionState.Value != NetworkSessionStateType.Offline)
            {
                return;
            }

            SessionState.Value = NetworkSessionStateType.Client;

            if (!_networkSessionService.StartClient())
            {
                SessionState.Value = NetworkSessionStateType.Offline;
            }
        }

        public void RefreshHostStartAvailability()
        {
            IsHostStartAvailable.Value = _networkSessionService.IsHostPortAvailable();
        }

        public void StopClient()
        {
            _networkSessionService.StopClient();
            SessionState.Value = NetworkSessionStateType.Offline;
            RefreshHostStartAvailability();
        }

        public void StopHost()
        {
            if (_networkSessionService.StopHost())
            {
                SessionState.Value = NetworkSessionStateType.Offline;
                RefreshHostStartAvailability();
            }
        }

        public void Dispose()
        {
            _clientNetworkMessagesService.HelloMessageReceived -= HandleHelloMessageReceived;
            _networkSessionService.OnClientSessionStopped -= HandleClientSessionStopped;
            _networkMessagesDiagnosticsService.MessageReported -= HandleDiagnosticsMessageReported;
            HelloText.Dispose();
            NetworkAddress.Dispose();
            ConnectionLog.Dispose();
            SessionState.Dispose();
            IsHostStartAvailable.Dispose();
        }

        private void HandleHelloMessageReceived(HelloMessage message)
        {
            HelloText.Value += $"[{DateTime.Now:HH:mm:ss dd.MM.yyyy}] [Server -> Client] {message.Text}\n";
        }

        private void HandleClientSessionStopped()
        {
            SessionState.Value = NetworkSessionStateType.Offline;
            RefreshHostStartAvailability();
        }

        private void HandleDiagnosticsMessageReported(NetworkDiagnosticsType diagnosticsType, string message)
        {
            string color = GetColor(diagnosticsType);
            ConnectionLog.Value += $"<color=#{color}>{message}</color>\n";
        }

        private string GetColor(NetworkDiagnosticsType diagnosticsType)
        {
            switch (diagnosticsType)
            {
                case NetworkDiagnosticsType.Success:
                    return "55D66B";
                case NetworkDiagnosticsType.Error:
                    return "FF5C5C";
                default:
                    return "C8D3E6";
            }
        }
    }
}
