using System;
using System.Net;
using System.Net.Sockets;
using kcp2k;
using Mirror;
using R3;
using UnityEngine;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkSessionService : INetworkSessionService, IInitializable, ITickable, IDisposable
    {
        private const double PORT_CHECK_INTERVAL_SECONDS = 1;

        private readonly NetworkManager _networkManager;
        private readonly KcpTransport _transport;
        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;
        private readonly ReactiveProperty<NetworkSessionStateType> _sessionState = new(NetworkSessionStateType.Offline);
        private readonly ReactiveProperty<bool> _isHostStartAvailable = new(false);
        private string _hostStartUnavailableReason = string.Empty;
        private double _nextPortCheckTime;
        private bool _hasReportedHostAvailability;

        public ReadOnlyReactiveProperty<NetworkSessionStateType> SessionState => _sessionState;
        public ReadOnlyReactiveProperty<bool> IsHostStartAvailable => _isHostStartAvailable;
        public string NetworkAddress => _networkManager.networkAddress;

        public MirrorNetworkSessionService(
            NetworkManager networkManager,
            KcpTransport transport,
            INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _networkManager = networkManager;
            _transport = transport;
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
        }

        public void Initialize()
        {
            RefreshState(true);
        }

        public void Tick()
        {
            RefreshState(false);
        }

        public void Dispose()
        {
            _sessionState.Dispose();
            _isHostStartAvailable.Dispose();
        }

        public void SetNetworkAddress(string networkAddress)
        {
            if (_sessionState.Value != NetworkSessionStateType.Offline)
            {
                return;
            }

            _networkManager.networkAddress = networkAddress.Trim();
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Server address set to {_networkManager.networkAddress}.");
        }

        private string GetHostStartUnavailableReason()
        {
            if (NetworkServer.active)
            {
                return "Host is already running.";
            }

            if (NetworkClient.active)
            {
                return "Host is unavailable while the client is connecting or connected. Stop Client first.";
            }

            if (_networkManager.transport != _transport)
            {
                return "Host is unavailable: NetworkManager must use the configured KCP transport.";
            }

            if (!_transport.Available())
            {
                return "Host is unavailable: KCP transport is not supported on this platform.";
            }

            AddressFamily addressFamily = _transport.DualMode ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            IPAddress address = _transport.DualMode ? IPAddress.IPv6Any : IPAddress.Any;

            try
            {
                using (Socket socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp))
                {
                    if (_transport.DualMode)
                    {
                        socket.DualMode = true;
                    }

                    socket.Bind(new IPEndPoint(address, _transport.Port));
                    return string.Empty;
                }
            }
            catch (SocketException exception)
            {
                if (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return $"Host is unavailable: UDP port {_transport.Port} is already in use.";
                }

                return $"Host is unavailable: cannot open UDP port {_transport.Port} ({exception.SocketErrorCode}).";
            }
            catch (NotSupportedException)
            {
                return "Host is unavailable: the selected IP mode is not supported on this platform.";
            }
        }

        public bool StartHost()
        {
            if (!CanStartSession())
            {
                return false;
            }

            RefreshState(true);

            if (!_isHostStartAvailable.Value)
            {
                return false;
            }

            return StartSession(_networkManager.StartHost, NetworkSessionStateType.Host);
        }

        public bool StartClient()
        {
            if (!CanStartSession())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_networkManager.networkAddress))
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "Client cannot start: enter the server address.");
                return false;
            }

            return StartSession(_networkManager.StartClient, NetworkSessionStateType.Client);
        }

        public bool StopClient()
        {
            if (NetworkServer.active)
            {
                return false;
            }

            try
            {
                _networkManager.StopClient();
                return true;
            }
            finally
            {
                RefreshState(true);
            }
        }

        public bool StopHost()
        {
            try
            {
                _networkManager.StopHost();
                return true;
            }
            finally
            {
                RefreshState(true);
            }
        }

        private bool CanStartSession()
        {
            if (_sessionState.Value != NetworkSessionStateType.Offline || NetworkServer.active || NetworkClient.active)
            {
                return false;
            }

            if (_networkManager.transport != _transport)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "The session service requires the configured KCP transport.");
                return false;
            }

            return true;
        }

        private bool StartSession(Action startSession, NetworkSessionStateType requestedState)
        {
            SetHostStartAvailability("Host is unavailable while a network session is starting.", NetworkDiagnosticsType.Information);
            _sessionState.Value = requestedState;
            bool started = false;

            try
            {
                string description = requestedState == NetworkSessionStateType.Host
                    ? $"Starting host on UDP port {_transport.Port}."
                    : $"Connecting to {_networkManager.networkAddress}:{_transport.Port}.";
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, description);
                startSession();
                started = requestedState == NetworkSessionStateType.Host ? NetworkServer.active : NetworkClient.active;
                return started;
            }
            catch (SocketException exception)
            {
                ReportStartFailure(exception.Message);
                return false;
            }
            catch (ArgumentException exception)
            {
                ReportStartFailure(exception.Message);
                return false;
            }
            catch (NotSupportedException exception)
            {
                ReportStartFailure(exception.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (!started)
                    {
                        CleanupFailedStart();
                    }
                }
                finally
                {
                    RefreshState(true);
                }
            }
        }

        private void CleanupFailedStart()
        {
            try
            {
                _networkManager.StopHost();
            }
            finally
            {
                try
                {
                    NetworkClient.Shutdown();
                }
                finally
                {
                    NetworkServer.Shutdown();
                }
            }
        }

        private void ReportStartFailure(string reason)
        {
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Network session could not start: {reason}");
        }

        private void RefreshState(bool forcePortCheck)
        {
            NetworkSessionStateType state = NetworkSessionStateType.Offline;

            if (NetworkServer.active)
            {
                state = NetworkSessionStateType.Host;
            }
            else if (NetworkClient.active)
            {
                state = NetworkSessionStateType.Client;
            }

            bool stateChanged = _sessionState.Value != state;

            if (state != NetworkSessionStateType.Offline)
            {
                SetHostStartAvailability(GetHostStartUnavailableReason(), NetworkDiagnosticsType.Information);
                _sessionState.Value = state;
                return;
            }

            double currentTime = Time.realtimeSinceStartupAsDouble;

            if (forcePortCheck || stateChanged || currentTime >= _nextPortCheckTime)
            {
                SetHostStartAvailability(GetHostStartUnavailableReason(), NetworkDiagnosticsType.Error);
                _nextPortCheckTime = currentTime + PORT_CHECK_INTERVAL_SECONDS;
            }

            _sessionState.Value = state;
        }

        private void SetHostStartAvailability(string reason, NetworkDiagnosticsType diagnosticsType)
        {
            bool reasonChanged = _hostStartUnavailableReason != reason;
            bool isAvailable = reason.Length == 0;
            _isHostStartAvailable.Value = isAvailable;
            _hostStartUnavailableReason = reason;

            if (_hasReportedHostAvailability && !reasonChanged)
            {
                return;
            }

            _hasReportedHostAvailability = true;
            _networkMessagesDiagnosticsService.Report(
                isAvailable ? NetworkDiagnosticsType.Success : diagnosticsType,
                isAvailable ? $"Host is available: UDP port {_transport.Port} is free." : reason);
        }
    }
}
