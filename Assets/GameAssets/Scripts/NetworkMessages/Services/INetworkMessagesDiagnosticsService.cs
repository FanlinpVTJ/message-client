using System;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public interface INetworkMessagesDiagnosticsService
    {
        event Action<NetworkDiagnosticsType, string> MessageReported;

        void Report(NetworkDiagnosticsType diagnosticsType, string message);
    }
}
