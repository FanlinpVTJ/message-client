using Zenject;
using Yuriy.MatchThree.NetworkMessages.Presentation;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Installers
{
    public sealed class NetworkMessagesInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IServerSubscriptionRegistry>().To<ServerSubscriptionRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<MirrorNetworkMessagesService>().AsSingle();
            Container.Bind<INetworkSessionService>().To<MirrorNetworkSessionService>().AsSingle();
            Container.BindInterfacesAndSelfTo<HelloGreetingService>().AsSingle();
            Container.BindInterfacesAndSelfTo<HelloMessageViewModel>().AsSingle();
        }
    }
}
