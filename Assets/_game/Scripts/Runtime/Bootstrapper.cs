using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Core.Structure;
using UnityEngine;

namespace Runtime
{
    public class Bootstrapper : MonoBehaviour
    {
        void Awake()
        {
            StructureManager.CheckInstance();
            GameData.CheckInstance();
            GameData.Instance.OnEnable();
        }
    }
}
