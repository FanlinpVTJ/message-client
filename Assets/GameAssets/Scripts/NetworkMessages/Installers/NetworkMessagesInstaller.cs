using kcp2k;
using Mirror;
using UnityEngine;
using Zenject;
using Yuriy.MatchThree.NetworkMessages.Contracts;
using Yuriy.MatchThree.NetworkMessages.Presentation;
using Yuriy.MatchThree.NetworkMessages.Services;

namespace Yuriy.MatchThree.NetworkMessages.Installers
{
    public sealed class NetworkMessagesInstaller : MonoInstaller
    {
        [SerializeField] private HelloMessageView _helloMessageViewPrefab;

        public override void InstallBindings()
        {
            Container.Bind<NetworkManager>().FromComponentInHierarchy().AsSingle();
            Container.Bind<KcpTransport>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IServerSubscriptionRegistry>().To<ServerSubscriptionRegistry>().AsSingle();
            Container.Bind<INetworkMessagesDiagnosticsService>().To<NetworkMessagesDiagnosticsService>().AsSingle();
            Container.BindInterfacesAndSelfTo<MirrorNetworkMessagesService>().AsSingle()
                .OnInstantiated<MirrorNetworkMessagesService>((context, service) => service.Register<HelloMessage>(NetworkMessageType.Hello));
            Container.BindInterfacesAndSelfTo<MirrorNetworkSessionService>().AsSingle();
            Container.BindInterfacesAndSelfTo<HelloGreetingService>().AsSingle();
            Container.BindInterfacesAndSelfTo<HelloMessageViewModel>().AsSingle();
            Container.Bind<HelloMessageView>().FromComponentInNewPrefab(_helloMessageViewPrefab).AsSingle().NonLazy();
        }
    }
}
