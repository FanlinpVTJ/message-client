using System;
using System.Collections.Generic;
using System.Globalization;
using R3;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class HelloMessageViewModel : ILateTickable, IDisposable
    {
        private const int MAX_HISTORY_ENTRIES = 200;

        private readonly IClientNetworkMessagesService _clientNetworkMessagesService;
        private readonly INetworkSessionService _networkSessionService;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;
        private readonly Queue<string> _helloHistory = new();
        private readonly Queue<string> _diagnosticsHistory = new();
        private bool _helloHistoryChanged;
        private bool _diagnosticsHistoryChanged;

        public ReactiveProperty<string> HelloText { get; }
        public ReactiveProperty<string> NetworkAddress { get; }
        public ReactiveProperty<string> ConnectionLog { get; }
        public ReadOnlyReactiveProperty<NetworkSessionStateType> SessionState => _networkSessionService.SessionState;
        public ReadOnlyReactiveProperty<bool> IsHostStartAvailable => _networkSessionService.IsHostStartAvailable;

        public HelloMessageViewModel(
            IClientNetworkMessagesService clientNetworkMessagesService,
            INetworkSessionService networkSessionService,
            INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _clientNetworkMessagesService = clientNetworkMessagesService;
            _networkSessionService = networkSessionService;
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
            HelloText = new ReactiveProperty<string>(string.Empty);
            NetworkAddress = new ReactiveProperty<string>(_networkSessionService.NetworkAddress);
            ConnectionLog = new ReactiveProperty<string>(string.Empty);

            _networkMessagesDiagnosticsService.MessageReported += HandleDiagnosticsMessageReported;
            _clientNetworkMessagesService.Subscribe<HelloMessage>(HandleHelloMessageReceived);
        }

        public void StartHost()
        {
            _networkSessionService.StartHost();
        }

        public void SetNetworkAddress(string networkAddress)
        {
            _networkSessionService.SetNetworkAddress(networkAddress);
            NetworkAddress.Value = _networkSessionService.NetworkAddress;
        }

        public void StartClient()
        {
            _networkSessionService.StartClient();
        }

        public void StopClient()
        {
            _networkSessionService.StopClient();
        }

        public void StopHost()
        {
            _networkSessionService.StopHost();
        }

        public void LateTick()
        {
            if (_helloHistoryChanged)
            {
                _helloHistoryChanged = false;
                HelloText.Value = string.Join("\n", _helloHistory);
            }

            if (_diagnosticsHistoryChanged)
            {
                _diagnosticsHistoryChanged = false;
                ConnectionLog.Value = string.Join("\n", _diagnosticsHistory);
            }
        }

        public void Dispose()
        {
            _networkMessagesDiagnosticsService.MessageReported -= HandleDiagnosticsMessageReported;
            _clientNetworkMessagesService.Unsubscribe<HelloMessage>();
            HelloText.Dispose();
            NetworkAddress.Dispose();
            ConnectionLog.Dispose();
            _helloHistory.Clear();
            _diagnosticsHistory.Clear();
        }

        private void HandleHelloMessageReceived(HelloMessage message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy", CultureInfo.InvariantCulture);
            string safeText = NetworkMessagesDiagnosticsService.FormatPlainText(message.Text);
            AddHistoryEntry(_helloHistory, $"[{timestamp}] [Server -> Client] {safeText}");
            _helloHistoryChanged = true;
        }

        private void HandleDiagnosticsMessageReported(NetworkDiagnosticsType diagnosticsType, string message)
        {
            string color = GetColor(diagnosticsType);
            AddHistoryEntry(_diagnosticsHistory, $"<color=#{color}>{message}</color>");
            _diagnosticsHistoryChanged = true;
        }

        private void AddHistoryEntry(Queue<string> history, string entry)
        {
            if (history.Count == MAX_HISTORY_ENTRIES)
            {
                history.Dequeue();
            }

            history.Enqueue(entry);
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
