namespace Yuriy.MatchThree.NetworkMessages.Models
{
    public sealed class HelloMessageModel
    {
        public string Text { get; }

        public HelloMessageModel(string text)
        {
            Text = text;
        }
    }
}
