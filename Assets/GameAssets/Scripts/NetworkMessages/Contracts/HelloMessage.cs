using Mirror;

namespace Yuriy.MatchThree.NetworkMessages.Contracts
{
    public struct HelloMessage : NetworkMessage
    {
        public const int MAX_TEXT_LENGTH = 1024;

        public string Text;
    }
}
