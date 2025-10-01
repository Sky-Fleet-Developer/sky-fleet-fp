using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Data;
using Core.Structure;
using UnityEngine;
using Zenject;

namespace Core
{
    public class InitGameSingletons : MonoInstaller, ILoadAtStart
    {
        [SerializeField] private GameData gameData;
        [SerializeField] ScriptableObject[] injectTargets;
        
        public Task Load()
        {
            Container.Inject(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                Container.Inject(injectTargets[i]);
            }
            gameData.Initialize();
            return Task.CompletedTask;
        }

        public override void InstallBindings()
        {
            Container.Bind<CycleService>().FromInstance(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                Container.Bind(injectTargets[i].GetType()).FromInstance(injectTargets[i]);
                if (injectTargets[i] is IInstallerWithContainer installer)
                {
                    installer.InstallBindings(Container);
                }
            }
            gameData.InstallChildren(Container);
        }
    }
}
