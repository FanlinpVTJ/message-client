using System.Globalization;
using System.Text;
using UnityEngine;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class NetworkMessagesDiagnosticsService : INetworkMessagesDiagnosticsService
    {
        private const int MAX_DISPLAY_LENGTH = 1200;

        public event System.Action<NetworkDiagnosticsType, string> MessageReported;

        public void Report(NetworkDiagnosticsType diagnosticsType, string message)
        {
            string safeMessage = FormatPlainText(message);

            if (diagnosticsType == NetworkDiagnosticsType.Error)
            {
                Debug.LogError($"[NetworkMessages] {safeMessage}");
            }
            else
            {
                Debug.Log($"[NetworkMessages] {safeMessage}");
            }

            MessageReported?.Invoke(diagnosticsType, safeMessage);
        }

        public static string FormatPlainText(string text)
        {
            StringBuilder result = new StringBuilder();

            foreach (char character in text)
            {
                if (result.Length >= MAX_DISPLAY_LENGTH)
                {
                    result.Append('…');
                    break;
                }

                if (character == '<')
                {
                    result.Append('‹');
                }
                else if (character == '>')
                {
                    result.Append('›');
                }
                else if (character == '\\')
                {
                    result.Append('＼');
                }
                else if (char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format || character == '\u2028' || character == '\u2029')
                {
                    result.Append(' ');
                }
                else
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }
    }
}
