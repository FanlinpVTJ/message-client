using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Contracts
{
    public struct HelloMessage : NetworkMessage, ISubscribedNetworkMessage
    {
        public const int MAX_TEXT_LENGTH = 1024;

        public string Text;

        public bool IsValid => !string.IsNullOrEmpty(Text) && Text.Length <= MAX_TEXT_LENGTH;

        public string Description => Text;
    }
}
