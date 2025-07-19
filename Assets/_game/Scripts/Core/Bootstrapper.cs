using System;
using System.Threading.Tasks;
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
            Task task = RunAsync();
            await task;
        }

        private async Task RunAsync()
        {
            var interfaces = GetComponentsInChildren<ILoadAtStart>(true);
            foreach (ILoadAtStart load in interfaces)
            {
                if (load.enabled)
                {
                    try
                    {
                        Container.Inject(load);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                    await load.Load();
                }
            }
            OnLoadComplete.Invoke();
            OnLoadComplete = new LateEvent();
        }

        public override void InstallBindings()
        {
            foreach (var monoInstaller in GetComponentsInChildren<MonoInstaller>())
            {
                if (monoInstaller == this)
                {
                    continue;
                }
                Container.Inject(monoInstaller);
                monoInstaller.InstallBindings();
            }
        }
    }
}