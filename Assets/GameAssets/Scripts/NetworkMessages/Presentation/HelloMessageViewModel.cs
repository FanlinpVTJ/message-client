using System;
using R3;
using Yuriy.MatchThree.NetworkMessages.Contracts;
using Yuriy.MatchThree.NetworkMessages.Models;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Presentation
{
    public sealed class HelloMessageViewModel : IDisposable
    {
        private readonly IClientNetworkMessagesService _clientNetworkMessagesService;
        private readonly INetworkSessionService _networkSessionService;

        public ReactiveProperty<string> HelloText { get; }

        public HelloMessageViewModel(
            IClientNetworkMessagesService clientNetworkMessagesService,
            INetworkSessionService networkSessionService)
        {
            _clientNetworkMessagesService = clientNetworkMessagesService;
            _networkSessionService = networkSessionService;
            HelloText = new ReactiveProperty<string>(string.Empty);

            _clientNetworkMessagesService.HelloMessageReceived += HandleHelloMessageReceived;
        }

        public void StartHost()
        {
            _networkSessionService.StartHost();
        }

        public void StartClient()
        {
            _networkSessionService.StartClient();
        }

        public void Dispose()
        {
            _clientNetworkMessagesService.HelloMessageReceived -= HandleHelloMessageReceived;
            HelloText.Dispose();
        }

        private void HandleHelloMessageReceived(HelloMessage message)
        {
            HelloMessageModel helloMessageModel = new HelloMessageModel(message.Text);
            HelloText.Value = helloMessageModel.Text;
        }
    }
}
