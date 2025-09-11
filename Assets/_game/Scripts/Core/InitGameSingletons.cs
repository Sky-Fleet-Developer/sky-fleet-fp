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
            gameData.Initialize();
            for (var i = 0; i < injectTargets.Length; i++)
            {
                Container.Inject(injectTargets[i]);
            }
            return Task.CompletedTask;
        }

        public override void InstallBindings()
        {
            Container.Bind<StructureUpdateModule>().FromInstance(StructureUpdateModule.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                Container.Bind(injectTargets[i].GetType()).FromInstance(injectTargets[i]);
                if (injectTargets[i] is IInstallerWithContainer { IsEnabled: true } installer)
                {
                    installer.DiContainer = Container;
                    installer.InstallBindings();
                }
            }
            gameData.InstallChildren(Container);
        }
    }
}
