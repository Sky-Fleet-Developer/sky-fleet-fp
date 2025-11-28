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
    public class InitGameSingletons : MonoBehaviour, IMyInstaller, ILoadAtStart
    {
        [SerializeField] private GameData gameData;
        [SerializeField] ScriptableObject[] injectTargets;

        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                container.Inject(injectTargets[i]);
            }
        }
        
        public Task Load()
        {
            gameData.Initialize();
            return Task.CompletedTask;
        }


        public void InstallBindings(DiContainer container)
        {
            container.Bind<CycleService>().FromInstance(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                container.Bind(injectTargets[i].GetType()).FromInstance(injectTargets[i]);
                if (injectTargets[i] is IMyInstaller installer)
                {
                    installer.InstallBindings(container);
                }
            }
            gameData.InstallChildren(container);
        }
    }
}
