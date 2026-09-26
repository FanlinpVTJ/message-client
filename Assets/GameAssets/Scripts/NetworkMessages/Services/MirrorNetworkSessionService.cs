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
        private double _nextPortCheckTime;

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

        public bool IsHostPortAvailable()
        {
            if (NetworkServer.active || NetworkClient.active || !_transport.Available() || _networkManager.transport != _transport)
            {
                return false;
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
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        public bool StartHost()
        {
            if (!CanStartSession())
            {
                return false;
            }

            if (!IsHostPortAvailable())
            {
                _isHostStartAvailable.Value = false;
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Host cannot start: UDP port {_transport.Port} cannot be opened with the current KCP settings.");
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
            _isHostStartAvailable.Value = false;
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
                _isHostStartAvailable.Value = false;
                _sessionState.Value = state;
                return;
            }

            double currentTime = Time.realtimeSinceStartupAsDouble;

            if (forcePortCheck || stateChanged || currentTime >= _nextPortCheckTime)
            {
                _isHostStartAvailable.Value = IsHostPortAvailable();
                _nextPortCheckTime = currentTime + PORT_CHECK_INTERVAL_SECONDS;
            }

            _sessionState.Value = state;
        }
    }
}
