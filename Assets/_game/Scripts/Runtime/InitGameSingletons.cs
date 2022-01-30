using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Structure;
using UnityEngine;

namespace Runtime
{
    public class InitGameSingletons : MonoBehaviour, ILoadAtStart
    {

        public Task Load()
        {
            StructureUpdateModule.CheckInstance();
            GameData.CheckInstance();
            GameData.Instance.OnEnable();
            return Task.CompletedTask;
        }
    }
}
