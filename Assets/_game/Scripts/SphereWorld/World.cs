using System.Threading.Tasks;
using Core.Boot_strapper;
using UnityEngine;
using Zenject;

namespace SphereWorld
{
    public class World : MonoInstaller, ILoadAtStart
    {
        [SerializeField] private WorldProfile worldProfile;
        
        bool ILoadAtStart.enabled
        {
            get => enabled && gameObject.activeInHierarchy;
        }

        public async Task Load()
        {
            
        }

        public override void InstallBindings()
        {
            Container.Bind<WorldProfile>().FromInstance(worldProfile);
        }
    }
}