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
        public Task Load()
        {
            gameData.Initialize();
            return Task.CompletedTask;
        }

        public override void InstallBindings()
        {
            Container.Bind<GameData>().FromInstance(gameData);
            Container.Bind<StructureUpdateModule>().FromInstance(StructureUpdateModule.Instance);
            gameData.InstallChildren(Container);
        }
    }
}
