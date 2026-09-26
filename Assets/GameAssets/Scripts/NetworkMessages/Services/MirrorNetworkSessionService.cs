using System;
using System.Net;
using System.Net.Sockets;
using Mirror;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class MirrorNetworkSessionService : INetworkSessionService
    {
        private const ushort HOST_PORT = 7777;

        private readonly INetworkMessagesDiagnosticsService _networkMessagesDiagnosticsService;

        public event Action OnClientSessionStopped;

        public MirrorNetworkSessionService(INetworkMessagesDiagnosticsService networkMessagesDiagnosticsService)
        {
            _networkMessagesDiagnosticsService = networkMessagesDiagnosticsService;
        }

        public void SetNetworkAddress(string networkAddress)
        {
            NetworkManager.singleton.networkAddress = networkAddress;
            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Server address set to {networkAddress}.");
        }

        public bool IsHostPortAvailable()
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                return false;
            }

            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                socket.DualMode = true;
                socket.Bind(new IPEndPoint(IPAddress.IPv6Any, HOST_PORT));
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                socket.Dispose();
            }
        }

        public void NotifyClientSessionStopped()
        {
            OnClientSessionStopped?.Invoke();
        }

        public bool StartHost()
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "Host cannot start because a network session is already active.");
                return false;
            }

            if (!IsHostPortAvailable())
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, $"Host cannot start because UDP port {HOST_PORT} is unavailable.");
                return false;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Starting host.");
            NetworkManager.singleton.StartHost();
            return true;
        }

        public bool StartClient()
        {
            if (NetworkClient.active)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Error, "Client cannot start because a client session is already active.");
                return false;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, $"Connecting to {NetworkManager.singleton.networkAddress}:7777.");
            NetworkManager.singleton.StartClient();
            return true;
        }

        public bool StopClient()
        {
            if (!NetworkClient.active)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Client connection is already closed.");
                return true;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Stopping client.");
            NetworkManager.singleton.StopClient();
            return true;
        }

        public bool StopHost()
        {
            if (!NetworkServer.active && !NetworkClient.active)
            {
                _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Host session is not active.");
                return false;
            }

            _networkMessagesDiagnosticsService.Report(NetworkDiagnosticsType.Information, "Stopping host.");
            NetworkManager.singleton.StopHost();
            return true;
        }
    }
}
