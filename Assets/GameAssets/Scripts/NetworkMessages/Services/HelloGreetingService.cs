using System;
using Mirror;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;

namespace Yuriy.MatchThree.NetworkMessages.Services
{
    public sealed class HelloGreetingService : IInitializable, IDisposable
    {
        private readonly IServerNetworkMessagesService _serverNetworkMessagesService;

        public HelloGreetingService(IServerNetworkMessagesService serverNetworkMessagesService)
        {
            _serverNetworkMessagesService = serverNetworkMessagesService;
        }

        public void Initialize()
        {
            _serverNetworkMessagesService.OnClientSubscribed += HandleClientSubscribed;
        }

        public void Dispose()
        {
            _serverNetworkMessagesService.OnClientSubscribed -= HandleClientSubscribed;
        }

        private void HandleClientSubscribed(NetworkConnectionToClient connection, NetworkMessageType messageType)
        {
            if (messageType != NetworkMessageType.Hello)
            {
                return;
            }

            HelloMessage helloMessage = new HelloMessage
            {
                Text = "Hello Client!"
            };

            _serverNetworkMessagesService.Send(connection, helloMessage);
        }
    }
}
