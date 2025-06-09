using Core.Boot_strapper;
using Core.SessionManager.GameProcess;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core
{
    public class Bootstrapper : MonoInstaller
    {
        public static LateEvent OnLoadComplete = new LateEvent();
        public override async void Start()
        {
            base.Start();
            foreach (ILoadAtStart load in GetComponentsInChildren<ILoadAtStart>(true))
            {
                Container.Inject(load);
                if(load.enabled) await load.Load();
            }
            OnLoadComplete.Invoke();
            OnLoadComplete = new LateEvent();
        }

        public override void InstallBindings()
        {
            foreach (var monoInstaller in GetComponentsInChildren<MonoInstaller>())
            {
                Container.Inject(monoInstaller);
                monoInstaller.InstallBindings();
            }
        }
    }
}