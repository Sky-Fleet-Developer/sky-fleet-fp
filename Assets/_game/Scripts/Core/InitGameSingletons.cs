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
        private DiContainer _container;

        public Task Load()
        {
            _container.Inject(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                _container.Inject(injectTargets[i]);
            }
            gameData.Initialize();
            return Task.CompletedTask;
        }


        public void InstallBindings(DiContainer container)
        {
            _container = container;
            _container.Bind<CycleService>().FromInstance(CycleService.Instance);
            for (var i = 0; i < injectTargets.Length; i++)
            {
                _container.Bind(injectTargets[i].GetType()).FromInstance(injectTargets[i]);
                if (injectTargets[i] is IMyInstaller installer)
                {
                    installer.InstallBindings(_container);
                }
            }
            gameData.InstallChildren(_container);
        }
    }
}
