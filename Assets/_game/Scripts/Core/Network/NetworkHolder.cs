using Shared;
using UnityEngine;
using Zenject;

namespace Core.Network
{
    public class NetworkHolder : MonoInstaller
    {
        private INetworkBehaviour networkBehaviour;
        
        public override void InstallBindings()
        {
            Container.BindInstance(this);
        }
    }
}
