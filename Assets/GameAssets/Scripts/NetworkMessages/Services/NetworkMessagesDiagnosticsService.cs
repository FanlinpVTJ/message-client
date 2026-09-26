using UnityEngine;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class NetworkMessagesDiagnosticsService : INetworkMessagesDiagnosticsService
    {
        public event System.Action<NetworkDiagnosticsType, string> MessageReported;

        public void Report(NetworkDiagnosticsType diagnosticsType, string message)
        {
            if (diagnosticsType == NetworkDiagnosticsType.Error)
            {
                Debug.LogError($"[NetworkMessages] {message}");
            }
            else
            {
                Debug.Log($"[NetworkMessages] {message}");
            }

            MessageReported?.Invoke(diagnosticsType, message);
        }
    }
}
